using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    // [SerializeField] private Button btnTimer;

    [SerializeField] private Button btnMoves;
    [SerializeField] private Button btnTimeChallenge;

    private UIMainManager m_mngr;

    private void Awake()
    {
        btnMoves.onClick.AddListener(OnClickMoves);
        // btnTimer.onClick.AddListener(OnClickTimer);
        btnTimeChallenge.onClick.AddListener(OnClickTimeChallenge);
    }

    private void OnDestroy()
    {
        if (btnMoves) btnMoves.onClick.RemoveAllListeners();
        // if (btnTimer) btnTimer.onClick.RemoveAllListeners();
        if (btnTimeChallenge) btnTimeChallenge.onClick.RemoveAllListeners();
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
        m_mngr.LoadLevelMoves();
    }

    private void OnClickTimeChallenge()
    {
        m_mngr.LoadLevelTimeChallenge();
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
