using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bot yapay zekası.
/// Tüm aksiyon animasyonlarını tam olarak oynatır; herkes ne yaptığını görür.
/// </summary>
public class BotAI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] TurnManager turnManager;
    [SerializeField] PlayerTurnController playerTurnController;
    [SerializeField] CardManager cardManager;

    void Awake()
    {
        ResolveReferences();
    }

    // ─────────────────────────────────────────
    //  Ana giriş noktaları
    // ─────────────────────────────────────────

    /// <summary>
    /// Botun tur kararını (bardak mı / kart mı) döndürür. Coroutine değil — anında çalışır.
    /// </summary>
    public bool DecideBotAction(int botPlayerID)
    {
        ResolveReferences();
        Player bot = turnManager?.GetPlayer(botPlayerID);
        if (bot == null) return false;
        return ShouldDrinkCup(bot);
    }

    /// <summary>
    /// SelectionPanel kapandıktan sonra çağrılır. Aksiyonu animasyonlu uygular.
    /// </summary>
    public void ExecuteBotDecision(int botPlayerID, bool willDrink)
    {
        StartCoroutine(ExecuteBotDecisionCoroutine(botPlayerID, willDrink));
    }

    private IEnumerator ExecuteBotDecisionCoroutine(int botPlayerID, bool willDrink)
    {
        ResolveReferences();

        if (!playerTurnController.TurAktif) yield break;

        Player bot = turnManager?.GetPlayer(botPlayerID);
        if (bot == null || !bot.IsAlive) yield break;

        if (willDrink)
        {
            int bardak = masaYonetici != null ? masaYonetici.GetRandomUnconsumedCupIndex() : -1;

            if (bardak >= 0)
            {
                // Tam animasyonlu bardak içme (CupClickTrigger + TuruSonlandir callback ile)
                playerTurnController.BardakSecVeIc(bardak);
            }
            else
            {
                // Bardak kalmadıysa kart çek
                yield return StartCoroutine(BotKartCekVeGoster(botPlayerID));
            }
        }
        else
        {
            yield return StartCoroutine(BotKartCekVeGoster(botPlayerID));
        }
    }

    // ─────────────────────────────────────────
    //  Kart çekme (herkese göster)
    // ─────────────────────────────────────────

    /// <summary>
    /// Bot kart çeker, CardRevealPanel ile herkese gösterir, kapanınca efekti uygular.
    /// </summary>
    private IEnumerator BotKartCekVeGoster(int botPlayerID)
    {
        if (cardManager == null || !playerTurnController.TurAktif)
            yield break;

        CardType cekilenKart = cardManager.CekKart();

        // --- Kartı herkese göster ---
        CardRevealPanelController reveal = FindAnyObjectByType<CardRevealPanelController>();
        if (reveal != null)
        {
            bool revealBitti = false;
            reveal.ShowCard(cekilenKart, () => revealBitti = true);
            yield return new WaitUntil(() => revealBitti);
        }

        if (!playerTurnController.TurAktif) yield break;

        // --- Açgözlülük Cezası: animasyonlu 2 bardak ---
        if (cekilenKart == CardType.AcgozlulukCezasi)
        {
            int[] bardaklar = masaYonetici != null
                ? masaYonetici.GetRandomDistinctUnconsumedCupIndices(2)
                : new int[0];
            int b1 = bardaklar.Length > 0 ? bardaklar[0] : -1;
            int b2 = bardaklar.Length > 1 ? bardaklar[1] : -1;
            playerTurnController.TetikleAcgozlulukCezasi(botPlayerID, b1, b2);
            yield break; // TuruSonlandir coroutine içinde çağrılır
        }

        // --- Diğer kartlar ---
        if (masaYonetici == null)
        {
            playerTurnController.TuruSonlandir();
            yield break;
        }

        int secilenAlanIndeksi = masaYonetici.GetRandomValid2x2TopLeftIndex();
        int[] farkliBardaklar  = masaYonetici.GetRandomDistinctUnconsumedCupIndices(2);
        int secilenBardak1     = farkliBardaklar.Length > 0 ? farkliBardaklar[0] : -1;
        int secilenBardak2     = farkliBardaklar.Length > 1 ? farkliBardaklar[1] : -1;
        int hedefOyuncuID      = RastgeleHedefOyuncu(botPlayerID);

        cardManager.UygulaKartEtkisi(
            cekilenKart,
            botPlayerID,
            secilenAlanIndeksi,
            secilenBardak1,
            secilenBardak2,
            hedefOyuncuID
        );

        playerTurnController.TuruSonlandir();
    }

    // ─────────────────────────────────────────
    //  Karar verici
    // ─────────────────────────────────────────

    bool ShouldDrinkCup(Player bot)
    {
        if (bot.currentState == PlayerState.Poisoned)
            return Random.Range(0, 100) < 90; // Zehirliyse %90 bardak iç

        return Random.Range(0, 100) < 40; // Sağlıklı: %40 bardak, %60 kart
    }

    // ─────────────────────────────────────────
    //  Yardımcı
    // ─────────────────────────────────────────

    int RastgeleHedefOyuncu(int aktifOyuncuID)
    {
        if (turnManager == null) return -1;

        var hedefler = new List<int>();
        int toplam   = turnManager.GetTotalPlayerCount();

        for (int i = 0; i < toplam; i++)
        {
            if (i != aktifOyuncuID && turnManager.IsPlayerAlive(i))
                hedefler.Add(i);
        }

        return hedefler.Count == 0 ? -1 : hedefler[Random.Range(0, hedefler.Count)];
    }

    void ResolveReferences()
    {
        if (masaYonetici        == null) masaYonetici        = FindAnyObjectByType<MasaYonetici>();
        if (turnManager         == null) turnManager         = FindAnyObjectByType<TurnManager>();
        if (playerTurnController == null) playerTurnController = FindAnyObjectByType<PlayerTurnController>();
        if (cardManager         == null) cardManager         = FindAnyObjectByType<CardManager>();
    }
}
