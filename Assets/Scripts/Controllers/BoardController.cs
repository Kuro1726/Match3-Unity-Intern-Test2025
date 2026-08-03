using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };
    public event Action<int, int, int> OnProgressChanged = delegate { };
    public bool IsBusy { get; private set; }

    private const float MoveDuration = 0.25f;
    private const float MatchDelay = 0.12f;
    private const int RequiredMatchSize = 3;
    private const float AutoActionDelay = 0.5f;
    private Board m_board;
    private BottomTray m_bottomTray;
    private GameManager m_gameManager;
    private Camera m_camera;
    private bool m_gameOver;
    private bool m_operationInProgress;
    private bool m_autoPlaying;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;
        m_gameManager.StateChangedAction += OnGameStateChange;
        m_camera = Camera.main;
        m_board = new Board(transform, gameSettings);
        m_board.Fill();

        int trayCapacity = Mathf.Max(RequiredMatchSize, gameSettings.BottomCellCount);
        float trayY = -gameSettings.BoardSizeY * 0.5f - 1f;
        m_bottomTray = new BottomTray(transform, trayCapacity, RequiredMatchSize, trayY);
        IsBusy = false;
        NotifyProgressChanged();
    }
    private void OnGameStateChange(GameManager.eStateGame state)
    {
        if (state == GameManager.eStateGame.GAME_STARTED) IsBusy = m_operationInProgress;
        if (state == GameManager.eStateGame.PAUSE) IsBusy = true;
        if (state == GameManager.eStateGame.GAME_OVER)
        {
            m_gameOver = true;
            IsBusy = true;
        }
    }
    public void Update()
    {
        if (m_autoPlaying) return;
        if (m_gameOver || IsBusy || m_camera == null) return;
        if (Input.GetMouseButtonDown(0) == false) return;

        Vector2 worldPosition = m_camera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPosition, Vector2.zero);
        Cell selectedCell = hits
            .Select(hit => hit.collider.GetComponent<Cell>())
            .Where(cell => m_board.IsCellSelectable(cell))
            .OrderByDescending(cell => cell.BoardLayer)
            .FirstOrDefault();
        if (selectedCell != null) TryMoveItemToTray(selectedCell);
    }

    public void StartAutoPlay(GameManager.ePlayMode mode)
    {
        if (mode == GameManager.ePlayMode.MANUAL) return;
        m_autoPlaying = true;
        if (mode == GameManager.ePlayMode.AUTO_WIN)
        {
            StartCoroutine(AutoWinCoroutine());
        }
        else
        {
            StartCoroutine(AutoLoseCoroutine());
        }
    }
    private void TryMoveItemToTray(Cell selectedCell)
    {
        if (selectedCell == null || m_bottomTray.IsFull) return;

        Item item;
        if (m_board.TryTakeItem(selectedCell, out item) == false) return;

        m_operationInProgress = true;
        IsBusy = true;
        OnMoveEvent();
        m_bottomTray.Add(item, MoveDuration, () => StartCoroutine(ResolveMoveCoroutine()));
        NotifyProgressChanged();
    }

    private IEnumerator AutoWinCoroutine()
    {
        int itemsLeftInGroup = 0;
        int targetLayer = -1;
        NormalItem.eNormalType targetType = default(NormalItem.eNormalType);
        yield return new WaitForSeconds(AutoActionDelay);

        while (m_gameOver == false && m_board.RemainingItemCount > 0)
        {
            while (m_gameOver == false && (IsBusy || m_gameManager.State != GameManager.eStateGame.GAME_STARTED))
            {
                yield return null;
            }
            if (m_gameOver) yield break;

            if (itemsLeftInGroup == 0)
            {
                if (m_board.TryFindSelectableTripleType(out targetType, out targetLayer) == false) yield break;
                itemsLeftInGroup = RequiredMatchSize;
            }

            Cell targetCell = m_board.FindCellOfType(targetType, targetLayer);
            if (targetCell == null) yield break;
            TryMoveItemToTray(targetCell);
            itemsLeftInGroup--;
            yield return new WaitForSeconds(AutoActionDelay);
        }
    }

    private IEnumerator AutoLoseCoroutine()
    {
        List<Cell> plan = m_board.BuildAutoLosePlan(m_bottomTray.Capacity, RequiredMatchSize - 1);
        if (plan.Count < m_bottomTray.Capacity)
        {
            FinishGame(GameManager.eGameResult.LOSE);
            yield break;
        }

        yield return new WaitForSeconds(AutoActionDelay);
        foreach (Cell targetCell in plan)
        {
            while (m_gameOver == false && (IsBusy || m_gameManager.State != GameManager.eStateGame.GAME_STARTED))
            {
                yield return null;
            }
            if (m_gameOver) yield break;

            TryMoveItemToTray(targetCell);
            yield return new WaitForSeconds(AutoActionDelay);
        }
    }
    private IEnumerator ResolveMoveCoroutine()
    {
        List<Item> match = m_bottomTray.FindMatch();
        if (match.Count == m_bottomTray.MatchSize)
        {
            m_bottomTray.ClearMatch(match);
            NotifyProgressChanged();
            yield return new WaitForSeconds(MatchDelay);
            m_bottomTray.Compact(MoveDuration);
            yield return new WaitForSeconds(MoveDuration);
            NotifyProgressChanged();
        }

        if (m_board.RemainingItemCount == 0 && m_bottomTray.Count == 0)
        {
            FinishGame(GameManager.eGameResult.WIN);
            yield break;
        }
        if (m_bottomTray.IsFull)
        {
            FinishGame(GameManager.eGameResult.LOSE);
            yield break;
        }
        m_operationInProgress = false;
        IsBusy = false;
    }
    private void FinishGame(GameManager.eGameResult result)
    {
        m_operationInProgress = false;
        IsBusy = false;
        m_gameOver = true;
        m_gameManager.CompleteGame(result);
    }

    private void NotifyProgressChanged()
    {
        OnProgressChanged(m_bottomTray.Count, m_bottomTray.Capacity, m_board.RemainingItemCount);
    }

    internal void Clear()
    {
        StopAllCoroutines();
        if (m_gameManager != null) m_gameManager.StateChangedAction -= OnGameStateChange;
        if (m_bottomTray != null)
        {
            m_bottomTray.Clear();
            m_bottomTray = null;
        }
        if (m_board != null)
        {
            m_board.Clear();
            m_board = null;
        }
    }
}

