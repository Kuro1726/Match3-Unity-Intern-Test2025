using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class BoardLayoutEditorValidation
{
    public static List<string> GetErrors(BoardLayoutSO layout)
    {
        if (layout == null) return new List<string> { "Board Layout is missing." };
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        return settings == null
            ? layout.GetValidationErrors()
            : layout.GetValidationErrors(settings.BoardSizeX, settings.BoardSizeY);
    }

    public static string FormatErrors(List<string> errors, int maximum = 8)
    {
        return string.Join("\n", errors.Take(maximum).Select(error => "- " + error).ToArray());
    }
}

public class BoardLayoutSaveValidator : UnityEditor.AssetModificationProcessor
{
    private static string[] OnWillSaveAssets(string[] paths)
    {
        List<string> allowedPaths = new List<string>(paths.Length);
        List<string> blockedMessages = new List<string>();
        foreach (string path in paths)
        {
            BoardLayoutSO layout = AssetDatabase.LoadAssetAtPath<BoardLayoutSO>(path);
            if (layout == null)
            {
                allowedPaths.Add(path);
                continue;
            }

            List<string> errors = BoardLayoutEditorValidation.GetErrors(layout);
            if (errors.Count == 0)
            {
                allowedPaths.Add(path);
                continue;
            }

            string message = layout.name + ":\n" + BoardLayoutEditorValidation.FormatErrors(errors);
            blockedMessages.Add(message);
            Debug.LogError("Board Layout was not saved because it is invalid.\n" + message, layout);
        }

        if (blockedMessages.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Invalid Board Layout - Save Blocked",
                string.Join("\n\n", blockedMessages.ToArray()),
                "OK");
        }
        return allowedPaths.ToArray();
    }
}
