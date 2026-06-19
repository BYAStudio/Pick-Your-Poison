using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Karakter seçme sahnesini yönetir.
/// Seçilen karakteri oyuncuya atar, diğerlerini botlara rastgele dağıtır ve oyun sahnelerini yükler.
/// </summary>
public class CharacterSelectionController : MonoBehaviour
{
    [Tooltip("Yüklenecek ana oyun sahnesinin adı")]
    [SerializeField] private string gameSceneName = "Oyun_Sahnesi";

    /// <summary>
    /// Bir karakter kartına tıklandığında çağrılır.
    /// </summary>
    /// <param name="selectedChar">Oyuncunun seçtiği karakter tipi</param>
    public void OnCharacterSelected(CharacterType selectedChar)
    {
        // 1. Tüm geçerli karakterleri listele
        var allChars = new List<CharacterType>
        {
            CharacterType.Doctor,
            CharacterType.Survivor,
            CharacterType.Chemist,
            CharacterType.Detective
        };

        // Seçilen karakteri listeden çıkar
        allChars.Remove(selectedChar);

        // 2. Kalan 3 karakteri botlar için karıştır
        for (int i = 0; i < allChars.Count; i++)
        {
            int randomIndex = Random.Range(i, allChars.Count);
            CharacterType temp = allChars[i];
            allChars[i] = allChars[randomIndex];
            allChars[randomIndex] = temp;
        }

        // 3. Karakter atamalarını oluştur (Oyuncu 0 = Seçilen, Oyuncu 1-3 = Botlar)
        var assignments = new CharacterType[4];
        assignments[0] = selectedChar;
        assignments[1] = allChars[0];
        assignments[2] = allChars[1];
        assignments[3] = allChars[2];

        // 4. Atamaları static değişken olarak GameSetupManager'a kaydet
        GameSetupManager.SecilenKarakterler = assignments;

        Debug.Log($"[CharacterSelection] Oyuncu: {selectedChar} seçti. Botlar: {assignments[1]}, {assignments[2]}, {assignments[3]} olarak atandı.");

        // 5. Oyun sahnelerini yükle: Önce Oyun_Sahnesi (Görsel), ardından Scene_Core (Mantık - Additive)
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Scene_Core'u additive olarak yükle
            SceneManager.sceneLoaded += OnCoreSceneLoaded;
            SceneManager.LoadScene("Scene_Core", LoadSceneMode.Additive);
        }
    }

    private void OnCoreSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Scene_Core")
        {
            SceneManager.sceneLoaded -= OnCoreSceneLoaded;

            // Scene_Core içerisindeki çakışan kamerayı devre dışı bırak
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Main Camera")
                {
                    go.SetActive(false);
                    Debug.Log("[CharacterSelection] Scene_Core içerisindeki çakışan kamera devre dışı bırakıldı.");
                }
            }
        }
    }
}
