using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    [Header("Slots")]
    public HotbarSlotUI[] hotbarSlots;

    [Header("Debug")]
    public bool showDebugLogs;

    private void Start()
    {
        AutoFindSlots();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            InventoryManager.Instance.OnSelectedHotbarChanged += HandleSelectedChanged;
        }

        SetupSlots();
        Refresh();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
            InventoryManager.Instance.OnSelectedHotbarChanged -= HandleSelectedChanged;
        }
    }

    private void AutoFindSlots()
    {
        if (hotbarSlots != null && hotbarSlots.Length > 0)
        {
            return;
        }

        hotbarSlots = GetComponentsInChildren<HotbarSlotUI>(true);
    }

    private void SetupSlots()
    {
        if (hotbarSlots == null)
        {
            return;
        }

        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (hotbarSlots[i] != null)
            {
                hotbarSlots[i].Setup(i);
            }
        }
    }

    private void HandleSelectedChanged(int selectedIndex)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (InventoryManager.Instance == null || hotbarSlots == null)
        {
            return;
        }

        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            InventorySlot slot = InventoryManager.Instance.GetSlot(i);
            bool selected = i == InventoryManager.Instance.selectedHotbarIndex;

            if (hotbarSlots[i] != null)
            {
                hotbarSlots[i].Refresh(slot, selected);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log("[HotbarUI] Refreshed.");
        }
    }
}