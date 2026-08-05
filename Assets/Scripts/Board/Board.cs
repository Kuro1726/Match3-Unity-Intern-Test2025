using System;
using System.Collections.Generic;
using UnityEngine;

public class Board
{
    private readonly int m_boardSizeX;
    private readonly int m_boardSizeY;
    private readonly int m_matchSize = 3;
    private readonly int m_layerCount;
    private readonly BoardLayoutSO m_layout;
    private readonly float m_itemBackgroundScale;
    private readonly float m_itemBackgroundOpacity;
    private readonly List<Cell[,]> m_layers = new List<Cell[,]>();
    private readonly List<Cell> m_cells = new List<Cell>();
    private readonly Dictionary<Cell, NormalItem.eNormalType> m_layoutItemTypes = new Dictionary<Cell, NormalItem.eNormalType>();
    private readonly Transform m_root;
    private int m_remainingItemCount;
    private int m_generationTypeCursor;

    public int RemainingItemCount => m_remainingItemCount;

    public Board(Transform root, GameSettings gameSettings)
    {
        m_root = root;
        m_boardSizeX = gameSettings.BoardSizeX;
        m_boardSizeY = gameSettings.BoardSizeY;
        m_layerCount = Mathf.Max(1, gameSettings.BoardLayerCount);
        m_layout = gameSettings.BoardLayout;
        m_itemBackgroundScale = gameSettings.ItemBackgroundScale;
        m_itemBackgroundOpacity = gameSettings.ItemBackgroundOpacity;
        if (m_layout != null && m_layout.HasItems) CreateBoardFromLayout();
        else CreateGeneratedBoard();
    }

    private void CreateGeneratedBoard()
    {
        Vector3 origin = new Vector3(-m_boardSizeX * 0.5f + 0.5f, -m_boardSizeY * 0.5f + 0.5f, 0f);
        for (int layer = 0; layer < m_layerCount; layer++)
        {
            int width = m_boardSizeX - layer;
            int height = m_boardSizeY - layer;
            if (width <= 0 || height <= 0) break;

            Cell[,] layerCells = new Cell[width, height];
            Vector3 layerOffset = new Vector3(layer * 0.5f, layer * 0.5f, layer * -0.01f);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    layerCells[x, y] = CreateCell(x, y, layer, origin + layerOffset + new Vector3(x, y, 0f));
                }
            }

