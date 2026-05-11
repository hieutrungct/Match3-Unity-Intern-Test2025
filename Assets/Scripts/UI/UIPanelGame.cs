using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelGame : MonoBehaviour, IMenu
{
    public Text TimeView; // nút hiển thị thời gian còn lại (chỉ dùng trong time trial)

    [SerializeField] private Button btnPause;

    private UIMainManager m_mngr;
    private Coroutine m_updateTimeCoroutine;

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
        if (m_mngr.GameManager.LevelMode == GameManager.eLevelMode.TIME_CHALLENGE)
        {
            TimeView.gameObject.SetActive(true);
            m_updateTimeCoroutine = StartCoroutine(UpdateTimeView());
        }
        else
        {
            TimeView.gameObject.SetActive(false);
        }
    }

    public void Hide()
    {
        if (m_updateTimeCoroutine != null)
        {
            StopCoroutine(m_updateTimeCoroutine);
            m_updateTimeCoroutine = null;
        }
        this.gameObject.SetActive(false);
    }

    private IEnumerator UpdateTimeView()
    {
        while (true)
        {
            TimeView.text = Mathf.CeilToInt(m_mngr.GameManager.TimeRemaining).ToString();
            yield return new WaitForSeconds(1f);
        }
    }
}
