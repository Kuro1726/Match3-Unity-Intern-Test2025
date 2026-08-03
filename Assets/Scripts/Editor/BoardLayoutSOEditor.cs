using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(BoardLayoutSO))]
public class BoardLayoutSOEditor : Editor
{
    private static readonly string[] TypeNames =
    {
        "Type 1 - Brown", "Type 2 - Red", "Type 3 - Yellow", "Type 4 - Blue",
        "Type 5 - Green", "Type 6 - Purple", "Type 7 - Pink"
    };

    private static readonly Color[] TypeColors =
    {
        new Color(0.43f, 0.24f, 0.11f),
        new Color(0.88f, 0.12f, 0.10f),
        new Color(1.00f, 0.76f, 0.05f),
        new Color(0.10f, 0.45f, 0.90f),
        new Color(0.12f, 0.68f, 0.25f),
        new Color(0.53f, 0.22f, 0.72f),
        new Color(0.96f, 0.30f, 0.65f)
    };

    private SerializedProperty m_snapStep;
    private SerializedProperty m_items;
    private ReorderableList m_itemList;
    private bool m_showPreview = true;
    private bool m_showLegend = true;
    private int m_layerFilter = -1;
    private string m_lastEditError;

    private void OnEnable()
    {
        m_snapStep = serializedObject.FindProperty("m_snapStep");
        m_items = serializedObject.FindProperty("m_items");
        m_itemList = new ReorderableList(serializedObject, m_items, false, true, true, true);
        m_itemList.drawHeaderCallback = DrawListHeader;
        m_itemList.drawElementCallback = DrawListElement;
        m_itemList.elementHeightCallback = GetElementHeight;
        m_itemList.onAddCallback = AddItem;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Board Layout Authoring", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_snapStep, new GUIContent("Position Snap", "Every edited position is rounded to this step."));
        DrawLayerToolbar();

        m_showLegend = EditorGUILayout.Foldout(m_showLegend, "Type Color Legend", true);
        if (m_showLegend) DrawLegend();

        m_showPreview = EditorGUILayout.Foldout(m_showPreview, "Board Preview", true);
        if (m_showPreview) DrawPreview();

        EditorGUILayout.Space();
        m_itemList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
        if (string.IsNullOrEmpty(m_lastEditError) == false)
        {
            EditorGUILayout.HelpBox(m_lastEditError, MessageType.Warning);
        }

        DrawActionButtons();
        DrawValidation();
    }

    private void DrawLayerToolbar()
    {
        int maxLayer = GetMaxLayer();
        int layerCount = Mathf.Max(1, maxLayer + 1);
        string[] options = new string[layerCount + 1];
        options[0] = "All";
        for (int layer = 0; layer < layerCount; layer++) options[layer + 1] = "Layer " + layer;

        EditorGUILayout.LabelField(string.Format("{0} items | {1} layer(s)", m_items.arraySize, maxLayer + 1), EditorStyles.miniBoldLabel);
        int selected = Mathf.Clamp(m_layerFilter + 1, 0, options.Length - 1);
        int changed = GUILayout.Toolbar(selected, options);
        if (changed != selected)
        {
            m_layerFilter = changed - 1;
            m_itemList.index = -1;
        }
    }

    private void DrawLegend()
    {
        Rect area = GUILayoutUtility.GetRect(100f, 58f, GUILayout.ExpandWidth(true));
        float width = area.width / 4f;
        for (int i = 0; i < TypeNames.Length; i++)
        {
            int row = i / 4;
            int column = i % 4;
            Rect entry = new Rect(area.x + column * width, area.y + row * 28f, width, 24f);
            Rect swatch = new Rect(entry.x + 2f, entry.y + 4f, 16f, 16f);
            EditorGUI.DrawRect(swatch, TypeColors[i]);
            EditorGUI.LabelField(new Rect(entry.x + 22f, entry.y, entry.width - 22f, entry.height), "T" + (i + 1) + " " + GetColorName(i), EditorStyles.miniLabel);
        }
    }