            if (layer > 0) AddBlockRelations(layerCells, m_layers[layer - 1]);
            m_layers.Add(layerCells);
        }
    }

    private void CreateBoardFromLayout()
    {
        Vector3 origin = new Vector3(-m_boardSizeX * 0.5f + 0.5f, -m_boardSizeY * 0.5f + 0.5f, 0f);
        foreach (string error in m_layout.GetValidationErrors(m_boardSizeX, m_boardSizeY))
        {
            Debug.LogWarning("Board layout: " + error);
        }

        foreach (BoardItemPlacement placement in m_layout.Items)
        {
            if (placement == null) continue;
            Vector2 gridPosition = m_layout.Snap(placement.GridPosition);
            int boardX = Mathf.RoundToInt(gridPosition.x * 2f);
            int boardY = Mathf.RoundToInt(gridPosition.y * 2f);
            Vector3 worldPosition = origin + new Vector3(gridPosition.x, gridPosition.y, placement.Layer * -0.01f);
            Cell cell = CreateCell(boardX, boardY, Mathf.Max(0, placement.Layer), worldPosition);
            m_layoutItemTypes[cell] = placement.ItemType;
        }

        AddLayoutBlockRelations();
    }

    private void AddLayoutBlockRelations()
    {
        foreach (Cell lowerCell in m_cells)
        {
            foreach (Cell upperCell in m_cells)
            {
                if (upperCell.BoardLayer <= lowerCell.BoardLayer) continue;
                Vector3 offset = upperCell.transform.position - lowerCell.transform.position;
                if (Mathf.Abs(offset.x) < 0.999f && Mathf.Abs(offset.y) < 0.999f)
                {
                    lowerCell.AddBlocker(upperCell);
                }
            }
        }
    }

    private Cell CreateCell(int x, int y, int layer, Vector3 position)
    {
        GameObject prefab = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        GameObject view = GameObject.Instantiate(prefab, position, Quaternion.identity, m_root);
        view.name = string.Format("BoardCell_L{0}_{1}_{2}", layer, x, y);
        Cell cell = view.GetComponent<Cell>();
        cell.Setup(x, y, layer);
        SpriteRenderer renderer = view.GetComponent<SpriteRenderer>();
        if (renderer)
        {
            renderer.sortingOrder = layer * 10;
            renderer.enabled = false;
        }
        m_cells.Add(cell);
        return cell;
    }

    private void AddBlockRelations(Cell[,] upperLayer, Cell[,] lowerLayer)
    {
        for (int x = 0; x < upperLayer.GetLength(0); x++)
        {
            for (int y = 0; y < upperLayer.GetLength(1); y++)
            {
                Cell blocker = upperLayer[x, y];
                lowerLayer[x, y].AddBlocker(blocker);
                lowerLayer[x + 1, y].AddBlocker(blocker);
                lowerLayer[x, y + 1].AddBlocker(blocker);
                lowerLayer[x + 1, y + 1].AddBlocker(blocker);
            }
        }
    }
    public void Fill()
    {
        m_remainingItemCount = 0;
        if (m_layoutItemTypes.Count > 0)
        {
            FillLayout();
        }
        else
        {
            m_generationTypeCursor = 0;
            foreach (Cell[,] layerCells in m_layers)
            {
                FillLayer(layerCells);
            }
        }
        RefreshBlockedVisuals();
    }

    private void FillLayout()
    {
        foreach (KeyValuePair<Cell, NormalItem.eNormalType> entry in m_layoutItemTypes)
        {
            AssignItem(entry.Key, entry.Value);
        }
    }

    private void FillLayer(Cell[,] layerCells)
    {
        int cellCount = layerCells.GetLength(0) * layerCells.GetLength(1);
        int playableCount = m_matchSize * (cellCount / m_matchSize);
        List<NormalItem.eNormalType> itemTypes = CreateBalancedItemTypes(playableCount);
        int typeIndex = 0;
        for (int y = 0; y < layerCells.GetLength(1); y++)
        {
            for (int x = 0; x < layerCells.GetLength(0); x++)
            {
                if (typeIndex >= itemTypes.Count) return;
                AssignItem(layerCells[x, y], itemTypes[typeIndex++]);
            }
        }
    }

    private void AssignItem(Cell cell, NormalItem.eNormalType itemType)
    {
        NormalItem item = new NormalItem();
        item.SetType(itemType);
        item.SetView();
        item.ConfigureBackground(m_itemBackgroundScale, m_itemBackgroundOpacity);
        item.SetViewRoot(m_root);
        cell.Assign(item);
        cell.ApplyItemPosition(false);
        m_remainingItemCount++;
    }
    private List<NormalItem.eNormalType> CreateBalancedItemTypes(int itemCount)
    {
        Array values = Enum.GetValues(typeof(NormalItem.eNormalType));
        List<NormalItem.eNormalType> result = new List<NormalItem.eNormalType>(itemCount);
        int groupCount = itemCount / m_matchSize;
        for (int group = 0; group < groupCount; group++)
        {
            int typeIndex = m_generationTypeCursor % values.Length;
            NormalItem.eNormalType type = (NormalItem.eNormalType)values.GetValue(typeIndex);
            for (int i = 0; i < m_matchSize; i++) result.Add(type);
            m_generationTypeCursor++;
        }

        for (int i = result.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            NormalItem.eNormalType temp = result[i];
            result[i] = result[randomIndex];
            result[randomIndex] = temp;
        }
        return result;
    }
    public bool TryTakeItem(Cell cell, out Item item)
    {
        item = null;
        if (IsCellSelectable(cell) == false) return false;
        item = cell.Item;
        cell.Free();
        m_remainingItemCount--;
        RefreshBlockedVisuals();
        return true;
    }

    public bool TryReturnItem(Cell cell, Item item)
    {
        if (Contains(cell) == false || cell.IsEmpty == false || item == null) return false;
        cell.Assign(item);
        m_remainingItemCount++;
        RefreshBlockedVisuals();
        return true;
    }

    public bool IsCellSelectable(Cell cell)
    {
        return Contains(cell) && cell.IsEmpty == false && cell.IsBlocked == false;
    }

    public bool TryBuildAutoWinPlan(int trayCapacity, out List<Cell> plan)
    {
        plan = new List<Cell>();
        List<Cell> activeCells = new List<Cell>();
        foreach (Cell cell in m_cells)
        {
            if (cell != null && cell.IsEmpty == false) activeCells.Add(cell);
        }

        if (activeCells.Count == 0) return true;
        if (activeCells.Count > 63 || trayCapacity < m_matchSize) return false;

        Dictionary<Cell, int> cellIndices = new Dictionary<Cell, int>();
        for (int index = 0; index < activeCells.Count; index++)
        {
            cellIndices[activeCells[index]] = index;
        }

        ulong[] blockerMasks = new ulong[activeCells.Count];
        int[] itemTypes = new int[activeCells.Count];
        for (int index = 0; index < activeCells.Count; index++)
        {
            Cell cell = activeCells[index];
            NormalItem item = cell.Item as NormalItem;
            if (item == null) return false;
            itemTypes[index] = (int)item.ItemType;

            foreach (Cell blocker in cell.Blockers)
            {
                int blockerIndex;
                if (blocker != null && blocker.IsEmpty == false && cellIndices.TryGetValue(blocker, out blockerIndex))
                {
                    blockerMasks[index] |= 1UL << blockerIndex;
                }
            }
        }

        int typeCount = Enum.GetValues(typeof(NormalItem.eNormalType)).Length;
        int[] trayTypeCounts = new int[typeCount];
        List<int> path = new List<int>(activeCells.Count);
        HashSet<AutoWinState> failedStates = new HashSet<AutoWinState>();
        ulong remainingMask = (1UL << activeCells.Count) - 1UL;
        if (FindAutoWinPath(remainingMask, 0, trayCapacity, activeCells, blockerMasks, itemTypes, trayTypeCounts, path, failedStates) == false)
        {
            return false;
        }

        foreach (int cellIndex in path) plan.Add(activeCells[cellIndex]);
        return true;
    }

    private bool FindAutoWinPath(
        ulong remainingMask,
        int trayCount,
        int trayCapacity,
        List<Cell> activeCells,
        ulong[] blockerMasks,
        int[] itemTypes,
        int[] trayTypeCounts,
        List<int> path,
        HashSet<AutoWinState> failedStates)
    {
        if (remainingMask == 0UL) return trayCount == 0;

        AutoWinState state = new AutoWinState(remainingMask, EncodeTrayCounts(trayTypeCounts));
        if (failedStates.Contains(state)) return false;

        List<int> candidates = new List<int>();
        for (int index = 0; index < activeCells.Count; index++)
        {
            ulong cellBit = 1UL << index;
            if ((remainingMask & cellBit) == 0UL) continue;
            if ((remainingMask & blockerMasks[index]) != 0UL) continue;
            candidates.Add(index);
        }

        candidates.Sort((left, right) =>
        {
            int leftType = itemTypes[left];
            int rightType = itemTypes[right];
            int trayPriority = trayTypeCounts[rightType].CompareTo(trayTypeCounts[leftType]);
            if (trayPriority != 0) return trayPriority;
            return activeCells[right].BoardLayer.CompareTo(activeCells[left].BoardLayer);
        });

        foreach (int cellIndex in candidates)
        {
            int itemType = itemTypes[cellIndex];
            int previousTypeCount = trayTypeCounts[itemType];
            int nextTrayCount = trayCount + 1;
            trayTypeCounts[itemType] = previousTypeCount + 1;
            if (trayTypeCounts[itemType] == m_matchSize)
            {
                trayTypeCounts[itemType] = 0;
                nextTrayCount -= m_matchSize;
            }

            bool fillsTray = nextTrayCount >= trayCapacity;
            if (fillsTray == false)
            {
                path.Add(cellIndex);
                ulong nextRemainingMask = remainingMask & ~(1UL << cellIndex);
                if (FindAutoWinPath(nextRemainingMask, nextTrayCount, trayCapacity, activeCells, blockerMasks, itemTypes, trayTypeCounts, path, failedStates))
                {
                    trayTypeCounts[itemType] = previousTypeCount;
                    return true;
                }
                path.RemoveAt(path.Count - 1);
            }
            trayTypeCounts[itemType] = previousTypeCount;
        }

        failedStates.Add(state);
        return false;
    }

    private static int EncodeTrayCounts(int[] trayTypeCounts)
    {
        int code = 0;
        int multiplier = 1;
        foreach (int count in trayTypeCounts)
        {
            code += count * multiplier;
            multiplier *= 3;
        }
        return code;
    }

    public List<Cell> BuildAutoLosePlan(int targetCount, int maxItemsPerType)
    {
        List<Cell> activeCells = new List<Cell>();
        foreach (Cell cell in m_cells)
        {
            if (cell != null && cell.IsEmpty == false) activeCells.Add(cell);
        }
        if (activeCells.Count > 63) return new List<Cell>();

        Dictionary<Cell, int> cellIndices = new Dictionary<Cell, int>();
        for (int index = 0; index < activeCells.Count; index++) cellIndices[activeCells[index]] = index;

        ulong[] blockerMasks = new ulong[activeCells.Count];
        int[] itemTypes = new int[activeCells.Count];
        for (int index = 0; index < activeCells.Count; index++)
        {
            Cell cell = activeCells[index];
            NormalItem item = cell.Item as NormalItem;
            if (item == null) return new List<Cell>();
            itemTypes[index] = (int)item.ItemType;
            foreach (Cell blocker in cell.Blockers)
            {
                int blockerIndex;
                if (blocker != null && blocker.IsEmpty == false && cellIndices.TryGetValue(blocker, out blockerIndex))
                {
                    blockerMasks[index] |= 1UL << blockerIndex;
                }
            }
        }

        int typeCount = Enum.GetValues(typeof(NormalItem.eNormalType)).Length;
        int[] selectedTypeCounts = new int[typeCount];
        List<int> path = new List<int>(targetCount);
        ulong remainingMask = activeCells.Count == 0 ? 0UL : (1UL << activeCells.Count) - 1UL;
        if (FindAutoLosePath(remainingMask, targetCount, maxItemsPerType, activeCells, blockerMasks, itemTypes, selectedTypeCounts, path) == false)
        {
            return new List<Cell>();
        }

        List<Cell> result = new List<Cell>(path.Count);
        foreach (int cellIndex in path) result.Add(activeCells[cellIndex]);
        return result;
    }

    private bool FindAutoLosePath(
        ulong remainingMask,
        int targetCount,
        int maxItemsPerType,
        List<Cell> activeCells,
        ulong[] blockerMasks,
        int[] itemTypes,
        int[] selectedTypeCounts,
        List<int> path)
    {
        if (path.Count >= targetCount) return true;

        List<int> candidates = new List<int>();
        for (int index = 0; index < activeCells.Count; index++)
        {
            ulong cellBit = 1UL << index;
            if ((remainingMask & cellBit) == 0UL || (remainingMask & blockerMasks[index]) != 0UL) continue;
            if (selectedTypeCounts[itemTypes[index]] >= maxItemsPerType) continue;
            candidates.Add(index);
        }
        candidates.Sort((left, right) =>
        {
            int typePriority = selectedTypeCounts[itemTypes[left]].CompareTo(selectedTypeCounts[itemTypes[right]]);
            if (typePriority != 0) return typePriority;
            return activeCells[right].BoardLayer.CompareTo(activeCells[left].BoardLayer);
        });

        foreach (int cellIndex in candidates)
        {
            int itemType = itemTypes[cellIndex];
            selectedTypeCounts[itemType]++;
            path.Add(cellIndex);
            if (FindAutoLosePath(remainingMask & ~(1UL << cellIndex), targetCount, maxItemsPerType, activeCells, blockerMasks, itemTypes, selectedTypeCounts, path))
            {
                selectedTypeCounts[itemType]--;
                return true;
            }
            path.RemoveAt(path.Count - 1);
            selectedTypeCounts[itemType]--;
        }
        return false;
    }

    private void RefreshBlockedVisuals()
    {
        foreach (Cell cell in m_cells)
        {
            if (cell.Item != null) cell.Item.SetBlockedVisual(cell.IsBlocked);
        }
    }

    private bool Contains(Cell target)
    {
        return target != null && m_cells.Contains(target);
    }

    private struct AutoWinState : IEquatable<AutoWinState>
    {
        private readonly ulong m_remainingMask;
        private readonly int m_trayCode;

        public AutoWinState(ulong remainingMask, int trayCode)
        {
            m_remainingMask = remainingMask;
            m_trayCode = trayCode;
        }

        public bool Equals(AutoWinState other)
        {
            return m_remainingMask == other.m_remainingMask && m_trayCode == other.m_trayCode;
        }

        public override bool Equals(object other)
        {
            return other is AutoWinState && Equals((AutoWinState)other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_remainingMask.GetHashCode() * 397) ^ m_trayCode;
            }
        }
    }

    public void Clear()
    {
        foreach (Cell cell in m_cells)
        {
            if (cell == null) continue;
            cell.Clear();
            GameObject.Destroy(cell.gameObject);
        }
        m_cells.Clear();
        m_layers.Clear();
        m_layoutItemTypes.Clear();
        m_remainingItemCount = 0;
    }
}
