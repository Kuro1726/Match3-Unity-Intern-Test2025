using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
    }

    public enum eGameResult
    {
        NONE,
        WIN,
        LOSE
    }

    public enum ePlayMode
    {
        MANUAL,
        AUTO_WIN,
        AUTO_LOSE
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }


    private GameSettings m_gameSettings;

    public eGameResult LastResult { get; private set; }
    public ePlayMode CurrentPlayMode { get; private set; }


    private BoardController m_boardController;

    private UIMainManager m_uiMenu;

    private void Awake()
    {
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        LoadLevel(ePlayMode.MANUAL);
    }

    public void LoadLevel(ePlayMode mode)
    {
        ClearLevel();
        LastResult = eGameResult.NONE;
        CurrentPlayMode = mode;
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.OnProgressChanged += OnProgressChanged;
        m_boardController.StartGame(this, m_gameSettings);

        State = eStateGame.GAME_STARTED;
        m_boardController.StartAutoPlay(mode);
    }

    public void GameOver()
    {
        CompleteGame(eGameResult.LOSE);
    }

    public void CompleteGame(eGameResult result)
    {
        if (State == eStateGame.GAME_OVER) return;
        if (LastResult != eGameResult.NONE) return;
        LastResult = result;
        StartCoroutine(WaitBoardController());
    }

    private void OnProgressChanged(int trayCount, int trayCapacity, int remainingItems)
    {
        m_uiMenu.UpdateGameProgress(trayCount, trayCapacity, remainingItems);
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.OnProgressChanged -= OnProgressChanged;
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_OVER;
    }
}
