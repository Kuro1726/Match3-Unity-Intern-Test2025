using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardLayout", menuName = "Match 3/Board Layout")]
public class BoardLayoutSO : ScriptableObject
{
    [SerializeField, Min(0.1f)] private float m_snapStep = 0.5f;
    [SerializeField] private List<BoardItemPlacement> m_items = new List<BoardItemPlacement>();

    public float SnapStep => Mathf.Max(0.1f, m_snapStep);
    public IReadOnlyList<BoardItemPlacement> Items => m_items;
    public bool HasItems => m_items != null && m_items.Count > 0;

    public void SnapAllPositions()
    {
        if (m_items == null) return;
        foreach (BoardItemPlacement item in m_items)
        {
            if (item == null) continue;
            item.Layer = Mathf.Max(0, item.Layer);
            item.GridPosition = Snap(item.GridPosition);
        }
    }

    public Vector2 Snap(Vector2 position)
    {
        float step = SnapStep;
        return new Vector2(Mathf.Round(position.x / step) * step, Mathf.Round(position.y / step) * step);
    }

    public List<string> GetValidationErrors()
    {
        List<string> errors = new List<string>();
        if (HasItems == false)
        {
            errors.Add("Layout does not contain any items.");
            return errors;
        }

        Dictionary<NormalItem.eNormalType, int> totalTypeCounts = new Dictionary<NormalItem.eNormalType, int>();
        Dictionary<int, Dictionary<NormalItem.eNormalType, int>> typeCountsByLayer = new Dictionary<int, Dictionary<NormalItem.eNormalType, int>>();
        foreach (BoardItemPlacement item in m_items)
        {
            if (item == null)
            {
                errors.Add("Layout contains a null item entry.");
                continue;
            }

            if (item.Layer < 0) errors.Add("An item has a negative layer.");
            Vector2 snapped = Snap(item.GridPosition);
            if ((snapped - item.GridPosition).sqrMagnitude > 0.0001f)
            {
                errors.Add(string.Format("Item at {0} is not aligned to the {1:0.##} grid.", item.GridPosition, SnapStep));
            }

            int count;
            totalTypeCounts.TryGetValue(item.ItemType, out count);
            totalTypeCounts[item.ItemType] = count + 1;

            Dictionary<NormalItem.eNormalType, int> layerCounts;
            if (typeCountsByLayer.TryGetValue(item.Layer, out layerCounts) == false)
            {
                layerCounts = new Dictionary<NormalItem.eNormalType, int>();
                typeCountsByLayer[item.Layer] = layerCounts;
            }
            layerCounts.TryGetValue(item.ItemType, out count);
            layerCounts[item.ItemType] = count + 1;
        }

        for (int firstIndex = 0; firstIndex < m_items.Count; firstIndex++)
        {
            BoardItemPlacement first = m_items[firstIndex];
            if (first == null || first.Layer < 0) continue;
            for (int secondIndex = firstIndex + 1; secondIndex < m_items.Count; secondIndex++)
            {
                BoardItemPlacement second = m_items[secondIndex];
                if (second == null || second.Layer != first.Layer) continue;
                Vector2 distance = first.GridPosition - second.GridPosition;
                if (Mathf.Abs(distance.x) < 0.999f && Mathf.Abs(distance.y) < 0.999f)
                {
                    errors.Add(string.Format("Layer {0} items at {1} and {2} overlap.", first.Layer, first.GridPosition, second.GridPosition));
                }
            }
        }

        Array allTypes = Enum.GetValues(typeof(NormalItem.eNormalType));
        foreach (NormalItem.eNormalType itemType in allTypes)
        {
            int count;
            if (totalTypeCounts.TryGetValue(itemType, out count) == false || count == 0)
            {
                errors.Add(string.Format("Type {0} is missing. The initial board must contain all fish types.", (int)itemType + 1));
            }
        }

        foreach (KeyValuePair<NormalItem.eNormalType, int> pair in totalTypeCounts)
        {
            if (pair.Value % 3 != 0)
            {
                errors.Add(string.Format("Type {0} has {1} items in total; the total must be divisible by 3.", (int)pair.Key + 1, pair.Value));
            }
        }

        foreach (KeyValuePair<int, Dictionary<NormalItem.eNormalType, int>> layerPair in typeCountsByLayer)
        {
            foreach (KeyValuePair<NormalItem.eNormalType, int> typePair in layerPair.Value)
            {
                if (typePair.Value % 3 != 0)
                {
                    errors.Add(string.Format("Layer {0}, Type {1} has {2} items; Autoplay requires this count to be divisible by 3.", layerPair.Key, (int)typePair.Key + 1, typePair.Value));
                }
            }
        }
        return errors;
    }

    public List<string> GetValidationErrors(int boardSizeX, int boardSizeY)
    {
        List<string> errors = GetValidationErrors();
        if (m_items == null) return errors;
        foreach (BoardItemPlacement item in m_items)
        {
            if (item == null || item.Layer < 0) continue;
            float min = item.Layer * 0.5f;
            float maxX = boardSizeX - 1f - min;
            float maxY = boardSizeY - 1f - min;
            Vector2 position = item.GridPosition;
            if (maxX < min || maxY < min || position.x < min - 0.001f || position.x > maxX + 0.001f || position.y < min - 0.001f || position.y > maxY + 0.001f)
            {
                errors.Add(string.Format("Layer {0} item at {1} is outside the {2} x {3} board.", item.Layer, position, boardSizeX, boardSizeY));
            }
        }
        return errors;
    }
}

[Serializable]
public class BoardItemPlacement
{
    public NormalItem.eNormalType ItemType;
    [Min(0)] public int Layer;
    public Vector2 GridPosition;
}
