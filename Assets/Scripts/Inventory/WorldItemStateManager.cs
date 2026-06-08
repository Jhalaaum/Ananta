using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldItemStateSaveData
{
    public List<WorldItemStateEntry> items = new List<WorldItemStateEntry>();
}

[Serializable]
public class WorldItemStateEntry
{
    public string worldItemID;
    public bool isCollected;
}

public class WorldItemStateManager : MonoBehaviour
{
    public static WorldItemStateManager Instance;

    private static readonly List<WorldPickupItem> registeredItems = new List<WorldPickupItem>();

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        Instance = this;
    }

    public static void Register(WorldPickupItem item)
    {
        if (item == null)
        {
            return;
        }

        if (!registeredItems.Contains(item))
        {
            registeredItems.Add(item);
        }
    }

    public static void Unregister(WorldPickupItem item)
    {
        if (item == null)
        {
            return;
        }

        registeredItems.Remove(item);
    }

    public WorldItemStateSaveData CaptureWorldItemStates()
    {
        WorldItemStateSaveData saveData = new WorldItemStateSaveData();

        CleanNullItems();

        foreach (WorldPickupItem item in registeredItems)
        {
            if (item == null)
            {
                continue;
            }

            saveData.items.Add(new WorldItemStateEntry
            {
                worldItemID = item.worldItemID,
                isCollected = item.isCollected
            });

            if (showDebugLogs)
            {
                Debug.Log($"[WorldItemStateManager] Saved {item.worldItemID}: collected={item.isCollected}");
            }
        }

        return saveData;
    }

    public void RestoreWorldItemStates(WorldItemStateSaveData saveData)
    {
        CleanNullItems();

        foreach (WorldPickupItem item in registeredItems)
        {
            if (item == null)
            {
                continue;
            }

            WorldItemStateEntry entry = FindEntry(saveData, item.worldItemID);

            if (entry == null)
            {
                // If the checkpoint does not know about this item, safest default:
                // make it available in the world.
                item.SetCollected(false);

                if (showDebugLogs)
                {
                    Debug.Log($"[WorldItemStateManager] No saved state for {item.worldItemID}. Restored as available.");
                }

                continue;
            }

            item.SetCollected(entry.isCollected);

            if (showDebugLogs)
            {
                Debug.Log($"[WorldItemStateManager] Restored {item.worldItemID}: collected={entry.isCollected}");
            }
        }
    }

    private WorldItemStateEntry FindEntry(WorldItemStateSaveData saveData, string worldItemID)
    {
        if (saveData == null || saveData.items == null)
        {
            return null;
        }

        foreach (WorldItemStateEntry entry in saveData.items)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.worldItemID == worldItemID)
            {
                return entry;
            }
        }

        return null;
    }

    private void CleanNullItems()
    {
        for (int i = registeredItems.Count - 1; i >= 0; i--)
        {
            if (registeredItems[i] == null)
            {
                registeredItems.RemoveAt(i);
            }
        }
    }
}