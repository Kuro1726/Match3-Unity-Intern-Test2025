using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnTimer;

    [SerializeField] private Button btnMoves;
    [SerializeField] private Button btnAutoplay;

    [SerializeField] private Button btnAutoLose;

    private UIMainManager m_mngr;

    private void Awake()
    {
        if (btnMoves)
        {
            RectTransform playRect = btnMoves.transform as RectTransform;
            playRect.anchoredPosition = new Vector2(0f, 90f);

            btnMoves.onClick.AddListener(OnClickMoves);
            btnAutoplay.onClick.AddListener(OnClickAutoplay);
            btnAutoLose.onClick.AddListener(OnClickAutoLose);
        }
        if (btnTimer) btnTimer.onClick.AddListener(OnClickTimer);
    }

    private Button CreateButton(Button source, string objectName, string label, Vector2 position)
    {
        Button button = Instantiate(source, source.transform.parent);
        button.name = objectName;
        button.onClick.RemoveAllListeners();
        RectTransform rect = button.transform as RectTransform;
        rect.anchoredPosition = position;
        Text text = button.GetComponentInChildren<Text>();
        if (text) text.text = label;
        return button;
    }

    private void OnDestroy()
    {
        if (btnMoves) btnMoves.onClick.RemoveAllListeners();
        if (btnTimer) btnTimer.onClick.RemoveAllListeners();
        if (btnAutoplay) btnAutoplay.onClick.RemoveAllListeners();
        if (btnAutoLose) btnAutoLose.onClick.RemoveAllListeners();
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    private void OnClickTimer()
    {
        m_mngr.LoadLevelTimer();
    }

    private void OnClickMoves()
    {
        m_mngr.LoadManualGame();
    }

    private void OnClickAutoplay()
    {
        m_mngr.LoadAutoWinGame();
    }

    private void OnClickAutoLose()
    {
        m_mngr.LoadAutoLoseGame();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
