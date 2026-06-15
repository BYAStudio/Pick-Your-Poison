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

        AudioManager.Instance?.PlaySFX(AudioManager.SFX.TurnStart);
    }

    public void TuruSonlandir()
    {
        if (!turAktif) return;
        turAktif = false;

        // Ölü oyuncunun turu "EndTurn" ile sonlandirilmamali.
        // Ölüm durumunda CheckGameEnd zaten ResolveCupEffect / ApplyPoisonToPlayer
        // tarafindan tetiklenir.
        if (turnManager != null && !turnManager.IsGameOver())
        {
            Player aktif = turnManager.GetActivePlayer();
            if (aktif != null && aktif.IsAlive)
            {
                turnManager.EndTurn();
            }
        }

        // Oyun bitmediyse sadece tur sonlandirma sesi çal
        if (turnManager == null || !turnManager.IsGameOver())
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.TurnEnd);
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

        // Bardak icerken ses
        if (icerik == CupType.POISON)
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PoisonDrink);
        else if (icerik == CupType.ANTIDOTE)
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.AntidoteDrink);
        else
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.CupDrink);

        // ResolveCupEffect icinde olum kaydi ve event firlatilir
        if (!aktifOyuncu.IsAlive)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PlayerDeath);
            TuruSonlandir();
            return;
        }

        // Bardak ictikten sonra tur otomatik biter
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

        // Hedef/Secim GEREKTIREN kartlar
        if (cekilenKart == CardType.AcgozlulukCezasi ||
            cekilenKart == CardType.ZehirTarama ||
            cekilenKart == CardType.PanzehirTarama ||
            cekilenKart == CardType.ZorakiIkram)
        {
            if (secimGerektirenKartlariOtomatikCoz)
            {
                BekleyenKartIcinOtomatikSecimYap(cekilenKart, aktifOyuncu.playerID);
            }
            else
            {
                kartSecimiBekleniyor = true;
                bekleyenKart = cekilenKart;
            }
        }
        else // Secim GEREKTIRMEYEN direkt kartlar (Kritik Doz, Girdap, Nefeslenme)
        {
            cardManager.UygulaKartEtkisi(cekilenKart, aktifOyuncu.playerID);
            // ApplyPoisonToPlayer / ResolveCupEffect icinde olum kaydi yapilir
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

        cardManager.UygulaKartEtkisi(bekleyenKart, aktifOyuncu.playerID, secilenAlan, bardak1, bardak2, hedefOyuncu);

        kartSecimiBekleniyor = false;
        bekleyenKart = default;
        // Olum kaydi CardManager icerisinde yapilir
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

        // Olum kaydi CardManager icerisinde yapilir
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
