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
        MOVES,
        TIME_CHALLENGE
    }
    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
        WIN,
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
    private BoardController m_boardController;
    private UIMainManager m_uiMenu;
    private eLevelMode m_levelMode;
    private bool m_isTimeTrial = false;
    private float m_timeRemaining = 60f;
    private Coroutine m_timerCoroutine;
    // Auto play
    private bool m_autoMode = false;
    private Coroutine m_autoCoroutine;

    private void Awake()
    {
        State = eStateGame.SETUP;
        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        m_uiMenu = FindObjectOfType<UIMainManager>();
        if (m_uiMenu != null)
            m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    void Update()
    {
        if (m_boardController != null)
            m_boardController.Update();
    }

    internal void SetState(eStateGame state)
    {
        State = state;
        if (State == eStateGame.PAUSE)
            DOTween.PauseAll();
        else
            DOTween.PlayAll();
    }

    // Khởi tạo level mới (không còn mode TIMER/MOVES)
    public void LoadLevel(eLevelMode mode = eLevelMode.MOVES)
    {
        ClearLevel();
        m_levelMode = mode;
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);
        m_boardController.OnBoardEmpty += GameWin;
        if (mode != eLevelMode.TIME_CHALLENGE)
        {
            m_boardController.OnBottomFull += GameLose;
        }
        if (mode == eLevelMode.TIME_CHALLENGE)
        {
            StartTimer(60f); // 1 minute
        }
        State = eStateGame.GAME_STARTED;
    }

    // Bắt đầu chế độ auto (tham số aimToWin: true = cố gắng thắng, false = cố gắng thua)
    public void StartAutoGame(bool aimToWin)
    {
        if (m_autoCoroutine != null)
            StopCoroutine(m_autoCoroutine);
        m_autoMode = true;
        LoadLevel();
        StartCoroutine(WaitAndStartAuto(aimToWin));
    }

    private IEnumerator WaitAndStartAuto(bool aimToWin)
    {
        // Chờ board controller sẵn sàng
        while (m_boardController == null || m_boardController.IsBusy)
            yield return null;
        m_autoCoroutine = StartCoroutine(AutoPlayCoroutine(aimToWin));
    }

    private IEnumerator AutoPlayCoroutine(bool aimToWin)
    {
        while (m_boardController != null && !m_boardController.IsGameOver)
        {
            var cells = m_boardController.GetNonEmptyCells();
            if (cells.Count == 0) break;

            Cell chosenCell = null;
            var bottomTypes = m_boardController.GetBottomSlotTypes(); // lấy 5 loại hiện có (hoặc None)

            if (aimToWin)
            {
                // Chiến thuật WIN: ưu tiên chọn item có loại đã xuất hiện dưới bottom
                // Nếu đã có 2 cái cùng loại → tạo match ngay.
                // Nếu chỉ có 1 cái → tạo cặp, lần sau sẽ match.
                chosenCell = FindCellThatMatchesBottom(cells, bottomTypes);
                if (chosenCell == null)
                {
                    // Không có loại nào trùng, chọn ngẫu nhiên
                    chosenCell = cells[UnityEngine.Random.Range(0, cells.Count)];
                }
            }
            else
            {
                // Chiến thuật LOSE: tránh tạo match, nhưng nếu không còn thì chọn bừa
                chosenCell = FindCellThatDoesNotCreateMatch(cells);
                if (chosenCell == null)
                    chosenCell = cells[UnityEngine.Random.Range(0, cells.Count)];
            }

            if (chosenCell != null)
            {
                m_boardController.StartCoroutine(m_boardController.MoveItemToBottom(chosenCell));
                while (m_boardController.IsBusy) yield return null;
            }
            yield return new WaitForSeconds(0.5f);
        }
        m_autoMode = false;
        m_autoCoroutine = null;
    }

    // Tìm cell có loại xuất hiện ít nhất 1 lần dưới bottom (kể cả đã có 2)
    private Cell FindCellThatMatchesBottom(List<Cell> cells, List<NormalItem.eNormalType> bottomTypes)
    {
        // Đếm số lượng mỗi loại dưới bottom
        Dictionary<NormalItem.eNormalType, int> counts = new Dictionary<NormalItem.eNormalType, int>();
        foreach (var t in bottomTypes)
        {
            if (t != NormalItem.eNormalType.TYPE_ONE || bottomTypes.Contains(t)) // lọc
            {
                if (!counts.ContainsKey(t)) counts[t] = 0;
                counts[t]++;
            }
        }
        foreach (Cell cell in cells)
        {
            NormalItem item = cell.Item as NormalItem;
            if (item != null && counts.ContainsKey(item.ItemType))
            {
                return cell;
            }
        }
        return null;
    }

    // Tìm cell có item mà khi thêm vào bottom KHÔNG tạo match
    private Cell FindCellThatDoesNotCreateMatch(List<Cell> cells)
    {
        foreach (Cell cell in cells)
        {
            NormalItem item = cell.Item as NormalItem;
            if (item != null && !m_boardController.WouldCreateMatch(item.ItemType))
                return cell;
        }
        return null;
    }

    private void GameWin()
    {
        if (m_autoMode && m_autoCoroutine != null)
            StopCoroutine(m_autoCoroutine);
        if (m_boardController != null)
            m_boardController.SetGameOver(true);
        State = eStateGame.WIN;
        Debug.Log("You win!");
        // m_uiMenu?.ShowWinScreen();
    }

    private void GameLose()
    {
        if (m_autoMode && m_autoCoroutine != null)
            StopCoroutine(m_autoCoroutine);
        if (m_boardController != null)
            m_boardController.SetGameOver(true);
        State = eStateGame.GAME_OVER;
        Debug.Log("You lose!");
        // m_uiMenu?.ShowLoseScreen();
    }

    private void StartTimer(float duration)
    {
        m_timeRemaining = duration;
        if (m_timerCoroutine != null)
            StopCoroutine(m_timerCoroutine);
        m_timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine()
    {
        while (m_timeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            m_timeRemaining -= 1f;
        }
        // Time up, check if board is empty
        if (m_boardController != null && !m_boardController.IsBoardEmpty())
        {
            GameLose();
        }
    }

    internal void ClearLevel()
    {
        if (m_boardController != null)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
        m_autoMode = false;
        if (m_autoCoroutine != null)
        {
            StopCoroutine(m_autoCoroutine);
            m_autoCoroutine = null;
        }
        if (m_timerCoroutine != null)
        {
            StopCoroutine(m_timerCoroutine);
            m_timerCoroutine = null;
        }
    }
    
}