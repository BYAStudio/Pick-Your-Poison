using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tur sırası, yön, zehirlenme sayacı ve oyun bitişi (GDD §3–4).
/// </summary>
public class TurnManager : MonoBehaviour
{
    public const int VarsayilanZehirlenmeSuresi = Player.VarsayilanZehirlenmeSuresi;

    public enum TurnDirection
    {
        SaatYonu = 1,
        TersYon = -1
    }

    [Header("Oyuncu / Tur")]
    [SerializeField] int oyuncuSayisi = 4;
    [SerializeField] int aktifOyuncuIndeksi;
    [SerializeField] TurnDirection turYonu = TurnDirection.SaatYonu;

    [Header("Durum")]
    [SerializeField] bool oyunBitti;
    [SerializeField] int kazananOyuncuID = -1;

    readonly List<Player> oyuncular = new List<Player>();

    // UI'in dinleyebilecegi olay: oyuncu zehir suresi dolunca oldu
    public static event Action<Player> OnPlayerDiedFromPoisonTimer;

    public int AktifOyuncuIndeksi => aktifOyuncuIndeksi;
    public TurnDirection TurYonu => turYonu;
    public bool OyunBitti => oyunBitti;
    public int KazananOyuncuID => kazananOyuncuID;
    public IReadOnlyList<Player> Oyuncular => oyuncular;

    void Awake()
    {
        InitializeTurnSystem();
    }

    #region Baslatma

    public void InitializeTurnSystem()
    {
        aktifOyuncuIndeksi = 0;
        turYonu = TurnDirection.SaatYonu;
        oyunBitti = false;
        kazananOyuncuID = -1;
        CreatePlayers();
    }

    void CreatePlayers()
    {
        oyuncular.Clear();

        for (int i = 0; i < oyuncuSayisi; i++)
            oyuncular.Add(new Player(i));
    }

    public void StartGame(int playerCount)
    {
        oyuncuSayisi = playerCount;
        InitializeTurnSystem();
    }

    public void SetPlayerCount(int count)
    {
        oyuncuSayisi = count;
    }

    public Player GetPlayer(int playerID)
    {
        if (playerID < 0 || playerID >= oyuncular.Count)
            return null;

        return oyuncular[playerID];
    }

    #endregion

    #region Aktif Oyuncu

    public int GetActivePlayerIndex()
    {
        return aktifOyuncuIndeksi;
    }

    public int GetActivePlayerID()
    {
        return aktifOyuncuIndeksi;
    }

    public Player GetActivePlayer()
    {
        return GetPlayer(aktifOyuncuIndeksi);
    }

    public void SetActivePlayer(int playerIndex)
    {
        if (!GecerliOyuncuIndeksi(playerIndex))
            return;

        aktifOyuncuIndeksi = playerIndex;
    }

    public bool GecerliOyuncuIndeksi(int indeks)
    {
        return indeks >= 0 && indeks < oyuncuSayisi;
    }

    #endregion

    #region Sira Gecisi

    /// <summary>
    /// Aktif oyuncunun turunu sonlandırır; sırayı bir sonraki yaşayan oyuncuya geçirir.
    /// </summary>
    public void EndTurn()
    {
        if (oyunBitti)
            return;

        TickPoisonedTimers();
        AdvanceTurn();
        CheckGameEnd();
    }

    public void AdvanceTurn()
    {
        PassTurnToNextPlayer();
    }

    public void PassTurnToNextPlayer()
    {
        int sonraki = FindNextLivingPlayerIndex(aktifOyuncuIndeksi);
        if (sonraki < 0)
            return;

        aktifOyuncuIndeksi = sonraki;
    }

    public int FindNextLivingPlayerIndex(int fromIndex)
    {
        if (oyuncular.Count == 0)
            return -1;

        int adim = (int)turYonu;
        int mevcut = fromIndex;

        for (int i = 0; i < oyuncuSayisi; i++)
        {
            mevcut = (mevcut + adim + oyuncuSayisi) % oyuncuSayisi;

            if (oyuncular[mevcut].IsAlive)
                return mevcut;
        }

        return -1;
    }

    #endregion

    #region Olu Oyuncular

    public void RegisterPlayerDeath(int playerID)
    {
        Player oyuncu = GetPlayer(playerID);
        if (oyuncu != null && oyuncu.IsAlive)
            oyuncu.Die();
    }

    public bool IsPlayerAlive(int playerID)
    {
        Player oyuncu = GetPlayer(playerID);
        return oyuncu != null && oyuncu.IsAlive;
    }

    public bool ShouldSkipPlayer(int playerID)
    {
        return !IsPlayerAlive(playerID);
    }

    public void SkipDeadPlayers()
    {
        if (ShouldSkipPlayer(aktifOyuncuIndeksi))
            PassTurnToNextPlayer();
    }

    public List<int> GetDeadPlayerIDs()
    {
        var oluListe = new List<int>();

        for (int i = 0; i < oyuncular.Count; i++)
        {
            if (oyuncular[i].currentState == PlayerState.Dead)
                oluListe.Add(oyuncular[i].playerID);
        }

        return oluListe;
    }

    #endregion

    #region Tur Yonu

    public TurnDirection GetTurnDirection()
    {
        return turYonu;
    }

    public void ReverseTurnDirection()
    {
        turYonu = turYonu == TurnDirection.SaatYonu
            ? TurnDirection.TersYon
            : TurnDirection.SaatYonu;
    }

