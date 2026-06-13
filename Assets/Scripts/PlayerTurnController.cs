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

    [Header("Durum")]
    [SerializeField] bool turAktif = false;
    [SerializeField] bool kartSecimiBekleniyor = false;
    [SerializeField] CardType bekleyenKart;
    [SerializeField] bool secimGerektirenKartlariOtomatikCoz = true;

    public bool TurAktif => turAktif;

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

        Player aktif = turnManager?.GetActivePlayer();
        if (aktif != null && aktif.IsAlive)
        {
            Debug.Log($"[PlayerTurnController] Oyuncu {aktif.playerID}'nin turu basladi.");
        }
    }

    public void TuruSonlandir()
    {
        if (!turAktif) return;
        turAktif = false;

        if (turnManager != null && !turnManager.IsGameOver())
        {
            turnManager.EndTurn();
        }
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
        turnManager.ResolveCupEffect(aktifOyuncu.playerID, icerik);

        Debug.Log($"[PlayerTurnController] Oyuncu {aktifOyuncu.playerID}, bardak {bardakIndeksi} icti: {icerik}");

        // Eger oyuncu olduyse olumu kaydet ve turu bitir
        if (!aktifOyuncu.IsAlive)
        {
            turnManager.RegisterPlayerDeath(aktifOyuncu.playerID);
            TuruSonlandir();
            return;
        }

        // Bardak ictikten sonra tur otomatik biter
        TuruSonlandir();
    }

    /// <summary>
    /// Aktif oyuncu kart ceker ve etkisini uygular.
    /// Negatif kartlar (Acgozluluk Cezasi, Kritik Doz) zorla bardak icmeye/zehirlenmeye yol acar.
    /// Diger kartlarda sira guvenle savulur.
    /// </summary>
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
        Debug.Log($"[PlayerTurnController] Oyuncu {aktifOyuncu.playerID} kart cekti: {cekilenKart}");

        // Hedef/Secim GEREKTIREN kartlar
        if (cekilenKart == CardType.AcgozlulukCezasi || 
            cekilenKart == CardType.ZehirTarama || 
            cekilenKart == CardType.PanzehirTarama || 
            cekilenKart == CardType.ZorakiIkram)
        {
            if (secimGerektirenKartlariOtomatikCoz)
            {
                Debug.Log($"[PlayerTurnController] {cekilenKart} cekildi. Gecerli hedefler otomatik secilerek cozuluyor.");
                BekleyenKartIcinOtomatikSecimYap(cekilenKart, aktifOyuncu.playerID);
            }
            else
            {
                kartSecimiBekleniyor = true;
                bekleyenKart = cekilenKart;
                Debug.Log($"[PlayerTurnController] {cekilenKart} cekildi. UI'dan secim bekleniyor...");
            }
        }
        else // Secim GEREKTIRMEYEN direkt kartlar (Kritik Doz, Girdap, Nefeslenme)
        {
            cardManager.UygulaKartEtkisi(cekilenKart, aktifOyuncu.playerID);
            
            if (!aktifOyuncu.IsAlive) turnManager.RegisterPlayerDeath(aktifOyuncu.playerID);
            
            TuruSonlandir();
        }
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
        
        // Bekleyen karti CardManager'a gonder ve uygula
        cardManager.UygulaKartEtkisi(bekleyenKart, aktifOyuncu.playerID, secilenAlan, bardak1, bardak2, hedefOyuncu);

        // Islemler bittigine gore state'i sifirla
        kartSecimiBekleniyor = false;
        bekleyenKart = default;

        if (!aktifOyuncu.IsAlive) turnManager.RegisterPlayerDeath(aktifOyuncu.playerID);

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
            Debug.Log($"[PlayerTurnController] Survivor Oyuncu {aktifOyuncu.playerID} tur atladi. Kalan hak: {aktifOyuncu.skipHakki}");
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

        // Karakter kontrolu
        if (aktifOyuncu.characterType != CharacterType.Chemist)
        {
            Debug.LogWarning("[PlayerTurnController] Aktif oyuncu Kimyager degil.");
            return false;
        }

        // Hak kontrolu
        if (aktifOyuncu.chemistAbilityUsed)
        {
            Debug.LogWarning("[PlayerTurnController] Kimyager yetenegi zaten kullanilmis.");
            return false;
        }

        if (masaYonetici == null) return false;

        // MasaYonetici uzerinden indeksleri cek (sadece icilmemis bardaklar)
        zehirIndeksi = masaYonetici.FindRandomCupIndexOfType(CupType.POISON, unconsumedOnly: true);
        panzehirIndeksi = masaYonetici.FindRandomCupIndexOfType(CupType.ANTIDOTE, unconsumedOnly: true);

        // Hakki dusur ve flag'i isaretle
        aktifOyuncu.chemistAbilityUsed = true;
        
        Debug.Log($"[PlayerTurnController] Kimyager yetenegi kullanildi. Zehir: {zehirIndeksi}, Panzehir: {panzehirIndeksi}");
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

        // Karakter kontrolu
        if (aktifOyuncu.characterType != CharacterType.Detective)
        {
            Debug.LogWarning("[PlayerTurnController] Aktif oyuncu Dedektif degil.");
            return false;
        }

        // Hak kontrolu
        if (aktifOyuncu.detectiveAbilityUsed)
        {
            Debug.LogWarning("[PlayerTurnController] Dedektif yetenegi zaten kullanilmis.");
            return false;
        }

        if (masaYonetici == null) return false;

        // Bardagin tipini ogren
        bardakTipi = masaYonetici.GetCupType(bardakIndeksi);

        // Hakki dusur ve flag'i isaretle
        aktifOyuncu.detectiveAbilityUsed = true;

        Debug.Log($"[PlayerTurnController] Dedektif yetenegi kullanildi. Bardak {bardakIndeksi} icerigi: {bardakTipi}");
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
            case CardType.AcgozlulukCezasi:
            {
                int[] bardaklar = masaYonetici.GetRandomDistinctUnconsumedCupIndices(2);
                if (bardaklar.Length > 0) bardak1 = bardaklar[0];
                if (bardaklar.Length > 1) bardak2 = bardaklar[1];
                break;
            }

            case CardType.ZorakiIkram:
            {
                int[] bardaklar = masaYonetici.GetRandomDistinctUnconsumedCupIndices(1);
                if (bardaklar.Length > 0) bardak1 = bardaklar[0];
                hedefOyuncu = RastgeleHayattaOlanHedefSec(aktifOyuncuID);
                break;
            }
        }

        cardManager.UygulaKartEtkisi(kart, aktifOyuncuID, secilenAlan, bardak1, bardak2, hedefOyuncu);

        Player aktifOyuncu = turnManager.GetPlayer(aktifOyuncuID);
        if (aktifOyuncu != null && !aktifOyuncu.IsAlive)
            turnManager.RegisterPlayerDeath(aktifOyuncu.playerID);

        TuruSonlandir();
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
