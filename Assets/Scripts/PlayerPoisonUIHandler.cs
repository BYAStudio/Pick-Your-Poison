using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Oyuncu panellerindeki zehir durumunu, karakter rollerini ve kalan tur göstergelerini (yeşil yuvarlaklar) yönetir.
/// </summary>
public class PlayerPoisonUIHandler : MonoBehaviour
{
    private class PanelRefs
    {
        public GameObject panelGo;
        public TMP_Text nameText;
        public TMP_Text statusText;
        public TMP_Text durationText;
        public List<GameObject> dots;
        public GameObject buyutecButton;
    }

    private List<PanelRefs> panels = new List<PanelRefs>();
    private TurnManager turnManager;
    private PlayerTurnController playerTurnController;

    private GameObject dedektifBilgiPaneli;
    private TMP_Text toplamZehirText;
    private TMP_Text toplamPanzehirText;
    private GameObject kimyagerAnalizPaneli;
    private GameObject dedektifSonucPaneli;

    void Awake()
    {
        turnManager = FindAnyObjectByType<TurnManager>();
        playerTurnController = FindAnyObjectByType<PlayerTurnController>();
    }

    void Start()
    {
        InitializePanels();
        UpdateAllPanels();
    }

    public void InitializePanels()
    {
        panels.Clear();
        GameObject canvasGo = GameObject.Find("Canvas");
        for (int i = 1; i <= 4; i++)
        {
            string panelName = $"Oyucu_Panel_{i}";
            GameObject panelGo = null;
            if (canvasGo != null)
            {
                Transform t = canvasGo.transform.Find(panelName);
                if (t != null) panelGo = t.gameObject;
            }
            if (panelGo == null)
            {
                panelGo = GameObject.Find(panelName);
            }

            if (panelGo != null)
            {
                // Enlarge player panels by 25%
                var rt = panelGo.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = new Vector3(1.25f, 1.25f, 1.25f);
                }

                PanelRefs refs = new PanelRefs();
                refs.panelGo = panelGo;
                refs.nameText = FindChildText(panelGo, "Oyuncu_Adi_Text");
                
                // Enlarge character names and make them bold
                if (refs.nameText != null)
                {
                    if (refs.nameText.fontSize < 28f)
                    {
                        refs.nameText.fontSize = 28f;
                    }
                    refs.nameText.fontStyle = FontStyles.Bold;
                }

                refs.statusText = FindChildText(panelGo, "Zehir_Durumu_Text");
                refs.durationText = FindChildText(panelGo, "Zehir_Suresi_Text");
                
                var btnTr = panelGo.transform.Find("Button");
                if (btnTr != null) refs.buyutecButton = btnTr.gameObject;

                refs.dots = new List<GameObject>();
                for (int d = 1; d <= 4; d++)
                {
                    Transform dotTr = panelGo.transform.Find($"nokta_{d}");
                    if (dotTr != null)
                    {
                        refs.dots.Add(dotTr.gameObject);
                    }
                }
                panels.Add(refs);
            }
        }

