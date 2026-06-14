using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Faz 3: Sahne yonetimi, oyun yeniden baslatma, ana menuye donus ve cikis islemlerini
/// tek bir merkezden yonetir. UI butonlari bu scriptteki metodlara baglanir.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Sahne Ayarlari")]
    [Tooltip("Oyun sahnesinin adi veya build index'i")]
    [SerializeField] string gameSceneName = "Scene_Core";

    [Tooltip("Ana menu sahnesinin adi veya build index'i")]
    [SerializeField] string mainMenuSceneName = "MainMenu";

    [Header("Davranis")]
    [Tooltip("Restart'ta sahne mi yoksa TurnManager/Masa reset mi kullanilsin?")]
    [SerializeField] bool useSceneReloadForRestart = true;

    [Header("Referanslar (opsiyonel, reset modu icin)")]
    [SerializeField] TurnManager turnManager;
    [SerializeField] MasaYonetici masaYonetici;
    [SerializeField] GameSetupManager gameSetupManager;

    void Awake()
    {
        // Singleton benzeri yapi: birden fazla GameManager olmasini engelle
        if (FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        ResolveReferences();
    }

    #region Public API — UI Butonlarina Baglanabilir

    /// <summary>
    /// "Yeniden Oyna" butonu icin: sahneleri reload ederek tum durumu sifirlar.
    /// </summary>
    public void RestartGame()
    {
        if (useSceneReloadForRestart)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // Alternatif: Ay nesneleri uzerinden reset (daha hizli, sahne yuklemesiz)
            ResetInPlace();
        }
    }

    /// <summary>
    /// "Ana Menuye Don" butonu icin.
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[GameManager] Ana menu sahnesi tanimlanmamis. Lütfen Inspector'dan 'Main Menu Scene Name' alanini doldurun.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// "Oyundan Cik" butonu icin. Editor'de calismayabilir (build'te aktif).
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[GameManager] Editor modunde Application.Quit() calismaz. Play modunu durduruyor.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Belirtilen sahneye gecis yapar (custom gecisler icin).
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameManager] Yuklenecek sahne adi bos.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Belirtilen sahne index'ini yukler (Build Settings sirasina gore).
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"[GameManager] Gecersiz sahne index'i: {sceneIndex}. Build Settings'i kontrol edin.");
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    #endregion

    #region Yardimci

    void ResolveReferences()
    {
        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (masaYonetici == null)
            masaYonetici = FindAnyObjectByType<MasaYonetici>();

        if (gameSetupManager == null)
            gameSetupManager = FindAnyObjectByType<GameSetupManager>();
    }

    /// <summary>
    /// Sahne reload YAPMADAN mevcut nesneleri sifirlar (alternatif restart modu).
    /// </summary>
    void ResetInPlace()
    {
        if (turnManager != null)
            turnManager.ResetGame();

        if (masaYonetici != null)
            masaYonetici.ResetTable();

        if (gameSetupManager != null)
            gameSetupManager.ResetGameSetup();

        Debug.Log("[GameManager] Oyun yerinde sifirlandi (sahne reload yapilmadi).");
    }

    #endregion
}
