using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ana Menü sahnesindeki UI elemanlarını ve geçişleri kontrol eder.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Karakter seçme sahnesinin adı")]
    [SerializeField] private string characterSelectionSceneName = "Karakter_Secme";

    [Tooltip("Öğretici paneli")]
    [SerializeField] private GameObject tutorialPanel;

    void Start()
    {
        AudioManager.Instance?.PlayMainMenuMusic();

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        // Dynamically create Tutorial and Exit Buttons based on the Play Button
        Transform playBtnTr = transform.Find("PlayButton");
        if (playBtnTr == null)
        {
            playBtnTr = transform.Find("Canvas/PlayButton");
        }

        if (playBtnTr != null)
        {
            var playRt = playBtnTr.GetComponent<RectTransform>();

            // Create Tutorial Button (ÖĞRETİCİ)
            GameObject tutorialBtnGo = Instantiate(playBtnTr.gameObject, playBtnTr.parent);
            tutorialBtnGo.name = "TutorialButton";
            var tutorialRt = tutorialBtnGo.GetComponent<RectTransform>();
            if (playRt != null && tutorialRt != null)
            {
                tutorialRt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, -100f);
            }
            var tutText = tutorialBtnGo.GetComponentInChildren<TMPro.TMP_Text>();
            if (tutText != null) tutText.text = "ÖĞRETİCİ";
            var tutBtn = tutorialBtnGo.GetComponent<UnityEngine.UI.Button>();
            if (tutBtn != null)
            {
                tutBtn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                tutBtn.onClick.AddListener(() => ShowTutorial(true));
            }

            // Create Exit Button (ÇIKIŞ)
            GameObject exitBtnGo = Instantiate(playBtnTr.gameObject, playBtnTr.parent);
            exitBtnGo.name = "ExitButton";
            var exitRt = exitBtnGo.GetComponent<RectTransform>();
            if (playRt != null && exitRt != null)
            {
                exitRt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, -200f);
            }
            var exitText = exitBtnGo.GetComponentInChildren<TMPro.TMP_Text>();
            if (exitText != null) exitText.text = "ÇIKIŞ";
            var exitBtn = exitBtnGo.GetComponent<UnityEngine.UI.Button>();
            if (exitBtn != null)
            {
                exitBtn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                exitBtn.onClick.AddListener(QuitGame);
            }
        }
    }

    /// <summary>
    /// Öğretici panelini gösterir veya gizler.
    /// </summary>
    public void ShowTutorial(bool show)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(show);
        }

        // Hide/show other menu buttons
        Transform playBtnTr = transform.Find("PlayButton") ?? transform.Find("Canvas/PlayButton");
        if (playBtnTr != null) playBtnTr.gameObject.SetActive(!show);

        Transform exitBtnTr = transform.Find("ExitButton") ?? transform.Find("Canvas/ExitButton");
        if (exitBtnTr != null) exitBtnTr.gameObject.SetActive(!show);

        Transform tutorialBtnTr = transform.Find("TutorialButton") ?? transform.Find("Canvas/TutorialButton");
        if (tutorialBtnTr != null) tutorialBtnTr.gameObject.SetActive(!show);
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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlaySFX(AudioManager.SFX.StartGame);
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
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
