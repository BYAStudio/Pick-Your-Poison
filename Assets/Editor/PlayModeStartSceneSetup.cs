using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Unity Editor'de hangi sahne açık olursa olsun, Play tuşuna basıldığında
/// oyunun her zaman Ana_Menu sahnesinden başlamasını sağlar.
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartSceneSetup
{
    static PlayModeStartSceneSetup()
    {
        string scenePath = "Assets/Scenes/Ana_Menu.unity";
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset != null)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }
}
