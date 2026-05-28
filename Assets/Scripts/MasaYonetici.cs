using System.Collections.Generic;
using UnityEngine;

public class MasaYonetici : MonoBehaviour
{
    public const int BardakSayisi = 36;
    public const int ZorunluToplamZehir = 12;

    [SerializeField] List<Cup> bardaklar = new List<Cup>(BardakSayisi);

    public IReadOnlyList<Cup> Bardaklar => bardaklar;

    void Awake()
    {
        InitializeCups();
    }

    #region Baslatma

    void InitializeCups()
    {
        bardaklar.Clear();

        for (int i = 0; i < BardakSayisi; i++)
            bardaklar.Add(new Cup());
    }

    #endregion

    #region Erisim

    public bool GecerliIndeks(int indeks)
    {
        return indeks >= 0 && indeks < bardaklar.Count;
    }

    public Cup GetCup(int indeks)
    {
        if (!GecerliIndeks(indeks))
        {
            Debug.LogWarning($"[MasaYonetici] Gecersiz bardak indeksi: {indeks}");
            return null;
        }

        return bardaklar[indeks];
    }

    public int GetCupCount()
    {
        return bardaklar.Count;
    }

    #endregion

    #region Durum Sorgulama

    public CupType GetCupType(int indeks)
    {
        Cup cup = GetCup(indeks);
        return cup != null ? cup.cupType : CupType.EMPTY;
    }

    public bool IsConsumed(int indeks)
    {
        Cup cup = GetCup(indeks);
        return cup != null && cup.isConsumed;
    }

    public bool IsRevealed(int indeks)
    {
        Cup cup = GetCup(indeks);
        return cup != null && cup.isRevealed;
    }

    public int GetOwnerID(int indeks)
    {
        Cup cup = GetCup(indeks);
        return cup != null ? cup.ownerID : -1;
    }

    public int CountByType(CupType tip)
    {
        int count = 0;
        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (bardaklar[i].cupType == tip)
                count++;
        }

        return count;
    }

    #endregion

    #region Durum Guncelleme

    public void SetCupType(int indeks, CupType tip)
    {
        Cup cup = GetCup(indeks);
        if (cup != null)
            cup.cupType = tip;
    }

    public void SetConsumed(int indeks, bool consumed)
    {
        Cup cup = GetCup(indeks);
        if (cup != null)
            cup.isConsumed = consumed;
    }

    public void SetRevealed(int indeks, bool revealed)
    {
        Cup cup = GetCup(indeks);
        if (cup != null)
            cup.isRevealed = revealed;
    }

    public void SetOwnerID(int indeks, int ownerId)
    {
        Cup cup = GetCup(indeks);
        if (cup != null)
            cup.ownerID = ownerId;
    }

    public void ResetTable()
    {
        InitializeCups();
    }

    #endregion

    #region Zehir Cakisma Sistemi

    /// <summary>
    /// Bu sistem oyunun Hafıza Tuzağı mekaniğini oluşturur.
    /// </summary>
    public bool PlacePoison(int targetIndex, int playerID)
    {
        if (!GecerliIndeks(targetIndex))
            return false;

        if (CountByType(CupType.POISON) >= ZorunluToplamZehir)
        {
            Debug.LogWarning("[MasaYonetici] Masada zaten 12 zehir bulunuyor.");
            return false;
        }

        Cup hedef = bardaklar[targetIndex];

        if (hedef.cupType == CupType.POISON)
        {
            int bosBardakIndeksi = GetRandomEmptyCupIndex();
            if (bosBardakIndeksi < 0)
            {
                Debug.LogWarning("[MasaYonetici] Cakisma: rastgele bos bardak bulunamadi.");
                return false;
            }

            ApplyPoisonToCup(bosBardakIndeksi, playerID);
            return true;
        }

        if (hedef.cupType != CupType.EMPTY)
        {
            Debug.LogWarning($"[MasaYonetici] Zehir yerlestirilemedi. Bardak tipi: {hedef.cupType}");
            return false;
        }

        ApplyPoisonToCup(targetIndex, playerID);
        return true;
    }

    void ApplyPoisonToCup(int indeks, int playerID)
    {
        Cup cup = bardaklar[indeks];
        cup.cupType = CupType.POISON;
        cup.ownerID = playerID;
    }

    int GetRandomEmptyCupIndex()
    {
        var bosIndeksler = new List<int>();

        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (bardaklar[i].cupType == CupType.EMPTY)
                bosIndeksler.Add(i);
        }

        if (bosIndeksler.Count == 0)
            return -1;

        int secim = Random.Range(0, bosIndeksler.Count);
        return bosIndeksler[secim];
    }

    #endregion
}
