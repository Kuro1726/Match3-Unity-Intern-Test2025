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
    private readonly List<Cell[,]> m_layers = new List<Cell[,]>();
    private readonly List<Cell> m_cells = new List<Cell>();
    private readonly Dictionary<Cell, NormalItem.eNormalType> m_layoutItemTypes = new Dictionary<Cell, NormalItem.eNormalType>();
    private readonly Transform m_root;
    private int m_remainingItemCount;

    public int RemainingItemCount => m_remainingItemCount;

    public Board(Transform root, GameSettings gameSettings)
    {
        m_root = root;
        m_boardSizeX = gameSettings.BoardSizeX;
        m_boardSizeY = gameSettings.BoardSizeY;
        m_layerCount = Mathf.Max(1, gameSettings.BoardLayerCount);
        m_layout = gameSettings.BoardLayout;
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
        if (renderer) renderer.sortingOrder = layer * 10;
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
            int typeIndex = group - values.Length * (group / values.Length);
            NormalItem.eNormalType type = (NormalItem.eNormalType)values.GetValue(typeIndex);
            for (int i = 0; i < m_matchSize; i++) result.Add(type);
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

    public bool IsCellSelectable(Cell cell)
    {
        return Contains(cell) && cell.IsEmpty == false && cell.IsBlocked == false;
    }

    public bool TryFindSelectableTripleType(out NormalItem.eNormalType itemType, out int boardLayer)
    {
        boardLayer = -1;
        foreach (Cell cell in m_cells)
        {
            if (cell.IsEmpty == false && cell.BoardLayer > boardLayer)
            {
                boardLayer = cell.BoardLayer;
            }
        }

        if (boardLayer < 0)
        {
            itemType = default(NormalItem.eNormalType);
            return false;
        }

        Dictionary<NormalItem.eNormalType, int> counts = new Dictionary<NormalItem.eNormalType, int>();
        foreach (Cell cell in m_cells)
        {
            if (cell.BoardLayer != boardLayer || IsCellSelectable(cell) == false) continue;
            NormalItem item = cell.Item as NormalItem;
            if (item == null) continue;
            int count;
            counts.TryGetValue(item.ItemType, out count);
            counts[item.ItemType] = count + 1;
            if (count + 1 >= m_matchSize)
            {
                itemType = item.ItemType;
                return true;
            }
        }
        itemType = default(NormalItem.eNormalType);
        return false;
    }

    public Cell FindCellOfType(NormalItem.eNormalType itemType, int boardLayer)
    {
        foreach (Cell cell in m_cells)
        {
            NormalItem item = cell.Item as NormalItem;
            if (cell.BoardLayer == boardLayer && IsCellSelectable(cell) && item != null && item.ItemType == itemType) return cell;
        }
        return null;
    }
    public List<Cell> BuildAutoLosePlan(int targetCount, int maxItemsPerType)
    {
        List<Cell> result = new List<Cell>(targetCount);
        Dictionary<NormalItem.eNormalType, int> counts = new Dictionary<NormalItem.eNormalType, int>();
        foreach (Cell cell in m_cells)
        {
            if (result.Count >= targetCount) break;
            if (IsCellSelectable(cell) == false) continue;
            NormalItem item = cell.Item as NormalItem;
            if (item == null) continue;
            int count;
            counts.TryGetValue(item.ItemType, out count);
            if (count >= maxItemsPerType) continue;
            counts[item.ItemType] = count + 1;
            result.Add(cell);
        }
        return result;
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
