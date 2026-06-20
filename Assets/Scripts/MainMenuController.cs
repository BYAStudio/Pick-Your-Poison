using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ana Menü sahnesindeki UI elemanlarını ve geçişleri kontrol eder.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Karakter seçme sahnesinin adı")]
    [SerializeField] private string characterSelectionSceneName = "Karakter_Secme";

    void Start()
    {
        // Dynamically create an Exit Button based on the Play Button
        Transform playBtnTr = transform.Find("PlayButton");
        if (playBtnTr == null)
        {
            playBtnTr = transform.Find("Canvas/PlayButton");
        }

        if (playBtnTr != null)
        {
            GameObject exitBtnGo = Instantiate(playBtnTr.gameObject, playBtnTr.parent);
            exitBtnGo.name = "ExitButton";
            
            var playRt = playBtnTr.GetComponent<RectTransform>();
            var exitRt = exitBtnGo.GetComponent<RectTransform>();
            if (playRt != null && exitRt != null)
            {
                // Position it 100 units below the Play Button
                exitRt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, -100f);
            }

            // Update text label to "ÇIKIŞ"
            var textComp = exitBtnGo.GetComponentInChildren<TMPro.TMP_Text>();
            if (textComp != null)
            {
                textComp.text = "ÇIKIŞ";
            }
            else
            {
                var legacyText = exitBtnGo.GetComponentInChildren<UnityEngine.UI.Text>();
                if (legacyText != null) legacyText.text = "ÇIKIŞ";
            }

            // Hook up onClick listener
            var btn = exitBtnGo.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(QuitGame);
            }
        }
    }

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

    /// <summary>
    /// Çıkış butonuna tıklandığında oyunu kapatır.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MainMenuController] Çıkış yapılıyor...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
