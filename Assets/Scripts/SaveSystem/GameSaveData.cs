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


    public MoneySaveData money = new MoneySaveData();
    public DisguiseSaveData disguise = new DisguiseSaveData();
    public WorldSuspicionSaveData worldSuspicion = new WorldSuspicionSaveData();
    public ZoneAlertSaveData zoneAlert = new ZoneAlertSaveData();
    public StoryFlagSaveData storyFlags = new StoryFlagSaveData();
    public InventorySaveData inventorySaveData;
    public WorldItemStateSaveData worldItemStateSaveData;
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