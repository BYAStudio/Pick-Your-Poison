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
        // Play endgame sequence (sound1 then sound2)
        AudioManager.Instance?.PlayEndGameSequence();

        // Bind UI dynamically if references are not set
        if (titleText == null)
            titleText = transform.Find("Container/Title")?.GetComponent<TMP_Text>();
        if (messageText == null)
            messageText = transform.Find("Container/Message")?.GetComponent<TMP_Text>();
        if (restartButton == null)
            restartButton = transform.Find("Container/RestartButton")?.GetComponent<Button>();
        if (mainMenuButton == null)
            mainMenuButton = transform.Find("Container/MainMenuButton")?.GetComponent<Button>();

        // Set Texts and Layout Positions
        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(WinnerMessageTitle) ? "OYUN BİTTİ" : WinnerMessageTitle;
            RectTransform rt = titleText.rectTransform;
            if (rt != null)
            {
                // Keep stretch width and top center pivot, just change Y position
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -200f);
                rt.sizeDelta = new Vector2(0f, 100f);
            }
        }
        
        if (messageText != null)
        {
            messageText.text = string.IsNullOrEmpty(WinnerMessageBody) ? "Herkes öldü!" : WinnerMessageBody;
            RectTransform rt = messageText.rectTransform;
            if (rt != null)
            {
                // Align under Title by changing anchor to top-stretch
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -310f);
                rt.sizeDelta = new Vector2(-40f, 200f);
            }
        }

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
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("Karakter_Secme");
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("Ana_Menu");
    }
}
