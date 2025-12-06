using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script qui maintient un objet XR interactable à une hauteur fixe au-dessus d'une surface
/// et bloque sa rotation pour qu'il reste plat. L'objet peut se déplacer uniquement sur les axes X et Z.
/// </summary>
public class XRClampToSurface : MonoBehaviour
{
    [Header("Configuration de la surface")]
    [Tooltip("La surface (Transform) sur laquelle l'objet doit rester. Si vide, cherchera automatiquement un objet nommé 'Surface'.")]
    public Transform surface;

    [Header("Paramètres de position")]
    [Tooltip("Hauteur au-dessus de la surface")]
    public float offsetY = 0.1f;

    [Header("Contraintes")]
    [Tooltip("Bloque la rotation pendant le grab (recommandé)")]
    public bool lockRotation = true;

    [Tooltip("Rotation cible si lockRotation est activé")]
    public Vector3 targetRotation = Vector3.zero;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private float targetY;
    private bool isGrabbed;
    private bool originalGravity;
    private Quaternion originalRotation;
    private float grabStartTime;
    private const float GRAB_SMOOTH_DURATION = 0.1f; // Durée de transition douce au grab

    void Start()
    {
        // Récupère le composant XRGrabInteractable
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        if (grabInteractable == null)
        {
            Debug.LogError($"[XRClampToSurface] Aucun composant XRGrabInteractable trouvé sur {gameObject.name}. Ce script nécessite un XRGrabInteractable.");
            enabled = false;
            return;
        }

        // Récupère le Rigidbody s'il existe
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning($"[XRClampToSurface] Aucun Rigidbody trouvé sur {gameObject.name}. Le script fonctionnera mais peut être moins efficace.");
        }
        else
        {
            // Sauvegarde la gravité originale
            originalGravity = rb.useGravity;
            originalRotation = transform.rotation;
        }

        // Si aucune surface n'est assignée, essaie de la trouver automatiquement
        if (surface == null)
        {
            GameObject surfaceObj = GameObject.Find("Surface");
            if (surfaceObj != null)
            {
                surface = surfaceObj.transform;
                Debug.Log($"[XRClampToSurface] Surface trouvée automatiquement: {surfaceObj.name}");
            }
            else
            {
                Debug.LogWarning($"[XRClampToSurface] Aucune surface assignée sur {gameObject.name}. Assurez-vous d'assigner une surface dans l'inspecteur.");
            }
        }

        // Calcule la hauteur cible initiale
        if (surface != null)
        {
            targetY = surface.position.y + offsetY;
        }

        // S'abonne aux événements de grab/release
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        grabStartTime = Time.time;
        
        if (surface != null)
        {
            targetY = surface.position.y + offsetY;
            
            // Ajuste doucement la position Y au moment du grab pour éviter le bounce
            if (rb != null && !rb.isKinematic)
            {
                Vector3 pos = rb.position;
                float currentY = pos.y;
                float deltaY = targetY - currentY;
                
                // Si l'écart est important, ajuste progressivement
                if (Mathf.Abs(deltaY) > 0.01f)
                {
                    pos.y = Mathf.Lerp(currentY, targetY, 0.3f); // Ajuste à 30% pour éviter le bounce brutal
                    rb.MovePosition(pos);
                }
            }
            else if (rb == null)
            {
                Vector3 pos = transform.position;
                pos.y = targetY;
                transform.position = pos;
            }
        }

        // Sauvegarde la rotation actuelle au moment du grab
        if (lockRotation)
        {
            originalRotation = transform.rotation;
        }

        // Désactive la gravité pendant le grab pour éviter que l'objet "vole"
        if (rb != null)
        {
            rb.useGravity = false;
        }
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Réactive la gravité
        if (rb != null)
        {
            rb.useGravity = originalGravity;
        }
    }

    void Update()
    {
        // Met à jour la hauteur cible si la surface bouge
        if (surface != null)
        {
            targetY = surface.position.y + offsetY;
        }
    }

    void FixedUpdate()
    {
        if (!isGrabbed || grabInteractable == null || !grabInteractable.isSelected || surface == null)
            return;

        ApplyConstraints();
    }

    void ApplyConstraints()
    {
        if (rb != null && !rb.isKinematic)
        {
            // Contrainte de position Y avec seuil pour éviter les micro-corrections
            float currentY = rb.position.y;
            float deltaY = targetY - currentY;
            
            // Pendant les premiers instants du grab, utilise une interpolation plus douce
            float timeSinceGrab = Time.time - grabStartTime;
            bool useSmoothTransition = timeSinceGrab < GRAB_SMOOTH_DURATION;
            
            // Ne corrige que si l'écart est significatif (évite les tremblements)
            if (Mathf.Abs(deltaY) > 0.001f)
            {
                Vector3 pos = rb.position;
                
                if (useSmoothTransition)
                {
                    // Interpolation douce pendant la transition initiale
                    float t = timeSinceGrab / GRAB_SMOOTH_DURATION;
                    pos.y = Mathf.Lerp(currentY, targetY, t * 0.5f + 0.1f); // Transition progressive
                }
                else
                {
                    // Force directement après la transition
                    pos.y = targetY;
                }
                
                rb.MovePosition(pos);
            }

            // Annule doucement la vélocité verticale au lieu de la forcer à zéro brutalement
            Vector3 velocity = rb.linearVelocity;
            if (Mathf.Abs(velocity.y) > 0.01f)
            {
                velocity.y = Mathf.Lerp(velocity.y, 0f, Time.fixedDeltaTime * 20f);
                rb.linearVelocity = velocity;
            }
            else
            {
                velocity.y = 0f;
                rb.linearVelocity = velocity;
            }

            // Contrainte de rotation avec interpolation douce
            if (lockRotation)
            {
                Quaternion targetRot = Quaternion.Euler(targetRotation);
                Quaternion currentRot = rb.rotation;
                
                // Interpole doucement vers la rotation cible
                if (Quaternion.Angle(currentRot, targetRot) > 0.5f)
                {
                    rb.MoveRotation(Quaternion.Lerp(currentRot, targetRot, Time.fixedDeltaTime * 15f));
                }
                else
                {
                    rb.MoveRotation(targetRot);
                }
                
                // Réduit doucement la vélocité angulaire
                Vector3 angularVel = rb.angularVelocity;
                if (angularVel.magnitude > 0.01f)
                {
                    angularVel = Vector3.Lerp(angularVel, Vector3.zero, Time.fixedDeltaTime * 20f);
                    rb.angularVelocity = angularVel;
                }
                else
                {
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        else if (rb == null)
        {
            // Pas de Rigidbody, modifie directement mais avec seuil
            float currentY = transform.position.y;
            float deltaY = targetY - currentY;
            
            if (Mathf.Abs(deltaY) > 0.001f)
            {
                Vector3 pos = transform.position;
                pos.y = targetY;
                transform.position = pos;
            }

            if (lockRotation)
            {
                transform.rotation = Quaternion.Euler(targetRotation);
            }
        }
    }

    void OnDestroy()
    {
        // Nettoie les listeners
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }

        // Réactive la gravité si on est toujours en grab (sécurité)
        if (rb != null && isGrabbed)
        {
            rb.useGravity = originalGravity;
        }
    }
}