internal class BottomTray
{
    private readonly List<Cell> m_cells = new List<Cell>();
    private readonly List<Item> m_items = new List<Item>();
    public int Count => m_items.Count;
    public int Capacity => m_cells.Count;
    public int MatchSize { get; private set; }
    public bool IsFull => Count >= Capacity;

    public BottomTray(Transform root, int capacity, int matchSize, float yPosition)
    {
        MatchSize = Mathf.Max(3, matchSize);
        GameObject prefab = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        float startX = -capacity * 0.5f + 0.5f;
        for (int i = 0; i < capacity; i++)
        {
            GameObject view = GameObject.Instantiate(prefab, new Vector3(startX + i, yPosition, 0f), Quaternion.identity, root);
            view.name = string.Format("BottomCell_{0}", i);
            Cell cell = view.GetComponent<Cell>();
            cell.Setup(i, -1);
            m_cells.Add(cell);
        }
    }
    public void Add(Item item, float duration, Action onComplete)
    {
        m_items.Add(item);
        Cell destination = m_cells[m_items.Count - 1];
        destination.Assign(item);
        item.SetSortingLayerHigher();
        item.View.DOKill();
        item.View.DOMove(destination.transform.position, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                item.SetSortingLayerLower();
                if (onComplete != null) onComplete();
            });
    }
    public List<Item> FindMatch()
    {
        foreach (Item item in m_items)
        {
            List<Item> sameItems = m_items.Where(other => item.IsSameType(other)).ToList();
            if (sameItems.Count == MatchSize) return sameItems;
        }
        return new List<Item>();
    }
    public void ClearMatch(List<Item> match)
    {
        foreach (Item item in match)
        {
            if (item.Cell != null) item.Cell.Free();
            m_items.Remove(item);
            item.ExplodeView();
        }
    }
    public void Compact(float duration)
    {
        foreach (Cell cell in m_cells) cell.Free();
        for (int i = 0; i < m_items.Count; i++)
        {
            Item item = m_items[i];
            Cell destination = m_cells[i];
            destination.Assign(item);
            item.View.DOKill();
            item.View.DOMove(destination.transform.position, duration).SetEase(Ease.OutQuad);
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
        m_items.Clear();
        m_cells.Clear();
    }
}