        if (canvasGo != null)
        {
            var dbpTr = canvasGo.transform.Find("Dedektif_Bilgi_Paneli");
            if (dbpTr != null)
            {
                dedektifBilgiPaneli = dbpTr.gameObject;
                toplamZehirText = dbpTr.Find("Toplam_Zehir_Text")?.GetComponent<TMP_Text>();
                toplamPanzehirText = dbpTr.Find("Toplam_Panzehir_Text")?.GetComponent<TMP_Text>();
            }

            var kapTr = canvasGo.transform.Find("Kimyager_Analiz_Paneli");
            if (kapTr != null) kimyagerAnalizPaneli = kapTr.gameObject;

            var dspTr = canvasGo.transform.Find("Dedektif_Sonuc_Paneli");
            if (dspTr != null) dedektifSonucPaneli = dspTr.gameObject;
        }
    }

    private TMP_Text FindChildText(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null)
        {
            return child.GetComponent<TMP_Text>();
        }
        return null;
    }

    /// <summary>
    /// Tüm oyuncuların durumlarına göre panelleri günceller.
    /// </summary>
    public void UpdateAllPanels()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (playerTurnController == null) playerTurnController = FindAnyObjectByType<PlayerTurnController>();
        if (turnManager == null) return;
        
        if (panels.Count == 0)
        {
            InitializePanels();
        }

        var players = turnManager.Oyuncular;
        for (int i = 0; i < players.Count; i++)
        {
            if (i >= panels.Count) break;

            Player player = players[i];
            PanelRefs panel = panels[i];

            // Büyüteç butonunun durumunu ayarla (büyüteç kullanılana kadar kimyager/dedektif panelinde olmalı, kullanılırsa deaktif edilmeli)
            if (panel.buyutecButton != null)
            {
                if (i == 0) // Human player panel
                {
                    bool isMyTurn = (turnManager.GetActivePlayerID() == 0) && (player.IsAlive);
                    if (player.characterType == CharacterType.Chemist)
                    {
                        panel.buyutecButton.SetActive(true); // Always keep active/visible
                        var btnComp = panel.buyutecButton.GetComponent<UnityEngine.UI.Button>();
                        if (btnComp != null) btnComp.interactable = isMyTurn && !player.chemistAbilityUsed; // Deactivate if used
                    }
                    else if (player.characterType == CharacterType.Detective)
                    {
                        panel.buyutecButton.SetActive(true); // Always keep active/visible
                        var btnComp = panel.buyutecButton.GetComponent<UnityEngine.UI.Button>();
                        if (btnComp != null) btnComp.interactable = isMyTurn && !player.detectiveAbilityUsed; // Deactivate if used
                    }
                    else
                    {
                        panel.buyutecButton.SetActive(false);
                    }
                }
                else
                {
                    panel.buyutecButton.SetActive(false);
                }
            }

            // 1. Oyuncu Karakter İsmi ve ID Güncellemesi
            if (panel.nameText != null)
            {
                string karakterName = GetKarakterName(player.characterType);
                panel.nameText.text = karakterName;
            }

            // 2. Durum ve Yuvarlak (Nokta) Göstergeleri Güncellemesi
            if (player.currentState == PlayerState.Dead)
            {
                if (panel.statusText != null)
                {
                    panel.statusText.gameObject.SetActive(true);
                    panel.statusText.text = "<color=red>ÖLDÜ</color>";
                }
                if (panel.durationText != null) 
                    panel.durationText.gameObject.SetActive(false);
                
                // Ölü oyuncuda tüm noktaları gizle
                foreach (var dot in panel.dots)
                {
                    if (dot != null) dot.SetActive(false);
                }
            }
            else if (player.currentState == PlayerState.Poisoned)
            {
                if (panel.statusText != null)
                {
                    panel.statusText.gameObject.SetActive(false); // Zehir yazısı kaldırılacak
                }
                if (panel.durationText != null)
                {
                    panel.durationText.gameObject.SetActive(true);
                    panel.durationText.text = "Kalan Tur";
                }

                // Doktor için 4, diğerleri için 3 maksimum yuvarlak hakkı
                int maxDots = player.characterType == CharacterType.Doctor ? 4 : 3;

                // Doktor panelinde nokta_4 yoksa dinamik olarak oluştur
                if (maxDots == 4 && panel.dots.Count < 4)
                {
                    Transform dot4Tr = panel.panelGo.transform.Find("nokta_4");
                    if (dot4Tr == null)
                    {
                        Transform dot3Tr = panel.panelGo.transform.Find("nokta_3");
                        Transform dot2Tr = panel.panelGo.transform.Find("nokta_2");
                        if (dot3Tr != null && dot2Tr != null)
                        {
                            GameObject dot4Go = Instantiate(dot3Tr.gameObject, panel.panelGo.transform);
                            dot4Go.name = "nokta_4";
                            dot4Go.transform.localPosition = new Vector3(130f, -45f, 0f);
                            panel.dots.Add(dot4Go);
                        }
                    }
                    else if (!panel.dots.Contains(dot4Tr.gameObject))
                    {
                        panel.dots.Add(dot4Tr.gameObject);
                    }
                }

                // Kalan zehir süresi (timer) kadar yuvarlağı aktif et, kalanını gizle
                for (int d = 0; d < panel.dots.Count; d++)
                {
                    if (panel.dots[d] == null) continue;

                    if (d < maxDots && d < player.poisonedTimer)
                    {
                        panel.dots[d].SetActive(true);
                    }
                    else
                    {
                        panel.dots[d].SetActive(false);
                    }
                }
            }
            else // Healthy (Sağlıklı)
            {
                if (panel.statusText != null) 
                    panel.statusText.gameObject.SetActive(false);
                
                if (panel.durationText != null) 
                    panel.durationText.gameObject.SetActive(false);
                
                // Sağlıklı oyuncuda noktaları tamamen gizle
                foreach (var dot in panel.dots)
                {
                    if (dot != null) dot.SetActive(false);
                }
            }
        }

        // Dedektif_Bilgi_Paneli durumunu güncelle
        if (dedektifBilgiPaneli != null)
        {
            var humanPlayer = turnManager.GetPlayer(0);
            bool isHumanDetective = (humanPlayer != null && humanPlayer.characterType == CharacterType.Detective && humanPlayer.IsAlive);
            dedektifBilgiPaneli.SetActive(isHumanDetective);
            if (isHumanDetective)
            {
                if (toplamZehirText != null)
                    toplamZehirText.text = "Zehir: " + turnManager.GetRemainingPoisonCount();
                if (toplamPanzehirText != null)
                    toplamPanzehirText.text = "Panzehir: " + turnManager.GetRemainingAntidoteCount();

                var dbpButtonTr = dedektifBilgiPaneli.transform.Find("Bardaga_Bak");
                if (dbpButtonTr != null)
                {
                    bool isMyTurn = (turnManager.GetActivePlayerID() == 0);
                    dbpButtonTr.gameObject.SetActive(true); // Always keep active/visible
                    var btnComp = dbpButtonTr.GetComponent<UnityEngine.UI.Button>();
                    if (btnComp != null) btnComp.interactable = isMyTurn && !humanPlayer.detectiveAbilityUsed; // Deactivate if used
                }
            }
        }
    }

    private string GetKarakterName(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Doctor: return "Doktor";
            case CharacterType.Survivor: return "Hayatta Kalan";
            case CharacterType.Chemist: return "Kimyager";
            case CharacterType.Detective: return "Dedektif";
            default: return "Oyuncu";
        }
    }
}
