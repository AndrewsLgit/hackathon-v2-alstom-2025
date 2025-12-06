using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script qui "colle" un objet XR interactable à une surface en verrouillant sa hauteur Y
/// et sa rotation. La hauteur est verrouillée au moment du grab.
/// L'objet peut se déplacer uniquement sur les axes X et Z.
/// </summary>
public class StickToSurface : MonoBehaviour
{
    [Header("Configuration de la surface")]
    [Tooltip("La surface sur laquelle coller l'objet. Si vide, cherchera automatiquement un objet nommé 'Surface'.")]
    public Transform surface;

    [Header("Paramètres de position")]
    [Tooltip("Décalage vertical au-dessus de la surface")]
    public float offsetY = 0.02f;

    [Header("Contraintes")]
    [Tooltip("Bloque la rotation pendant le grab (recommandé)")]
    public bool lockRotation = true;

    [Tooltip("Rotation cible si lockRotation est activé")]
    public Vector3 targetRotation = Vector3.zero;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private float lockedY;
    private bool isGrabbed;
    private bool originalGravity;
    private Quaternion lockedRotation;

    void Start()
    {
        // Récupère le composant XRGrabInteractable
        grab = GetComponent<XRGrabInteractable>();

        if (grab == null)
        {
            Debug.LogError($"[StickToSurface] Aucun composant XRGrabInteractable trouvé sur {gameObject.name}. Ce script nécessite un XRGrabInteractable.");
            enabled = false;
            return;
        }

        // Récupère le Rigidbody s'il existe
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning($"[StickToSurface] Aucun Rigidbody trouvé sur {gameObject.name}. Le script fonctionnera mais peut être moins efficace.");
        }
        else
        {
            // Sauvegarde la gravité originale
            originalGravity = rb.useGravity;
        }

        // Si aucune surface n'est assignée, essaie de la trouver automatiquement
        if (surface == null)
        {
            GameObject surfaceObj = GameObject.Find("Surface");
            if (surfaceObj != null)
            {
                surface = surfaceObj.transform;
                Debug.Log($"[StickToSurface] Surface trouvée automatiquement: {surfaceObj.name}");
            }
            else
            {
                Debug.LogWarning($"[StickToSurface] Aucune surface assignée sur {gameObject.name}. Assurez-vous d'assigner une surface dans l'inspecteur.");
            }
        }

        // Calcule la hauteur de verrouillage initiale
        if (surface != null)
        {
            lockedY = surface.position.y + offsetY;
        }

        // S'abonne aux événements de grab/release
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // Verrouille la hauteur au moment du grab
        if (surface != null)
        {
            lockedY = surface.position.y + offsetY;
        }

        // Verrouille la rotation au moment du grab
        if (lockRotation)
        {
            lockedRotation = transform.rotation;
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

    void FixedUpdate()
    {
        if (!isGrabbed || grab == null || !grab.isSelected || surface == null)
            return;

        ApplyConstraints();
    }

    void ApplyConstraints()
    {
        if (rb != null && !rb.isKinematic)
        {
            // Contrainte de position Y avec seuil pour éviter les micro-corrections
            float currentY = rb.position.y;
            float deltaY = lockedY - currentY;
            
            // Ne corrige que si l'écart est significatif (évite les tremblements)
            if (Mathf.Abs(deltaY) > 0.001f)
            {
                Vector3 pos = rb.position;
                pos.y = lockedY;
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
                Quaternion currentRot = rb.rotation;
                
                // Interpole doucement vers la rotation verrouillée
                if (Quaternion.Angle(currentRot, lockedRotation) > 0.5f)
                {
                    rb.MoveRotation(Quaternion.Lerp(currentRot, lockedRotation, Time.fixedDeltaTime * 15f));
                }
                else
                {
                    rb.MoveRotation(lockedRotation);
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
            float deltaY = lockedY - currentY;
            
            if (Mathf.Abs(deltaY) > 0.001f)
            {
                Vector3 pos = transform.position;
                pos.y = lockedY;
                transform.position = pos;
            }

            if (lockRotation)
            {
                transform.rotation = lockedRotation;
            }
        }
    }

    void OnDestroy()
    {
        // Nettoie les listeners pour éviter les fuites mémoire
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }

        // Réactive la gravité si on est toujours en grab (sécurité)
        if (rb != null && isGrabbed)
        {
            rb.useGravity = originalGravity;
        }
    }
}
