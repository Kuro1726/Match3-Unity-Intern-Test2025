using System;
using UnityEngine;
using System.Collections.Generic;

public class Cell : MonoBehaviour
{
    public int BoardX { get; private set; }

    public int BoardY { get; private set; }

    public int BoardLayer { get; private set; }

    public Item Item { get; private set; }

    public Cell NeighbourUp { get; set; }

    public Cell NeighbourRight { get; set; }

    public Cell NeighbourBottom { get; set; }

    public Cell NeighbourLeft { get; set; }


    public bool IsEmpty => Item == null;

    private readonly List<Cell> m_blockers = new List<Cell>();

    public bool IsBlocked
    {
        get
        {
            foreach (Cell blocker in m_blockers)
            {
                if (blocker != null && blocker.IsEmpty == false) return true;
            }
            return false;
        }
    }

    public void Setup(int cellX, int cellY, int boardLayer = 0)
    {
        this.BoardX = cellX;
        this.BoardY = cellY;
        this.BoardLayer = boardLayer;
    }

    public void AddBlocker(Cell blocker)
    {
        if (blocker != null && m_blockers.Contains(blocker) == false)
        {
            m_blockers.Add(blocker);
        }
    }

    public bool IsNeighbour(Cell other)
    {
        return BoardX == other.BoardX && Mathf.Abs(BoardY - other.BoardY) == 1 ||
            BoardY == other.BoardY && Mathf.Abs(BoardX - other.BoardX) == 1;
    }


    public void Free()
    {
        if (Item != null)
        {
            Item.SetCell(null);
        }

        Item = null;
    }

    public void Assign(Item item)
    {
        Item = item;
        Item.SetCell(this);
        Item.SetSortingOrder(BoardLayer * 10);
    }

    public void ApplyItemPosition(bool withAppearAnimation)
    {
        Item.SetViewPosition(this.transform.position);

        if (withAppearAnimation)
        {
            Item.ShowAppearAnimation();
        }
    }

    internal void Clear()
    {
        if (Item != null)
        {
            Item.Clear();
            Item = null;
        }
    }

    internal bool IsSameType(Cell other)
    {
        return Item != null && other.Item != null && Item.IsSameType(other.Item);
    }

    internal void ExplodeItem()
    {
        if (Item == null) return;

        Item.ExplodeView();
        Item = null;
    }

    internal void AnimateItemForHint()
    {
        Item.AnimateForHint();
    }

    internal void StopHintAnimation()
    {
        Item.StopAnimateForHint();
    }

    internal void ApplyItemMoveToPosition()
    {
        Item.AnimationMoveToPosition();
    }
}
