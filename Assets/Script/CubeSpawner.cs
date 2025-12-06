using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script qui fait apparaître des cubes sur une surface via une interface UI.
/// Les cubes sont instanciés depuis un prefab et positionnés sur la surface.
/// </summary>
public class CubeSpawner : MonoBehaviour
{
    [Header("Configuration du Prefab")]
    [Tooltip("Le prefab du cube à faire apparaître (doit avoir XRGrabInteractable et XRClampToSurface)")]
    public GameObject cubePrefab;

    [Header("Configuration de la surface")]
    [Tooltip("La surface sur laquelle faire apparaître les cubes. Si vide, cherchera automatiquement un objet nommé 'Surface'.")]
    public Transform surface;

    [Header("Paramètres de spawn")]
    [Tooltip("Position de spawn relative au centre de la surface (X, Z)")]
    public Vector2 spawnOffset = Vector2.zero;

    [Tooltip("Hauteur au-dessus de la surface (doit correspondre à offsetY des scripts de contrainte)")]
    public float spawnHeight = 0.1f;

    [Tooltip("Nombre maximum de cubes pouvant exister simultanément (0 = illimité)")]
    public int maxCubes = 10;

    private int currentCubeCount = 0;

    void Start()
    {
        // Si aucune surface n'est assignée, essaie de la trouver automatiquement
        if (surface == null)
        {
            GameObject surfaceObj = GameObject.Find("Surface");
            if (surfaceObj != null)
            {
                surface = surfaceObj.transform;
                Debug.Log($"[CubeSpawner] Surface trouvée automatiquement: {surfaceObj.name}");
            }
            else
            {
                Debug.LogWarning($"[CubeSpawner] Aucune surface assignée sur {gameObject.name}. Assurez-vous d'assigner une surface dans l'inspecteur.");
            }
        }

        if (cubePrefab == null)
        {
            Debug.LogError($"[CubeSpawner] Aucun prefab de cube assigné sur {gameObject.name}. Assurez-vous d'assigner un prefab dans l'inspecteur.");
        }
    }

    /// <summary>
    /// Méthode publique appelée par l'interface UI pour faire apparaître un cube
    /// </summary>
    public void SpawnCube()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("[CubeSpawner] Impossible de faire apparaître un cube : aucun prefab assigné.");
            return;
        }

        if (surface == null)
        {
            Debug.LogError("[CubeSpawner] Impossible de faire apparaître un cube : aucune surface assignée.");
            return;
        }

        // Vérifie le nombre maximum de cubes
        if (maxCubes > 0 && currentCubeCount >= maxCubes)
        {
            Debug.LogWarning($"[CubeSpawner] Nombre maximum de cubes atteint ({maxCubes}). Supprimez un cube avant d'en créer un nouveau.");
            return;
        }

        // Calcule la position de spawn
        // Récupère l'offsetY depuis XRClampToSurface si présent pour synchroniser la hauteur
        XRClampToSurface clampScript = cubePrefab.GetComponent<XRClampToSurface>();
        float actualSpawnHeight = spawnHeight;
        if (clampScript != null)
        {
            actualSpawnHeight = clampScript.offsetY; // Utilise l'offsetY du script pour synchroniser
        }
        
        Vector3 spawnPosition = surface.position;
        spawnPosition.x += spawnOffset.x;
        spawnPosition.y = surface.position.y + actualSpawnHeight;
        spawnPosition.z += spawnOffset.y;

        // Sauvegarde le scale original du prefab
        Vector3 originalScale = cubePrefab.transform.localScale;

        // Instancie le cube sans parent pour éviter les problèmes de scale
        GameObject newCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity, null);

        // Force le scale à rester celui du prefab (évite les modifications par le parent)
        newCube.transform.localScale = originalScale;

        // Assigne la surface à tous les scripts qui en ont besoin
        AssignSurfaceToCube(newCube);

        // S'abonne à l'événement de destruction pour mettre à jour le compteur
        SurfaceBoundaryDestroyer destroyer = newCube.GetComponent<SurfaceBoundaryDestroyer>();
        if (destroyer != null)
        {
            destroyer.OnCubeDestroyed.AddListener(OnCubeDestroyed);
        }
        else
        {
            // Si le script n'existe pas, on s'abonne directement au XRGrabInteractable
            XRGrabInteractable grabInteractable = newCube.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                // On ajoute le script de destruction si il n'existe pas
                destroyer = newCube.AddComponent<SurfaceBoundaryDestroyer>();
                destroyer.surface = surface;
                destroyer.OnCubeDestroyed.AddListener(OnCubeDestroyed);
            }
        }

        currentCubeCount++;
        Debug.Log($"[CubeSpawner] Cube créé. Total: {currentCubeCount}");
    }

    /// <summary>
    /// Assigne la surface à tous les scripts du cube qui en ont besoin
    /// </summary>
    void AssignSurfaceToCube(GameObject cube)
    {
        if (surface == null)
            return;

        // Assigne la surface à XRClampToSurface
        XRClampToSurface clampScript = cube.GetComponent<XRClampToSurface>();
        if (clampScript != null && clampScript.surface == null)
        {
            clampScript.surface = surface;
            Debug.Log($"[CubeSpawner] Surface assignée à XRClampToSurface sur {cube.name}");
        }

        // Assigne la surface à StickToSurface
        StickToSurface stickScript = cube.GetComponent<StickToSurface>();
        if (stickScript != null && stickScript.surface == null)
        {
            stickScript.surface = surface;
            Debug.Log($"[CubeSpawner] Surface assignée à StickToSurface sur {cube.name}");
        }

        // Assigne la surface à ConstrainToSurface
        ConstrainToSurface constrainScript = cube.GetComponent<ConstrainToSurface>();
        if (constrainScript != null && constrainScript.surface == null)
        {
            constrainScript.surface = surface;
            Debug.Log($"[CubeSpawner] Surface assignée à ConstrainToSurface sur {cube.name}");
        }

        // Assigne la surface à SurfaceBoundaryDestroyer
        SurfaceBoundaryDestroyer destroyerScript = cube.GetComponent<SurfaceBoundaryDestroyer>();
        if (destroyerScript != null && destroyerScript.surface == null)
        {
            destroyerScript.surface = surface;
            Debug.Log($"[CubeSpawner] Surface assignée à SurfaceBoundaryDestroyer sur {cube.name}");
        }
    }

    void OnCubeDestroyed()
    {
        currentCubeCount = Mathf.Max(0, currentCubeCount - 1);
        Debug.Log($"[CubeSpawner] Cube détruit. Total restant: {currentCubeCount}");
    }

    /// <summary>
    /// Méthode pour détruire tous les cubes existants
    /// </summary>
    public void DestroyAllCubes()
    {
        SurfaceBoundaryDestroyer[] allDestroyers = FindObjectsOfType<SurfaceBoundaryDestroyer>();
        foreach (var destroyer in allDestroyers)
        {
            if (destroyer != null)
            {
                Destroy(destroyer.gameObject);
            }
        }
        currentCubeCount = 0;
        Debug.Log("[CubeSpawner] Tous les cubes ont été détruits.");
    }
}

