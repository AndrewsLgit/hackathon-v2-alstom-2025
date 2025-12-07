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

    [Tooltip("Maintient la hauteur même après le release (recommandé pour garder le cube sur la surface)")]
    public bool maintainHeightWhenNotGrabbed = false;

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
            
            // Si on doit maintenir la hauteur même sans grab, désactive la gravité dès le début
            if (maintainHeightWhenNotGrabbed)
            {
                rb.useGravity = false;
            }
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
            
            // Si on doit maintenir la hauteur même sans grab, positionne immédiatement
            if (maintainHeightWhenNotGrabbed)
            {
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 pos = rb.position;
                    pos.y = targetY;
                    rb.position = pos;
                }
                else if (rb == null)
                {
                    Vector3 pos = transform.position;
                    pos.y = targetY;
                    transform.position = pos;
                }
            }
        }

        // S'abonne aux événements de grab/release
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        grabStartTime = Time.time;
        
        // Désactive la gravité IMMÉDIATEMENT pour éviter toute chute
        if (rb != null)
        {
            rb.useGravity = false;
            // Réinitialise aussi la vélocité verticale immédiatement
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;
        }
        
        if (surface != null)
        {
            targetY = surface.position.y + offsetY;
            
            // NE force PAS immédiatement la position pour éviter le conflit avec le toolkit
            // La correction se fera dans LateUpdate() de manière plus douce
        }

        // Sauvegarde la rotation actuelle au moment du grab
        if (lockRotation)
        {
            originalRotation = transform.rotation;
        }
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Si on doit maintenir la hauteur même après release, garde la gravité désactivée
        if (rb != null)
        {
            if (maintainHeightWhenNotGrabbed)
            {
                // Garde la gravité désactivée pour que le cube reste sur la surface
                rb.useGravity = false;
                // Réinitialise la vélocité verticale pour éviter toute chute
                Vector3 vel = rb.linearVelocity;
                vel.y = 0f;
                rb.linearVelocity = vel;
            }
            else
            {
                // Réactive la gravité normale
                rb.useGravity = originalGravity;
            }
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

    void LateUpdate()
    {
        // Applique les contraintes dans LateUpdate() pour corriger APRÈS que le XR toolkit ait déplacé l'objet
        // LateUpdate s'exécute après tous les Update(), donc après le mouvement du toolkit
        if (surface != null)
        {
            bool shouldConstrain = false;
            
            if (isGrabbed && grabInteractable != null && grabInteractable.isSelected)
            {
                // Pendant le grab, applique toujours les contraintes
                shouldConstrain = true;
            }
            else if (maintainHeightWhenNotGrabbed)
            {
                // Même sans grab, maintient la hauteur si l'option est activée
                shouldConstrain = true;
            }
            
            if (shouldConstrain)
            {
                // Corrige la position Y en douceur pour éviter les sauts
                if (rb != null && !rb.isKinematic)
                {
                    float currentY = rb.position.y;
                    float deltaY = targetY - currentY;
                    
                    // Si le cube a été déplacé, corrige avec une interpolation douce
                    if (Mathf.Abs(deltaY) > 0.001f)
                    {
                        Vector3 pos = rb.position;
                        
                        // Pendant le grab, utilise une interpolation très rapide mais pas instantanée
                        // pour éviter les conflits avec le toolkit
                        if (isGrabbed)
                        {
                            // Interpolation rapide mais douce (85% vers la cible par frame)
                            pos.y = Mathf.Lerp(currentY, targetY, 0.85f);
                        }
                        else
                        {
                            // Sans grab, force directement
                            pos.y = targetY;
                        }
                        
                        rb.position = pos;
                        
                        // Réinitialise la vélocité verticale de manière douce
                        Vector3 vel = rb.linearVelocity;
                        vel.y = Mathf.Lerp(vel.y, 0f, 0.9f);
                        rb.linearVelocity = vel;
                    }
                }
                else if (rb == null)
                {
                    // Pas de Rigidbody, modifie directement
                    Vector3 pos = transform.position;
                    if (Mathf.Abs(pos.y - targetY) > 0.001f)
                    {
                        if (isGrabbed)
                        {
                            pos.y = Mathf.Lerp(pos.y, targetY, 0.85f);
                        }
                        else
                        {
                            pos.y = targetY;
                        }
                        transform.position = pos;
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (surface == null)
            return;

        bool shouldConstrain = false;
        
        if (isGrabbed && grabInteractable != null && grabInteractable.isSelected)
        {
            // Pendant le grab, applique toujours les contraintes
            shouldConstrain = true;
        }
        else if (maintainHeightWhenNotGrabbed)
        {
            // Même sans grab, maintient la hauteur si l'option est activée
            shouldConstrain = true;
        }
        
        if (shouldConstrain)
        {
            ApplyConstraints();
        }
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
            // Utilise une interpolation douce pour éviter les conflits avec le toolkit
            if (Mathf.Abs(deltaY) > 0.001f)
            {
                Vector3 pos = rb.position;
                
                // Utilise toujours une interpolation douce pour éviter les sauts
                // Laisse LateUpdate() gérer la correction principale
                pos.y = Mathf.Lerp(currentY, targetY, 0.7f);
                
                rb.MovePosition(pos);
            }

            // Annule doucement la vélocité verticale
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Lerp(velocity.y, 0f, Time.fixedDeltaTime * 15f);
            rb.linearVelocity = velocity;

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
