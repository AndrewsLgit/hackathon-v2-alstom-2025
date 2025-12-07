using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Script qui fait apparaître des cubes sur une surface via une interface UI.
/// Les cubes sont instanciés depuis un prefab et positionnés sur la surface.
/// </summary>
public class CubeSpawner : MonoBehaviour
{
    [Header("Configuration des Prefabs")]
    [Tooltip("Les prefabs de cubes à faire apparaître (doit avoir XRGrabInteractable et XRClampToSurface). Utilisez plusieurs prefabs pour différents types de cubes.")]
    public GameObject[] cubePrefabs = new GameObject[1];

    [Tooltip("Le prefab par défaut (déprécié, utilisez cubePrefabs[0] à la place). Conservé pour compatibilité.")]
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

        // Migration automatique : si cubePrefab est défini mais cubePrefabs est vide, on l'ajoute
        if (cubePrefab != null && (cubePrefabs == null || cubePrefabs.Length == 0 || cubePrefabs[0] == null))
        {
            cubePrefabs = new GameObject[] { cubePrefab };
            Debug.Log($"[CubeSpawner] Migration automatique : cubePrefab ajouté à cubePrefabs[0]");
        }

        // Vérifie qu'au moins un prefab est disponible
        if (cubePrefabs == null || cubePrefabs.Length == 0 || cubePrefabs[0] == null)
        {
            Debug.LogError($"[CubeSpawner] Aucun prefab de cube assigné sur {gameObject.name}. Assurez-vous d'assigner au moins un prefab dans cubePrefabs.");
        }
    }

    /// <summary>
    /// Méthode publique appelée par l'interface UI pour faire apparaître un cube (utilise le premier prefab)
    /// </summary>
    public void SpawnCube()
    {
        SpawnCube(0);
    }

    /// <summary>
    /// Méthode publique pour faire apparaître un cube avec un prefab spécifique par index
    /// </summary>
    /// <param name="prefabIndex">Index du prefab dans le tableau cubePrefabs (0 = premier prefab)</param>
    public void SpawnCube(int prefabIndex)
    {
        GameObject prefabToSpawn = GetPrefabByIndex(prefabIndex);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[CubeSpawner] Impossible de faire apparaître un cube : prefab à l'index {prefabIndex} n'existe pas.");
            return;
        }

        SpawnCube(prefabToSpawn);
    }

    /// <summary>
    /// Méthode publique pour faire apparaître un cube avec un prefab spécifique
    /// </summary>
    /// <param name="prefab">Le prefab à instancier</param>
    public void SpawnCube(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[CubeSpawner] Impossible de faire apparaître un cube : prefab est null.");
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
        XRClampToSurface clampScript = prefab.GetComponent<XRClampToSurface>();
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
        Vector3 originalScale = prefab.transform.localScale;

        // Instancie le cube sans parent pour éviter les problèmes de scale
        GameObject newCube = Instantiate(prefab, spawnPosition, Quaternion.identity, null);

        // Force le scale à rester celui du prefab (évite les modifications par le parent)
        newCube.transform.localScale = originalScale;

        // Stabilise immédiatement le cube sur la surface pour éviter le bounce
        StabilizeCubeOnSurface(newCube, spawnPosition);

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
        Debug.Log($"[CubeSpawner] Cube créé ({prefab.name}). Total: {currentCubeCount}");
    }

    /// <summary>
    /// Récupère un prefab par son index, avec validation
    /// </summary>
    private GameObject GetPrefabByIndex(int index)
    {
        if (cubePrefabs == null || cubePrefabs.Length == 0)
        {
            // Fallback sur cubePrefab pour compatibilité
            if (cubePrefab != null)
            {
                return cubePrefab;
            }
            return null;
        }

        if (index < 0 || index >= cubePrefabs.Length)
        {
            Debug.LogWarning($"[CubeSpawner] Index {index} invalide. Utilisation du premier prefab (index 0).");
            index = 0;
        }

        if (cubePrefabs[index] == null)
        {
            Debug.LogWarning($"[CubeSpawner] Prefab à l'index {index} est null. Tentative avec le premier prefab disponible.");
            // Cherche le premier prefab non-null
            for (int i = 0; i < cubePrefabs.Length; i++)
            {
                if (cubePrefabs[i] != null)
                {
                    return cubePrefabs[i];
                }
            }
            return null;
        }

        return cubePrefabs[index];
    }

    /// <summary>
    /// Stabilise le cube sur la surface immédiatement après le spawn pour éviter le bounce
    /// </summary>
    void StabilizeCubeOnSurface(GameObject cube, Vector3 targetPosition)
    {
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Sauvegarde l'état original de la gravité
            bool originalGravity = rb.useGravity;
            
            // Désactive temporairement la gravité pour éviter la chute lors du spawn
            rb.useGravity = false;
            
            // Positionne le cube exactement à la bonne hauteur
            rb.position = targetPosition;
            
            // Réinitialise toutes les vélocités pour éviter tout mouvement
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Force la mise à jour de la position immédiatement
            rb.MovePosition(targetPosition);
            
            // Synchronise la position transform avec le Rigidbody
            cube.transform.position = targetPosition;
            
            // Réactive la gravité après stabilisation pour permettre le comportement normal
            // Le XRClampToSurface désactivera la gravité lors du grab si nécessaire
            StartCoroutine(RestoreGravityAfterStabilization(rb, originalGravity));
        }
        else
        {
            // Pas de Rigidbody, position directe
            cube.transform.position = targetPosition;
        }
    }

    /// <summary>
    /// Réactive la gravité après stabilisation si nécessaire
    /// </summary>
    System.Collections.IEnumerator RestoreGravityAfterStabilization(Rigidbody rb, bool originalGravity)
    {
        // Attend plusieurs frames pour que la physique se stabilise complètement
        // et que le cube soit bien positionné avant de réactiver la gravité
        yield return null;
        yield return null;
        
        if (rb != null)
        {
            // Vérifie si le cube a un XRClampToSurface qui gérera la gravité lors du grab
            XRClampToSurface clampScript = rb.GetComponent<XRClampToSurface>();
            if (clampScript != null)
            {
                // Garde la gravité désactivée - XRClampToSurface la gérera lors du grab/release
                // Cela évite que le cube tombe avant d'être grab
                rb.useGravity = false;
            }
            else
            {
                // Pas de script de contrainte, réactive la gravité normale
                rb.useGravity = originalGravity;
            }
        }
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

