using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script simple pour connecter un bouton UI à un CubeSpawner.
/// À attacher sur un bouton UI (Button component).
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Le CubeSpawner qui va créer les cubes. Si vide, cherchera automatiquement dans la scène.")]
    public CubeSpawner cubeSpawner;

    private Button button;

    void Start()
    {
        // Récupère le composant Button
        button = GetComponent<Button>();

        // Si aucun spawner n'est assigné, essaie de le trouver dans la scène
        if (cubeSpawner == null)
        {
            cubeSpawner = FindObjectOfType<CubeSpawner>();
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
        if (cubeSpawner != null)
        {
            cubeSpawner.SpawnCube();
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

