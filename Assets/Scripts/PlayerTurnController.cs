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

    public bool TurAktif => turAktif;
    public bool IsWaitingForActionSelection { get; private set; } = false;
    public CardRevealPanelController CardRevealPanel => cardRevealPanelController;

    private Dictionary<int, CupClickTrigger> cupTriggers = new Dictionary<int, CupClickTrigger>();

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
    }

    #region Tur Kontrolu

    public void YeniTurBasladi()
    {
        if (turnManager != null && turnManager.IsGameOver())
        {
            turAktif = false;
            return;
        }

        turAktif = true;
        kartSecimiBekleniyor = false;

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
            bannerMetni = $"⚔  Sıra Sende\n{karakter}";
        else
            bannerMetni = $"🤖  Sıra:\n{karakter}";

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
    private void ShowTurnBanner(string text, float duration)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject bannerGO = new GameObject("TurnBanner");
        bannerGO.transform.SetParent(canvas.transform, false);
        StartingBannerController banner = bannerGO.AddComponent<StartingBannerController>();
        banner.Initialize(text, duration);
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

        if (playerID == 0)
            return karakterIsmi;

        // Botlar için ek etiket
        return $"Bot {playerID} ({karakterIsmi})";
    }

    public void TuruSonlandir()
    {
        if (!turAktif) return;
        turAktif = false;

        // Ölü oyuncunun turu "EndTurn" ile sonlandirilmamali.
        if (turnManager != null && !turnManager.IsGameOver())
        {
            Player aktif = turnManager.GetActivePlayer();
            if (aktif != null && aktif.IsAlive)
            {
                turnManager.EndTurn();
            }
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
        Player oyuncu = turnManager.GetPlayer(playerID);
        if (oyuncu == null || !oyuncu.IsAlive) return;

        turnManager.ResolveCupEffect(playerID, icerik);
        PlayDrinkSound(icerik);

        if (!oyuncu.IsAlive)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
            TuruSonlandir();
            return;
        }

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
        // Açgözlülük Cezası: animasyonlu 2 bardak içme (coroutine ile)
        if (cekilenKart == CardType.AcgozlulukCezasi)
        {
            int[] bardaklar = masaYonetici != null
                ? masaYonetici.GetRandomDistinctUnconsumedCupIndices(2)
                : new int[0];
            int b1 = bardaklar.Length > 0 ? bardaklar[0] : -1;
            int b2 = bardaklar.Length > 1 ? bardaklar[1] : -1;
            IsWaitingForActionSelection = true;
            StartCoroutine(AcgozlulukCezasiAnimasyonlu(aktifOyuncuID, b1, b2));
            return;
        }

        // Hedef/Secim GEREKTIREN diger kartlar
        if (cekilenKart == CardType.ZehirTarama ||
            cekilenKart == CardType.PanzehirTarama ||
            cekilenKart == CardType.ZorakiIkram)
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

            if (!turnManager.IsPlayerAlive(oyuncuID))
            {
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
                IsWaitingForActionSelection = false;
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

            if (!turnManager.IsPlayerAlive(oyuncuID))
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
        }

        IsWaitingForActionSelection = false;
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

    #endregion
}
