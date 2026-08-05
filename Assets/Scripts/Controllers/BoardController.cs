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
    public event Action<float> OnTimeChanged = delegate { };
    public bool IsBusy { get; private set; }

    private const int RequiredMatchSize = 3;
    private const float AutoActionDelay = 0.5f;
    private Board m_board;
    private BottomTray m_bottomTray;
    private GameManager m_gameManager;
    private Camera m_camera;
    private bool m_gameOver;
    private bool m_operationInProgress;
    private bool m_autoPlaying;
    private bool m_isTimeAttack;
    private float m_moveDuration;
    private float m_clearDuration;
    private float m_timeRemaining;
    private int m_lastReportedSecond = -1;
    private readonly Dictionary<Item, Cell> m_itemOrigins = new Dictionary<Item, Cell>();

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;
        m_gameManager.StateChangedAction += OnGameStateChange;
        m_isTimeAttack = m_gameManager.CurrentPlayMode == GameManager.ePlayMode.TIME_ATTACK;
        m_timeRemaining = Mathf.Max(1f, gameSettings.TimeAttackDuration);
        m_camera = Camera.main;
        m_board = new Board(transform, gameSettings);
        m_board.Fill();

        int trayCapacity = Mathf.Max(RequiredMatchSize, gameSettings.BottomCellCount);
        float trayY = -gameSettings.BoardSizeY * 0.5f - 1f;
        m_moveDuration = Mathf.Max(0.05f, gameSettings.ItemMoveDuration);
        m_clearDuration = Mathf.Max(0.05f, gameSettings.ItemClearDuration);
        m_bottomTray = new BottomTray(transform, trayCapacity, RequiredMatchSize, trayY, gameSettings);
        IsBusy = false;
        NotifyProgressChanged();
        if (m_isTimeAttack) NotifyTimeChanged(true);
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
        UpdateTimeAttackTimer();
        if (m_autoPlaying) return;
        if (m_gameOver || IsBusy || m_camera == null) return;
        if (Input.GetMouseButtonDown(0) == false) return;

        Vector2 worldPosition = m_camera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPosition, Vector2.zero);
        if (m_isTimeAttack)
        {
            Cell selectedTrayCell = hits
                .Select(hit => hit.collider.GetComponent<Cell>())
                .FirstOrDefault(cell => m_bottomTray.Contains(cell) && cell.IsEmpty == false);
            if (selectedTrayCell != null)
            {
                TryReturnItemToBoard(selectedTrayCell);
                return;
            }
        }

        Cell selectedCell = hits
            .Select(hit => hit.collider.GetComponent<Cell>())
            .Where(cell => m_board.IsCellSelectable(cell))
            .OrderByDescending(cell => cell.BoardLayer)
            .FirstOrDefault();
        if (selectedCell != null) TryMoveItemToTray(selectedCell);
    }

    public void StartAutoPlay(GameManager.ePlayMode mode)
    {
        if (mode != GameManager.ePlayMode.AUTO_WIN && mode != GameManager.ePlayMode.AUTO_LOSE) return;
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
        if (m_itemOrigins.ContainsKey(item) == false) m_itemOrigins[item] = selectedCell;

        m_operationInProgress = true;
        IsBusy = true;
        OnMoveEvent();
        m_bottomTray.Add(item, m_moveDuration, () => StartCoroutine(ResolveMoveCoroutine()));
        NotifyProgressChanged();
    }

    private void TryReturnItemToBoard(Cell trayCell)
    {
        Item item;
        if (m_bottomTray.TryGetItem(trayCell, out item) == false) return;

        Cell originCell;
        if (m_itemOrigins.TryGetValue(item, out originCell) == false || originCell == null || originCell.IsEmpty == false) return;

        m_operationInProgress = true;
        IsBusy = true;
        if (m_bottomTray.Remove(item, m_moveDuration) == false || m_board.TryReturnItem(originCell, item) == false)
        {
            m_operationInProgress = false;
            IsBusy = false;
            return;
        }

        NotifyProgressChanged();
        item.SetSortingLayerHigher();
        item.View.DOKill();
        item.View.DOMove(originCell.transform.position, m_moveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                item.SetSortingOrder(originCell.BoardLayer * 10);
                m_operationInProgress = false;
                IsBusy = false;
                NotifyProgressChanged();
            });
    }

    private void UpdateTimeAttackTimer()
    {
        if (m_isTimeAttack == false || m_gameOver || m_gameManager == null) return;
        if (m_gameManager.State != GameManager.eStateGame.GAME_STARTED) return;

        m_timeRemaining = Mathf.Max(0f, m_timeRemaining - Time.deltaTime);
        NotifyTimeChanged(false);
        if (m_timeRemaining <= 0f && m_board != null && m_board.RemainingItemCount > 0)
        {
            FinishGame(GameManager.eGameResult.LOSE);
        }
    }

    private IEnumerator AutoWinCoroutine()
    {
        List<Cell> plan;
        if (m_board.TryBuildAutoWinPlan(m_bottomTray.Capacity, out plan) == false)
        {
            Debug.LogError("Autoplay could not find a winning sequence for this board layout.");
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

            if (m_board.IsCellSelectable(targetCell) == false)
            {
                Debug.LogError("Autoplay plan became invalid before all items were cleared.");
                yield break;
            }

            TryMoveItemToTray(targetCell);
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
        if (m_gameOver) yield break;
        List<Item> match = m_bottomTray.FindMatch();
        if (match.Count == m_bottomTray.MatchSize)
        {
            foreach (Item matchedItem in match) m_itemOrigins.Remove(matchedItem);
            m_bottomTray.ClearMatch(match);
            NotifyProgressChanged();
            yield return new WaitForSeconds(m_clearDuration);
            m_bottomTray.Compact(m_moveDuration);
            yield return new WaitForSeconds(m_moveDuration);
            NotifyProgressChanged();
        }

        if (m_board.RemainingItemCount == 0 && (m_isTimeAttack || m_bottomTray.Count == 0))
        {
            FinishGame(GameManager.eGameResult.WIN);
            yield break;
        }
        if (m_isTimeAttack == false && m_bottomTray.IsFull)
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

    private void NotifyTimeChanged(bool force)
    {
        int displayedSecond = Mathf.CeilToInt(m_timeRemaining);
        if (force == false && displayedSecond == m_lastReportedSecond) return;
        m_lastReportedSecond = displayedSecond;
        OnTimeChanged(m_timeRemaining);
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
        m_itemOrigins.Clear();
    }
}

internal class BottomTray
{
    private readonly List<Cell> m_cells = new List<Cell>();
    private readonly List<Item> m_items = new List<Item>();
    private readonly Ease m_moveEase;
    private readonly float m_movePunchScale;
    private readonly int m_movePunchVibrato;
    private readonly float m_movePunchElasticity;
    private readonly float m_clearDuration;
    private readonly Ease m_clearEase;
    public int Count => m_items.Count;
    public int Capacity => m_cells.Count;
    public int MatchSize { get; private set; }
    public bool IsFull => Count >= Capacity;

    public BottomTray(Transform root, int capacity, int matchSize, float yPosition, GameSettings settings)
    {
        MatchSize = Mathf.Max(3, matchSize);
        m_moveEase = settings.ItemMoveEase;
        m_movePunchScale = Mathf.Max(0f, settings.ItemMovePunchScale);
        m_movePunchVibrato = Mathf.Max(1, settings.ItemMovePunchVibrato);
        m_movePunchElasticity = Mathf.Clamp01(settings.ItemMovePunchElasticity);
        m_clearDuration = Mathf.Max(0.05f, settings.ItemClearDuration);
        m_clearEase = settings.ItemClearEase;
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
        Sequence moveSequence = DOTween.Sequence();
        moveSequence.Join(item.View.DOMove(destination.transform.position, duration).SetEase(m_moveEase));
        if (m_movePunchScale > 0f)
        {
            moveSequence.Join(item.View.DOPunchScale(Vector3.one * m_movePunchScale, duration, m_movePunchVibrato, m_movePunchElasticity));
        }
        moveSequence.OnComplete(() =>
            {
                item.SetSortingLayerLower();
                if (onComplete != null) onComplete();
            });
    }

    public bool Contains(Cell cell)
    {
        return cell != null && m_cells.Contains(cell);
    }

    public bool TryGetItem(Cell cell, out Item item)
    {
        item = null;
        if (Contains(cell) == false || cell.IsEmpty) return false;
        item = cell.Item;
        return item != null && m_items.Contains(item);
    }

    public bool Remove(Item item, float compactDuration)
    {
        if (item == null || m_items.Remove(item) == false) return false;
        if (item.Cell != null) item.Cell.Free();
        Compact(compactDuration);
        return true;
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
            item.ExplodeView(m_clearDuration, m_clearEase);
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
