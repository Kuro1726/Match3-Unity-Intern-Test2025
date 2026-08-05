using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelGame : MonoBehaviour,IMenu
{
    public Text LevelConditionView;

    [SerializeField] private Button btnPause;

    private UIMainManager m_mngr;
    private int m_trayCount;
    private int m_trayCapacity;
    private int m_remainingItems;
    private float m_remainingSeconds;
    private bool m_showTimeAttackTimer;

    private void Awake()
    {
        btnPause.onClick.AddListener(OnClickPause);
    }

    private void OnClickPause()
    {
        m_mngr.ShowPauseMenu();
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    public void UpdateProgress(int trayCount, int trayCapacity, int remainingItems)
    {
        m_trayCount = trayCount;
        m_trayCapacity = trayCapacity;
        m_remainingItems = remainingItems;
        RefreshStatusText();
    }

    public void ConfigureMode(bool showTimeAttackTimer)
    {
        m_showTimeAttackTimer = showTimeAttackTimer;
        m_remainingSeconds = 0f;
        RefreshStatusText();
    }

    public void UpdateTime(float remainingSeconds)
    {
        m_remainingSeconds = Mathf.Max(0f, remainingSeconds);
        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (LevelConditionView == null) return;
        string progress = string.Format("TRAY: {0}/{1}{3}ITEMS: {2}", m_trayCount, m_trayCapacity, m_remainingItems, Environment.NewLine);
        if (m_showTimeAttackTimer == false)
        {
            LevelConditionView.text = progress;
            return;
        }

        int totalSeconds = Mathf.CeilToInt(m_remainingSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        LevelConditionView.text = string.Format("TIME: {0:00}:{1:00}{2}{3}", minutes, seconds, Environment.NewLine, progress);
    }
}
