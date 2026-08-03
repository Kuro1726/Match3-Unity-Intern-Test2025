using System;
using System.Collections.Generic;
using UnityEngine;

public class Board
{
    private readonly int m_boardSizeX;
    private readonly int m_boardSizeY;
    private readonly int m_matchSize;
    private readonly Cell[,] m_cells;
    private readonly Transform m_root;
    private int m_remainingItemCount;

    public int RemainingItemCount => m_remainingItemCount;

    public Board(Transform root, GameSettings gameSettings)
    {
        m_root = root;
        m_boardSizeX = gameSettings.BoardSizeX;
        m_boardSizeY = gameSettings.BoardSizeY;
        m_matchSize = 3;
        m_cells = new Cell[m_boardSizeX, m_boardSizeY];
        CreateBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-m_boardSizeX * 0.5f + 0.5f, -m_boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefab = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < m_boardSizeX; x++)
        {
            for (int y = 0; y < m_boardSizeY; y++)
            {
                GameObject view = GameObject.Instantiate(prefab, origin + new Vector3(x, y, 0f), Quaternion.identity, m_root);
                view.name = string.Format("BoardCell_{0}_{1}", x, y);
                Cell cell = view.GetComponent<Cell>();
                cell.Setup(x, y);
                m_cells[x, y] = cell;
            }
        }
    }
    public void Fill()
    {
        int cellCount = m_boardSizeX * m_boardSizeY;
        int playableItemCount = m_matchSize * (cellCount / m_matchSize);
        FillCells(CreateBalancedItemTypes(playableItemCount));
        m_remainingItemCount = playableItemCount;
    }

    private void FillCells(List<NormalItem.eNormalType> itemTypes)
    {
        int typeIndex = 0;
        for (int y = 0; y < m_boardSizeY; y++)
        {
            for (int x = 0; x < m_boardSizeX; x++)
            {
                if (typeIndex >= itemTypes.Count) return;
                NormalItem item = new NormalItem();
                item.SetType(itemTypes[typeIndex++]);
                item.SetView();
                item.SetViewRoot(m_root);
                m_cells[x, y].Assign(item);
                m_cells[x, y].ApplyItemPosition(false);
            }
        }
    }
    private List<NormalItem.eNormalType> CreateBalancedItemTypes(int itemCount)
    {
        Array values = Enum.GetValues(typeof(NormalItem.eNormalType));
        List<NormalItem.eNormalType> result = new List<NormalItem.eNormalType>(itemCount);
        int groupCount = itemCount / m_matchSize;
        for (int group = 0; group < groupCount; group++)
        {
            NormalItem.eNormalType type = (NormalItem.eNormalType)values.GetValue(group - values.Length * (group / values.Length));
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
        if (Contains(cell) == false) return false;
        if (cell.IsEmpty) return false;

        item = cell.Item;
        cell.Free();
        m_remainingItemCount--;
        return true;
    }

    public Cell FindFirstOccupiedCell()
    {
        for (int y = 0; y < m_boardSizeY; y++)
        {
            for (int x = 0; x < m_boardSizeX; x++)
            {
                if (m_cells[x, y].IsEmpty == false) return m_cells[x, y];
            }
        }
        return null;
    }

    public Cell FindCellOfType(NormalItem.eNormalType itemType)
    {
        for (int y = 0; y < m_boardSizeY; y++)
        {
            for (int x = 0; x < m_boardSizeX; x++)
            {
                NormalItem item = m_cells[x, y].Item as NormalItem;
                if (item != null && item.ItemType == itemType) return m_cells[x, y];
            }
        }
        return null;
    }

    public List<Cell> BuildAutoLosePlan(int targetCount, int maxItemsPerType)
    {
        List<Cell> result = new List<Cell>(targetCount);
        Dictionary<NormalItem.eNormalType, int> counts = new Dictionary<NormalItem.eNormalType, int>();
        for (int y = 0; y < m_boardSizeY && result.Count < targetCount; y++)
        {
            for (int x = 0; x < m_boardSizeX && result.Count < targetCount; x++)
            {
                NormalItem item = m_cells[x, y].Item as NormalItem;
                if (item == null) continue;
                int count;
                counts.TryGetValue(item.ItemType, out count);
                if (count >= maxItemsPerType) continue;
                counts[item.ItemType] = count + 1;
                result.Add(m_cells[x, y]);
            }
        }
        return result;
    }

    private bool Contains(Cell target)
    {
        if (target == null) return false;
        int x = target.BoardX;
        int y = target.BoardY;
        if (x < 0 || x >= m_boardSizeX) return false;
        if (y < 0 || y >= m_boardSizeY) return false;
        return m_cells[x, y] == target;
    }
    public void Clear()
    {
        for (int x = 0; x < m_boardSizeX; x++)
        {
            for (int y = 0; y < m_boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell == null) continue;
                cell.Clear();
                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
        m_remainingItemCount = 0;
    }
}
