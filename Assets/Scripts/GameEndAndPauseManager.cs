using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameEndAndPauseManager : MonoBehaviour
{
    private TurnManager turnManager;
    private GameManager gameManager;

    private GameObject pauseButtonGo;
    private GameObject pausePanelGo;
    private GameObject gameOverPanelGo;

    private bool isPaused = false;

    void Awake()
    {
        turnManager = FindAnyObjectByType<TurnManager>();
        gameManager = FindAnyObjectByType<GameManager>();

        if (turnManager != null)
        {
            turnManager.OnGameOver += HandleGameOverEvent;
        }
    }

    void Start()
    {
        CreatePauseButton();
        CreatePausePanel();
    }

    void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnGameOver -= HandleGameOverEvent;
        }
    }

    private void CreatePauseButton()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;

        // Create Pause Button container
        pauseButtonGo = new GameObject("PauseButton");
        pauseButtonGo.transform.SetParent(canvasGo.transform, false);

        var rt = pauseButtonGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(110f, 40f);

        var img = pauseButtonGo.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 0.85f); // Sleek dark glass

        var outline = pauseButtonGo.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.3f, 0.5f); // Soft gold border
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        var btn = pauseButtonGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => TogglePause(true));

        // Button Text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(pauseButtonGo.transform, false);

        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "⏸ DURDUR";
        tmp.fontSize = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.85f, 0.3f, 1f); // Gold text
    }

    private void CreatePausePanel()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;

        // Fullscreen overlay
        pausePanelGo = new GameObject("PauseOverlay");
        pausePanelGo.transform.SetParent(canvasGo.transform, false);
        pausePanelGo.SetActive(false);

        var rt = pausePanelGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = pausePanelGo.AddComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.04f, 0.85f); // Fullscreen dim blur look

        // Center Container
        GameObject containerGo = new GameObject("Container");
        containerGo.transform.SetParent(pausePanelGo.transform, false);

        var cRt = containerGo.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(360f, 260f);

        var cImg = containerGo.AddComponent<Image>();
        cImg.color = new Color(0.06f, 0.06f, 0.09f, 0.98f); // Sleek dark solid

        var cOutline = containerGo.AddComponent<Outline>();
        cOutline.effectColor = new Color(1f, 0.85f, 0.3f, 0.7f); // Gold border
        cOutline.effectDistance = new Vector2(2f, 2f);

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(containerGo.transform, false);

        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -30f);
        titleRt.sizeDelta = new Vector2(0f, 40f);

        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "OYUN DURDURULDU";
        titleTmp.fontSize = 24f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(1f, 0.85f, 0.3f, 1f);

        // Button 1: Devam Et
        CreateUIButton(containerGo.transform, "ResumeButton", "DEVAM ET", new Color(0.12f, 0.45f, 0.2f, 0.95f), new Vector2(0f, 15f), () => TogglePause(false));

        // Button 2: Oyundan Ayrıl
        CreateUIButton(containerGo.transform, "LeaveButton", "OYUNDAN AYRIL", new Color(0.7f, 0.15f, 0.15f, 0.95f), new Vector2(0f, -50f), LeaveGame);
    }

    private void CreateUIButton(Transform parent, string name, string labelText, Color bgColor, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject buttonGo = new GameObject(name);
        buttonGo.transform.SetParent(parent, false);

        var rt = buttonGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(240f, 45f);

        var img = buttonGo.AddComponent<Image>();
        img.color = bgColor;

        var outline = buttonGo.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        outline.effectDistance = new Vector2(1f, 1f);

        var btn = buttonGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClickAction);

        // Text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform, false);

        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    public void TogglePause(bool pause)
    {
        isPaused = pause;
        if (pausePanelGo != null)
        {
            pausePanelGo.SetActive(pause);
        }

        // Set timescale to 0 (completely stops physical time/updates) or 1
        Time.timeScale = pause ? 0f : 1f;

        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);
    }

    public void LeaveGame()
    {
        Time.timeScale = 1f; // Always restore timescale
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);

        if (gameManager != null)
        {
            gameManager.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene("Ana_Menu");
        }
    }

    private void HandleGameOverEvent(bool singleWinner)
    {
        StartCoroutine(ShowGameOverPanelDelayed());
    }

    private IEnumerator ShowGameOverPanelDelayed()
    {
        // Wait 2.5 seconds to let the death animation/sound finish before blocking screen
        yield return new WaitForSeconds(2.5f);

        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) yield break;

        // Hide pause button
        if (pauseButtonGo != null) pauseButtonGo.SetActive(false);

        // Fullscreen overlay
        gameOverPanelGo = new GameObject("GameOverOverlay");
        gameOverPanelGo.transform.SetParent(canvasGo.transform, false);

        var rt = gameOverPanelGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = gameOverPanelGo.AddComponent<Image>();
        img.color = new Color(0.04f, 0.04f, 0.06f, 0.98f); // Main menu dark glass background style

        // Center Container
        GameObject containerGo = new GameObject("Container");
        containerGo.transform.SetParent(gameOverPanelGo.transform, false);

        var cRt = containerGo.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(500f, 380f);

        var cImg = containerGo.AddComponent<Image>();
        cImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        var cOutline = containerGo.AddComponent<Outline>();
        cOutline.effectColor = new Color(1f, 0.85f, 0.3f, 0.7f); // Gold border
        cOutline.effectDistance = new Vector2(2f, 2f);

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(containerGo.transform, false);

        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(0f, 50f);

        List<int> survivors = turnManager != null ? turnManager.GetAlivePlayerIDs() : new List<int>();
        bool hasSurvivors = survivors.Count > 0;

        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = hasSurvivors ? "TEBRİKLER!" : "OYUN BİTTİ";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(1f, 0.85f, 0.3f, 1f);

        // Message
        GameObject msgGo = new GameObject("Message");
        msgGo.transform.SetParent(containerGo.transform, false);

        var msgRt = msgGo.AddComponent<RectTransform>();
        msgRt.anchorMin = new Vector2(0f, 0.5f);
        msgRt.anchorMax = new Vector2(1f, 0.5f);
        msgRt.pivot = new Vector2(0.5f, 0.5f);
        msgRt.anchoredPosition = new Vector2(0f, 40f);
        msgRt.sizeDelta = new Vector2(-40f, 100f);

        var msgTmp = msgGo.AddComponent<TextMeshProUGUI>();
        msgTmp.fontSize = 20f;
        msgTmp.alignment = TextAlignmentOptions.Center;
        msgTmp.color = Color.white;
        msgTmp.enableWordWrapping = true;

        if (hasSurvivors)
        {
            List<string> survivorNames = new List<string>();
            foreach (int id in survivors)
            {
                Player p = turnManager.GetPlayer(id);
                if (p != null)
                {
                    survivorNames.Add(GetKarakterName(p.characterType));
                }
            }
            string formattedNames = string.Join(", ", survivorNames);
            msgTmp.text = $"{formattedNames}\n\nhayatta kalmayı başardı!";
        }
        else
        {
            msgTmp.text = "Herkes öldü!\n\nHayatta kalan kimse olmadı.";
        }

        // Button 1: Yeniden Oyna
        CreateUIButton(containerGo.transform, "RestartButton", "YENİDEN OYNA", new Color(0.12f, 0.45f, 0.2f, 0.95f), new Vector2(0f, -50f), RestartGame);

        // Button 2: Ana Menüye Dön
        CreateUIButton(containerGo.transform, "MainMenuButton", "ANA MENÜYE DÖN", new Color(0.1f, 0.1f, 0.15f, 0.95f), new Vector2(0f, -110f), ReturnToMainMenu);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);

        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);

        if (gameManager != null)
        {
            gameManager.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene("Ana_Menu");
        }
    }

    private string GetKarakterName(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Doctor: return "Doktor";
            case CharacterType.Survivor: return "Hayatta Kalan";
            case CharacterType.Chemist: return "Kimyager";
            case CharacterType.Detective: return "Dedektif";
            default: return "Oyuncu";
        }
    }
}
