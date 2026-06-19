using System.Collections;
using UnityEngine;

public class CupClickTrigger : MonoBehaviour
{
    public int cupIndex;

    private Vector3 originalScale;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    private bool isHighlighted = false;
    private GameSetupManager setupManager;

    void Awake()
    {
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            originalColor = Color.white;
        }
    }

    void Start()
    {
        setupManager = FindAnyObjectByType<GameSetupManager>();
        if (setupManager != null)
        {
            setupManager.OnSetupComplete += ResetHighlight;
        }
    }

    void OnDestroy()
    {
        if (setupManager != null)
        {
            setupManager.OnSetupComplete -= ResetHighlight;
        }
    }

    void OnMouseEnter()
    {
        if (isHighlighted) return;

        // 1. Setup asamasindaysek ve sira bizdeyse bardağı parildat
        if (setupManager != null && setupManager.CurrentPhase == GamePhase.Setup && setupManager.MevcutZehirKoyanOyuncuIndeksi == 0)
        {
            MasaYonetici masaYonetici = FindAnyObjectByType<MasaYonetici>();
            if (masaYonetici != null && masaYonetici.GetCupType(cupIndex) == CupType.EMPTY)
            {
                transform.localScale = originalScale * 1.1f;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(0f, 0.9f, 1f, 1f); // Parlayan turkuaz
                }
            }
            return;
        }

        // 2. Normal oyun sirasindaysak, sira bizdeyse ve secim/gosterim beklemiyorsak parildat
        PlayerTurnController turnController = FindAnyObjectByType<PlayerTurnController>();
        TurnManager turnManager = FindAnyObjectByType<TurnManager>();
        if (turnController != null && turnController.TurAktif && !turnController.IsWaitingForActionSelection &&
            (turnController.CardRevealPanel == null || !turnController.CardRevealPanel.gameObject.activeSelf) &&
            turnManager != null && turnManager.GetActivePlayerID() == 0)
        {
            MasaYonetici masaYonetici = FindAnyObjectByType<MasaYonetici>();
            if (masaYonetici != null && !masaYonetici.IsConsumed(cupIndex))
            {
                transform.localScale = originalScale * 1.1f;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(0f, 0.9f, 1f, 1f); // Parlayan turkuaz
                }
            }
        }
    }

    void OnMouseExit()
    {
        if (isHighlighted) return;
        ResetToNormal();
    }

    void ResetToNormal()
    {
        transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    void ResetHighlight()
    {
        isHighlighted = false;
        ResetToNormal();
    }

    void OnMouseDown()
    {
        // 1. Setup asamasindaysek zehir yerlestir
        if (setupManager != null && setupManager.CurrentPhase == GamePhase.Setup)
        {
            if (setupManager.MevcutZehirKoyanOyuncuIndeksi == 0)
            {
                MasaYonetici masaYonetici = FindAnyObjectByType<MasaYonetici>();
                if (masaYonetici != null && masaYonetici.GetCupType(cupIndex) == CupType.EMPTY)
                {
                    isHighlighted = true;
                    transform.localScale = originalScale * 1.1f;
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = new Color(0f, 0.9f, 1f, 1f); // Parlayan turkuaz
                    }

                    setupManager.OyuncuZehirKoy(cupIndex);
                }
            }
            return;
        }

        // 2. Dedektif bardak secim modundaysa dedektif yetenegini tetikle
        DetectiveCupInspector detectiveInspector = FindAnyObjectByType<DetectiveCupInspector>();
        if (detectiveInspector != null && detectiveInspector.BardakSecimModuAktifMi())
        {
            detectiveInspector.OnBardakButtonClicked(cupIndex);
            return;
        }

        // 3. Normal oyun sirasindaysek bardagi ic
        PlayerTurnController turnController = FindAnyObjectByType<PlayerTurnController>();
        TurnManager turnManager = FindAnyObjectByType<TurnManager>();
        if (turnController != null && turnController.TurAktif && !turnController.IsWaitingForActionSelection &&
            turnManager != null && turnManager.GetActivePlayerID() == 0)
        {
            var reveal = turnController.CardRevealPanel;
            if (reveal == null || !reveal.gameObject.activeSelf)
            {
                turnController.BardakSecVeIc(cupIndex);
            }
        }
    }

    /// <summary>
    /// Bardağı anime ederek içer. 
    /// Faz 1: 1.5s şiddetli titreme (gerilim!)
    /// Faz 2: 0.6s kaldırma + eğme + renk değişimi
    /// Faz 3: 0.5s bekleme (içeriği gör)
    /// Faz 4: 0.4s sönme
    /// Son: Yerde renkli iz bırak
    /// </summary>
    public void PlayDrinkAnimation(CupType cupType, System.Action onComplete)
    {
        StartCoroutine(DrinkCoroutine(cupType, onComplete));
    }

    private IEnumerator DrinkCoroutine(CupType cupType, System.Action onComplete)
    {
        isHighlighted = true; // Prevent hovers/clicks during animation

        // --- Faz 1: Uzun titreme (gerilim hissi) ---
        float elapsed = 0f;
        float shakeDuration = 2.5f;   // 1.5 → 2.5s
        Vector3 startPos = transform.localPosition;    // Local (animasyon için)
        Vector3 worldStartPos = transform.position;    // World (puddle spawn için)

        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            // Titreme şiddeti zamanla artar (giderek daha korkutucu)
            float intensity = Mathf.Lerp(0.04f, 0.12f, progress);
            float xOffset = Random.Range(-intensity, intensity);
            float yOffset = Random.Range(-intensity, intensity);
            transform.localPosition = startPos + new Vector3(xOffset, yOffset, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = startPos;

        // --- Faz 2: Kaldırma + eğme + renk değişimi ---
        elapsed = 0f;
        float riseTime = 0.8f;   // 0.6 → 0.8s
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, -35f);
        Vector3 targetPos = startPos + new Vector3(0f, 0.35f, 0f);

        Color revealColor;
        switch (cupType)
        {
            case CupType.POISON:
                revealColor = new Color(0.1f, 1f, 0.1f, 1f);   // Parlak yeşil
                break;
            case CupType.ANTIDOTE:
                revealColor = new Color(1f, 1f, 1f, 1f);         // Parlak beyaz
                break;
            default:
                revealColor = new Color(0.2f, 0.6f, 1f, 1f);    // Parlak mavi
                break;
        }

        while (elapsed < riseTime)
        {
            float t = elapsed / riseTime;
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(originalColor, revealColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = targetPos;
        transform.localRotation = targetRot;
        if (spriteRenderer != null)
            spriteRenderer.color = revealColor;

        // --- Faz 3: Rengi göster ---
        yield return new WaitForSeconds(1.2f);  // 0.5 → 1.2s

        // --- Faz 4: Sönme ---
        elapsed = 0f;
        float fadeTime = 0.5f;  // 0.4 → 0.5s
        Vector3 currentScale = transform.localScale;

        while (elapsed < fadeTime)
        {
            float t = elapsed / fadeTime;
            transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, t);
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(revealColor.r, revealColor.g, revealColor.b, 1f - t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;

        // --- Yerde iz bırak (world pozisyonunda, SetActive öncesinde!) ---
        SpawnPuddle(cupType, worldStartPos);

        // Bardak gizle
        gameObject.SetActive(false);

        onComplete?.Invoke();
    }

    /// <summary>
    /// Bardağın döküldüğü yerde geçici renkli bir iz bırakır.
    /// Zehir → Yeşil, Panzehir → Beyaz, Boş → Mavi. 4 saniyede solar.
    /// </summary>
    private void SpawnPuddle(CupType cupType, Vector3 worldPos)
    {
        Color puddleColor;
        switch (cupType)
        {
            case CupType.POISON:
                puddleColor = new Color(0.1f, 0.85f, 0.1f, 0.8f);
                break;
            case CupType.ANTIDOTE:
                puddleColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
                break;
            default:
                puddleColor = new Color(0.2f, 0.55f, 1f, 0.8f);
                break;
        }

        // Daire texture oluştur
        int texSize = 64;
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texSize * texSize];
        float center = texSize / 2f;
        float outerRadius = texSize / 2f - 1f;
        float innerRadius = outerRadius - 8f;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = x - center, dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 0f;
                if (dist < innerRadius)
                    alpha = 1f;
                else if (dist < outerRadius)
                    alpha = 1f - (dist - innerRadius) / (outerRadius - innerRadius);
                pixels[y * texSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        Sprite puddleSprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 100f);

        GameObject puddle = new GameObject("CupPuddle");
        // Bardağın dünya pozisyonuna yerleştir, Z biraz arkaya
        puddle.transform.position = new Vector3(worldPos.x, worldPos.y - 0.05f, worldPos.z + 0.1f);
        // Ovalleştir (dökülmüş sıvı görünümü)
        puddle.transform.localScale = new Vector3(0.9f, 0.35f, 1f);

        SpriteRenderer sr = puddle.AddComponent<SpriteRenderer>();
        sr.sprite = puddleSprite;
        sr.color = puddleColor;
        sr.sortingOrder = 5; // Bardakların üstünde

        // PuddleFader: kendi coroutine'ını puddle'a ait MonoBehaviour üzerinde çalıştırır.
        // Bu sayede CupClickTrigger deactivate olsa bile fade devam eder.
        PuddleFader fader = puddle.AddComponent<PuddleFader>();
        fader.StartFade(sr, puddleColor, 4.0f);
    }

    public void ResetVisualState()
    {
        gameObject.SetActive(true);
        isHighlighted = false;
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