    private float GetElementHeight(int index)
    {
        if (IsElementVisible(index) == false) return 0f;
        return EditorGUIUtility.singleLineHeight + 8f;
    }

    private bool IsElementVisible(int index)
    {
        if (m_layerFilter < 0) return true;
        SerializedProperty element = m_items.GetArrayElementAtIndex(index);
        return element.FindPropertyRelative("Layer").intValue == m_layerFilter;
    }

    private void DrawListHeader(Rect rect)
    {
        const float swatchWidth = 20f;
        const float layerWidth = 48f;
        const float gap = 5f;
        float typeWidth = Mathf.Max(105f, rect.width * 0.34f);
        EditorGUI.LabelField(new Rect(rect.x + swatchWidth, rect.y, typeWidth - swatchWidth - gap, rect.height), "Item Type", EditorStyles.boldLabel);
        EditorGUI.LabelField(new Rect(rect.x + typeWidth, rect.y, layerWidth, rect.height), "Layer", EditorStyles.boldLabel);
        EditorGUI.LabelField(new Rect(rect.x + typeWidth + layerWidth + gap, rect.y, rect.width - typeWidth - layerWidth - gap, rect.height), "Grid Position", EditorStyles.boldLabel);
    }

    private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (IsElementVisible(index) == false) return;
        SerializedProperty element = m_items.GetArrayElementAtIndex(index);
        SerializedProperty type = element.FindPropertyRelative("ItemType");
        SerializedProperty layer = element.FindPropertyRelative("Layer");
        SerializedProperty position = element.FindPropertyRelative("GridPosition");
        int typeIndex = Mathf.Clamp(type.enumValueIndex, 0, TypeColors.Length - 1);

