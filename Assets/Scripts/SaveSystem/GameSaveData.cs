using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int saveVersion = 1;

    public string saveName;
    public string savedAt;

    public PlayerSaveData player = new PlayerSaveData();

    public QuestSaveCollection questProgress = new QuestSaveCollection();

    // Future-ready sections.
    public InventorySaveData inventory = new InventorySaveData();
    public MoneySaveData money = new MoneySaveData();
    public DisguiseSaveData disguise = new DisguiseSaveData();
    public WorldSuspicionSaveData worldSuspicion = new WorldSuspicionSaveData();
    public ZoneAlertSaveData zoneAlert = new ZoneAlertSaveData();
    public StoryFlagSaveData storyFlags = new StoryFlagSaveData();
}

[Serializable]
public class InventorySaveData
{
    public List<ItemStackSaveData> items = new List<ItemStackSaveData>();
}

[Serializable]
public class ItemStackSaveData
{
    public string itemID;
    public int amount;
}

[Serializable]
public class MoneySaveData
{
    public int amount;
}

[Serializable]
public class DisguiseSaveData
{
    public string equippedDisguiseID;
}

[Serializable]
public class WorldSuspicionSaveData
{
    public int globalSuspicion;
}

[Serializable]
public class ZoneAlertSaveData
{
    public List<ZoneAlertEntrySaveData> zones = new List<ZoneAlertEntrySaveData>();
}

[Serializable]
public class ZoneAlertEntrySaveData
{
    public string zoneID;
    public int alertLevel;
}

[Serializable]
public class StoryFlagSaveData
{
    public List<StoryFlagEntrySaveData> flags = new List<StoryFlagEntrySaveData>();
}

[Serializable]
public class StoryFlagEntrySaveData
{
    public string flagID;
    public bool value;
}