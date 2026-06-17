using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kimyager karakterinin analiz yetenegi icin oyun mantigi kontrolcusu.
/// UI tasarimina dokunmaz; sadece durum (state) yonetimi ve veri baglantisi yapar.
/// 
/// Mantiksal Akis:
/// 1. Büyüteç butonuna tiklanma olayini dinler
/// 2. Masadan 1 zehir ve 1 panzehir konumunu ceker
/// 3. Kimyager analiz panelini aktif eder ve verileri baglar
/// 4. Büyüteç butonunu deaktif eder (tekrar kullanim engellenir)
/// </summary>
public class KimyagerAnalizController : MonoBehaviour
{
    [Header("Oyun Referanslari")]
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] TurnManager turnManager;

    [Header("UI Elementleri (Mevcut - Sadece Referans)")]
    [Tooltip("Buyutec ikonu butonu (tiklanabilir olmasi gereken)")]
    [SerializeField] Button buyutecButonu;

    [Tooltip("Kimyager analiz paneli (aktif/pasif yapılacak)")]
    [SerializeField] GameObject kimyagerAnalizPaneli;

    [Tooltip("Zehir konumunu gosterecek TextMeshPro objesi")]
    [SerializeField] TMP_Text zehirYeriText;

    [Tooltip("Panzehir konumunu gosterecek TextMeshPro objesi")]
    [SerializeField] TMP_Text panzehirYeriText;

    /// <summary>
    /// Kimyager yeteneginin kullanilip kullanilmadigini takip eder.
    /// Oyun boyunca sadece 1 kez kullanilabilir.
    /// </summary>
    bool yetenekKullanildi = false;

    void Awake()
    {
        ReferanslariCoz();
        KullaniciArayuzunuHazirla();
    }

    #region Yetenek Mantigi

    /// <summary>
    /// Büyüteç butonuna tiklandiginda cagrilir.
    /// Kimyager analiz yetenegini tetikler.
    /// </summary>
    public void AnalizYeteneginiKullan()
    {
        if (yetenekKullanildi)
        {
            Debug.LogWarning("[KimyagerAnaliz] Kimyager yetenegi zaten kullanildi!");
            return;
        }

        Player aktifOyuncu = turnManager?.GetActivePlayer();
        if (aktifOyuncu == null || aktifOyuncu.characterType != CharacterType.Chemist)
        {
            Debug.LogWarning("[KimyagerAnaliz] Aktif oyuncu kimyager degil!");
            return;
        }

        if (aktifOyuncu.chemistAbilityUsed)
        {
            Debug.LogWarning("[KimyagerAnaliz] Bu kimyager yetenegini zaten kullanmis!");
            yetenekKullanildi = true;
            BuyutecButonunuDevreDisiBirak();
            return;
        }

        // 1. Veri Cekme: Masadan 1 zehir ve 1 panzehir konumu
        int zehirIndeksi = masaYonetici?.FindFirstCupIndexOfType(CupType.POISON, unconsumedOnly: true) ?? -1;
        int panzehirIndeksi = masaYonetici?.FindFirstCupIndexOfType(CupType.ANTIDOTE, unconsumedOnly: true) ?? -1;

        if (zehirIndeksi == -1 && panzehirIndeksi == -1)
        {
            Debug.LogWarning("[KimyagerAnaliz] Masada artik zehir veya panzehir kalmamis!");
            return;
        }

        // 2. Paneli Aktifleştir ve Verileri Bagla
        PaneliVerilerleGuncelle(zehirIndeksi, panzehirIndeksi);
        AnalizPaneliniAktifEt();

        // 3. Yetenegi isaretle ve deaktif et
        yetenekKullanildi = true;
        aktifOyuncu.chemistAbilityUsed = true;
        BuyutecButonunuDevreDisiBirak();

        Debug.Log($"[KimyagerAnaliz] Yetenek kullanildi. Zehir: {zehirIndeksi}, Panzehir: {panzehirIndeksi}");
    }

    /// <summary>
    /// Kapat butonuna tiklandiginda cagrilir.
    /// Kimyager analiz panelini deaktif eder.
    /// </summary>
    public void PaneliKapat()
    {
        if (kimyagerAnalizPaneli != null)
        {
            kimyagerAnalizPaneli.SetActive(false);
            Debug.Log("[KimyagerAnaliz] Analiz paneli kapatildi.");
        }
    }

    #endregion

    #region Yardimci Metodlar

    void ReferanslariCoz()
    {
        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();
    }

    void KullaniciArayuzunuHazirla()
    {
        if (buyutecButonu != null)
        {
            // Mevcut butonun click event'ine baglan
            buyutecButonu.onClick.AddListener(AnalizYeteneginiKullan);
        }
        else
        {
            Debug.LogWarning("[KimyagerAnaliz] Buyutec butonu referansi tanimlanmamis!");
        }

        if (kimyagerAnalizPaneli != null)
        {
            // Baslangicta panel kapali olmalidir
            kimyagerAnalizPaneli.SetActive(false);
        }
    }

    void PaneliVerilerleGuncelle(int zehirIndeks, int panzehirIndeks)
    {
        if (zehirYeriText != null)
        {
            if (zehirIndeks >= 0)
                zehirYeriText.text = $"Zehir Yeri: Bardak {zehirIndeks + 1}";
            else
                zehirYeriText.text = "Zehir Yeri: Kalan zehir yok";
        }

        if (panzehirYeriText != null)
        {
            if (panzehirIndeks >= 0)
                panzehirYeriText.text = $"Panzehir Yeri: Bardak {panzehirIndeks + 1}";
            else
                panzehirYeriText.text = "Panzehir Yeri: Kalan panzehir yok";
        }
    }

    void AnalizPaneliniAktifEt()
    {
        if (kimyagerAnalizPaneli != null)
        {
            kimyagerAnalizPaneli.SetActive(true);
        }
    }

    void BuyutecButonunuDevreDisiBirak()
    {
        if (buyutecButonu != null)
        {
            buyutecButonu.interactable = false;
        }
    }

    #endregion
}
