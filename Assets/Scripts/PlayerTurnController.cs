using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faz 1: Aktif oyuncunun tur aksiyonlarini yonetir.
/// </summary>
public class PlayerTurnController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] TurnManager turnManager;
    [SerializeField] CardManager cardManager;
    [SerializeField] SelectionPanelController selectionPanelController;
    [SerializeField] CardRevealPanelController cardRevealPanelController;

    [Header("Durum")]
    [SerializeField] bool turAktif = false;
    [SerializeField] bool kartSecimiBekleniyor = false;
    [SerializeField] CardType bekleyenKart;
    [SerializeField] bool secimGerektirenKartlariOtomatikCoz = true;

    [Header("Açgözlülük Cezası")]
    [SerializeField] private bool acgozlulukCezasiAktif = false;
    [SerializeField] private int acgozlulukKalanBardak = 0;

    [Header("Detective / Chemist / Scan Ability")]
    [SerializeField] private bool detectiveInspectionModeActive = false;
    [SerializeField] private bool areaSelectionModeActive = false;
    [SerializeField] private CardType areaSelectionCard;

    private PlayerPoisonUIHandler uiHandler;

    public bool TurAktif => turAktif;
    public bool IsWaitingForActionSelection { get; private set; } = false;
    public bool AcgozlulukCezasiAktif => acgozlulukCezasiAktif;
    public CardRevealPanelController CardRevealPanel => cardRevealPanelController;
    public bool DetectiveInspectionModeActive => detectiveInspectionModeActive;
    public bool AreaSelectionModeActive => areaSelectionModeActive;
    public CardType AreaSelectionCard => areaSelectionCard;

    private Dictionary<int, CupClickTrigger> cupTriggers = new Dictionary<int, CupClickTrigger>();
    private List<int> chemistHighlightedCups = new List<int>();
    private List<int> detectiveHighlightedCups = new List<int>();

    // Karakter isimlerini Türkçe döndürür
    private static readonly Dictionary<CharacterType, string> KarakterIsimleri = new Dictionary<CharacterType, string>
    {
        { CharacterType.None,      "Oyuncu"        },
        { CharacterType.Doctor,    "Doktor"         },
        { CharacterType.Survivor,  "Hayatta Kalan"  },
        { CharacterType.Chemist,   "Kimyager"       },
        { CharacterType.Detective, "Dedektif"       }
    };

    void Awake()
    {
        ResolveReferences();
        
        // Add UI handler if not present
        if (FindAnyObjectByType<PlayerPoisonUIHandler>() == null)
        {
            gameObject.AddComponent<PlayerPoisonUIHandler>();
        }
        uiHandler = FindAnyObjectByType<PlayerPoisonUIHandler>();

        // Add Pause and End GameManager if not present
        if (FindAnyObjectByType<GameEndAndPauseManager>() == null)
        {
            gameObject.AddComponent<GameEndAndPauseManager>();
        }

        // Ekranın üst kısmındaki "Sira_Text" nesnesini gizle
        GameObject.Find("Sira_Text")?.SetActive(false);
        GameObject.Find("Canvas/Sira_Text")?.SetActive(false);
    }

    void Start()
    {
        BindMagnifyingGlassButtons();
    }

    private void BindMagnifyingGlassButtons()
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            // Panel 1 button (human player)
            var p1Button = canvasGo.transform.Find("Oyucu_Panel_1/Button")?.GetComponent<UnityEngine.UI.Button>();
            if (p1Button != null)
            {
                p1Button.onClick.RemoveAllListeners();
                p1Button.onClick.AddListener(OnMagnifyingGlassClicked);
            }

            // Dedektif Bilgi Paneli / Bardaga_Bak button
            var dbpButton = canvasGo.transform.Find("Dedektif_Bilgi_Paneli/Bardaga_Bak")?.GetComponent<UnityEngine.UI.Button>();
            if (dbpButton != null)
            {
                dbpButton.onClick.RemoveAllListeners();
                dbpButton.onClick.AddListener(OnMagnifyingGlassClicked);
            }

            // Kimyager close button
            var chemistClose = canvasGo.transform.Find("Kimyager_Analiz_Paneli/Kapat_Butonu")?.GetComponent<UnityEngine.UI.Button>();
            if (chemistClose != null)
            {
                chemistClose.onClick.RemoveAllListeners();
                chemistClose.onClick.AddListener(() => {
                    var panel = canvasGo.transform.Find("Kimyager_Analiz_Paneli");
                    if (panel != null) panel.gameObject.SetActive(false);
                });
            }

            // Detective close button
            var detectiveClose = canvasGo.transform.Find("Dedektif_Sonuc_Paneli/Kapat_Butonu")?.GetComponent<UnityEngine.UI.Button>();
            if (detectiveClose != null)
            {
                detectiveClose.onClick.RemoveAllListeners();
                detectiveClose.onClick.AddListener(() => {
                    var panel = canvasGo.transform.Find("Dedektif_Sonuc_Paneli");
                    if (panel != null) panel.gameObject.SetActive(false);
                });
            }
        }
    }

    #region Tur Kontrolu

    public void YeniTurBasladi()
    {
        ResetChemistHighlights();
        ResetDetectiveHighlights();

        if (turnManager != null && turnManager.IsGameOver())
        {
            turAktif = false;
            return;
        }

        // Deaktif yetenek panelleri
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            var chemistPanel = canvasGo.transform.Find("Kimyager_Analiz_Paneli");
            if (chemistPanel != null) chemistPanel.gameObject.SetActive(false);

            var detectivePanel = canvasGo.transform.Find("Dedektif_Sonuc_Paneli");
            if (detectivePanel != null) detectivePanel.gameObject.SetActive(false);
        }

        turAktif = true;
        kartSecimiBekleniyor = false;

        if (uiHandler != null) uiHandler.UpdateAllPanels();

        AudioManager.Instance?.PlaySFX(AudioManager.SFX.TurnStart);

        // Survivor sira atlama UI'ini guncelle
        FindAnyObjectByType<SurvivorSkipTurnHandler>()?.UIElementleriniGuncelle();

        // Tur başlangıç banner'ını göster, sonra aksiyonu başlat
        StartCoroutine(TurnBannerVeBaslat());
    }

    /// <summary>
    /// Önce "Sıra Sende [Karakter]" banner'ını gösterir (2s),
    /// sonra oyuncu veya bot için seçim panelini açar.
    /// Botlar için panel otomatik karar verir ve vurgular.
    /// </summary>
    private IEnumerator TurnBannerVeBaslat()
    {
        int activeID = turnManager != null ? turnManager.GetActivePlayerID() : 0;
        string karakter = GetPlayerDisplayName(activeID);

        string bannerMetni;
        if (activeID == 0)
            bannerMetni = $"[!]  Sıra Sende\n{karakter}";
        else
            bannerMetni = $"Sıra:\n{karakter}";

        ShowTurnBanner(bannerMetni, 2.0f);

        yield return new WaitForSeconds(2.0f);

        if (turnManager != null && turnManager.IsGameOver())
            yield break;

        activeID = turnManager != null ? turnManager.GetActivePlayerID() : 0;

        if (activeID > 0)
        {
            // ─── BOT TURU ───
            BotAI botAI = FindAnyObjectByType<BotAI>();
            if (botAI == null) yield break;

            // Bot ne yapacağına karar ver
            bool willDrink = botAI.DecideBotAction(activeID);

            // SelectionPanel'i bot moduyla göster (2-3s düşünme + 2s vurgu)
            SelectionPanelController panel =
                selectionPanelController ?? FindSceneObjectOfType<SelectionPanelController>();

            if (panel != null)
            {
                bool panelBitti = false;
                float thinkingDelay = Random.Range(2.5f, 4.0f); // Bot "düşünme" süresi
                panel.ShowForBot(willDrink, thinkingDelay, () => panelBitti = true);
                yield return new WaitUntil(() => panelBitti);
            }

            if (turnManager != null && turnManager.IsGameOver()) yield break;

            // Aksiyonu çalıştır (animasyonlu)
            botAI.ExecuteBotDecision(activeID, willDrink);
        }
        else
        {
            // ─── OYUNCU TURU ───
            IsWaitingForActionSelection = true;

            SelectionPanelController panel =
                selectionPanelController ?? FindSceneObjectOfType<SelectionPanelController>();

            if (panel != null)
                panel.ShowPanel();
        }
    }

    /// <summary>
    /// Canvas üzerinde geçici bir "sıra banner'ı" oluşturur.
    /// </summary>
    public void ShowTurnBanner(string text, float duration)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject bannerGO = new GameObject("TurnBanner");
        bannerGO.transform.SetParent(canvas.transform, false);
        StartingBannerController banner = bannerGO.AddComponent<StartingBannerController>();
        banner.Initialize(text, duration, 0.72f, 0.88f);
    }

    /// <summary>
    /// Oyuncunun Türkçe görüntü ismini döndürür.
    /// </summary>
    private string GetPlayerDisplayName(int playerID)
    {
        if (turnManager == null) return "Oyuncu";

        Player p = turnManager.GetPlayer(playerID);
        string karakterIsmi = (p != null && KarakterIsimleri.TryGetValue(p.characterType, out string isim))
            ? isim
            : "Oyuncu";

        return karakterIsmi;
    }

    public void TuruSonlandir()
    {
        if (!turAktif) return;
        turAktif = false;

        if (turnManager != null && !turnManager.IsGameOver())
        {
            turnManager.EndTurn();
        }

        // Oyun bitmediyse tur sonlandirma sesi çal ve yeni turu baslat
        if (turnManager == null || !turnManager.IsGameOver())
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.TurnEnd);
            YeniTurBasladi();
        }
    }

    public void OnDrinkCupSelected()
    {
        IsWaitingForActionSelection = false;
    }

    public void OnDrawCardSelected()
    {
        IsWaitingForActionSelection = false;
        KartCekVeSiraSav();
    }

    #endregion

    #region Aksiyonlar

    /// <summary>
    /// Aktif oyuncu masadan bir bardak secer ve icer.
    /// </summary>
    public void BardakSecVeIc(int bardakIndeksi)
    {
        if (!turAktif)
        {
            Debug.LogWarning("[PlayerTurnController] Tur aktif degil.");
            return;
        }

        if (turnManager == null || turnManager.IsGameOver())
            return;

        Player aktifOyuncu = turnManager.GetActivePlayer();
        if (aktifOyuncu == null || !aktifOyuncu.IsAlive)
        {
            Debug.LogWarning("[PlayerTurnController] Aktif oyuncu hayatta degil.");
            return;
        }

        if (masaYonetici == null)
            return;

        if (masaYonetici.IsConsumed(bardakIndeksi))
        {
            Debug.LogWarning($"[PlayerTurnController] Bardak {bardakIndeksi} zaten icilmis.");
            return;
        }

        CupType icerik = masaYonetici.ConsumeCupForPlayer(bardakIndeksi, aktifOyuncu.playerID);

        // Trigger'ı bul (lazy: dictionary'de yoksa sağne'de ara)
        CupClickTrigger trigger = null;
        if (!cupTriggers.TryGetValue(bardakIndeksi, out trigger))
        {
            foreach (var t in FindObjectsByType<CupClickTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.cupIndex == bardakIndeksi)
                {
                    trigger = t;
                    cupTriggers[bardakIndeksi] = t;
                    break;
                }
            }
        }

        if (trigger != null)
        {
            IsWaitingForActionSelection = true;
            trigger.PlayDrinkAnimation(icerik, () =>
            {
                IsWaitingForActionSelection = false;
                CompleteDrinking(aktifOyuncu.playerID, icerik);
            });
        }
        else
        {
            CompleteDrinking(aktifOyuncu.playerID, icerik);
        }
    }

    private void CompleteDrinking(int playerID, CupType icerik)
    {
        StartCoroutine(CompleteDrinkingCoroutine(playerID, icerik));
    }

    private IEnumerator CompleteDrinkingCoroutine(int playerID, CupType icerik)
    {
        Player oyuncu = turnManager.GetPlayer(playerID);
        if (oyuncu == null || !oyuncu.IsAlive) yield break;

        turnManager.ResolveCupEffect(playerID, icerik);
        PlayDrinkSound(icerik);

        // Update UI panels immediately!
        if (uiHandler == null) uiHandler = FindAnyObjectByType<PlayerPoisonUIHandler>();
        if (uiHandler != null) uiHandler.UpdateAllPanels();

        if (!oyuncu.IsAlive)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
            acgozlulukCezasiAktif = false;
            acgozlulukKalanBardak = 0;
            // Wait 2 seconds so we see what residue remains
            yield return new WaitForSeconds(2.0f);
            TuruSonlandir();
            yield break;
        }

        if (acgozlulukCezasiAktif)
        {
            acgozlulukKalanBardak--;
            if (acgozlulukKalanBardak > 0)
            {
                // Wait 2 seconds so we see what residue remains
                yield return new WaitForSeconds(2.0f);
                ShowTurnBanner("1 Bardak Daha Seç!", 2.0f);
                IsWaitingForActionSelection = false; // Allow click
                yield break;
            }
            else
            {
                acgozlulukCezasiAktif = false;
                yield return new WaitForSeconds(2.0f);
                TuruSonlandir();
                yield break;
            }
        }

        // Wait 2 seconds so we see what residue remains
        yield return new WaitForSeconds(2.0f);
        TuruSonlandir();
    }

    /// <summary>
    /// Aktif oyuncu kart ceker. Eger kart secim gerektiriyorsa (Acgozluluk, Zoraki Ikram, Tarama)
    /// oyuncuyu "Secim Bekleme" moduna sokar. Diger kartlarda direkt uygular ve turu bitirir.
    /// </summary>
    public void KartCekVeSiraSav()
    {
        if (!turAktif || kartSecimiBekleniyor) return;
        if (turnManager == null || turnManager.IsGameOver()) return;

        Player aktifOyuncu = turnManager.GetActivePlayer();
        if (aktifOyuncu == null || !aktifOyuncu.IsAlive) return;

        CardType cekilenKart = cardManager.CekKart();

        // Kart Gosterim Paneli ile goster
        CardRevealPanelController reveal = cardRevealPanelController;
        if (reveal == null)
        {
            reveal = FindSceneObjectOfType<CardRevealPanelController>();
        }

        if (reveal != null)
        {
            // Kart paneli kapanınca efekti uygula
            reveal.ShowCard(cekilenKart, () =>
            {
                ApplyCardEffectAndFinish(cekilenKart, aktifOyuncu.playerID);
            });
        }
        else
        {
            ApplyCardEffectAndFinish(cekilenKart, aktifOyuncu.playerID);
        }
    }

    private void ApplyCardEffectAndFinish(CardType cekilenKart, int aktifOyuncuID)
    {
        // Açgözlülük Cezası:
        if (cekilenKart == CardType.AcgozlulukCezasi)
        {
            if (aktifOyuncuID == 0) // Human player
            {
                acgozlulukCezasiAktif = true;
                acgozlulukKalanBardak = 2;
                IsWaitingForActionSelection = false; // Allow cup clicking
                ShowTurnBanner("AÇGÖZLÜLÜK CEZASI!\nMasadan 2 bardak seçip iç!", 3.0f);
            }
            else // Bot player
            {
                int[] bardaklar = masaYonetici != null
                    ? masaYonetici.GetRandomDistinctUnconsumedCupIndices(2)
                    : new int[0];
                int b1 = bardaklar.Length > 0 ? bardaklar[0] : -1;
                int b2 = bardaklar.Length > 1 ? bardaklar[1] : -1;
                IsWaitingForActionSelection = true;
                StartCoroutine(AcgozlulukCezasiAnimasyonlu(aktifOyuncuID, b1, b2));
            }
            return;
        }

        // Zehir ve Panzehir Tarama (2x2 Alan Secimi)
        if (cekilenKart == CardType.PanzehirTarama || cekilenKart == CardType.ZehirTarama)
        {
            if (aktifOyuncuID == 0) // Human player
            {
                areaSelectionModeActive = true;
                areaSelectionCard = cekilenKart;
                IsWaitingForActionSelection = false; // Allow cup clicking
                string kartIsmi = (cekilenKart == CardType.PanzehirTarama) ? "PANZEHİR TARAMA" : "ZEHİR TARAMA";
                ShowTurnBanner($"{kartIsmi}!\nTarayacağın 2x2 alanı seçmek için bir bardağın üzerine gel ve tıkla!", 4.0f);
            }
            else // Bot player
            {
                BotAI botAI = FindAnyObjectByType<BotAI>();
                if (botAI != null)
                {
                    botAI.StartBotAreaSelection(aktifOyuncuID, cekilenKart);
                }
            }
            return;
        }

        // Hedef/Secim GEREKTIREN diger kartlar
        if (cekilenKart == CardType.ZorakiIkram)
        {
            if (secimGerektirenKartlariOtomatikCoz)
            {
                BekleyenKartIcinOtomatikSecimYap(cekilenKart, aktifOyuncuID);
            }
            else
            {
                kartSecimiBekleniyor = true;
                bekleyenKart = cekilenKart;
            }
        }
        else // Secim GEREKTIRMEYEN direkt kartlar (KritikDoz, Girdap, Nefeslenme)
        {
            cardManager.UygulaKartEtkisi(cekilenKart, aktifOyuncuID);
            TuruSonlandir();
        }
    }

    /// <summary>
    /// Açgözlük Cezası için animasyonlu 2 bardak içme.
    /// BotAI tarafindan da çağırılabilir.
    /// </summary>
    public void TetikleAcgozlulukCezasi(int oyuncuID, int bardak1, int bardak2)
    {
        IsWaitingForActionSelection = true;
        StartCoroutine(AcgozlulukCezasiAnimasyonlu(oyuncuID, bardak1, bardak2));
    }

    /// <summary>
    /// Açgözlük Cezası için animasyonlu 2 bardak içme.
    /// İki bardak sırayla animate edilir; arada 0.5s bekleme var.
    /// </summary>
    private IEnumerator AcgozlulukCezasiAnimasyonlu(int oyuncuID, int bardak1, int bardak2)
    {
        // --- Bardak 1 ---
        if (bardak1 >= 0 && masaYonetici != null && !masaYonetici.IsConsumed(bardak1))
        {
            CupType tip1 = masaYonetici.ConsumeCupForPlayer(bardak1, oyuncuID);

            if (cupTriggers.TryGetValue(bardak1, out CupClickTrigger trigger1))
            {
                bool done = false;
                trigger1.PlayDrinkAnimation(tip1, () => done = true);
                yield return new WaitUntil(() => done);
            }

            turnManager.ResolveCupEffect(oyuncuID, tip1);
            PlayDrinkSound(tip1);

            if (uiHandler != null) uiHandler.UpdateAllPanels();

            if (!turnManager.IsPlayerAlive(oyuncuID))
            {
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
                IsWaitingForActionSelection = false;
                yield return new WaitForSeconds(2.0f); // Wait 2s to see puddle/residue
                TuruSonlandir();
                yield break;
            }
        }

        // İki bardak arasında kısa nefes
        yield return new WaitForSeconds(0.5f);

        // --- Bardak 2 ---
        if (bardak2 >= 0 && masaYonetici != null && !masaYonetici.IsConsumed(bardak2))
        {
            CupType tip2 = masaYonetici.ConsumeCupForPlayer(bardak2, oyuncuID);

            if (cupTriggers.TryGetValue(bardak2, out CupClickTrigger trigger2))
            {
                bool done = false;
                trigger2.PlayDrinkAnimation(tip2, () => done = true);
                yield return new WaitUntil(() => done);
            }

            turnManager.ResolveCupEffect(oyuncuID, tip2);
            PlayDrinkSound(tip2);

            if (uiHandler != null) uiHandler.UpdateAllPanels();

            if (!turnManager.IsPlayerAlive(oyuncuID))
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
        }

        IsWaitingForActionSelection = false;
        yield return new WaitForSeconds(2.0f); // Wait 2s to see puddle/residue
        TuruSonlandir();
    }

    /// <summary>
    /// UI tarafindan cagirilir. Acgozluluk, Tarama veya Zoraki Ikram gibi kartlar icin
    /// oyuncu secimlerini yaptiginda bu metot tetiklenir ve bekleyen kart uygulanir.
    /// </summary>
    public void BekleyenKartIcinHedefSecildi(int secilenAlan = 0, int bardak1 = -1, int bardak2 = -1, int hedefOyuncu = -1)
    {
        if (!turAktif || !kartSecimiBekleniyor)
        {
            Debug.LogWarning("[PlayerTurnController] Bekleyen bir kart secimi yok veya tur aktif degil.");
            return;
        }

        Player aktifOyuncu = turnManager.GetActivePlayer();

        cardManager.UygulaKartEtkisi(bekleyenKart, aktifOyuncu.playerID, secilenAlan, bardak1, bardak2, hedefOyuncu);

        kartSecimiBekleniyor = false;
        bekleyenKart = default;
        TuruSonlandir();
    }

    public void IptalBekleyenKart()
    {
        if (!kartSecimiBekleniyor)
            return;

        Debug.LogWarning($"[PlayerTurnController] Bekleyen kart iptal edildi: {bekleyenKart}");
        kartSecimiBekleniyor = false;
        bekleyenKart = default;
        TuruSonlandir();
    }

    /// <summary>
    /// Survivor yetenegi: oyun boyunca 2 kez tur atlama hakki.
    /// </summary>
    public bool TurAtla()
    {
        if (!turAktif) return false;

        Player aktifOyuncu = turnManager.GetActivePlayer();
        if (aktifOyuncu == null || !aktifOyuncu.IsAlive)
            return false;

        if (aktifOyuncu.characterType == CharacterType.Survivor && aktifOyuncu.skipHakki > 0)
        {
            aktifOyuncu.skipHakki--;
            TuruSonlandir();
            return true;
        }

        Debug.LogWarning("[PlayerTurnController] Tur atlamak icin Survivor yetenegi veya skip hakki kalmamis.");
        return false;
    }

    /// <summary>
    /// Kimyager yetenegi: Oyun boyunca 1 kez, masadaki 1 zehir ve 1 panzehirin konumunu (indeksini) ogrenir.
    /// Turu sonlandirmaz (Free Action).
    /// </summary>
    public bool KimyagerYetenegiKullan(out int zehirIndeksi, out int panzehirIndeksi)
    {
        zehirIndeksi = -1;
        panzehirIndeksi = -1;

        if (!turAktif || turnManager == null) return false;

        Player aktifOyuncu = turnManager.GetActivePlayer();
        if (aktifOyuncu == null || !aktifOyuncu.IsAlive) return false;

        if (aktifOyuncu.characterType != CharacterType.Chemist)
        {
            Debug.LogWarning("[PlayerTurnController] Aktif oyuncu Kimyager degil.");
            return false;
        }

        if (aktifOyuncu.chemistAbilityUsed)
        {
            Debug.LogWarning("[PlayerTurnController] Kimyager yetenegi zaten kullanilmis.");
            return false;
        }

        if (masaYonetici == null) return false;

        zehirIndeksi = masaYonetici.FindRandomCupIndexOfType(CupType.POISON, unconsumedOnly: true);
        panzehirIndeksi = masaYonetici.FindRandomCupIndexOfType(CupType.ANTIDOTE, unconsumedOnly: true);

        aktifOyuncu.chemistAbilityUsed = true;
        return true;
    }

    /// <summary>
    /// Dedektif aktif yetenegi: Oyun boyunca 1 kez, istedigi bir bardagin icerigine gizlice bakar.
    /// Turu sonlandirmaz (Free Action).
    /// </summary>
    public bool DedektifYetenegiKullan(int bardakIndeksi, out CupType bardakTipi)
    {
        bardakTipi = CupType.EMPTY;

        if (!turAktif || turnManager == null) return false;

        Player aktifOyuncu = turnManager.GetActivePlayer();
        if (aktifOyuncu == null || !aktifOyuncu.IsAlive) return false;

        if (aktifOyuncu.characterType != CharacterType.Detective)
        {
            Debug.LogWarning("[PlayerTurnController] Aktif oyuncu Dedektif degil.");
            return false;
        }

        if (aktifOyuncu.detectiveAbilityUsed)
        {
            Debug.LogWarning("[PlayerTurnController] Dedektif yetenegi zaten kullanilmis.");
            return false;
        }

        if (masaYonetici == null) return false;

        bardakTipi = masaYonetici.GetCupType(bardakIndeksi);
        aktifOyuncu.detectiveAbilityUsed = true;
        return true;
    }

    #endregion

    #region Yardimci

    void ResolveReferences()
    {
        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();

        if (selectionPanelController == null)
            selectionPanelController = FindSceneObjectOfType<SelectionPanelController>();

        if (cardRevealPanelController == null)
            cardRevealPanelController = FindSceneObjectOfType<CardRevealPanelController>();

        cupTriggers.Clear();
        foreach (var trigger in FindObjectsByType<CupClickTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            cupTriggers[trigger.cupIndex] = trigger;
        }
    }

    private T FindSceneObjectOfType<T>() where T : MonoBehaviour
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in objects)
        {
            if (obj.gameObject.scene.name != null)
            {
                return obj;
            }
        }
        return null;
    }

    void BekleyenKartIcinOtomatikSecimYap(CardType kart, int aktifOyuncuID)
    {
        if (masaYonetici == null || cardManager == null)
        {
            Debug.LogWarning("[PlayerTurnController] Otomatik kart cozumu icin referanslar eksik.");
            TuruSonlandir();
            return;
        }

        int secilenAlan = masaYonetici.GetRandomValid2x2TopLeftIndex();
        int bardak1 = -1;
        int bardak2 = -1;
        int hedefOyuncu = -1;

        switch (kart)
        {
            case CardType.ZorakiIkram:
            {
                int[] bardaklar = masaYonetici.GetRandomDistinctUnconsumedCupIndices(1);
                if (bardaklar.Length > 0) bardak1 = bardaklar[0];
                hedefOyuncu = RastgeleHayattaOlanHedefSec(aktifOyuncuID);
                break;
            }
        }

        cardManager.UygulaKartEtkisi(kart, aktifOyuncuID, secilenAlan, bardak1, bardak2, hedefOyuncu);
        TuruSonlandir();
    }

    private void PlayDrinkSound(CupType tip)
    {
        if (tip == CupType.POISON)
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PoisonDrink);
        else if (tip == CupType.ANTIDOTE)
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.AntidoteDrink);
        else
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.CupDrink);
    }

    int RastgeleHayattaOlanHedefSec(int haricOyuncuID)
    {
        if (turnManager == null)
            return -1;

        var hedefler = new List<int>();

        for (int i = 0; i < turnManager.GetTotalPlayerCount(); i++)
        {
            if (i == haricOyuncuID)
                continue;

            if (turnManager.IsPlayerAlive(i))
                hedefler.Add(i);
        }

        if (hedefler.Count == 0)
            return -1;

        return hedefler[Random.Range(0, hedefler.Count)];
    }

    public CupClickTrigger GetCupTrigger(int index)
    {
        if (cupTriggers.TryGetValue(index, out var trigger))
            return trigger;
        return null;
    }

    public void OnAreaSelected(int topLeftIndex)
    {
        if (!areaSelectionModeActive) return;
        areaSelectionModeActive = false;

        StartCoroutine(ResolveAreaSelectionCoroutine(0, areaSelectionCard, topLeftIndex));
    }

    public IEnumerator ResolveAreaSelectionCoroutine(int playerID, CardType card, int topLeftIndex)
    {
        string cardName = card == CardType.PanzehirTarama ? "PANZEHİR" : "ZEHİR";
        CupType targetType = card == CardType.PanzehirTarama ? CupType.ANTIDOTE : CupType.POISON;

        int count = masaYonetici.Count2x2Area(topLeftIndex, targetType, unconsumedOnly: true);
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ScanReveal);

        // Convert index to row and column
        int row = (topLeftIndex / 6) + 1;
        int col = (topLeftIndex % 6) + 1;

        Player p = turnManager.GetPlayer(playerID);
        string name = p.characterType == CharacterType.Doctor ? "Doktor" : p.characterType == CharacterType.Survivor ? "Hayatta Kalan" : p.characterType == CharacterType.Chemist ? "Kimyager" : p.characterType == CharacterType.Detective ? "Dedektif" : "Oyuncu";

        // Highlight the scanned area during the result display
        Highlight2x2Area(topLeftIndex, true);

        // "Çıkan sonucu tüm oyuncular görebilmeli."
        string text = $"{cardName} TARAMA SONUCU\n{name} {row}. satır {col}. sütun etrafını taradı:\nSeçilen 2x2 alanda {count} tane {cardName.ToLower()} var!";
        ShowTurnBanner(text, 4.0f);

        yield return new WaitForSeconds(4.0f);

        Highlight2x2Area(topLeftIndex, false);

        TuruSonlandir();
    }

    private void OnMagnifyingGlassClicked()
    {
        Player activePlayer = turnManager.GetActivePlayer();
        if (activePlayer == null || activePlayer.playerID != 0) return;

        if (activePlayer.characterType == CharacterType.Chemist)
        {
            if (activePlayer.chemistAbilityUsed) return;
            
            int zehirIndeksi, panzehirIndeksi;
            if (KimyagerYetenegiKullan(out zehirIndeksi, out panzehirIndeksi))
            {
                var canvasGo = GameObject.Find("Canvas");
                var panel = canvasGo.transform.Find("Kimyager_Analiz_Paneli");
                if (panel != null)
                {
                    panel.gameObject.SetActive(true);
                    var zText = panel.Find("Zehir_Yeri")?.GetComponent<TMPro.TextMeshProUGUI>();
                    var pText = panel.Find("Panzehir_Yeri")?.GetComponent<TMPro.TextMeshProUGUI>();

                    int zRow = (zehirIndeksi / 6) + 1;
                    int zCol = (zehirIndeksi % 6) + 1;
                    int pRow = (panzehirIndeksi / 6) + 1;
                    int pCol = (panzehirIndeksi % 6) + 1;

                    if (zText != null)
                        zText.text = zehirIndeksi >= 0 ? $"Zehir Yeri: {zRow}. satır {zCol}. sütun" : "Zehir Yeri: Yok";
                    if (pText != null)
                        pText.text = panzehirIndeksi >= 0 ? $"Panzehir Yeri: {pRow}. satır {pCol}. sütun" : "Panzehir Yeri: Yok";
                }

                chemistHighlightedCups.Clear();
                if (zehirIndeksi >= 0)
                {
                    var trigger = GetCupTrigger(zehirIndeksi);
                    if (trigger != null)
                    {
                        trigger.HighlightVisual(true);
                        trigger.SetHighlightedState(true);
                        var sr = trigger.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = new Color(0.2f, 1f, 0.2f, 1f); // Parlayan yeşil (Zehir)
                        chemistHighlightedCups.Add(zehirIndeksi);
                    }
                }
                if (panzehirIndeksi >= 0)
                {
                    var trigger = GetCupTrigger(panzehirIndeksi);
                    if (trigger != null)
                    {
                        trigger.HighlightVisual(true);
                        trigger.SetHighlightedState(true);
                        var sr = trigger.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f); // Parlayan beyaz (Panzehir)
                        chemistHighlightedCups.Add(panzehirIndeksi);
                    }
                }

                ShowTurnBanner("Kimyager Özelliğini Kullandı!", 3.0f);
                uiHandler?.UpdateAllPanels();
            }
        }
        else if (activePlayer.characterType == CharacterType.Detective)
        {
            if (activePlayer.detectiveAbilityUsed) return;

            detectiveInspectionModeActive = true;
            ShowTurnBanner("Dedektif Büyütecini Kullandı!\nİncelemek istediğin 1 bardağa tıkla!", 3.0f);
            uiHandler?.UpdateAllPanels();
        }
    }

    public void OnDetectiveCupInspected(int cupIndex)
    {
        if (!detectiveInspectionModeActive) return;
        detectiveInspectionModeActive = false;

        CupType content;
        if (DedektifYetenegiKullan(cupIndex, out content))
        {
            // Highlight the inspected cup until the detective ends their turn
            var trigger = GetCupTrigger(cupIndex);
            if (trigger != null)
            {
                trigger.HighlightVisual(true);
                trigger.SetHighlightedState(true);
                var sr = trigger.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(1f, 0.85f, 0.1f, 1f); // Glowing gold/yellow for Detective
                }
                detectiveHighlightedCups.Add(cupIndex);
            }

            int row = (cupIndex / 6) + 1;
            int col = (cupIndex % 6) + 1;

            var canvasGo = GameObject.Find("Canvas");
            var panel = canvasGo.transform.Find("Dedektif_Sonuc_Paneli");
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                var text = panel.Find("Sonuc_Yazisi")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    string contentStr = content == CupType.POISON ? "<color=red>☠ ZEHİR</color>" : content == CupType.ANTIDOTE ? "<color=green>✓ Panzehir</color>" : "○ Boş";
                    text.text = $"{row}. satır {col}. sütundaki bardak içeriği:\n{contentStr}";
                }
            }

            ShowTurnBanner($"Dedektif {row}. satır {col}. sütundaki bardağı inceledi!", 3.0f);
            uiHandler?.UpdateAllPanels();
        }
    }

    public void Highlight2x2Area(int topLeftIndex, bool highlight)
    {
        if (masaYonetici == null) return;
        int[] indices = masaYonetici.Get2x2Indices(topLeftIndex);
        foreach (int idx in indices)
        {
            var trigger = GetCupTrigger(idx);
            if (trigger != null)
            {
                trigger.HighlightVisual(highlight);
            }
        }
    }

    private void ResetChemistHighlights()
    {
        foreach (int idx in chemistHighlightedCups)
        {
            var trigger = GetCupTrigger(idx);
            if (trigger != null)
            {
                trigger.ResetVisualState();
            }
        }
        chemistHighlightedCups.Clear();
    }

    private void ResetDetectiveHighlights()
    {
        foreach (int idx in detectiveHighlightedCups)
        {
            var trigger = GetCupTrigger(idx);
            if (trigger != null)
            {
                trigger.ResetVisualState();
            }
        }
        detectiveHighlightedCups.Clear();
    }

    #endregion
}
