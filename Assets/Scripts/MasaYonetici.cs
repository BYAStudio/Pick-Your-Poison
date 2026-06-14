using System.Collections.Generic;
using UnityEngine;

public class MasaYonetici : MonoBehaviour
{
    public const int BardakSayisi = 36;
    public const int ZorunluToplamZehir = 12;

    [Header("Grid Ayarlari")]
    [SerializeField] int sutunSayisi = 6;

    public int SutunSayisi => sutunSayisi;
    public int SatirSayisi => BardakSayisi / sutunSayisi;

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

    public int CountUnconsumedByType(CupType tip)
    {
        int count = 0;

        for (int i = 0; i < bardaklar.Count; i++)
        {
            Cup cup = bardaklar[i];
            if (!cup.isConsumed && cup.cupType == tip)
                count++;
        }

        return count;
    }

    public int CountUnconsumedTotal()
    {
        int count = 0;

        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (!bardaklar[i].isConsumed)
                count++;
        }

        return count;
    }

    public bool AreAllCupsConsumed() => CountUnconsumedTotal() == 0;

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

    public bool PlaceAntidote(int targetIndex)
    {
        if (!GecerliIndeks(targetIndex))
            return false;

        Cup hedef = bardaklar[targetIndex];
        if (hedef.cupType != CupType.EMPTY)
            return false;

        hedef.cupType = CupType.ANTIDOTE;
        hedef.ownerID = -1;
        return true;
    }

    public int PlaceRandomAntidotes(int count)
    {
        int yerlestirilen = 0;

        for (int i = 0; i < count; i++)
        {
            int bosBardakIndeksi = GetRandomEmptyCupIndex();
            if (bosBardakIndeksi < 0)
                break;

            if (PlaceAntidote(bosBardakIndeksi))
                yerlestirilen++;
        }

        return yerlestirilen;
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

        // Hafıza Tuzağı: Hedef bardak boş değilse (Zehir VEYA Panzehir varsa) rastgele boş bardağa kaydır.
        if (hedef.cupType != CupType.EMPTY)
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

    public int GetRandomUnconsumedCupIndex()
    {
        var uygunIndeksler = new List<int>();

        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (!bardaklar[i].isConsumed)
                uygunIndeksler.Add(i);
        }

        if (uygunIndeksler.Count == 0)
            return -1;

        int secim = Random.Range(0, uygunIndeksler.Count);
        return uygunIndeksler[secim];
    }

    public int GetRandomUnconsumedCupIndexExcluding(ICollection<int> haricIndeksler)
    {
        var uygunIndeksler = new List<int>();

        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (bardaklar[i].isConsumed)
                continue;

            if (haricIndeksler != null && haricIndeksler.Contains(i))
                continue;

            uygunIndeksler.Add(i);
        }

        if (uygunIndeksler.Count == 0)
            return -1;

        int secim = Random.Range(0, uygunIndeksler.Count);
        return uygunIndeksler[secim];
    }

    public CupType ConsumeCup(int indeks)
    {
        Cup cup = GetCup(indeks);
        if (cup == null || cup.isConsumed)
            return CupType.EMPTY;

        cup.isConsumed = true;
        cup.isRevealed = true;
        return cup.cupType;
    }

    /// <summary>
    /// Belirtilen oyuncu icin masadan bir bardak ictirir (Zoraki Ikram vb.).
    /// ownerID (bardagi zehirleyen) korunur, icen kisi consumedByID'ye yazilir.
    /// </summary>
    public CupType ConsumeCupForPlayer(int indeks, int playerID)
    {
        Cup cup = GetCup(indeks);
        if (cup == null || cup.isConsumed)
            return CupType.EMPTY;

        CupType tip = ConsumeCup(indeks);
        cup.consumedByID = playerID;
        return tip;
    }

    #endregion

    #region 2x2 Alan Tarama (Kart Efektleri)

    public bool IsValid2x2TopLeft(int indeks)
    {
        if (!GecerliIndeks(indeks))
            return false;

        int col = indeks % sutunSayisi;
        int row = indeks / sutunSayisi;
        int maxCol = sutunSayisi - 2;
        int maxRow = SatirSayisi - 2;

        return col <= maxCol && row <= maxRow;
    }

    public int[] Get2x2Indices(int topLeftIndex)
    {
        if (!IsValid2x2TopLeft(topLeftIndex))
            return new int[0];

        return new int[]
        {
            topLeftIndex,
            topLeftIndex + 1,
            topLeftIndex + sutunSayisi,
            topLeftIndex + sutunSayisi + 1
        };
    }

    public int Count2x2Area(int topLeftIndex, CupType tip, bool unconsumedOnly = false)
    {
        int[] indices = Get2x2Indices(topLeftIndex);
        int count = 0;

        for (int i = 0; i < indices.Length; i++)
        {
            Cup cup = GetCup(indices[i]);
            if (cup == null)
                continue;

            if (unconsumedOnly && cup.isConsumed)
                continue;

            if (cup.cupType == tip)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Belirtilen tipteki ilk bardagin indeksini dondurur.
    /// unconsumedOnly = true ise sadece icilmemis (aktif) bardaklara bakar.
    /// Kimyager yetenegi gibi icilmis bardaklarin gosterilmemesi gereken durumlar icin kullanilir.
    /// </summary>
    public int FindFirstCupIndexOfType(CupType tip, bool unconsumedOnly = false)
    {
        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (bardaklar[i].cupType == tip)
            {
                if (unconsumedOnly && bardaklar[i].isConsumed)
                    continue;
                return i;
            }
        }
        return -1;
    }

    public int FindRandomCupIndexOfType(CupType tip, bool unconsumedOnly = false)
    {
        var uygunIndeksler = new List<int>();

        for (int i = 0; i < bardaklar.Count; i++)
        {
            Cup cup = bardaklar[i];
            if (cup.cupType != tip)
                continue;

            if (unconsumedOnly && cup.isConsumed)
                continue;

            uygunIndeksler.Add(i);
        }

        if (uygunIndeksler.Count == 0)
            return -1;

        return uygunIndeksler[Random.Range(0, uygunIndeksler.Count)];
    }

    public int GetRandomValid2x2TopLeftIndex()
    {
        var uygunIndeksler = new List<int>();

        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (IsValid2x2TopLeft(i))
                uygunIndeksler.Add(i);
        }

        if (uygunIndeksler.Count == 0)
            return 0;

        return uygunIndeksler[Random.Range(0, uygunIndeksler.Count)];
    }

    public int[] GetRandomDistinctUnconsumedCupIndices(int count)
    {
        if (count <= 0)
            return new int[0];

        var uygunIndeksler = new List<int>();

        for (int i = 0; i < bardaklar.Count; i++)
        {
            if (!bardaklar[i].isConsumed)
                uygunIndeksler.Add(i);
        }

        if (uygunIndeksler.Count == 0)
            return new int[0];

        for (int i = 0; i < uygunIndeksler.Count; i++)
        {
            int rastgeleIndeks = Random.Range(i, uygunIndeksler.Count);
            int gecici = uygunIndeksler[i];
            uygunIndeksler[i] = uygunIndeksler[rastgeleIndeks];
            uygunIndeksler[rastgeleIndeks] = gecici;
        }

        int alinacak = Mathf.Min(count, uygunIndeksler.Count);
        int[] sonuc = new int[alinacak];

        for (int i = 0; i < alinacak; i++)
            sonuc[i] = uygunIndeksler[i];

        return sonuc;
    }

    #endregion
}
