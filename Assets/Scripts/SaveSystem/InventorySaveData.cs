using System;
using System.Collections.Generic;

[Serializable]
public class InventorySaveData
{
    public List<InventorySlotSaveData> slots = new List<InventorySlotSaveData>();
    public int selectedHotbarIndex;
}

[Serializable]
public class InventorySlotSaveData
{
    public string itemID;
    public int amount;
}