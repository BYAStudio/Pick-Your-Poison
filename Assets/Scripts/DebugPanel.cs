using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Test sırasında arka plan verilerini toplar. Görsel UI sonradan bağlanacak.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] TurnManager turnManager;
    [SerializeField] GameSetupManager gameSetupManager;
    [SerializeField] PlayerTurnController playerTurnController;
    [SerializeField] CardManager cardManager;
    [SerializeField] BotAI botAI;

    [Header("Faz 1 Test Ayarlari")]
    [SerializeField] bool otomatikKurulum = true;
    [SerializeField] int oyuncuSayisi = 4;
    [SerializeField] int oyuncuBasinaZehir = 3;
    [SerializeField] int baslangicPanzehirSayisi = 6;

    [Header("Klavye")]
    [SerializeField] Key konsolaYazdirTusu = Key.F12;
    [SerializeField] Key resetTusu = Key.R;
    [SerializeField] Key rastgeleBardakIcTusu = Key.P;
    [SerializeField] Key kartCekTusu = Key.K;
    [SerializeField] Key botTurnTusu = Key.B;
    [SerializeField] Key turBitirTusu = Key.N;
    [SerializeField] Key yonDegistirTusu = Key.V;
    [SerializeField] Key aktifOyuncuyuZehirleTusu = Key.Z;
    [SerializeField] Key aktifOyuncuyuIyilestirTusu = Key.X;

    readonly List<int> zehirliBardakIndeksleri = new List<int>();
    readonly List<int> panzehirBardakIndeksleri = new List<int>();
    readonly Queue<string> olayGecmisi = new Queue<string>();

    const int MaksimumOlayKaydi = 10;

    public IReadOnlyList<int> ZehirliBardakIndeksleri => zehirliBardakIndeksleri;
    public IReadOnlyList<int> PanzehirBardakIndeksleri => panzehirBardakIndeksleri;

    void Awake()
    {
        ResolveReferences();
    }

    void Start()
    {
        if (otomatikKurulum)
            FazBirKurulumunuHazirla();
    }

    void Update()
    {
        if (WasPressed(konsolaYazdirTusu))
            UpdateDebugInfo();

        if (WasPressed(resetTusu))
            FazBirKurulumunuHazirla();

        if (WasPressed(rastgeleBardakIcTusu))
            AktifOyuncuyaRastgeleBardakIcir();

        if (WasPressed(kartCekTusu))
            AktifOyuncuyaKartCektir();

        if (WasPressed(botTurnTusu))
            BotTurnCalistir();

        if (WasPressed(turBitirTusu))
            SiradakiOyuncuyaGec();

        if (WasPressed(yonDegistirTusu))
            TurYonunuDegistir();

        if (WasPressed(aktifOyuncuyuZehirleTusu))
            AktifOyuncuyuDirektZehirle();

        if (WasPressed(aktifOyuncuyuIyilestirTusu))
            AktifOyuncuyuIyilestir();
    }

    bool _referencesResolved;

    void ResolveReferences()
    {
        if (_referencesResolved) return;

        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (gameSetupManager == null)
            gameSetupManager = FindAnyObjectByType<GameSetupManager>();

        if (playerTurnController == null)
            playerTurnController = FindAnyObjectByType<PlayerTurnController>();

        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();

        if (botAI == null)
            botAI = FindAnyObjectByType<BotAI>();

        _referencesResolved = true;
    }

    bool WasPressed(Key key)
    {
        return Keyboard.current != null &&
               Keyboard.current[key] != null &&
               Keyboard.current[key].wasPressedThisFrame;
    }

    #region Veri Toplama

    public void CollectCupData()
    {
        zehirliBardakIndeksleri.Clear();
        panzehirBardakIndeksleri.Clear();

        if (masaYonetici == null)
            return;

        int bardakSayisi = masaYonetici.GetCupCount();

        for (int i = 0; i < bardakSayisi; i++)
        {
            CupType tip = masaYonetici.GetCupType(i);

            if (tip == CupType.POISON)
                zehirliBardakIndeksleri.Add(i);
            else if (tip == CupType.ANTIDOTE)
                panzehirBardakIndeksleri.Add(i);
        }
    }

    public string BuildPlayerStatesReport()
    {
        if (turnManager == null)
            return "TurnManager referansi yok.";

        var sb = new StringBuilder();

        IReadOnlyList<Player> oyuncular = turnManager.Oyuncular;

        for (int i = 0; i < oyuncular.Count; i++)
        {
            Player oyuncu = oyuncular[i];
            string karakter = oyuncu.characterType != CharacterType.None ? oyuncu.characterType.ToString() : "Yok";
            string skip = oyuncu.characterType == CharacterType.Survivor ? $" [Skip:{oyuncu.skipHakki}]" : "";
            sb.AppendLine($"  Oyuncu {oyuncu.playerID} ({karakter}): {oyuncu.currentState}{skip}");
        }

        return sb.ToString();
    }

    public string BuildPoisonTimerReport()
    {
        if (turnManager == null)
            return "TurnManager referansi yok.";

        var sb = new StringBuilder();
        IReadOnlyList<Player> oyuncular = turnManager.Oyuncular;
        bool bulundu = false;

        for (int i = 0; i < oyuncular.Count; i++)
        {
            Player oyuncu = oyuncular[i];

            if (oyuncu.currentState != PlayerState.Poisoned)
                continue;

            bulundu = true;
            sb.AppendLine($"  Oyuncu {oyuncu.playerID}: {oyuncu.poisonedTimer} tur kaldi");
        }

        if (!bulundu)
            sb.AppendLine("  (Zehirlenmis oyuncu yok)");

        return sb.ToString();
    }

    public string BuildTurnReport()
    {
        if (turnManager == null)
            return "TurnManager referansi yok.";

        return
            $"  Aktif oyuncu: {turnManager.GetActivePlayerID()}\n" +
            $"  Tur yonu: {turnManager.GetTurnDirection()}\n" +
            $"  Oyun bitti: {turnManager.IsGameOver()}";
    }

    #endregion

    #region Faz 1 Test Akisi

    public void FazBirKurulumunuHazirla()
    {
        ResolveReferences();

        olayGecmisi.Clear();

        if (gameSetupManager != null)
        {
            gameSetupManager.BaslatSetup();
            gameSetupManager.OtomatikZehirYerlestirme();
            LogEvent($"Yeni Faz 1 kurulumu (GameSetupManager). Zehir: {masaYonetici?.CountByType(CupType.POISON)}, Panzehir: {masaYonetici?.CountByType(CupType.ANTIDOTE)}");
        }
        else if (masaYonetici != null && turnManager != null)
        {
            // Fallback: eski dogrudan kurulum
            masaYonetici.ResetTable();
            turnManager.StartGame(oyuncuSayisi);

            for (int oyuncu = 0; oyuncu < oyuncuSayisi; oyuncu++)
            {
                for (int i = 0; i < oyuncuBasinaZehir; i++)
                {
                    int hedef = Random.Range(0, masaYonetici.GetCupCount());
                    masaYonetici.PlacePoison(hedef, oyuncu);
                }
            }

            int yerlestirilenPanzehir = masaYonetici.PlaceRandomAntidotes(baslangicPanzehirSayisi);
            LogEvent($"Yeni Faz 1 kurulumu (fallback). Zehir: {masaYonetici.CountByType(CupType.POISON)}, Panzehir: {yerlestirilenPanzehir}");
        }

        if (gameSetupManager == null)
            playerTurnController?.YeniTurBasladi();
        UpdateDebugInfo();
    }

    public void AktifOyuncuyaRastgeleBardakIcir()
    {
        ResolveReferences();

        if (masaYonetici == null || turnManager == null || turnManager.IsGameOver())
            return;

        int bardakIndeksi = masaYonetici.GetRandomUnconsumedCupIndex();
        if (bardakIndeksi < 0)
        {
            LogEvent("Icilmemis bardak kalmadi.");
            return;
        }

        if (playerTurnController != null)
        {
            playerTurnController.BardakSecVeIc(bardakIndeksi);
            Player aktif = turnManager.GetActivePlayer();
            if (aktif != null)
                LogEvent($"Oyuncu {aktif.playerID} bardak {bardakIndeksi} icti.");
        }
        else
        {
            // Fallback: dogrudan masa/turn manager uzerinden
            int aktifOyuncuID = turnManager.GetActivePlayerID();
            Player aktifOyuncu = turnManager.GetActivePlayer();

            if (aktifOyuncu == null || !aktifOyuncu.IsAlive)
            {
                LogEvent("Aktif oyuncu hayatta degil; bardak icilemedi.");
                return;
            }

            CupType bardakTipi = masaYonetici.ConsumeCup(bardakIndeksi);
            turnManager.ResolveCupEffect(aktifOyuncuID, bardakTipi);
            LogEvent($"Oyuncu {aktifOyuncuID} bardak {bardakIndeksi} icti: {bardakTipi}");
            SiradakiOyuncuyaGec();
        }
    }

    public void SiradakiOyuncuyaGec()
    {
        if (turnManager == null || turnManager.IsGameOver())
            return;

        int oncekiOyuncu = turnManager.GetActivePlayerID();
        turnManager.EndTurn();
        int yeniOyuncu = turnManager.GetActivePlayerID();

        playerTurnController?.YeniTurBasladi();

        if (turnManager.IsGameOver())
        {
            LogEvent($"Oyun bitti. Kazanan: {turnManager.KazananOyuncuID}");
        }
        else
        {
            LogEvent($"Tur bitti. Oyuncu {oncekiOyuncu} -> Oyuncu {yeniOyuncu}");
        }

        UpdateDebugInfo();
    }

    public void TurYonunuDegistir()
    {
        if (turnManager == null)
            return;

        turnManager.ReverseTurnDirection();
        LogEvent($"Tur yonu degisti: {turnManager.GetTurnDirection()}");
        UpdateDebugInfo();
    }

    public void AktifOyuncuyaKartCektir()
    {
        if (playerTurnController == null || turnManager == null || turnManager.IsGameOver())
            return;

        int oyuncuID = turnManager.GetActivePlayerID();
        playerTurnController.KartCekVeSiraSav();
        LogEvent($"Oyuncu {oyuncuID} kart cekti.");
        UpdateDebugInfo();
    }

    public void BotTurnCalistir()
    {
        if (botAI == null || turnManager == null || turnManager.IsGameOver())
            return;

        int aktifOyuncuID = turnManager.GetActivePlayerID();
        botAI.ExecuteBotTurn(aktifOyuncuID);
        UpdateDebugInfo();
    }

    public void AktifOyuncuyuDirektZehirle()
    {
        if (turnManager == null || turnManager.IsGameOver())
            return;

        int oyuncuID = turnManager.GetActivePlayerID();
        turnManager.ApplyPoisonToPlayer(oyuncuID);
        LogEvent($"Oyuncu {oyuncuID} direkt zehirlendi.");
        UpdateDebugInfo();
    }

    public void AktifOyuncuyuIyilestir()
    {
        if (turnManager == null || turnManager.IsGameOver())
            return;

        int oyuncuID = turnManager.GetActivePlayerID();
        turnManager.ClearPoisonedTimer(oyuncuID);
        LogEvent($"Oyuncu {oyuncuID} iyilestirildi.");
        UpdateDebugInfo();
    }

    void LogEvent(string mesaj)
    {
        while (olayGecmisi.Count >= MaksimumOlayKaydi)
            olayGecmisi.Dequeue();

        olayGecmisi.Enqueue(mesaj);
        Debug.Log($"[DebugPanel] {mesaj}", this);
    }

    #endregion

    #region Ekran UI

    void OnGUI()
    {
        CollectCupData();

        GUI.Box(new Rect(12, 12, 480, 480), "Faz 2 Debug Panel");

        GUILayout.BeginArea(new Rect(24, 42, 456, 442));
        GUILayout.Label("Kontroller");
        GUILayout.Label($"[{resetTusu}] Kurulumu sifirla");
        GUILayout.Label($"[{rastgeleBardakIcTusu}] Aktif oyuncuya rastgele bardak ictir");
        GUILayout.Label($"[{kartCekTusu}] Aktif oyuncuya kart cektir");
        GUILayout.Label($"[{botTurnTusu}] Bot turu calistir");
        GUILayout.Label($"[{turBitirTusu}] Turu bitir");
        GUILayout.Label($"[{yonDegistirTusu}] Yonu degistir");
        GUILayout.Label($"[{aktifOyuncuyuZehirleTusu}] Aktif oyuncuyu zehirle");
        GUILayout.Label($"[{aktifOyuncuyuIyilestirTusu}] Aktif oyuncuyu iyilestir");
        GUILayout.Label($"[{konsolaYazdirTusu}] Konsola tam rapor yaz");

        GUILayout.Space(8);
        GUILayout.Label("Oyun Durumu");
        GUILayout.Label(BuildTurnReport());
        GUILayout.Label($"  Hayatta kalan: {(turnManager != null ? turnManager.GetLivingPlayerCount() : 0)}");
        GUILayout.Label($"  Kalan zehirli bardak: {(masaYonetici != null ? masaYonetici.CountUnconsumedByType(CupType.POISON) : 0)}");
        GUILayout.Label($"  Kalan panzehir: {(masaYonetici != null ? masaYonetici.CountUnconsumedByType(CupType.ANTIDOTE) : 0)}");
        GUILayout.Label($"  Son Kart: {(cardManager != null ? cardManager.SonCekilenKart.ToString() : "Yok")} (RNG:{(cardManager != null ? cardManager.SonRNGDegeri : 0)})");

        GUILayout.Space(8);
        GUILayout.Label("Oyuncular (Karakter)");
        GUILayout.TextArea(BuildPlayerStatesReport(), GUILayout.Height(90));

        GUILayout.Space(6);
        GUILayout.Label("Zehir Sayaçlari");
        GUILayout.TextArea(BuildPoisonTimerReport(), GUILayout.Height(60));

        GUILayout.Space(6);
        GUILayout.Label("Son Olaylar");
        GUILayout.TextArea(BuildEventLogReport(), GUILayout.Height(80));
        GUILayout.EndArea();
    }

    string BuildEventLogReport()
    {
        if (olayGecmisi.Count == 0)
            return "  (olay yok)";

        return "  " + string.Join("\n  ", olayGecmisi);
    }

    #endregion

    #region Konsol Cikti

    [ContextMenu("Update Debug Info")]
    public void UpdateDebugInfo()
    {
        ResolveReferences();
        CollectCupData();

        var rapor = new StringBuilder();
        rapor.AppendLine("===== DEBUG PANEL =====");

        rapor.AppendLine("[Zehirli Bardaklar]");
        rapor.AppendLine(FormatIndexList(zehirliBardakIndeksleri));

        rapor.AppendLine("[Panzehir Bardaklari]");
        rapor.AppendLine(FormatIndexList(panzehirBardakIndeksleri));

        rapor.AppendLine("[Oyuncu Durumlari]");
        rapor.Append(BuildPlayerStatesReport());

        rapor.AppendLine("[Aktif Tur]");
        rapor.Append(BuildTurnReport());

        rapor.AppendLine("[Zehirlenme Sayaclari]");
        rapor.Append(BuildPoisonTimerReport());

        if (cardManager != null)
        {
            rapor.AppendLine($"[Son Kart] {cardManager.SonCekilenKart} (RNG: {cardManager.SonRNGDegeri})");
        }

        rapor.AppendLine("=======================");

        Debug.Log(rapor.ToString(), this);
    }

    static string FormatIndexList(IReadOnlyList<int> indeksler)
    {
        if (indeksler == null || indeksler.Count == 0)
            return "  (yok)";

        return "  " + string.Join(", ", indeksler);
    }

    #endregion
}
