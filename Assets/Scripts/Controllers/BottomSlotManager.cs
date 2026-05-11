using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BottomSlotManager : MonoBehaviour
{
    public int slotCount = 5;
    public Transform[] slotPositions;

    private BottomSlot[] slots;
    public event System.Action OnBottomFull;
    public event System.Action OnItemsMatched;

    public BottomSlot[] Slots { get { return slots; } }
    public Transform[] SlotPositions { get { return slotPositions; } }

    public void Initialize(Transform[] positions)
    {
        slotPositions = positions;
        slotCount = positions.Length;
        slots = new BottomSlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = new BottomSlot();
            slots[i].transform = slotPositions[i];

            slots[i].item = null;
        }
    }
    // Thêm item vào ô trống đầu tiên
    public bool AddItem(Item item, Cell originalCell, System.Action onComplete = null)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].item == null)
            {
                slots[i].item = item;
                slots[i].originalCell = originalCell;
                // Di chuyển view đến vị trí slot
                item.View.DOMove(slots[i].transform.position, 0.2f).OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
                return true;
            }
        }
        // Không còn ô trống -> thua
        OnBottomFull?.Invoke();
        return false;
    }

    // Kiểm tra và xóa các bộ 3 giống nhau (lặp lại cho đến khi hết)
    public void CheckAndClearMatches()
    {
        bool anyMatch = true;
        while (anyMatch)
        {
            anyMatch = false;
            // Nhóm các item theo loại
            Dictionary<NormalItem.eNormalType, List<int>> groups = new Dictionary<NormalItem.eNormalType, List<int>>();
            for (int i = 0; i < slotCount; i++)
            {
                if (slots[i].item is NormalItem normal)
                {
                    if (!groups.ContainsKey(normal.ItemType))
                        groups[normal.ItemType] = new List<int>();
                    groups[normal.ItemType].Add(i);
                }
            }
            // Xóa nếu có ít nhất 3
            foreach (var kvp in groups)
            {
                if (kvp.Value.Count >= 3)
                {
                    anyMatch = true;
                    // Xóa 3 cái đầu tiên (hoặc tất cả, nhưng theo yêu cầu là chính xác 3)
                    for (int i = 0; i < 3; i++)
                    {
                        int idx = kvp.Value[i];
                        slots[idx].item.ExplodeView(); // tự động destroy view
                        slots[idx].item = null;
                        slots[idx].originalCell = null;
                    }
                    OnItemsMatched?.Invoke();
                    break; // sau khi xóa, thoát vòng lặp và kiểm tra lại từ đầu
                }
            }
        }
    }

    public bool IsFull()
    {
        foreach (var slot in slots)
            if (slot.item == null) return false;
        return true;
    }

    public void ClearAll()
    {
        foreach (var slot in slots)
        {
            if (slot.item != null)
            {
                slot.item.Clear();
                slot.item = null;
                slot.originalCell = null;
            }
        }
    }

    public class BottomSlot
    {
        public Transform transform;
        public Item item;
        public Cell originalCell;
    }
    public List<NormalItem.eNormalType> GetCurrentTypes()
    {
        List<NormalItem.eNormalType> types = new List<NormalItem.eNormalType>();
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].item is NormalItem normal)
                types.Add(normal.ItemType);
            else
                types.Add(NormalItem.eNormalType.TYPE_ONE); // chỉ để fill, không dùng
        }
        return types;
    }

    // Kiểm tra nếu thêm một item loại type vào bottom có tạo thành bộ 3 ngay lập tức không
    public bool WouldCreateMatch(NormalItem.eNormalType type)
    {
        int count = 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].item is NormalItem normal && normal.ItemType == type)
                count++;
        }
        return count >= 2; // đã có 2 cái cùng loại -> thêm cái thứ 3 sẽ tạo match
    }
}
