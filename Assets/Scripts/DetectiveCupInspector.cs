using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Dedektif karakterinin "bardağa bak" (cup inspection) yetenegi icin oyun mantigi kontrolcusu.
/// UI tasarimina dokunmaz; sadece durum (state) yonetimi ve veri baglantisi yapar.
/// 
/// Mantiksal Akis:
/// 1. On Kontrol: Yetenek zaten kullanildi mi kontrol et
/// 2. Tetikleme: Butona tiklaninca hedef bardak secim modunu baslat
/// 3. Icerik Cozumleme: Secilen bardagin icerigini veritabanindan cek
/// 4. Deaktivasyon: Yetenegi kalici olarak deaktif et
/// </summary>
public class DetectiveCupInspector : MonoBehaviour
{
    [Header("Oyun Referanslari")]
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] TurnManager turnManager;

    [Header("UI Elementleri (Mevcut - Sadece Referans)")]
    [Tooltip("Bardağa bak butonu (tiklanabilir olmasi gereken)")]
    [SerializeField] Button bardagaBakButonu;

    [Tooltip("Dedektif sonuc paneli (aktif/pasif yapılacak)")]
    [SerializeField] GameObject dedektifSonucPaneli;

    [Tooltip("Bardak icerigini gosterecek TextMeshPro objesi")]
    [SerializeField] TMP_Text bardakIcerigiText;

    [Tooltip("Opsiyonel: Sonuc panelindeki bardak numarasini gosterecek text")]
    [SerializeField] TMP_Text bardakNumarasiText;

    /// <summary>
    /// Dedektif yeteneginin kullanilip kullanilmadigini takip eder.
    /// Oyun boyunca sadece 1 kez kullanilabilir.
    /// </summary>
    bool yetenekKullanildi = false;

    /// <summary>
    /// Bardak secim modunun aktif olup olmadigini takip eder.
    /// true = Oyuncu bardak seciyor, false = normal oyun akisi
    /// </summary>
    bool bardakSecimModuAktif = false;

    void Awake()
    {
        ReferanslariCoz();
        KullaniciArayuzunuHazirla();
    }

    #region Yetenek Mantigi

    /// <summary>
    /// "Bardağa bak" butonuna tiklandiginda cagrilir.
    /// Dedektif bardak inceleme yetenegini baslatir.
    /// </summary>
    public void BardagaBakYeteneginiBaslat()
    {
        // 1. ÖN KONTROL: Yetenek zaten kullanildi mi?
        if (yetenekKullanildi)
        {
            Debug.LogWarning("[DedektifCupInspector] Dedektif yetenegi zaten kullanildi!");
            return;
        }

        Player aktifOyuncu = turnManager?.GetActivePlayer();
        if (aktifOyuncu == null || aktifOyuncu.characterType != CharacterType.Detective)
        {
            Debug.LogWarning("[DedektifCupInspector] Aktif oyuncu dedektif degil!");
            return;
        }

        if (aktifOyuncu.detectiveAbilityUsed)
        {
            Debug.LogWarning("[DedektifCupInspector] Bu dedektif yetenegini zaten kullanmis!");
            yetenekKullanildi = true;
            BardagaBakButonunuDevreDisiBirak();
            return;
        }

        // 2. TETİKLEME: Bardak secim modunu baslat
        BardakSecimModunuBaslat();

        Debug.Log("[DedektifCupInspector] Bardak secim modu baslatildi. Lutfen bir bardak secin.");
    }

    /// <summary>
    /// Oyuncu bir bardak sectiginde cagrilir.
    /// Bardagin icerigini cozumler ve dedektife gosterir.
    /// </summary>
    /// <param name="secilenBardakIndeksi">Secilen bardagin indeksi (0-35)</param>
    public void BardakSecildi(int secilenBardakIndeksi)
    {
        // Bardak secim modu aktif degilse isleme
        if (!bardakSecimModuAktif)
            return;

        // Indeks kontrolu
        if (!masaYonetici.GecerliIndeks(secilenBardakIndeksi))
        {
            Debug.LogWarning($"[DedektifCupInspector] Gecersiz bardak indeksi: {secilenBardakIndeksi}");
            return;
        }

        // 3. İÇERİK ÇÖZÜMLEME: Bardagin icerigini cek
        CupType bardakIcerigi = masaYonetici.GetCupType(secilenBardakIndeksi);
        bool bardakTuketildiMi = masaYonetici.IsConsumed(secilenBardakIndeksi);

        // Bardak icerigini metne donustur
        string icerikMesaji = BardakIceriginiMetneCevir(bardakIcerigi, bardakTuketildiMi);

        // Sonuc panelini verilerle guncelle
        SonucPaneliniGuncelle(secilenBardakIndeksi, icerikMesaji);

        // Paneli aktif et
        DedektifPaneliniAktifEt();

        // 4. DEAKTİVASYON: Yetenegi kalici olarak deaktif et
        yetenekKullanildi = true;

        Player aktifOyuncu = turnManager.GetActivePlayer();
        if (aktifOyuncu != null)
        {
            aktifOyuncu.detectiveAbilityUsed = true;
        }

        BardagaBakButonunuDevreDisiBirak();
        BardakSecimModunuBitir();

        Debug.Log($"[DedektifCupInspector] Bardak {secilenBardakIndeksi + 1} incelendi. Icerik: {icerikMesaji}");
    }

    /// <summary>
    /// Bardak secim modunu sonlandirir (panel kapatildiginda cagrilmali).
    /// </summary>
    public void BardakSecimModunuSonlandir()
    {
        BardakSecimModunuBitir();
        DedektifPaneliniPasifEt();
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
        if (bardagaBakButonu != null)
        {
            // Mevcut butonun click event'ine baglan
            bardagaBakButonu.onClick.AddListener(BardagaBakYeteneginiBaslat);
        }
        else
        {
            Debug.LogWarning("[DedektifCupInspector] Bardaga bak butonu referansi tanimlanmamis!");
        }

        if (dedektifSonucPaneli != null)
        {
            // Baslangicta panel kapali olmalidir
            dedektifSonucPaneli.SetActive(false);
        }
    }

    void BardakSecimModunuBaslat()
    {
        bardakSecimModuAktif = true;
        // Bardak secim modu basladi - bardak butonlari artik secim icin hazir
        // UI tarafinda bardak butonlarina onClick event'leri eklenmeli
    }

    void BardakSecimModunuBitir()
    {
        bardakSecimModuAktif = false;
    }

    string BardakIceriginiMetneCevir(CupType icerik, bool tuketildiMi)
    {
        if (tuketildiMi)
        {
            return "Bu bardak zaten bosaltilmis!";
        }

        switch (icerik)
        {
            case CupType.POISON:
                return "☠ ZEHIR!";
            case CupType.ANTIDOTE:
                return "✓ Panzehir";
            case CupType.EMPTY:
                return "○ Boş";
            default:
                return "? Bilinmeyen";
        }
    }

    void SonucPaneliniGuncelle(int bardakIndeksi, string icerikMesaji)
    {
        if (bardakNumarasiText != null)
        {
            bardakNumarasiText.text = $"Bardak {bardakIndeksi + 1}";
        }

        if (bardakIcerigiText != null)
        {
            bardakIcerigiText.text = icerikMesaji;
        }
    }

    void DedektifPaneliniAktifEt()
    {
        if (dedektifSonucPaneli != null)
        {
            dedektifSonucPaneli.SetActive(true);
        }
    }

    void DedektifPaneliniPasifEt()
    {
        if (dedektifSonucPaneli != null)
        {
            dedektifSonucPaneli.SetActive(false);
        }
    }

    void BardagaBakButonunuDevreDisiBirak()
    {
        if (bardagaBakButonu != null)
        {
            bardagaBakButonu.interactable = false;
        }
    }

    #endregion

    #region Public API — Bardak Butonlarina Baglanabilir

    /// <summary>
    /// Bardak secim modunun aktif olup olmadigini kontrol eder.
    /// UI bardak butonlari bu degeri kontrol ederek tiklama olaylarini yonlendirebilir.
    /// </summary>
    public bool BardakSecimModuAktifMi()
    {
        return bardakSecimModuAktif;
    }

    /// <summary>
    /// UI bardak butonlarindan cagrilmak uzere.
    /// Bardak secim modu aktifken herhangi bir bardaga tiklaninca bu metot cagrilmali.
    /// </summary>
    public void OnBardakButtonClicked(int bardakIndeksi)
    {
        if (BardakSecimModuAktifMi())
        {
            BardakSecildi(bardakIndeksi);
        }
    }

    #endregion
}
