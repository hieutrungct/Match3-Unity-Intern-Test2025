using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;
    private BottomSlotManager m_bottomSlotManager;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;
    public event Action OnBoardEmpty = delegate { };
    public event Action OnBottomFull = delegate { };

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        // Tạo bottom slots
        GameObject bottomObj = new GameObject("BottomSlots");
        bottomObj.transform.SetParent(this.transform);
        bottomObj.transform.position = new Vector3(0, -m_gameSettings.BoardSizeY * 0.5f - 1f, 0);
        m_bottomSlotManager = bottomObj.AddComponent<BottomSlotManager>();

        // Tạo 5 vị trí động
        Transform[] positions = new Transform[5];
        float startX = -2f;
        for (int i = 0; i < 5; i++)
        {
            GameObject slotGO = new GameObject("Slot" + i);
            slotGO.transform.SetParent(bottomObj.transform);
            slotGO.transform.position = new Vector3(startX + i * 1f, -4, 0);
            positions[i] = slotGO.transform;
        }
        m_bottomSlotManager.Initialize(positions);
        m_bottomSlotManager.OnBottomFull += () => { if (!m_gameOver) OnBottomFull?.Invoke(); };
        m_bottomSlotManager.OnItemsMatched += () => { }; // có thể thêm hiệu ứng

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        // FindMatchesAndCollapse();
        // Sau khi fill, kiểm tra nếu bảng trống ngay từ đầu(không thể)
        CheckBoardEmpty();
    }
    private void CheckBoardEmpty()
    {
        for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
            for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
                if (!m_board.GetCell(x, y).IsEmpty)
                    return;
        // Bảng trống -> thắng
        if (!m_gameOver)
            OnBoardEmpty?.Invoke();
    }

    public bool IsBoardEmpty()
    {
        for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
            for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
                if (!m_board.GetCell(x, y).IsEmpty)
                    return false;
        return true;
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                // StopHints();
                break;
        }
    }

    public void Update()
    {
        if (m_gameOver || IsBusy) return;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell cell = hit.collider.GetComponent<Cell>();
                if (cell != null && !cell.IsEmpty)
                {
                    StartCoroutine(MoveItemToBottom(cell));
                }
            }
        }
    }
    public IEnumerator MoveItemToBottom(Cell cell)
    {
        IsBusy = true;
        Item item = cell.Item;
        if (item == null) { IsBusy = false; yield break; }

        // Thử thêm vào bottom
        bool success = m_bottomSlotManager.AddItem(item, () =>
        {
            // Sau khi animation di chuyển hoàn tất, xóa item khỏi cell
            cell.Free();
        });
        if (!success)
        {
            // Bottom đầy -> thua (sự kiện đã gọi)
            IsBusy = false;
            yield break;
        }

        // Chờ animation di chuyển (0.2s) + một chút
        yield return new WaitForSeconds(0.25f);
        // Kiểm tra match trong bottom
        m_bottomSlotManager.CheckAndClearMatches();
        // Kiểm tra bảng trống
        CheckBoardEmpty();

        IsBusy = false;
    }

    // public void Update()
    // {
    //     if (m_gameOver) return;
    //     if (IsBusy) return;

    //     if (!m_hintIsShown)
    //     {
    //         m_timeAfterFill += Time.deltaTime;
    //         if (m_timeAfterFill > m_gameSettings.TimeForHint)
    //         {
    //             m_timeAfterFill = 0f;
    //             ShowHint();
    //         }
    //     }

    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
    //         if (hit.collider != null)
    //         {
    //             m_isDragging = true;
    //             m_hitCollider = hit.collider;
    //         }
    //     }

    //     if (Input.GetMouseButtonUp(0))
    //     {
    //         ResetRayCast();
    //     }

    //     if (Input.GetMouseButton(0) && m_isDragging)
    //     {
    //         var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
    //         if (hit.collider != null)
    //         {
    //             if (m_hitCollider != null && m_hitCollider != hit.collider)
    //             {
    //                 StopHints();

    //                 Cell c1 = m_hitCollider.GetComponent<Cell>();
    //                 Cell c2 = hit.collider.GetComponent<Cell>();
    //                 if (AreItemsNeighbor(c1, c2))
    //                 {
    //                     IsBusy = true;
    //                     SetSortingLayer(c1, c2);
    //                     m_board.Swap(c1, c2, () =>
    //                     {
    //                         FindMatchesAndCollapse(c1, c2);
    //                     });

    //                     ResetRayCast();
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             ResetRayCast();
    //         }
    //     }
    // }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        m_board.Clear();
        m_bottomSlotManager.ClearAll();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }
    // Helper để auto player lấy danh sách cell không rỗng
    public System.Collections.Generic.List<Cell> GetNonEmptyCells()
    {
        var list = new System.Collections.Generic.List<Cell>();
        for (int x = 0; x < m_gameSettings.BoardSizeX; x++)
            for (int y = 0; y < m_gameSettings.BoardSizeY; y++)
            {
                var cell = m_board.GetCell(x, y);
                if (!cell.IsEmpty) list.Add(cell);
            }
        return list;
    }

    public bool IsGameOver => m_gameOver;
    public void SetGameOver(bool over) => m_gameOver = over;
    // Hỏi bottom slot manager xem thêm item loại X có tạo match không
    public bool WouldCreateMatch(NormalItem.eNormalType type)
    {
        return m_bottomSlotManager.WouldCreateMatch(type);
    }

    // Lấy danh sách các loại item hiện có dưới bottom (có thể dùng để debug)
    public List<NormalItem.eNormalType> GetBottomSlotTypes()
    {
        return m_bottomSlotManager.GetCurrentTypes();
    }
}
