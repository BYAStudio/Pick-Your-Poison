using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardRevealPanelController : MonoBehaviour
{
    public static CardRevealPanelController Instance { get; private set; }

    [Header("UI Elementleri")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject okayButton;

    [Header("Kart Spriteleri (Sırayla)")]
    [Tooltip("0: Acgozluluk, 1: KritikDoz, 2: ZehirTarama, 3: PanzehirTarama, 4: Girdap, 5: Nefeslenme, 6: ZorakiIkram")]
    [SerializeField] private Sprite[] cardSprites;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (okayButton != null)
        {
            okayButton.SetActive(false);
        }
    }

    public void ShowCard(CardType cardType, System.Action onComplete)
    {
        gameObject.SetActive(true);

        float openingLength = 0.8f; // Default fallback
        float waitingLength = 2.0f; // Default fallback

        if (AudioManager.Instance != null)
        {
            AudioClip openingClip = AudioManager.Instance.GetClip(AudioManager.SFX.CardOpening);
            if (openingClip != null)
            {
                openingLength = openingClip.length;
            }

            AudioClip waitingClip = AudioManager.Instance.GetClip(AudioManager.SFX.CardWaiting);
            if (waitingClip != null)
            {
                waitingLength = waitingClip.length;
            }
        }

        // Total shake duration is opening length + waiting length
        float totalShakeDuration = openingLength + waitingLength;

        int index = (int)cardType;

        // Kart resmini ata ama önce gizle (dramatik reveal için)
        if (cardSprites != null && index >= 0 && index < cardSprites.Length)
        {
            if (cardImage != null)
            {
                cardImage.sprite = cardSprites[index];
                // Başlangıçta görünmez – titreme sonrası açılacak
                cardImage.color = new Color(1f, 1f, 1f, 0f);
                cardImage.gameObject.SetActive(cardSprites[index] != null);
            }
        }
        else
        {
            if (cardImage != null)
                cardImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (descriptionText != null)
        {
            descriptionText.text = GetCardDescription(cardType);
            descriptionText.color = new Color(descriptionText.color.r, descriptionText.color.g, descriptionText.color.b, 0f);
            descriptionText.gameObject.SetActive(false); // Hide during shake phase
        }

        StartCoroutine(DramaticRevealCoroutine(cardType, totalShakeDuration, openingLength, onComplete));
    }

    private string GetCardDescription(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.AcgozlulukCezasi:
                return "AÇGÖZLÜLÜK CEZASI\n\nMasadan 2 bardak içmek zorundasın!";
            case CardType.KritikDoz:
                return "KRİTİK DOZ\n\nDirekt olarak zehirlendin!";
            case CardType.ZehirTarama:
                return "ZEHİR TARAMA\n\nSeçtiğin 2x2 alandaki zehir sayısını herkes görür!";
            case CardType.PanzehirTarama:
                return "PANZEHİR TARAMA\n\nSeçtiğin 2x2 alandaki panzehir sayısını herkes görür!";
            case CardType.Girdap:
                return "GİRDAP\n\nTur sırasının yönü tersine döndü!";
            case CardType.Nefeslenme:
                return "NEFESLENME\n\nBu turu güvenle pas geçtin!";
            case CardType.ZorakiIkram:
                return "ZORAKİ İKRAM\n\nBir rakibine masadan istediğin bir bardağı içir!";
            default:
                return "";
        }
    }

    /// <summary>
    /// Dramatik kart açılma:
    /// Faz 1 (2s): Panel titrer, açıklama görünür, kart resmi gizli.
    /// Faz 2 (0.5s): Kart resmi parlayarak açığa çıkar.
    /// Faz 3 (2s): Kart görünür kalır.
    /// Faz 4: Panel kapanır, callback çağrılır.
    /// </summary>
    private IEnumerator DramaticRevealCoroutine(CardType cardType, float shakeDuration, float waitDelay, System.Action onComplete)
    {
        Vector3 originalPos = transform.localPosition;

        // Play card waiting sound after opening sound completes
        StartCoroutine(PlayCardWaitingDelayed(waitDelay));

        // --- Faz 1: Titreme ---
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            // Titreme şiddeti zamanla azalır (ama başta yoğun)
            float intensity = Mathf.Lerp(12f, 2f, progress);
            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity * 0.5f, intensity * 0.5f);
            transform.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;

        // Play card reveal sound immediately after shake finishes (when card appears on screen)
        PlayCardRevealSound(cardType);

        // --- Faz 2: Kart resmi ve açıklama fade-in (0.8s) ---
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
        }
        elapsed = 0f;
        float revealTime = 0.8f;   // 0.5 → 0.8s
        Color origTextColor = descriptionText != null ? descriptionText.color : Color.white;
        while (elapsed < revealTime)
        {
            float t = elapsed / revealTime;
            if (cardImage != null)
                cardImage.color = new Color(1f, 1f, 1f, t);
            if (descriptionText != null)
                descriptionText.color = new Color(origTextColor.r, origTextColor.g, origTextColor.b, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (cardImage != null)
            cardImage.color = Color.white;
        if (descriptionText != null)
            descriptionText.color = new Color(origTextColor.r, origTextColor.g, origTextColor.b, 1f);

        // --- Faz 3: Kart görünür (3 saniye) ---
        yield return new WaitForSeconds(3.0f);  // 2.0 → 3.0s

        // Kapat
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private void PlayCardRevealSound(CardType cardType)
    {
        if (cardType == CardType.AcgozlulukCezasi || cardType == CardType.KritikDoz)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.NegativeCard);
        }
        else if (cardType == CardType.Nefeslenme || cardType == CardType.ZorakiIkram)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PositiveCard);
        }
        else // ZehirTarama, PanzehirTarama, Girdap
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.NotrCard);
        }
    }

    private IEnumerator PlayCardWaitingDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.CardWaiting);
    }
}
