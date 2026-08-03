using UnityEngine;
using UnityEditor;

public class MainToolMenu
{
    private const string pathResourcesFolder = "Assets/Resources";

    private const string menuTitle = "Game Tools/";

    [MenuItem(menuTitle + "!! Create Game Settings !!", false, 530)]
    static void CreateGameData()
    {
        GameSettings asset = ScriptableObject.CreateInstance<GameSettings>();

        AssetDatabase.CreateAsset(asset, "Assets/Resources/gamesettings.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }


    [MenuItem(menuTitle + "Open Game Settings", false, 410)]
    static void OpenGameData()
    {
        GameSettings asset = Resources.Load<GameSettings>("gamesettings");

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    [MenuItem(menuTitle + "Open Board Layout", false, 420)]
    static void OpenBoardLayout()
    {
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        if (settings == null || settings.BoardLayout == null)
        {
            EditorUtility.DisplayDialog("Board Layout", "Assign a BoardLayoutSO in Game Settings first.", "OK");
            return;
        }

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = settings.BoardLayout;
    }

}
