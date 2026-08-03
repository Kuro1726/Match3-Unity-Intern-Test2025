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
        if (LevelConditionView != null)
        {
            LevelConditionView.text = string.Format("TRAY: {0}/{1}{3}ITEMS: {2}", trayCount, trayCapacity, remainingItems, Environment.NewLine);
        }
    }
}
