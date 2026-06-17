using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Oyuncu panelindeki zehir durumunu ve kalan tur gostergelerini yonetir.
/// UI tasarimina dokunmaz; sadece durum (state) yonetimi ve UI kontrolu yapar.
/// 
/// Mantiksal Akis:
/// 1. Baslangicta zehir bolumu gizli (deaktif)
/// 2. Zehir icilince ActivatePoison() cagrilir → UI aktif olur
/// 3. Her tur DecreasePoisonTurn() cagrilir → Bir yeşil yuvarlak gizlenir
/// 4. Tum turler bitince zehir bolumu deaktif olur
/// </summary>
public class PlayerPoisonUIHandler : MonoBehaviour
{
    [Header("UI Elementleri (Mevcut - Sadece Referans)")]
    [Tooltip("Zehir bolumunu iceren ana panel GameObject'i")]
    [SerializeField] GameObject poisonSectionPanel;

    [Tooltip("Kalan tur gostergelerini iceren panel GameObject'i")]
    [SerializeField] GameObject remainingTurnsPanel;

    [Tooltip("Yesil yuvarlak simgeleri (sirali: son turdan ilk tura)")]
    [SerializeField] List<GameObject> greenCircleIndicators = new List<GameObject>();

    /// <summary>
    /// Kacin kalan zehir tur sayisini takip eder.
    /// </summary>
    int currentPoisonTurnsRemaining = 0;

    /// <summary>
    /// Zehir UI'inin su an aktif olup olmadiğini takip eder.
    /// </summary>
    bool isPoisonUIActive = false;

    void Start()
    {
        // Baslangic durumunu ayarla: Zehir bolumu varsayilan olarak gizli
        InitializePoisonUIState();
    }

    #region Public API — Disaridan Cagrilmali

    /// <summary>
    /// Oyuncu zehir ictiginde cagrilmalidir.
    /// Zehir UI bolumlerini aktif eder ve gosterge sifirlar.
    /// </summary>
    /// <param name="totalPoisonTurns">Zehrin toplam etki suresi (tur sayisi)</param>
    public void ActivatePoison(int totalPoisonTurns)
    {
        if (totalPoisonTurns <= 0)
        {
            Debug.LogWarning("[PlayerPoisonUI] Gecersiz zehir tur sayisi!");
            return;
        }

        currentPoisonTurnsRemaining = totalPoisonTurns;
        isPoisonUIActive = true;

        // Zehir bolumlerini aktif et
        PoisonUIBolumleriniAktifEt();

        // Yesil yuvarlak gostergeleri guncelle
        YesilYuvarlaklariGuncelle();

        Debug.Log($"[PlayerPoisonUI] Zehir aktif edildi. Kalan tur: {currentPoisonTurnsRemaining}");
    }

    /// <summary>
    /// Oyuncunun her yeni turu geldiginde cagrilmalidir.
    /// Bir yesil yuvarlak simgeyi gizler ve zehir tur sayisini azaltir.
    /// </summary>
    public void DecreasePoisonTurn()
    {
        if (!isPoisonUIActive)
        {
            Debug.LogWarning("[PlayerPoisonUI] Zehir UI aktif degil!");
            return;
        }

        if (currentPoisonTurnsRemaining <= 0)
        {
            Debug.LogWarning("[PlayerPoisonUI] Zehir turu zaten kalmamis!");
            PoisonUIDevreDisiBirak();
            return;
        }

        // Tur sayisini azalt
        currentPoisonTurnsRemaining--;

        // Bir yesil yuvarlagi gizle
        BirYesilYuvarlagiGizle();

        Debug.Log($"[PlayerPoisonUI] Zehir turu azaldi. Kalan tur: {currentPoisonTurnsRemaining}");

        // Tum turler bittiyse UI'i kapat
        if (currentPoisonTurnsRemaining <= 0)
        {
            PoisonUIDevreDisiBirak();
        }
    }

    /// <summary>
    /// Zehrin panzehirle tedavi edildiginde cagrilmalidir.
    /// Zehir UI'ini tamamen kapatir ve durumu sifirlar.
    /// </summary>
    public void CurePoison()
    {
        currentPoisonTurnsRemaining = 0;
        isPoisonUIActive = false;
        PoisonUIDevreDisiBirak();
        
        Debug.Log("[PlayerPoisonUI] Zehir tedavi edildi, UI kapatildi.");
    }

    /// <summary>
    /// Mevcut kalan zehir tur sayisini dondurur (UI disindaki sistemler icin).
    /// </summary>
    public int GetRemainingPoisonTurns()
    {
        return currentPoisonTurnsRemaining;
    }

    /// <summary>
    /// Zehir UI'inin aktif olup olmadigini kontrol eder.
    /// </summary>
    public bool IsPoisonUIActive()
    {
        return isPoisonUIActive;
    }

    #endregion

    #region Yardimci Metodlar

    void InitializePoisonUIState()
    {
        // Baslangicta zehir bolumleri gizli olmalidir
        if (poisonSectionPanel != null)
        {
            poisonSectionPanel.SetActive(false);
        }

        if (remainingTurnsPanel != null)
        {
            remainingTurnsPanel.SetActive(false);
        }

        // Yesil yuvarlaklari sifirla (hepsini gizle)
        TumYesilYuvarlaklariGizle();

        currentPoisonTurnsRemaining = 0;
        isPoisonUIActive = false;
    }

    void PoisonUIBolumleriniAktifEt()
    {
        if (poisonSectionPanel != null)
        {
            poisonSectionPanel.SetActive(true);
        }

        if (remainingTurnsPanel != null)
        {
            remainingTurnsPanel.SetActive(true);
        }
    }

    void PoisonUIDevreDisiBirak()
    {
        if (poisonSectionPanel != null)
        {
            poisonSectionPanel.SetActive(false);
        }

        if (remainingTurnsPanel != null)
        {
            remainingTurnsPanel.SetActive(false);
        }

        // Yesil yuvarlaklari sifirla
        TumYesilYuvarlaklariGizle();

        isPoisonUIActive = false;
        currentPoisonTurnsRemaining = 0;
    }

    void YesilYuvarlaklariGuncelle()
    {
        // Once tum yuvarlaklari gizle
        TumYesilYuvarlaklariGizle();

        // Kalan tur sayisi kadar yuvarlagi aktif et
        // NOT: greenCircleIndicators listesindeki siralamaya gore aktif edilir
        // Ornek: 3 tur kalsa → son 3 yuvarlak aktif olur
        int aktifEdilecekSayi = Mathf.Min(currentPoisonTurnsRemaining, greenCircleIndicators.Count);

        // Listenin sonundan basa dogru aktif et (son tur = son yuvarlak)
        for (int i = 0; i < aktifEdilecekSayi; i++)
        {
            int indeks = greenCircleIndicators.Count - 1 - i;
            if (indeks >= 0 && indeks < greenCircleIndicators.Count)
            {
                if (greenCircleIndicators[indeks] != null)
                {
                    greenCircleIndicators[indeks].SetActive(true);
                }
            }
        }
    }

    void BirYesilYuvarlagiGizle()
    {
        // Kalan tur sayisina gore en sondan bir yuvarlagi gizle
        // currentPoisonTurnsRemaining zaten azaltildi, simdi UI'i guncelle
        YesilYuvarlaklariGuncelle();
    }

    void TumYesilYuvarlaklariGizle()
    {
        for (int i = 0; i < greenCircleIndicators.Count; i++)
        {
            if (greenCircleIndicators[i] != null)
            {
                greenCircleIndicators[i].SetActive(false);
            }
        }
    }

    #endregion
}
