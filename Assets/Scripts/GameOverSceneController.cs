using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameOverSceneController : MonoBehaviour
{
    public static string WinnerMessageTitle = "";
    public static string WinnerMessageBody = "";

    [Header("Referanslar")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    void Start()
    {
        // Bind UI dynamically if references are not set
        if (titleText == null)
            titleText = transform.Find("Container/Title")?.GetComponent<TMP_Text>();
        if (messageText == null)
            messageText = transform.Find("Container/Message")?.GetComponent<TMP_Text>();
        if (restartButton == null)
            restartButton = transform.Find("Container/RestartButton")?.GetComponent<Button>();
        if (mainMenuButton == null)
            mainMenuButton = transform.Find("Container/MainMenuButton")?.GetComponent<Button>();

        // Set Texts
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(WinnerMessageTitle) ? "OYUN BİTTİ" : WinnerMessageTitle;
        
        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(WinnerMessageBody) ? "Herkes öldü!" : WinnerMessageBody;

        // Bind Button Listeners
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);
        SceneManager.LoadScene("Karakter_Secme");
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);
        SceneManager.LoadScene("Ana_Menu");
    }
}
