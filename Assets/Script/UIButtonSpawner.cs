using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script simple pour connecter un bouton UI à un CubeSpawner.
/// Permet de spawner différents types de cubes selon le bouton.
/// À attacher sur un bouton UI (Button component).
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Le CubeSpawner qui va créer les cubes. Si vide, cherchera automatiquement dans la scène.")]
    public CubeSpawner cubeSpawner;

    [Header("Type de Cube à Spawner")]
    [Tooltip("Méthode de sélection du prefab à spawner")]
    public SpawnMethod spawnMethod = SpawnMethod.ByIndex;

    [Tooltip("Index du prefab dans le tableau cubePrefabs du CubeSpawner (0 = premier prefab, 1 = deuxième, etc.)")]
    public int prefabIndex = 0;

    [Tooltip("Prefab spécifique à spawner (ignore l'index si défini)")]
    public GameObject specificPrefab;

    public enum SpawnMethod
    {
        ByIndex,        // Utilise l'index dans cubePrefabs
        ByPrefab        // Utilise le prefab spécifique
    }

    private Button button;

    void Start()
    {
        // Récupère le composant Button
        button = GetComponent<Button>();

        // Si aucun spawner n'est assigné, essaie de le trouver dans la scène
        if (cubeSpawner == null)
        {
            cubeSpawner = Object.FindFirstObjectByType<CubeSpawner>();
            if (cubeSpawner == null)
            {
                Debug.LogError($"[UIButtonSpawner] Aucun CubeSpawner trouvé dans la scène. Assurez-vous d'en créer un ou d'assigner la référence dans l'inspecteur.");
                button.interactable = false;
                return;
            }
        }

        // Connecte le bouton au spawner
        button.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        if (cubeSpawner == null)
        {
            Debug.LogError("[UIButtonSpawner] Aucun CubeSpawner assigné. Impossible de spawner un cube.");
            return;
        }

        switch (spawnMethod)
        {
            case SpawnMethod.ByIndex:
                // Spawn avec l'index spécifié
                cubeSpawner.SpawnCube(prefabIndex);
                break;

            case SpawnMethod.ByPrefab:
                // Spawn avec le prefab spécifique
                if (specificPrefab != null)
                {
                    cubeSpawner.SpawnCube(specificPrefab);
                }
                else
                {
                    Debug.LogWarning($"[UIButtonSpawner] Aucun prefab spécifique assigné sur {gameObject.name}. Utilisation de l'index à la place.");
                    cubeSpawner.SpawnCube(prefabIndex);
                }
                break;

            default:
                // Fallback : spawn du premier cube
                cubeSpawner.SpawnCube(0);
                break;
        }
    }

    void OnDestroy()
    {
        // Nettoie le listener
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}

