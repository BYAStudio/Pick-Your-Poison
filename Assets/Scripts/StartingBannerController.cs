using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StartingBannerController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private float displayDuration = 2f;
    private float fadeDuration = 0.4f;
    private float elapsed = 0f;
    private bool fading = false;

    /// <summary>
    /// Banner'ı başlatır.
    /// duration = 0 ise varsayılan (2s) kullanılır.
    /// yMin/yMax anchor ile dikey konumu ayarlanabilir.
    /// </summary>
    public void Initialize(string text, float duration = 0f, float yMin = 0.4f, float yMax = 0.6f)
    {
        if (duration > 0f)
            displayDuration = duration;

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Arka plan paneli (sleek dark glassmorphism)
        var bgImage = gameObject.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);

        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, yMin);
        rectTransform.anchorMax = new Vector2(1f, yMax);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;


        // Metin objesi
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(transform, false);
        
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmpText = textGo.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 40f;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.color = new Color(1f, 0.85f, 0.3f, 1f); // Altın sarısı
        tmpText.enableWordWrapping = true;

        // Yumuşak açılış animasyonu
        transform.localScale = new Vector3(1f, 0f, 1f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        
        // Açılış animasyonu (0.2s içinde scale Y → 1)
        if (elapsed < 0.2f)
        {
            transform.localScale = new Vector3(1f, elapsed / 0.2f, 1f);
        }
        else if (transform.localScale.y != 1f)
        {
            transform.localScale = Vector3.one;
        }

        if (!fading)
        {
            if (elapsed >= displayDuration)
            {
                fading = true;
                elapsed = 0f;
            }
        }
        else
        {
            float t = elapsed / fadeDuration;
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
            else
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - t;
            }
        }
    }
}
