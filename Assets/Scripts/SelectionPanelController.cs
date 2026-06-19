using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyuncuya "Bardak İç / Kart Seç" seçeneğini sunar.
/// Botlar için de gösterilir: bot "düşünür", kararını vurgular, sonra panel kapanır.
/// </summary>
public class SelectionPanelController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Button drawCardButton;
    [SerializeField] private Button drinkButton;

    private PlayerTurnController turnController;
    private TMP_Text botStatusLabel;

    // Buton orijinal renkleri (bot vurgulaması sonrası sıfırlamak için)
    private Color drinkBtnOriginalColor   = Color.white;
    private Color cardBtnOriginalColor    = Color.white;

    void Awake()
    {
        turnController = FindAnyObjectByType<PlayerTurnController>();

        if (drawCardButton != null)
        {
            drawCardButton.onClick.AddListener(OnDrawCardClicked);
            var img = drawCardButton.GetComponent<Image>();
            if (img != null) cardBtnOriginalColor = img.color;
        }
        if (drinkButton != null)
        {
            drinkButton.onClick.AddListener(OnDrinkClicked);
            var img = drinkButton.GetComponent<Image>();
            if (img != null) drinkBtnOriginalColor = img.color;
        }
    }

    // ─────────────────────────────────────────
    //  Oyuncu için normal panel
    // ─────────────────────────────────────────

    public void ShowPanel()
    {
        SetBotLabelVisible(false);
        SetButtonsInteractable(true);
        gameObject.SetActive(true);
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────
    //  Bot için otomatik panel
    // ─────────────────────────────────────────

    /// <summary>
    /// Bot'un seçimini herkese gösterir.
    /// willDrink=true → Bardak İç vurgulanır, false → Kart Seç vurgulanır.
    /// thinkingDelay: bot karar verene kadar geçen süre (saniye).
    /// onComplete: panel kapandıktan sonra çağrılır.
    /// </summary>
    public void ShowForBot(bool willDrink, float thinkingDelay, System.Action onComplete)
    {
        StartCoroutine(BotSelectionCoroutine(willDrink, thinkingDelay, onComplete));
    }

    private IEnumerator BotSelectionCoroutine(bool willDrink, float thinkingDelay, System.Action onComplete)
    {
        // Paneli aç, butonları devre dışı bırak (bot için tıklanamaz)
        SetButtonsInteractable(false);
        ResetButtonColors();
        gameObject.SetActive(true);

        // "Bot düşünüyor..." etiketi göster
        TMP_Text lbl = GetOrCreateBotLabel();
        lbl.text     = "🤖  Bot düşünüyor...";
        lbl.color    = new Color(1f, 0.9f, 0.3f, 1f);
        SetBotLabelVisible(true);

        // Düşünme süresi (titrek pulsing metin ile bekle)
        float elapsed = 0f;
        while (elapsed < thinkingDelay)
        {
            elapsed += Time.deltaTime;
            // Her 0.6s'de "..." değişimi
            int dots = (int)(elapsed / 0.5f) % 4;
            lbl.text = "🤖  Bot düşünüyor" + new string('.', dots);
            yield return null;
        }

        // Kararı göster
        string karar = willDrink ? "BARDAK İÇ!" : "KART ÇEK!";
        lbl.text  = $"🤖  Bot kararı:\n{karar}";
        lbl.color = new Color(1f, 0.5f, 0.2f, 1f);

        // Seçilen butonu vurgula (altın sarısı flaş)
        Button chosenBtn    = willDrink ? drinkButton    : drawCardButton;
        Button notChosenBtn = willDrink ? drawCardButton : drinkButton;

        if (chosenBtn != null)
        {
            var img = chosenBtn.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 0.85f, 0.1f, 1f); // altın sarısı
        }
        if (notChosenBtn != null)
        {
            var img = notChosenBtn.GetComponent<Image>();
            if (img != null) img.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // soluk
        }

        // Kararı 2 saniye göster
        yield return new WaitForSeconds(2.0f);

        // Temizle ve kapat
        ResetButtonColors();
        SetButtonsInteractable(true);
        SetBotLabelVisible(false);
        HidePanel();

        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────
    //  Buton olayları (oyuncu için)
    // ─────────────────────────────────────────

    private void OnDrawCardClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);
        HidePanel();
        if (turnController != null)
            turnController.OnDrawCardSelected();
    }

    private void OnDrinkClicked()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ButtonClick);
        HidePanel();
        if (turnController != null)
            turnController.OnDrinkCupSelected();
    }

    // ─────────────────────────────────────────
    //  Yardımcı
    // ─────────────────────────────────────────

    private void SetButtonsInteractable(bool value)
    {
        if (drawCardButton != null) drawCardButton.interactable = value;
        if (drinkButton    != null) drinkButton.interactable    = value;
    }

    private void ResetButtonColors()
    {
        if (drawCardButton != null)
        {
            var img = drawCardButton.GetComponent<Image>();
            if (img != null) img.color = cardBtnOriginalColor;
        }
        if (drinkButton != null)
        {
            var img = drinkButton.GetComponent<Image>();
            if (img != null) img.color = drinkBtnOriginalColor;
        }
    }

    private void SetBotLabelVisible(bool visible)
    {
        if (botStatusLabel != null)
            botStatusLabel.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Bot durumu için dinamik metin etiketi oluşturur (ilk kullanımda).
    /// Panelin alt bölümüne eklenir.
    /// </summary>
    private TMP_Text GetOrCreateBotLabel()
    {
        if (botStatusLabel != null)
            return botStatusLabel;

        GameObject labelGO = new GameObject("BotStatusLabel");
        labelGO.transform.SetParent(transform, false);

        var rect = labelGO.AddComponent<RectTransform>();
        // Panelin alt yarısında, tam genişlikte
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0.3f);
        rect.offsetMin = new Vector2(8f, 4f);
        rect.offsetMax = new Vector2(-8f, -4f);

        botStatusLabel = labelGO.AddComponent<TextMeshProUGUI>();
        botStatusLabel.alignment     = TextAlignmentOptions.Center;
        botStatusLabel.fontSize      = 22f;
        botStatusLabel.fontStyle     = FontStyles.Bold;
        botStatusLabel.color         = new Color(1f, 0.9f, 0.3f, 1f);
        botStatusLabel.enableWordWrapping = true;

        return botStatusLabel;
    }
}
