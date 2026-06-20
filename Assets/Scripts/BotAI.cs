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

    private string GetKarakterName(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Doctor: return "Doktor";
            case CharacterType.Survivor: return "Hayatta Kalan";
            case CharacterType.Chemist: return "Kimyager";
            case CharacterType.Detective: return "Dedektif";
            default: return "Bot";
        }
    }

    public void StartBotAreaSelection(int botPlayerID, CardType card)
    {
        StartCoroutine(BotAreaSelectionCoroutine(botPlayerID, card));
    }

    private IEnumerator BotAreaSelectionCoroutine(int botPlayerID, CardType card)
    {
        ResolveReferences();
        Player bot = turnManager?.GetPlayer(botPlayerID);
        if (bot == null || !bot.IsAlive) yield break;

        string charName = GetKarakterName(bot.characterType);
        playerTurnController.ShowTurnBanner($"{charName} düşünüyor...", 3.5f);

        // Simulating the bot hovering over 2-3 random 2x2 areas
        int hoverCount = Random.Range(2, 4);
        for (int h = 0; h < hoverCount; h++)
        {
            yield return new WaitForSeconds(0.5f);
            int randomArea = masaYonetici != null ? masaYonetici.GetRandomValid2x2TopLeftIndex() : 0;
            if (playerTurnController != null)
                playerTurnController.Highlight2x2Area(randomArea, true);
            yield return new WaitForSeconds(0.6f);
            if (playerTurnController != null)
                playerTurnController.Highlight2x2Area(randomArea, false);
        }

        yield return new WaitForSeconds(0.3f);

        int secilenAlan = masaYonetici != null ? masaYonetici.GetRandomValid2x2TopLeftIndex() : 0;
        
        // Highlight the final choice
        if (playerTurnController != null)
            playerTurnController.Highlight2x2Area(secilenAlan, true);
        yield return new WaitForSeconds(0.8f);
        if (playerTurnController != null)
            playerTurnController.Highlight2x2Area(secilenAlan, false);

        yield return StartCoroutine(playerTurnController.ResolveAreaSelectionCoroutine(botPlayerID, card, secilenAlan));
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

        // Bot Ability usage
        if (bot.characterType == CharacterType.Chemist && !bot.chemistAbilityUsed)
        {
            if (Random.Range(0, 100) < 30) // 30% chance
            {
                bot.chemistAbilityUsed = true;
                playerTurnController.ShowTurnBanner("Kimyager özel yeteneğini kullandı!", 2.5f);
                yield return new WaitForSeconds(2.5f);
            }
        }
        else if (bot.characterType == CharacterType.Detective && !bot.detectiveAbilityUsed)
        {
            if (Random.Range(0, 100) < 30) // 30% chance
            {
                int cupToInspect = masaYonetici != null ? masaYonetici.GetRandomUnconsumedCupIndex() : -1;
                if (cupToInspect >= 0)
                {
                    bot.detectiveAbilityUsed = true;
                    int row = (cupToInspect / 6) + 1;
                    int col = (cupToInspect % 6) + 1;
                    playerTurnController.ShowTurnBanner($"Dedektif {row}. satır {col}. sütundaki bardağı inceledi!", 3.0f);
                    yield return new WaitForSeconds(3.0f);
                }
            }
        }

        if (willDrink)
        {
            int bardak = masaYonetici != null ? masaYonetici.GetRandomUnconsumedCupIndex() : -1;

            if (bardak >= 0)
            {
                string charName = GetKarakterName(bot.characterType);
                playerTurnController.ShowTurnBanner($"{charName} bardak seçiyor...", 3.5f);

                // Simulate bot "hovering" over 2-3 random cups
                int hoverCount = Random.Range(2, 4);
                for (int h = 0; h < hoverCount; h++)
                {
                    int randomCup = masaYonetici.GetRandomUnconsumedCupIndex();
                    if (randomCup >= 0 && randomCup != bardak)
                    {
                        var trigger = playerTurnController.GetCupTrigger(randomCup);
                        if (trigger != null)
                        {
                            trigger.HighlightVisual(true);
                            yield return new WaitForSeconds(0.6f);
                            trigger.HighlightVisual(false);
                            yield return new WaitForSeconds(0.2f);
                        }
                    }
                }

                // Highlight the final chosen cup for 0.5s before drinking
                var finalTrigger = playerTurnController.GetCupTrigger(bardak);
                if (finalTrigger != null)
                {
                    finalTrigger.HighlightVisual(true);
                    yield return new WaitForSeconds(0.5f);
                    finalTrigger.HighlightVisual(false);
                }

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
        CardRevealPanelController reveal = playerTurnController != null ? playerTurnController.CardRevealPanel : null;
        if (reveal == null)
        {
            reveal = FindAnyObjectByType<CardRevealPanelController>(FindObjectsInactive.Include);
        }

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

        // --- Zehir ve Panzehir Tarama ---
        if (cekilenKart == CardType.PanzehirTarama || cekilenKart == CardType.ZehirTarama)
        {
            yield return StartCoroutine(BotAreaSelectionCoroutine(botPlayerID, cekilenKart));
            yield break;
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

        FindAnyObjectByType<PlayerPoisonUIHandler>()?.UpdateAllPanels();

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
