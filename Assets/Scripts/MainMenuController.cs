using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ana Menü sahnesindeki UI elemanlarını ve geçişleri kontrol eder.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Karakter seçme sahnesinin adı")]
    [SerializeField] private string characterSelectionSceneName = "Karakter_Secme";

    /// <summary>
    /// Play butonuna tıklandığında karakter seçme sahnesine geçiş yapar.
    /// </summary>
    public void PlayGame()
    {
        if (string.IsNullOrEmpty(characterSelectionSceneName))
        {
            Debug.LogError("[MainMenuController] Yüklenecek karakter seçme sahnesi adı boş!");
            return;
        }

        Debug.Log($"[MainMenuController] Karakter seçme sahnesi yükleniyor: {characterSelectionSceneName}");
        SceneManager.LoadScene(characterSelectionSceneName);
    }
}
