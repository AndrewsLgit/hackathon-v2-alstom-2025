using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script qui détruit un objet s'il est lâché en dehors des limites de la surface.
/// Doit être utilisé avec XRClampToSurface ou StickToSurface.
/// </summary>
public class SurfaceBoundaryDestroyer : MonoBehaviour
{
    [Header("Configuration de la surface")]
    [Tooltip("La surface (Transform) qui définit les limites. Si vide, cherchera automatiquement un objet nommé 'Surface'.")]
    public Transform surface;

    [Header("Limites de la surface")]
    [Tooltip("Limites de la surface sur l'axe X (min, max)")]
    public Vector2 boundsX = new Vector2(-0.5f, 0.5f);

    [Tooltip("Limites de la surface sur l'axe Z (min, max)")]
    public Vector2 boundsZ = new Vector2(-0.5f, 0.5f);

    [Header("Paramètres")]
    [Tooltip("Marge de tolérance en dehors des limites avant destruction")]
    public float tolerance = 0.1f;

    [Tooltip("Délai avant destruction (en secondes) après avoir été lâché en dehors")]
    public float destroyDelay = 0.5f;

    [Header("Événements")]
    [Tooltip("Événement déclenché quand le cube est détruit")]
    public UnityEvent OnCubeDestroyed;

    private XRGrabInteractable grabInteractable;
    private bool wasGrabbed = false;
    private bool isDestroying = false;

    void Start()
    {
        // Récupère le composant XRGrabInteractable
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogWarning($"[SurfaceBoundaryDestroyer] Aucun composant XRGrabInteractable trouvé sur {gameObject.name}. Le script fonctionnera mais ne détectera pas les releases.");
        }
        else
        {
            // S'abonne aux événements de grab/release
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }

        // Si aucune surface n'est assignée, essaie de la trouver automatiquement
        if (surface == null)
        {
            GameObject surfaceObj = GameObject.Find("Surface");
            if (surfaceObj != null)
            {
                surface = surfaceObj.transform;
                Debug.Log($"[SurfaceBoundaryDestroyer] Surface trouvée automatiquement: {surfaceObj.name}");
            }
            else
            {
                Debug.LogWarning($"[SurfaceBoundaryDestroyer] Aucune surface assignée sur {gameObject.name}. Assurez-vous d'assigner une surface dans l'inspecteur.");
            }
        }

        // Note: Les limites doivent être configurées manuellement dans l'inspecteur
        // Assurez-vous que boundsX et boundsZ correspondent aux limites de votre surface
        // Si vous utilisez ConstrainToSurface, copiez les mêmes valeurs de boundsX et boundsZ
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        wasGrabbed = true;
        isDestroying = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (wasGrabbed)
        {
            wasGrabbed = false;
            CheckAndDestroy();
        }
    }

    void Update()
    {
        // Vérifie aussi en continu si l'objet n'est pas grab et est en dehors
        if (!wasGrabbed && grabInteractable != null && !grabInteractable.isSelected && !isDestroying)
        {
            CheckAndDestroy();
        }
    }

    void CheckAndDestroy()
    {
        if (surface == null || isDestroying)
            return;

        Vector3 pos = transform.position;
        Vector3 surfacePos = surface.position;

        // Calcule la position relative à la surface
        float relativeX = pos.x - surfacePos.x;
        float relativeZ = pos.z - surfacePos.z;

        // Vérifie si l'objet est en dehors des limites (avec tolérance)
        bool outsideX = relativeX < (boundsX.x - tolerance) || relativeX > (boundsX.y + tolerance);
        bool outsideZ = relativeZ < (boundsZ.x - tolerance) || relativeZ > (boundsZ.y + tolerance);

        if (outsideX || outsideZ)
        {
            Debug.Log($"[SurfaceBoundaryDestroyer] {gameObject.name} est en dehors de la surface. Destruction dans {destroyDelay} secondes.");
            isDestroying = true;
            Invoke(nameof(DestroyCube), destroyDelay);
        }
    }

    void DestroyCube()
    {
        if (OnCubeDestroyed != null)
        {
            OnCubeDestroyed.Invoke();
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Nettoie les listeners
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    /// <summary>
    /// Dessine les limites dans l'éditeur (gizmos)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (surface == null)
            return;

        Gizmos.color = Color.red;
        Vector3 center = surface.position;
        Vector3 size = new Vector3(boundsX.y - boundsX.x, 0.1f, boundsZ.y - boundsZ.x);
        Vector3 pos = center + new Vector3((boundsX.x + boundsX.y) / 2f, 0.05f, (boundsZ.x + boundsZ.y) / 2f);
        
        Gizmos.DrawWireCube(pos, size);
    }
}

