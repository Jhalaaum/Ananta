using System;

[Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int amount;

    public InventorySlot(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }

    public bool IsEmpty()
    {
        return itemData == null || amount <= 0;
    }

    public bool CanStackWith(ItemData item)
    {
        if (itemData == null || item == null)
        {
            return false;
        }

        if (itemData.itemID != item.itemID)
        {
            return false;
        }

        if (!itemData.isStackable)
        {
            return false;
        }

        return amount < itemData.maxStackSize;
    }

    public int GetSpaceRemaining()
    {
        if (itemData == null)
        {
            return 0;
        }

        return itemData.maxStackSize - amount;
    }
}