    public void SetTurnDirection(TurnDirection direction)
    {
        turYonu = direction;
    }

    #endregion

    #region Poisoned Timer

    public void ApplyPoisonToPlayer(int playerID, int turnsToSurvive = -1)
    {
        Player oyuncu = GetPlayer(playerID);
        if (oyuncu == null) return;

        int sure = turnsToSurvive >= 0 ? turnsToSurvive : oyuncu.GetPoisonSurvivalTurns();
        oyuncu.ApplyPoison(sure);
    }

    public void ResolveCupEffect(int playerID, CupType cupType)
    {
        Player oyuncu = GetPlayer(playerID);
        if (oyuncu == null || !oyuncu.IsAlive)
            return;

        switch (cupType)
        {
            case CupType.POISON:
                oyuncu.ApplyPoison(oyuncu.GetPoisonSurvivalTurns());
                break;

            case CupType.ANTIDOTE:
                oyuncu.CurePoison();
                break;
        }
    }

    public void AssignCharacter(int playerID, CharacterType type)
    {
        Player oyuncu = GetPlayer(playerID);
        if (oyuncu == null)
            return;

        oyuncu.characterType = type;

        if (type == CharacterType.Survivor)
            oyuncu.skipHakki = 2;
    }

    /// <summary>
    /// Tur bitiminde aktif oyuncunun zehir sayacını azaltır.
    /// </summary>
    public void TickPoisonedTimers()
    {
        Player aktif = GetActivePlayer();
        if (aktif == null || aktif.currentState != PlayerState.Poisoned)
            return;

        aktif.TickPoisonedTimer();

        if (!aktif.IsAlive)
            OnPoisonedTimerExpired(aktif.playerID);
    }

    public void SetPoisonedTimer(int playerID, int turnsRemaining)
    {
        Player oyuncu = GetPlayer(playerID);
        if (oyuncu == null || !oyuncu.IsAlive)
            return;

        oyuncu.currentState = PlayerState.Poisoned;
        oyuncu.poisonedTimer = turnsRemaining;
    }

    public int GetPoisonedTurnsRemaining(int playerID)
    {
        Player oyuncu = GetPlayer(playerID);
        if (oyuncu == null || oyuncu.currentState != PlayerState.Poisoned)
            return 0;

        return oyuncu.poisonedTimer;
    }

    public bool IsPlayerPoisoned(int playerID)
    {
        Player oyuncu = GetPlayer(playerID);
        return oyuncu != null && oyuncu.currentState == PlayerState.Poisoned;
    }

    public void ClearPoisonedTimer(int playerID)
    {
        GetPlayer(playerID)?.CurePoison();
    }

    public void OnPoisonedTimerExpired(int playerID)
    {
        Player oyuncu = GetPlayer(playerID);
        Debug.Log($"[TurnManager] Oyuncu {playerID} panzehir bulamadan öldü.");
        OnPlayerDiedFromPoisonTimer?.Invoke(oyuncu);
    }

    #endregion

    #region Dedektif Pasif Yetenek (UI Erisimi)

    MasaYonetici _cachedMasaYonetici;

    MasaYonetici GetMasaYonetici()
    {
        if (_cachedMasaYonetici == null)
            _cachedMasaYonetici = FindAnyObjectByType<MasaYonetici>();

        return _cachedMasaYonetici;
    }

    /// <summary>
    /// Masada anlik olarak kalan (icilmemis) toplam zehirli bardak sayisi.
    /// Dedektif pasif yetenegi icin UI'in erisebilecegi metot.
    /// </summary>
    public int GetRemainingPoisonCount()
    {
        var masa = GetMasaYonetici();
        return masa != null ? masa.CountUnconsumedByType(CupType.POISON) : 0;
    }

    /// <summary>
    /// Masada anlik olarak kalan (icilmemis) toplam panzehirli bardak sayisi.
    /// Dedektif pasif yetenegi icin UI'in erisebilecegi metot.
    /// </summary>
    public int GetRemainingAntidoteCount()
    {
        var masa = GetMasaYonetici();
        return masa != null ? masa.CountUnconsumedByType(CupType.ANTIDOTE) : 0;
    }

    #endregion

    #region Oyun Bitisi

    public bool CheckGameEnd()
    {
        int hayatta = GetLivingPlayerCount();

        if (hayatta > 1)
            return false;

        oyunBitti = true;

        if (hayatta == 1)
        {
            for (int i = 0; i < oyuncular.Count; i++)
            {
                if (oyuncular[i].IsAlive)
                {
                    kazananOyuncuID = oyuncular[i].playerID;
                    break;
                }
            }
        }
        else
        {
            kazananOyuncuID = -1;
        }

        return oyunBitti;
    }

    public bool IsGameOver()
    {
        return oyunBitti;
    }

    public int GetLivingPlayerCount()
    {
        int count = 0;

        for (int i = 0; i < oyuncular.Count; i++)
        {
            if (oyuncular[i].IsAlive)
                count++;
        }

        return count;
    }

    public int GetTotalPlayerCount()
    {
        return oyuncular.Count;
    }

    public void EndGame(int winnerPlayerID)
    {
        oyunBitti = true;
        kazananOyuncuID = winnerPlayerID;
    }

    public void ResetGame()
    {
        InitializeTurnSystem();
    }

    #endregion
}
