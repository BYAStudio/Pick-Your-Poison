using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Survivor karakterinin tur atlama haklarini ve UI elementlerini yonetir.
/// UI tasarimina dokunmaz; sadece durum (state) yonetimi ve UI kontrolu yapar.
/// 
/// Mantiksal Akis:
/// 1. Sira Survivor'a geldiginde "Sira Atla" butonu ve "Kalan Hak" text'ini aktif eder
/// 2. Survivor degilse UI elementlerini gizler
/// 3. Kalan hak 0 oldugunda butonu deaktif eder
/// </summary>
public class SurvivorSkipTurnHandler : MonoBehaviour
{
    [Header("Oyun Referanslari")]
    [SerializeField] TurnManager turnManager;

    [Header("UI Elementleri (Mevcut - Sadece Referans)")]
    [Tooltip("Sira atla butonu (Survivor icin aktif/pasif yapılacak)")]
    [SerializeField] Button skipTurnButton;

    [Tooltip("Kalan hak sayisini gosterecek TextMeshPro objesi")]
    [SerializeField] TMP_Text remainingSkipsText;

    /// <summary>
    /// UI elementlerinin gorunurlugunu ve aktiflik durumunu kontrol eder.
    /// Her tur baslangicinda cagrilmalidir.
    /// </summary>
    void Start()
    {
        ReferanslariCoz();
        
        // TurnManager'dan tur degisikliklerini dinle
        if (turnManager != null)
        {
            UIElementleriniGuncelle();
        }
    }

    #region Public API — Tur Baslangicinda Cagrilmali

    /// <summary>
    /// Her tur baslangicinda veya oyuncu degistiginde cagrilmali.
    /// UI elementlerini aktif oyuncunun karakterine gore gunceller.
    /// </summary>
    public void UIElementleriniGuncelle()
    {
        Player aktifOyuncu = turnManager?.GetActivePlayer();
        if (aktifOyuncu == null)
        {
            UIElementleriniGiz();
            return;
        }

        // Aktif oyuncu Survivor mi ve hayatta mi? (Sadece insan oyuncu 0 ise ve Survivor ise)
        if (aktifOyuncu.playerID == 0 && aktifOyuncu.characterType == CharacterType.Survivor && aktifOyuncu.IsAlive)
        {
            var selectionPanel = FindAnyObjectByType<SelectionPanelController>();
            bool isSelectionPanelActive = selectionPanel != null && selectionPanel.gameObject.activeSelf;

            var turnController = FindAnyObjectByType<PlayerTurnController>();
            bool isChoosingCup = turnController != null && turnController.IsChoosingDrinkCup;

            if (isSelectionPanelActive || isChoosingCup)
            {
                SurvivorUIElementleriniAktifEt(aktifOyuncu);
                return;
            }
        }

        UIElementleriniGiz();
    }

    /// <summary>
    /// "Sira Atla" butonuna tiklandiginda cagrilir.
    /// Survivor'un tur atlama hakki varsa bir hak tuketerek turu atlar.
    /// </summary>
    public void SiraAtlaButonunaTiklandi()
    {
        Player aktifOyuncu = turnManager?.GetActivePlayer();
        if (aktifOyuncu == null || aktifOyuncu.characterType != CharacterType.Survivor)
        {
            Debug.LogWarning("[SurvivorSkipTurn] Aktif oyuncu Survivor degil!");
            return;
        }

        // Kalan hak kontrolu
        if (aktifOyuncu.skipHakki <= 0)
        {
            Debug.LogWarning("[SurvivorSkipTurn] Survivor'un tur atlama hakki kalmamis!");
            SiraAtlaButonunuDevreDisiBirak();
            return;
        }

        PlayerTurnController turnController = FindAnyObjectByType<PlayerTurnController>();
        if (turnController != null)
        {
            var secimPanel = FindAnyObjectByType<SelectionPanelController>();
            if (secimPanel != null)
            {
                secimPanel.HidePanel();
            }

            turnController.TurAtla();
            UIElementleriniGuncelle();
        }
    }

    #endregion

    #region Yardimci Metodlar

    void ReferanslariCoz()
    {
        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        // Buton click event'ini bagla
        if (skipTurnButton != null)
        {
            skipTurnButton.onClick.AddListener(SiraAtlaButonunaTiklandi);
        }
        else
        {
            Debug.LogWarning("[SurvivorSkipTurn] Sira atla butonu referansi tanimlanmamis!");
        }
    }

    void SurvivorUIElementleriniAktifEt(Player survivor)
    {
        if (skipTurnButton != null)
        {
            if (survivor.skipHakki <= 0)
            {
                skipTurnButton.gameObject.SetActive(false);
            }
            else
            {
                skipTurnButton.gameObject.SetActive(true);
                skipTurnButton.interactable = true;
            }

            TMP_Text btnText = skipTurnButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = "Hayatta Kalan Turu Atlama Hakkı";
            }
        }

        if (remainingSkipsText != null)
        {
            if (survivor.skipHakki <= 0)
            {
                remainingSkipsText.gameObject.SetActive(false);
            }
            else
            {
                remainingSkipsText.gameObject.SetActive(true);
                KalanHakTextiniGuncelle(survivor.skipHakki);
            }
        }
    }

    void UIElementleriniGiz()
    {
        if (skipTurnButton != null)
        {
            skipTurnButton.gameObject.SetActive(false);
        }

        if (remainingSkipsText != null)
        {
            remainingSkipsText.gameObject.SetActive(false);
        }
    }

    void KalanHakTextiniGuncelle(int kalanHak)
    {
        if (remainingSkipsText != null)
        {
            remainingSkipsText.text = $"Kalan Hak: {kalanHak}";
        }
    }

    void SiraAtlaButonunuDevreDisiBirak()
    {
        if (skipTurnButton != null)
        {
            skipTurnButton.interactable = false;
            Debug.Log("[SurvivorSkipTurn] Sira atla butonu deaktif edildi (hak kalmadi).");
        }
    }

    #endregion
}
