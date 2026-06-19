using System;
using UnityEngine;

public enum GamePhase
{
    Setup,
    Playing,
    GameOver
}

/// <summary>
/// Faz 1: Oyun baslangic setup'unu yonetir.
/// 1. Sistem rastgele 6 panzehir yerlestirir.
/// 2. Her oyuncu sirayla 3 zehir koyar (cakisma -> Hafiza Tuzagi).
/// 3. Setup bitince TurnManager devreye girer.
/// </summary>
public class GameSetupManager : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] TurnManager turnManager;
    [SerializeField] PlayerTurnController playerTurnController;

    public static CharacterType[] SecilenKarakterler;

    [Header("Ayarlar")]
    [SerializeField] int oyuncuSayisi = 4;
    [SerializeField] int oyuncuBasinaZehir = 3;
    [SerializeField] int baslangicPanzehirSayisi = 6;
    [SerializeField] CharacterType[] oyuncuKarakterSecimleri;

    int mevcutZehirKoyanOyuncuIndeksi = 0;
    int mevcutOyuncununKoyduguZehirSayisi = 0;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Setup;
    public int MevcutZehirKoyanOyuncuIndeksi => mevcutZehirKoyanOyuncuIndeksi;
    public int KalanZehirSayisi => oyuncuBasinaZehir - mevcutOyuncununKoyduguZehirSayisi;
    public bool SetupTamamlandi => CurrentPhase != GamePhase.Setup;

    public event Action OnSetupComplete;
    public event Action<int> OnPoisonPlacedByPlayer;
    public event Action<int> OnNextPlayerSetupTurn;

    void Awake()
    {
        ResolveReferences();
    }

    #region Setup Akisi

    public void BaslatSetup()
    {
        ResolveReferences();

        if (SecilenKarakterler != null && SecilenKarakterler.Length > 0)
        {
            oyuncuKarakterSecimleri = SecilenKarakterler;
        }

        if (masaYonetici == null || turnManager == null)
        {
            Debug.LogError("[GameSetupManager] Gerekli referanslar eksik.");
            return;
        }

        CurrentPhase = GamePhase.Setup;
        mevcutZehirKoyanOyuncuIndeksi = 0;
        mevcutOyuncununKoyduguZehirSayisi = 0;

        masaYonetici.ResetTable();
        turnManager.StartGame(oyuncuSayisi);

        // 2. Ilk oyuncu zehir koymaya baslar
        OnNextPlayerSetupTurn?.Invoke(mevcutZehirKoyanOyuncuIndeksi);
    }

    /// <summary>
    /// Mevcut setup sirasindaki oyuncu bir bardaga zehir koyar.
    /// Cakisma olursa Hafiza Tuzagi sistemi otomatik calisir (MasaYonetici icerisinde).
    /// </summary>
    public bool OyuncuZehirKoy(int bardakIndeksi)
    {
        if (CurrentPhase != GamePhase.Setup)
        {
            Debug.LogWarning("[GameSetupManager] Setup zaten tamamlandi.");
            return false;
        }

        bool basarili = masaYonetici.PlacePoison(bardakIndeksi, mevcutZehirKoyanOyuncuIndeksi);
        if (!basarili)
        {
            Debug.LogWarning($"[GameSetupManager] Oyuncu {mevcutZehirKoyanOyuncuIndeksi} icin zehir yerlestirilemedi.");
            return false;
        }

        mevcutOyuncununKoyduguZehirSayisi++;
        OnPoisonPlacedByPlayer?.Invoke(mevcutZehirKoyanOyuncuIndeksi);

        // Bu oyuncu zehirlerini bitirdi mi?
        if (mevcutOyuncununKoyduguZehirSayisi >= oyuncuBasinaZehir)
        {
            mevcutZehirKoyanOyuncuIndeksi++;
            mevcutOyuncununKoyduguZehirSayisi = 0;

            // Tum oyuncular bitirdi mi?
            if (mevcutZehirKoyanOyuncuIndeksi >= oyuncuSayisi)
            {
                SetupBitir();
            }
            else
            {
                // Eger oyuncu 0 (insan oyuncu) zehir yerlestirmeyi bitirdiyse botlar sırayla çakışmadan koysun
                if (mevcutZehirKoyanOyuncuIndeksi == 1)
                {
                    ExecuteBotsPoisonPlacing();
                }
                else
                {
                    OnNextPlayerSetupTurn?.Invoke(mevcutZehirKoyanOyuncuIndeksi);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Test amacli: kalan tum zehirleri otomatik rastgele yerlestirir.
    /// </summary>
    public void OtomatikZehirYerlestirme()
    {
        if (CurrentPhase != GamePhase.Setup)
            return;

        for (int oyuncu = mevcutZehirKoyanOyuncuIndeksi; oyuncu < oyuncuSayisi; oyuncu++)
        {
            int kalanZehir = (oyuncu == mevcutZehirKoyanOyuncuIndeksi)
                ? KalanZehirSayisi
                : oyuncuBasinaZehir;

            for (int i = 0; i < kalanZehir; i++)
            {
                int hedef = UnityEngine.Random.Range(0, masaYonetici.GetCupCount());
                masaYonetici.PlacePoison(hedef, oyuncu);
            }
        }

        SetupBitir();
    }

    void ExecuteBotsPoisonPlacing()
    {
        for (int botOyuncu = 1; botOyuncu < oyuncuSayisi; botOyuncu++)
        {
            for (int i = 0; i < oyuncuBasinaZehir; i++)
            {
                int bosBardak = masaYonetici.GetRandomEmptyCupIndex();
                if (bosBardak >= 0)
                {
                    masaYonetici.PlacePoison(bosBardak, botOyuncu);
                }
            }
        }
        SetupBitir();
    }

    void SetupBitir()
    {
        // Zehirleme islemi bittikten sonra kalan bos bardaklarin altısına panzehir koyar
        masaYonetici.PlaceRandomAntidotes(baslangicPanzehirSayisi);

        CurrentPhase = GamePhase.Playing;

        if (oyuncuKarakterSecimleri != null && oyuncuKarakterSecimleri.Length > 0)
            KarakterleriAta(oyuncuKarakterSecimleri);
        else
            KarakterleriRastgeleAta();

        // Oyun random bir kisiden baslar
        int randomStartPlayer = UnityEngine.Random.Range(0, oyuncuSayisi);
        turnManager.SetActivePlayer(randomStartPlayer);

        // Giris banner'ini olustur ve goster (ust kisimda, tur banner'iyla capismaz)
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var bannerGo = new GameObject("StartBanner");
            bannerGo.transform.SetParent(canvas.transform, false);
            var banner = bannerGo.AddComponent<StartingBannerController>();
            banner.Initialize("Tüm zehir ve panzehirler hazır!\nOyun başladı!", 2.5f, 0.72f, 0.92f);
        }

        OnSetupComplete?.Invoke();
        playerTurnController?.YeniTurBasladi();
    }

    public void VarsayilanKarakterleriAta()
    {
        if (turnManager == null) return;

        CharacterType[] varsayilanKarakterler = new CharacterType[]
        {
            CharacterType.Doctor,
            CharacterType.Survivor,
            CharacterType.Chemist,
            CharacterType.Detective
        };

        KarakterleriAta(varsayilanKarakterler);
    }

    public void KarakterleriRastgeleAta()
    {
        CharacterType[] varsayilanKarakterler = new CharacterType[]
        {
            CharacterType.Doctor,
            CharacterType.Survivor,
            CharacterType.Chemist,
            CharacterType.Detective
        };

        for (int i = 0; i < varsayilanKarakterler.Length; i++)
        {
            int rastgeleIndeks = UnityEngine.Random.Range(i, varsayilanKarakterler.Length);
            CharacterType gecici = varsayilanKarakterler[i];
            varsayilanKarakterler[i] = varsayilanKarakterler[rastgeleIndeks];
            varsayilanKarakterler[rastgeleIndeks] = gecici;
        }

        KarakterleriAta(varsayilanKarakterler);
    }

    /// <summary>
    /// Karakter secim hook'u: UI'dan veya disaridan karakter listesi ile cagrilabilir.
    /// Oyuncu sayisindan az karakter verilirse dongusal olarak tamamlanir.
    /// </summary>
    public void KarakterleriAta(CharacterType[] secilenKarakterler)
    {
        if (turnManager == null || secilenKarakterler == null || secilenKarakterler.Length == 0)
            return;

        int toplamOyuncu = turnManager.GetTotalPlayerCount();

        for (int i = 0; i < toplamOyuncu; i++)
        {
            CharacterType atanan = secilenKarakterler[i % secilenKarakterler.Length];
            turnManager.AssignCharacter(i, atanan);
        }
    }

    public void OyuncuKarakterleriniSec(CharacterType[] secilenKarakterler)
    {
        if (secilenKarakterler == null || secilenKarakterler.Length == 0)
        {
            oyuncuKarakterSecimleri = Array.Empty<CharacterType>();
            return;
        }

        oyuncuKarakterSecimleri = new CharacterType[secilenKarakterler.Length];
        Array.Copy(secilenKarakterler, oyuncuKarakterSecimleri, secilenKarakterler.Length);
    }

    #endregion

    #region Reset

    /// <summary>
    /// GameManager tarafindan cagrildiginda setup asamasini sifirlar.
    /// </summary>
    public void ResetGameSetup()
    {
        CurrentPhase = GamePhase.Setup;
        mevcutZehirKoyanOyuncuIndeksi = 0;
        mevcutOyuncununKoyduguZehirSayisi = 0;
        oyuncuKarakterSecimleri = Array.Empty<CharacterType>();
    }

    #endregion

    #region Yardimci

    void ResolveReferences()
    {
        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (playerTurnController == null)
            playerTurnController = FindAnyObjectByType<PlayerTurnController>();
    }

    #endregion
}
