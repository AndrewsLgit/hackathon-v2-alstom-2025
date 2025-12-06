using UnityEngine;

/// <summary>
/// Script qui contraint un objet à rester sur une surface en limitant ses positions X et Z
/// et en forçant sa hauteur Y à rester au-dessus de la surface.
/// </summary>
public class ConstrainToSurface : MonoBehaviour
{
    [Header("Configuration de la surface")]
    [Tooltip("La surface (Transform d'un plan) sur laquelle l'objet doit rester. Si vide, cherchera automatiquement un objet nommé 'Surface'.")]
    public Transform surface;

    [Header("Paramètres de position")]
    [Tooltip("Hauteur au-dessus de la surface")]
    public float offsetY = 0.1f;

    [Tooltip("Limites de la surface sur l'axe X (min, max)")]
    public Vector2 boundsX = new Vector2(-0.5f, 0.5f);

    [Tooltip("Limites de la surface sur l'axe Z (min, max)")]
    public Vector2 boundsZ = new Vector2(-0.5f, 0.5f);

    void Start()
    {
        // Si aucune surface n'est assignée, essaie de la trouver automatiquement
        if (surface == null)
        {
            GameObject surfaceObj = GameObject.Find("Surface");
            if (surfaceObj != null)
            {
                surface = surfaceObj.transform;
                Debug.Log($"[ConstrainToSurface] Surface trouvée automatiquement: {surfaceObj.name}");
            }
            else
            {
                Debug.LogWarning($"[ConstrainToSurface] Aucune surface assignée sur {gameObject.name}. Assurez-vous d'assigner une surface dans l'inspecteur.");
            }
        }
    }

    void LateUpdate()
    {
        // Vérifie que la surface existe avant de contraindre
        if (surface == null)
            return;

        Vector3 pos = transform.position;

        // Contraint les positions X et Z dans les limites de la surface
        pos.x = Mathf.Clamp(pos.x, boundsX.x, boundsX.y);
        pos.z = Mathf.Clamp(pos.z, boundsZ.x, boundsZ.y);

        // Force l'objet à rester "collé" à la surface avec l'offset Y
        pos.y = surface.position.y + offsetY;

        transform.position = pos;
    }

    /// <summary>
    /// Dessine les limites de la surface dans l'éditeur (gizmos)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (surface == null)
            return;

        Gizmos.color = Color.yellow;
        Vector3 center = surface.position;
        center.y += offsetY;
        
        Vector3 size = new Vector3(boundsX.y - boundsX.x, 0.01f, boundsZ.y - boundsZ.x);
        Vector3 pos = center + new Vector3((boundsX.x + boundsX.y) / 2f, 0, (boundsZ.x + boundsZ.y) / 2f);
        
        Gizmos.DrawWireCube(pos, size);
    }
}
