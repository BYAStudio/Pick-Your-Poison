using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faz 2: Kart cekme, RNG olasilik tablosu ve kart efektlerini yonetir.
/// </summary>
public class CardManager : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] TurnManager turnManager;

    // RNG araliklari (toplam %100)
    const int AcgozlulukMin = 0;
    const int AcgozlulukMax = 30;       // %30
    const int KritikDozMin = 30;
    const int KritikDozMax = 50;        // %20
    const int ZehirTaramaMin = 50;
    const int ZehirTaramaMax = 62;      // %12
    const int PanzehirTaramaMin = 62;
    const int PanzehirTaramaMax = 74;   // %12
    const int GirdapMin = 74;
    const int GirdapMax = 84;           // %10
    const int NefeslenmeMin = 84;
    const int NefeslenmeMax = 92;       // %8
    const int ZorakiIkramMin = 92;
    const int ZorakiIkramMax = 100;     // %8

    public CardType SonCekilenKart { get; private set; }
    public int SonRNGDegeri { get; private set; }

    void Awake()
    {
        ResolveReferences();
    }

    #region Kart Cekme

    public CardType CekKart()
    {
        int rng = Random.Range(0, 100);
        SonRNGDegeri = rng;
        SonCekilenKart = RNGToCardType(rng);

        AudioManager.Instance?.PlaySFX(AudioManager.SFX.CardDraw);

        return SonCekilenKart;
    }

    CardType RNGToCardType(int rng)
    {
        if (rng >= AcgozlulukMin && rng < AcgozlulukMax) return CardType.AcgozlulukCezasi;
        if (rng >= KritikDozMin && rng < KritikDozMax) return CardType.KritikDoz;
        if (rng >= ZehirTaramaMin && rng < ZehirTaramaMax) return CardType.ZehirTarama;
        if (rng >= PanzehirTaramaMin && rng < PanzehirTaramaMax) return CardType.PanzehirTarama;
        if (rng >= GirdapMin && rng < GirdapMax) return CardType.Girdap;
        if (rng >= NefeslenmeMin && rng < NefeslenmeMax) return CardType.Nefeslenme;
        return CardType.ZorakiIkram;
    }

    #endregion

    #region Kart Efektleri

    public void UygulaKartEtkisi(CardType kart, int aktifOyuncuID, int secilenAlanIndeksi = 0, int secilenBardak1 = -1, int secilenBardak2 = -1, int hedefOyuncuID = -1)
    {
        switch (kart)
        {
            case CardType.AcgozlulukCezasi:
                EtkiAcgozlulukCezasi(aktifOyuncuID, secilenBardak1, secilenBardak2);
                break;
            case CardType.KritikDoz:
                EtkiKritikDoz(aktifOyuncuID);
                break;
            case CardType.ZehirTarama:
                EtkiZehirTarama(secilenAlanIndeksi);
                break;
            case CardType.PanzehirTarama:
                EtkiPanzehirTarama(aktifOyuncuID, secilenAlanIndeksi);
                break;
            case CardType.Girdap:
                EtkiGirdap();
                break;
            case CardType.Nefeslenme:
                EtkiNefeslenme();
                break;
            case CardType.ZorakiIkram:
                EtkiZorakiIkram(aktifOyuncuID, hedefOyuncuID, secilenBardak1);
                break;
        }
    }

    void EtkiAcgozlulukCezasi(int aktifOyuncuID, int bardak1, int bardak2)
    {
        int[] secilenler = { bardak1, bardak2 };
        var icilenler = new HashSet<int>();

        for (int i = 0; i < 2; i++)
        {
            int bardak = secilenler[i];

            if (bardak < 0 || masaYonetici.IsConsumed(bardak) || icilenler.Contains(bardak))
            {
                int yedekBardak = masaYonetici.GetRandomUnconsumedCupIndexExcluding(icilenler);
                if (yedekBardak < 0)
                {
                    Debug.LogWarning("[CardManager] Acgozluluk Cezasi icin ikinci farkli bardak bulunamadi.");
                    continue;
                }

                Debug.LogWarning($"[CardManager] Gecersiz bardak secimi ({bardak}) yerine yedek bardak {yedekBardak} secildi.");
                bardak = yedekBardak;
            }

            CupType tip = masaYonetici.ConsumeCupForPlayer(bardak, aktifOyuncuID);
            icilenler.Add(bardak);
            turnManager.ResolveCupEffect(aktifOyuncuID, tip);

            // Bardak icerken ses
            if (tip == CupType.POISON)
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.PoisonDrink);
            else if (tip == CupType.ANTIDOTE)
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.AntidoteDrink);
            else
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.CupDrink);

            // Eger ilk bardakta zehirlenip olmusse, ikinci bardagi icmesine gerek yok!
            // Olum kaydi ResolveCupEffect icinde yapilir
            if (!turnManager.IsPlayerAlive(aktifOyuncuID))
            {
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
                break;
            }
        }
    }

    void EtkiKritikDoz(int aktifOyuncuID)
    {
        turnManager.ApplyPoisonToPlayer(aktifOyuncuID);
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.PoisonDrink);

        // Oyuncu olduyse olum sesi
        if (!turnManager.IsPlayerAlive(aktifOyuncuID))
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
    }

    void EtkiZehirTarama(int topLeftIndex)
    {
        if (!masaYonetici.IsValid2x2TopLeft(topLeftIndex))
        {
            Debug.LogWarning($"[CardManager] Gecersiz 2x2 alan baslangici: {topLeftIndex}. Guvenlik icin 0 atandi.");
            topLeftIndex = 0;
        }

        masaYonetici.Count2x2Area(topLeftIndex, CupType.POISON, unconsumedOnly: true);
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ScanReveal);
        // Sonuc UI'a aktarilir (tum oyunculara gosterilir)
    }

    void EtkiPanzehirTarama(int aktifOyuncuID, int topLeftIndex)
    {
        if (!masaYonetici.IsValid2x2TopLeft(topLeftIndex))
        {
            Debug.LogWarning($"[CardManager] Gecersiz 2x2 alan baslangici: {topLeftIndex}. Guvenlik icin 0 atandi.");
            topLeftIndex = 0;
        }

        masaYonetici.Count2x2Area(topLeftIndex, CupType.ANTIDOTE, unconsumedOnly: true);
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.ScanReveal);
        // Sonuc sadece aktif oyuncuya gosterilir
    }

    void EtkiGirdap()
    {
        turnManager.ReverseTurnDirection();
        AudioManager.Instance?.PlaySFX(AudioManager.SFX.DirectionReverse);
    }

    void EtkiNefeslenme()
    {
        // Tur guvenle savulur, ekstra islem gerekmez
    }

    void EtkiZorakiIkram(int aktifOyuncuID, int hedefOyuncuID, int secilenBardak)
    {
        if (hedefOyuncuID < 0 || secilenBardak < 0)
        {
            Debug.LogWarning("[CardManager] Zoraki Ikram iptal: Hedef oyuncu veya bardak secilmedi.");
            return;
        }

        if (!turnManager.IsPlayerAlive(hedefOyuncuID))
        {
            Debug.LogWarning($"[CardManager] Zoraki Ikram iptal: Hedef oyuncu ({hedefOyuncuID}) zaten olu.");
            return;
        }

        if (masaYonetici.IsConsumed(secilenBardak))
        {
            Debug.LogWarning($"[CardManager] Zoraki Ikram iptal: Bardak ({secilenBardak}) zaten icilmis.");
            return;
        }

        CupType tip = masaYonetici.ConsumeCupForPlayer(secilenBardak, hedefOyuncuID);
        turnManager.ResolveCupEffect(hedefOyuncuID, tip);

        // Bardak icerken ses
        if (tip == CupType.POISON)
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PoisonDrink);
        else if (tip == CupType.ANTIDOTE)
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.AntidoteDrink);
        else
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.CupDrink);

        // Olum sesi
        if (!turnManager.IsPlayerAlive(hedefOyuncuID))
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
    }

    #endregion

    #region Yardimci

    void ResolveReferences()
    {
        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();
    }

    #endregion
}