        Color background = TypeColors[typeIndex];
        background.a = isActive ? 0.34f : 0.16f;
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 2f), background);
        rect.y += 4f;
        rect.height = EditorGUIUtility.singleLineHeight;

        const float swatchWidth = 20f;
        const float layerWidth = 48f;
        const float gap = 5f;
        float typeWidth = Mathf.Max(105f, rect.width * 0.34f);
        Rect swatch = new Rect(rect.x + 3f, rect.y + 2f, 14f, 14f);
        EditorGUI.DrawRect(swatch, TypeColors[typeIndex]);
        Rect typeRect = new Rect(rect.x + swatchWidth, rect.y, typeWidth - swatchWidth - gap, rect.height);
        Rect layerRect = new Rect(rect.x + typeWidth, rect.y, layerWidth, rect.height);
        Rect positionRect = new Rect(layerRect.xMax + gap, rect.y, rect.xMax - layerRect.xMax - gap, rect.height);

        type.enumValueIndex = EditorGUI.Popup(typeRect, type.enumValueIndex, TypeNames);
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        int maxLayer = GetMaxSupportedLayer(settings);
        EditorGUI.BeginChangeCheck();
        int editedLayer = EditorGUI.IntField(layerRect, layer.intValue);
        Vector2 edited = EditorGUI.Vector2Field(positionRect, GUIContent.none, position.vector2Value);
        if (EditorGUI.EndChangeCheck())
        {
            int candidateLayer = Mathf.Clamp(editedLayer, 0, maxLayer);
            Vector2 snapped = ((BoardLayoutSO)target).Snap(edited);
            Vector2 candidatePosition = ClampPositionToBoard(snapped, candidateLayer, settings);
            if (HasSameLayerOverlap(index, candidatePosition, candidateLayer))
            {
                m_lastEditError = string.Format("Cannot move item {0}: Layer {1} position {2} overlaps another item.", index, candidateLayer, candidatePosition);
            }
            else
            {
                layer.intValue = candidateLayer;
                position.vector2Value = candidatePosition;
                m_lastEditError = null;
            }
        }
    }

    private void AddItem(ReorderableList list)
    {
        int layer = m_layerFilter >= 0 ? m_layerFilter : Mathf.Max(0, GetMaxLayer());
        Vector2 freePosition;
        if (TryFindFreePosition(layer, out freePosition) == false)
        {
            EditorUtility.DisplayDialog("Layer Is Full", "There is no free valid position in Layer " + layer + ". Remove an item or add a new layer first.", "OK");
            return;
        }
        int index = m_items.arraySize;
        m_items.InsertArrayElementAtIndex(index);
        SerializedProperty item = m_items.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("ItemType").enumValueIndex = 0;
        item.FindPropertyRelative("Layer").intValue = layer;
        item.FindPropertyRelative("GridPosition").vector2Value = freePosition;
        list.index = index;
    }

    private bool TryFindFreePosition(int layer, out Vector2 freePosition)
    {
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        int width = settings == null ? 5 : Mathf.Max(1, settings.BoardSizeX - layer);
        int height = settings == null ? 5 : Mathf.Max(1, settings.BoardSizeY - layer);
        float offset = layer * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 candidate = new Vector2(x + offset, y + offset);
                if (HasSameLayerOverlap(-1, candidate, layer) == false)
                {
                    freePosition = candidate;
                    return true;
                }
            }
        }
        freePosition = default(Vector2);
        return false;
    }

    private bool HasSameLayerOverlap(int ignoredIndex, Vector2 position, int layer)
    {
        for (int i = 0; i < m_items.arraySize; i++)
        {
            if (i == ignoredIndex) continue;
            SerializedProperty item = m_items.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("Layer").intValue != layer) continue;
            Vector2 otherPosition = item.FindPropertyRelative("GridPosition").vector2Value;
            Vector2 distance = position - otherPosition;
            if (Mathf.Abs(distance.x) < 0.999f && Mathf.Abs(distance.y) < 0.999f) return true;
        }
        return false;
    }

    private void DrawActionButtons()
    {
        BoardLayoutSO layout = (BoardLayoutSO)target;
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Add New Layer", GUILayout.Height(30f))) AddNewLayer(layout);
            int maxLayer = GetMaxLayer();
            int removeLayer = m_layerFilter >= 0 ? m_layerFilter : maxLayer;
            using (new EditorGUI.DisabledScope(maxLayer <= 0 || removeLayer < 0))
            {
                if (GUILayout.Button("- Remove Layer " + removeLayer, GUILayout.Height(30f))) RemoveLayer(layout, removeLayer);
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap All Positions"))
            {
                Undo.RecordObject(layout, "Snap board layout");
                layout.SnapAllPositions();
                EditorUtility.SetDirty(layout);
            }
            if (GUILayout.Button("Regenerate Full Layout")) GenerateFromGameSettings(layout);
        }
        if (GUILayout.Button("Assign This Layout To Game Settings")) AssignToGameSettings(layout);
        DrawOutOfBoundsRepair(layout);
    }

    private void AddNewLayer(BoardLayoutSO layout)
    {
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Board Layout", "Game Settings could not be found in Resources.", "OK");
            return;
        }

        int newLayer = GetMaxLayer() + 1;
        int width = settings.BoardSizeX - newLayer;
        int height = settings.BoardSizeY - newLayer;
        int playableCount = width > 0 && height > 0 ? 3 * (width * height / 3) : 0;
        if (playableCount < 3)
        {
            EditorUtility.DisplayDialog("Cannot Add Layer", "Board Size is too small to add another layer containing a valid triple.", "OK");
            return;
        }

        Undo.RecordObject(layout, "Add board layer");
        Undo.RecordObject(settings, "Increase board layer count");
        serializedObject.Update();
        AppendGeneratedLayer(settings, newLayer);
        serializedObject.ApplyModifiedProperties();
        settings.BoardLayerCount = Mathf.Max(settings.BoardLayerCount, newLayer + 1);
        EditorUtility.SetDirty(layout);
        EditorUtility.SetDirty(settings);
        m_layerFilter = newLayer;
        m_itemList.index = -1;
    }

    private void RemoveLayer(BoardLayoutSO layout, int removedLayer)
    {
        int maxLayer = GetMaxLayer();
        if (maxLayer <= 0 || removedLayer < 0 || removedLayer > maxLayer) return;
        if (EditorUtility.DisplayDialog(
            "Remove Layer " + removedLayer,
            "Remove every item in Layer " + removedLayer + "? Higher layers will move down automatically.",
            "Remove Layer",
            "Cancel") == false) return;

        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        Undo.RecordObject(layout, "Remove board layer");
        if (settings != null) Undo.RecordObject(settings, "Decrease board layer count");
        serializedObject.Update();

        for (int i = m_items.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty item = m_items.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("Layer").intValue == removedLayer)
            {
                m_items.DeleteArrayElementAtIndex(i);
            }
        }

        for (int i = 0; i < m_items.arraySize; i++)
        {
            SerializedProperty item = m_items.GetArrayElementAtIndex(i);
            SerializedProperty layer = item.FindPropertyRelative("Layer");
            if (layer.intValue <= removedLayer) continue;
            layer.intValue--;
            SerializedProperty position = item.FindPropertyRelative("GridPosition");
            position.vector2Value -= Vector2.one * 0.5f;
        }

        serializedObject.ApplyModifiedProperties();
        int newMaxLayer = GetMaxLayer();
        if (settings != null)
        {
            settings.BoardLayerCount = Mathf.Max(1, newMaxLayer + 1);
            EditorUtility.SetDirty(settings);
        }
        EditorUtility.SetDirty(layout);
        if (m_layerFilter >= 0) m_layerFilter = Mathf.Clamp(removedLayer, 0, newMaxLayer);
        m_itemList.index = -1;
        m_lastEditError = null;
    }

    private void GenerateFromGameSettings(BoardLayoutSO layout)
    {
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        if (settings == null) return;
        if (m_items.arraySize > 0 && EditorUtility.DisplayDialog("Replace Layout", "Replace every item in this layout?", "Replace", "Cancel") == false) return;

        Undo.RecordObject(layout, "Generate board layout");
        serializedObject.Update();
        m_items.ClearArray();
        for (int layer = 0; layer < Mathf.Max(1, settings.BoardLayerCount); layer++)
        {
            if (AppendGeneratedLayer(settings, layer) == false) break;
        }
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(layout);
        m_layerFilter = -1;
    }

    private bool AppendGeneratedLayer(GameSettings settings, int layer)
    {
        int width = settings.BoardSizeX - layer;
        int height = settings.BoardSizeY - layer;
        if (width <= 0 || height <= 0) return false;
        int playableCount = 3 * (width * height / 3);
        if (playableCount < 3) return false;

        List<NormalItem.eNormalType> types = CreateBalancedTypes(playableCount);
        for (int index = 0; index < playableCount; index++)
        {
            int itemIndex = m_items.arraySize;
            m_items.InsertArrayElementAtIndex(itemIndex);
            SerializedProperty item = m_items.GetArrayElementAtIndex(itemIndex);
            item.FindPropertyRelative("ItemType").enumValueIndex = (int)types[index];
            item.FindPropertyRelative("Layer").intValue = layer;
            int x = index % width;
            int y = index / width;
            item.FindPropertyRelative("GridPosition").vector2Value = new Vector2(x + layer * 0.5f, y + layer * 0.5f);
        }
        return true;
    }

    private static List<NormalItem.eNormalType> CreateBalancedTypes(int itemCount)
    {
        Array values = Enum.GetValues(typeof(NormalItem.eNormalType));
        List<NormalItem.eNormalType> types = new List<NormalItem.eNormalType>(itemCount);
        for (int group = 0; group < itemCount / 3; group++)
        {
            NormalItem.eNormalType type = (NormalItem.eNormalType)values.GetValue(group % values.Length);
            types.Add(type); types.Add(type); types.Add(type);
        }
        for (int i = types.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            NormalItem.eNormalType temp = types[i]; types[i] = types[randomIndex]; types[randomIndex] = temp;
        }
        return types;
    }

    private static void AssignToGameSettings(BoardLayoutSO layout)
    {
        List<string> errors = BoardLayoutEditorValidation.GetErrors(layout);
        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("Invalid Board Layout", "Fix these errors before assigning the layout:\n\n" + BoardLayoutEditorValidation.FormatErrors(errors), "OK");
            return;
        }
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Board Layout", "Game Settings could not be found in Resources.", "OK");
            return;
        }
        Undo.RecordObject(settings, "Assign board layout");
        settings.BoardLayout = layout;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private void DrawValidation()
    {
        BoardLayoutSO layout = (BoardLayoutSO)target;
        List<string> errors = BoardLayoutEditorValidation.GetErrors(layout);
        if (errors.Count == 0)
        {
            EditorGUILayout.HelpBox("Valid layout: no overlaps, all items are inside the board, and every Type count is divisible by 3 globally and per layer.", MessageType.Info);
            return;
        }
        foreach (string error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
    }

    private void DrawOutOfBoundsRepair(BoardLayoutSO layout)
    {
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        int outsideCount = CountOutOfBoundsItems(settings);
        if (outsideCount == 0) return;
        EditorGUILayout.HelpBox(string.Format("{0} item(s) are outside the board. They are ignored by the preview and should be removed.", outsideCount), MessageType.Warning);
        if (GUILayout.Button("Remove Items Outside Board") &&
            EditorUtility.DisplayDialog("Remove Outside Items", "Remove every item whose position is outside the board?", "Remove", "Cancel"))
        {
            Undo.RecordObject(layout, "Remove items outside board");
            serializedObject.Update();
            for (int i = m_items.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty item = m_items.GetArrayElementAtIndex(i);
                if (IsOutsideBoard(item, settings)) m_items.DeleteArrayElementAtIndex(i);
            }
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(layout);
            m_itemList.index = -1;
        }
    }

    private void DrawPreview()
    {
        BoardLayoutSO layout = (BoardLayoutSO)target;
        GameSettings settings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        Rect area = GUILayoutUtility.GetRect(100f, 340f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(area, new Color(0.10f, 0.11f, 0.14f));
        List<BoardItemPlacement> sorted = new List<BoardItemPlacement>(layout.Items);
        sorted.RemoveAll(item => item == null || (m_layerFilter >= 0 && item.Layer != m_layerFilter) || IsOutsideBoard(item, settings));
        if (sorted.Count == 0)
        {
            GUI.Label(area, "No items in this layer", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        float minX = 0f, minY = 0f;
        float maxX = settings == null ? float.MinValue : settings.BoardSizeX - 1f;
        float maxY = settings == null ? float.MinValue : settings.BoardSizeY - 1f;
        if (settings == null)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            foreach (BoardItemPlacement item in sorted)
            {
                minX = Mathf.Min(minX, item.GridPosition.x); maxX = Mathf.Max(maxX, item.GridPosition.x);
                minY = Mathf.Min(minY, item.GridPosition.y); maxY = Mathf.Max(maxY, item.GridPosition.y);
            }
        }
        float scale = Mathf.Min((area.width - 24f) / Mathf.Max(1f, maxX - minX + 1f), (area.height - 24f) / Mathf.Max(1f, maxY - minY + 1f));
        sorted.Sort((a, b) => a.Layer.CompareTo(b.Layer));
        foreach (BoardItemPlacement item in sorted)
        {
            float x = area.x + 12f + (item.GridPosition.x - minX) * scale;
            float y = area.yMax - 12f - (item.GridPosition.y - minY + 1f) * scale;
            Rect border = new Rect(x + 1f, y + 1f, scale - 2f, scale - 2f);
            EditorGUI.DrawRect(border, Color.black);
            Rect itemRect = new Rect(border.x + 2f, border.y + 2f, border.width - 4f, border.height - 4f);
            int typeIndex = Mathf.Clamp((int)item.ItemType, 0, TypeColors.Length - 1);
            EditorGUI.DrawRect(itemRect, TypeColors[typeIndex]);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = typeIndex == 2 ? Color.black : Color.white;
            GUI.Label(itemRect, string.Format("T{0}\nL{1}", typeIndex + 1, item.Layer), labelStyle);
        }
    }

    private int GetMaxLayer()
    {
        int maxLayer = -1;
        for (int i = 0; i < m_items.arraySize; i++)
        {
            int layer = m_items.GetArrayElementAtIndex(i).FindPropertyRelative("Layer").intValue;
            maxLayer = Mathf.Max(maxLayer, layer);
        }
        return maxLayer;
    }

    private int GetMaxSupportedLayer(GameSettings settings)
    {
        if (settings == null) return Mathf.Max(0, GetMaxLayer());
        int maxLayer = 0;
        int limit = Mathf.Min(settings.BoardSizeX, settings.BoardSizeY);
        for (int layer = 0; layer < limit; layer++)
        {
            int width = settings.BoardSizeX - layer;
            int height = settings.BoardSizeY - layer;
            if (3 * (width * height / 3) < 3) break;
            maxLayer = layer;
        }
        return maxLayer;
    }

    private static Vector2 ClampPositionToBoard(Vector2 position, int layer, GameSettings settings)
    {
        if (settings == null) return position;
        float min = layer * 0.5f;
        float maxX = settings.BoardSizeX - 1f - min;
        float maxY = settings.BoardSizeY - 1f - min;
        return new Vector2(Mathf.Clamp(position.x, min, maxX), Mathf.Clamp(position.y, min, maxY));
    }

    private int CountOutOfBoundsItems(GameSettings settings)
    {
        if (settings == null) return 0;
        int count = 0;
        for (int i = 0; i < m_items.arraySize; i++)
        {
            if (IsOutsideBoard(m_items.GetArrayElementAtIndex(i), settings)) count++;
        }
        return count;
    }

    private static bool IsOutsideBoard(SerializedProperty item, GameSettings settings)
    {
        if (settings == null) return false;
        int layer = item.FindPropertyRelative("Layer").intValue;
        Vector2 position = item.FindPropertyRelative("GridPosition").vector2Value;
        return IsOutsideBoard(position, layer, settings);
    }

    private static bool IsOutsideBoard(BoardItemPlacement item, GameSettings settings)
    {
        return item == null || IsOutsideBoard(item.GridPosition, item.Layer, settings);
    }

    private static bool IsOutsideBoard(Vector2 position, int layer, GameSettings settings)
    {
        if (settings == null || layer < 0) return layer < 0;
        float min = layer * 0.5f;
        float maxX = settings.BoardSizeX - 1f - min;
        float maxY = settings.BoardSizeY - 1f - min;
        return maxX < min || maxY < min || position.x < min - 0.001f || position.x > maxX + 0.001f || position.y < min - 0.001f || position.y > maxY + 0.001f;
    }

    private static string PositionKey(Vector2 position)
    {
        return string.Format("{0:0.###}:{1:0.###}", position.x, position.y);
    }

    private static string GetColorName(int typeIndex)
    {
        string fullName = TypeNames[Mathf.Clamp(typeIndex, 0, TypeNames.Length - 1)];
        int separator = fullName.IndexOf(" - ", StringComparison.Ordinal);
        return separator < 0 ? fullName : fullName.Substring(separator + 3);
    }
}
