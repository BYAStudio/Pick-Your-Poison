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
    const int AcgozlulukMax = 35;       // %35
    const int KritikDozMin = 35;
    const int KritikDozMax = 60;        // %25
    const int ZehirTaramaMin = 60;
    const int ZehirTaramaMax = 70;      // %10
    const int PanzehirTaramaMin = 70;
    const int PanzehirTaramaMax = 80;   // %10
    const int GirdapMin = 80;
    const int GirdapMax = 90;           // %10
    const int NefeslenmeMin = 90;
    const int NefeslenmeMax = 95;       // %5
    const int ZorakiIkramMin = 95;
    const int ZorakiIkramMax = 100;     // %5

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
                // Artik rastgele degil, disaridan gelen 2 bardak indeksini gonderiyoruz
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
                // Gelen hedef oyuncu ID'sini ve secilen ilk bardagi (secilenBardak1) kullaniyoruz
                EtkiZorakiIkram(aktifOyuncuID, hedefOyuncuID, secilenBardak1); 
                break;
        }
    }

    void EtkiAcgozlulukCezasi(int aktifOyuncuID, int bardak1, int bardak2)
    {
        Debug.Log($"[CardManager] Acgozluluk Cezasi: Oyuncu {aktifOyuncuID} kendi sectigi bardaklari ({bardak1}, {bardak2}) iciyor.");

        // Secilen bardaklari bir diziye alalim ki kolayca donelim
        int[] secilenler = { bardak1, bardak2 };
        var icilenler = new HashSet<int>();

        for (int i = 0; i < 2; i++)
        {
            int bardak = secilenler[i];

            // Gecersiz, icilmis veya tekrar secilen bardak geldiyse kalan bardaklardan yeni bir tane bul.
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

            Debug.Log($"[CardManager]  ({i + 1}/2) Bardak {bardak} icildi: {tip}");

            // Eger ilk bardakta zehirlenip olmusse, ikinci bardagi icmesine gerek yok!
            if (!turnManager.IsPlayerAlive(aktifOyuncuID))
            {
                Debug.Log($"[CardManager] Oyuncu ilk bardakta ({bardak}) oldugu icin ikinci bardagi icemiyor.");
                break;
            }
        }
    }

    void EtkiKritikDoz(int aktifOyuncuID)
    {
        Debug.Log($"[CardManager] Kritik Doz: Oyuncu {aktifOyuncuID} direkt zehirlendi.");
        turnManager.ApplyPoisonToPlayer(aktifOyuncuID);
    }

    void EtkiZehirTarama(int topLeftIndex)
    {
        // Gelen indeks gecerli degilse guvenlik amaciyla 0 yap
        if (!masaYonetici.IsValid2x2TopLeft(topLeftIndex))
        {
            Debug.LogWarning($"[CardManager] Gecersiz 2x2 alan baslangici: {topLeftIndex}. Guvenlik icin 0 atandi.");
            topLeftIndex = 0;
        }

        int zehirSayisi = masaYonetici.Count2x2Area(topLeftIndex, CupType.POISON, unconsumedOnly: true);
        Debug.Log($"[CardManager] Zehir Tarama (2x2 baslangic {topLeftIndex}): {zehirSayisi} zehirli bardak. HERKES GORDU.");
    }

    void EtkiPanzehirTarama(int aktifOyuncuID, int topLeftIndex)
    {
        // Gelen indeks gecerli degilse guvenlik amaciyla 0 yap
        if (!masaYonetici.IsValid2x2TopLeft(topLeftIndex))
        {
            Debug.LogWarning($"[CardManager] Gecersiz 2x2 alan baslangici: {topLeftIndex}. Guvenlik icin 0 atandi.");
            topLeftIndex = 0;
        }

        int panzehirSayisi = masaYonetici.Count2x2Area(topLeftIndex, CupType.ANTIDOTE, unconsumedOnly: true);
        Debug.Log($"[CardManager] Panzehir Tarama (2x2 baslangic {topLeftIndex}): {panzehirSayisi} panzehir. SADECE Oyuncu {aktifOyuncuID} GORDU.");
    }

    void EtkiGirdap()
    {
        Debug.Log("[CardManager] Girdap: Tur yonu degisti.");
        turnManager.ReverseTurnDirection();
    }

    void EtkiNefeslenme()
    {
        Debug.Log("[CardManager] Nefeslenme: Tur guvenle savuldu.");
    }

    void EtkiZorakiIkram(int aktifOyuncuID, int hedefOyuncuID, int secilenBardak)
    {
        // 1. Guvenlik Kontrolleri
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

        // 2. Efekti Uygula
        CupType tip = masaYonetici.ConsumeCupForPlayer(secilenBardak, hedefOyuncuID);
        turnManager.ResolveCupEffect(hedefOyuncuID, tip);
        
        Debug.Log($"[CardManager] Zoraki Ikram: Oyuncu {aktifOyuncuID}, Oyuncu {hedefOyuncuID}'ya bardak {secilenBardak} icirdi! Cikan sonuc: {tip}");
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
