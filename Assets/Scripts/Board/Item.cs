using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[Serializable]
public class Item
{
    public const float ClearAnimationDuration = 0.18f;

    public Cell Cell { get; private set; }

    public Transform View { get; private set; }

    private Transform m_backgroundView;
    private SpriteRenderer m_itemRenderer;
    private SpriteRenderer m_backgroundRenderer;
    private Color m_backgroundColor = Color.white;
    private int m_sortingOrder;


    public virtual void SetView()
    {
        string prefabname = GetPrefabName();

        if (!string.IsNullOrEmpty(prefabname))
        {
            GameObject prefab = Resources.Load<GameObject>(prefabname);
            if (prefab)
            {
                GameObject tileRoot = new GameObject(prefab.name + "_Tile");
                View = tileRoot.transform;
                Transform itemView = GameObject.Instantiate(prefab, View).transform;
                itemView.localPosition = Vector3.zero;
                itemView.localRotation = Quaternion.identity;
                m_itemRenderer = itemView.GetComponent<SpriteRenderer>();
                CreateBackgroundView();
                ApplySortingOrder();
            }
        }
    }

    private void CreateBackgroundView()
    {
        GameObject prefab = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        if (prefab == null || View == null) return;
        GameObject background = GameObject.Instantiate(prefab, View);
        background.name = "ItemBackground";
        m_backgroundView = background.transform;
        m_backgroundView.localPosition = Vector3.zero;
        m_backgroundView.localRotation = Quaternion.identity;
        m_backgroundView.localScale = Vector3.one;
        Collider2D[] colliders = background.GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders) collider.enabled = false;
        Cell backgroundCell = background.GetComponent<Cell>();
        if (backgroundCell != null) backgroundCell.enabled = false;
        m_backgroundRenderer = background.GetComponent<SpriteRenderer>();
        if (m_backgroundRenderer != null) m_backgroundColor = m_backgroundRenderer.color;
    }

    public void ConfigureBackground(float scale, float opacity)
    {
        if (m_backgroundView != null) m_backgroundView.localScale = Vector3.one * Mathf.Clamp(scale, 1f, 1.25f);
        if (m_backgroundRenderer == null) return;
        m_backgroundColor.a = Mathf.Clamp01(opacity);
        m_backgroundRenderer.color = m_backgroundColor;
    }

    protected virtual string GetPrefabName() { return string.Empty; }

    public virtual void SetCell(Cell cell)
    {
        Cell = cell;
    }

    internal void AnimationMoveToPosition()
    {
        if (View == null) return;

        View.DOMove(Cell.transform.position, 0.2f);
    }

    public void SetViewPosition(Vector3 pos)
    {
        if (View)
        {
            View.position = pos;
        }
    }

    public void SetViewRoot(Transform root)
    {
        if (View)
        {
            View.SetParent(root);
        }
    }

    public void SetSortingLayerHigher()
    {
        m_sortingOrder = 100;
        ApplySortingOrder();
    }


    public void SetSortingLayerLower()
    {
        m_sortingOrder = 0;
        ApplySortingOrder();
    }

    public void SetSortingOrder(int sortingOrder)
    {
        m_sortingOrder = sortingOrder;
        ApplySortingOrder();
    }

    private void ApplySortingOrder()
    {
        if (m_backgroundRenderer != null) m_backgroundRenderer.sortingOrder = m_sortingOrder;
        if (m_itemRenderer != null) m_itemRenderer.sortingOrder = m_sortingOrder + 1;
    }

    public void SetBlockedVisual(bool isBlocked)
    {
        if (View == null) return;
        if (m_itemRenderer != null) m_itemRenderer.color = isBlocked
            ? new Color(0.45f, 0.45f, 0.45f, 1f)
            : Color.white;
        if (m_backgroundRenderer != null)
        {
            Color color = m_backgroundColor;
            if (isBlocked)
            {
                color.r *= 0.55f;
                color.g *= 0.55f;
                color.b *= 0.55f;
            }
            m_backgroundRenderer.color = color;
        }
    }

    internal void ShowAppearAnimation()
    {
        if (View == null) return;

        Vector3 scale = View.localScale;
        View.localScale = Vector3.one * 0.1f;
        View.DOScale(scale, 0.1f);
    }

    internal virtual bool IsSameType(Item other)
    {
        return false;
    }

    internal virtual void ExplodeView()
    {
        ExplodeView(ClearAnimationDuration, Ease.InBack);
    }

    internal virtual void ExplodeView(float duration, Ease ease)
    {
        if (View)
        {
            View.DOKill();
            View.DOScale(Vector3.zero, Mathf.Max(0.05f, duration))
                .SetEase(ease)
                .OnComplete(
                () =>
                {
                    GameObject.Destroy(View.gameObject);
                    View = null;
                    m_backgroundView = null;
                    m_itemRenderer = null;
                    m_backgroundRenderer = null;
                });
        }
    }



    internal void AnimateForHint()
    {
        if (View)
        {
            View.DOPunchScale(View.localScale * 0.1f, 0.1f).SetLoops(-1);
        }
    }

    internal void StopAnimateForHint()
    {
        if (View)
        {
            View.DOKill();
        }
    }

    internal void Clear()
    {
        Cell = null;

        if (View)
        {
            View.DOKill();
            GameObject.Destroy(View.gameObject);
            View = null;
            m_backgroundView = null;
            m_itemRenderer = null;
            m_backgroundRenderer = null;
        }
    }
}
