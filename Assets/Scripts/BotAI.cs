using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faz 2: Bot yapay zekasi. Saglikliyse kart cek veya bardak ic.
/// Zehirliyse %90 ihtimalle bardak secmelidir.
/// Bot kart cektiginde UI beklemeden rastgele hedefler/bardaklar secerek otomatik resolve eder.
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

    /// <summary>
    /// Belirtilen bot oyuncusu icin tur aksiyonunu calistirir.
    /// </summary>
    public void ExecuteBotTurn(int botPlayerID)
    {
        if (turnManager == null || playerTurnController == null)
            return;

        if (!playerTurnController.TurAktif)
            return;

        Player bot = turnManager.GetPlayer(botPlayerID);
        if (bot == null || !bot.IsAlive)
            return;

        bool icmeli = ShouldDrinkCup(bot);

        if (icmeli)
        {
            int bardak = masaYonetici.GetRandomUnconsumedCupIndex();
            if (bardak >= 0)
            {
                Debug.Log($"[BotAI] Oyuncu {botPlayerID} (Bot) bardak icmeye karar verdi -> Bardak {bardak}.");
                playerTurnController.BardakSecVeIc(bardak);
            }
            else
            {
                Debug.Log($"[BotAI] Oyuncu {botPlayerID} (Bot) bardak bulamadi, kart cekiyor.");
                BotKartCekVeResolve(botPlayerID);
            }
        }
        else
        {
            Debug.Log($"[BotAI] Oyuncu {botPlayerID} (Bot) kart cekmeye karar verdi.");
            BotKartCekVeResolve(botPlayerID);
        }
    }

    /// <summary>
    /// Bot kart ceker ve UI beklemeden rastgele secimlerle efektini otomatik cozer.
    /// Acgozluluk Cezasi: 2 rastgele bardak, Zoraki Ikram: rastgele hedef + bardak,
    /// Tarama kartlari: rastgele 2x2 alan.
    /// </summary>
    void BotKartCekVeResolve(int botPlayerID)
    {
        if (cardManager == null)
        {
            Debug.LogWarning("[BotAI] CardManager referansi yok, kart cekilemiyor.");
            return;
        }

        if (!playerTurnController.TurAktif)
            return;

        CardType cekilenKart = cardManager.CekKart();
        Debug.Log($"[BotAI] Oyuncu {botPlayerID} (Bot) kart cekti: {cekilenKart} (RNG: {cardManager.SonRNGDegeri})");

        // Kart icin gerekli parametreleri otomatik olustur
        int secilenAlanIndeksi = RastgeleGecerli2x2TopLeft();
        int[] farkliBardaklar = masaYonetici.GetRandomDistinctUnconsumedCupIndices(2);
        int secilenBardak1 = farkliBardaklar.Length > 0 ? farkliBardaklar[0] : -1;
        int secilenBardak2 = farkliBardaklar.Length > 1 ? farkliBardaklar[1] : -1;
        int hedefOyuncuID = RastgeleHedefOyuncu(botPlayerID);

        cardManager.UygulaKartEtkisi(
            cekilenKart,
            botPlayerID,
            secilenAlanIndeksi,
            secilenBardak1,
            secilenBardak2,
            hedefOyuncuID
        );

        // Oyuncu olduyse kaydet
        Player bot = turnManager.GetPlayer(botPlayerID);
        if (bot != null && !bot.IsAlive)
        {
            turnManager.RegisterPlayerDeath(botPlayerID);
        }

        // Turu sonlandir
        playerTurnController.TuruSonlandir();
    }

    /// <summary>
    /// Botun bardak icmesi mi yoksa kart cekmesi mi gerektigine karar verir.
    /// </summary>
    bool ShouldDrinkCup(Player bot)
    {
        if (bot.currentState == PlayerState.Poisoned)
        {
            // Zehirliyse %90 bardak ic, %10 kart cek
            return Random.Range(0, 100) < 90;
        }

        // Saglikli: %40 bardak ic, %60 kart cek
        return Random.Range(0, 100) < 40;
    }

    /// <summary>
    /// Gecerli bir 2x2 top-left indeksi dondurur (bot tarama kartlari icin).
    /// </summary>
    int RastgeleGecerli2x2TopLeft()
    {
        if (masaYonetici == null)
            return 0;

        return masaYonetici.GetRandomValid2x2TopLeftIndex();
    }

    /// <summary>
    /// Aktif oyuncu disinda rastgele bir hayatta olan hedef oyuncu sec (Zoraki Ikram icin).
    /// </summary>
    int RastgeleHedefOyuncu(int aktifOyuncuID)
    {
        if (turnManager == null) return -1;

        var hedefler = new List<int>();
        int toplam = turnManager.GetTotalPlayerCount();

        for (int i = 0; i < toplam; i++)
        {
            if (i != aktifOyuncuID && turnManager.IsPlayerAlive(i))
                hedefler.Add(i);
        }

        if (hedefler.Count == 0)
            return -1;

        return hedefler[Random.Range(0, hedefler.Count)];
    }

    void ResolveReferences()
    {
        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (playerTurnController == null)
            playerTurnController = FindAnyObjectByType<PlayerTurnController>();

        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();
    }
}
