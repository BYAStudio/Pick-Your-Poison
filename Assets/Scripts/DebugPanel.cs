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

    readonly List<int> zehirliBardakIndeksleri = new List<int>();
    readonly List<int> panzehirBardakIndeksleri = new List<int>();

    public IReadOnlyList<int> ZehirliBardakIndeksleri => zehirliBardakIndeksleri;
    public IReadOnlyList<int> PanzehirBardakIndeksleri => panzehirBardakIndeksleri;

    void Awake()
    {
        ResolveReferences();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            UpdateDebugInfo();
    }

    void ResolveReferences()
    {
        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();
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
            sb.AppendLine($"  Oyuncu {oyuncu.playerID}: {oyuncu.currentState}");
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
