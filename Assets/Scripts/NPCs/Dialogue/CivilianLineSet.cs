using UnityEngine;

[CreateAssetMenu(fileName = "New Civilian Line Set", menuName = "Dialogue/Civilian Line Set")]
public class CivilianLineSet : ScriptableObject
{
    [Header("Identity")]
    public string lineSetID;
    public CivilianType civilianType;

    [Header("Speaker")]
    public DialogueActor defaultActor;

    [Header("Prompt")]
    public string promptText = "Press E to talk";

    [Header("Lines")]
    public CivilianLineEntry[] lines;

    public CivilianLineEntry GetRandomAvailableLine()
    {
        if (lines == null || lines.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (CivilianLineEntry entry in lines)
        {
            if (entry == null)
            {
                continue;
            }

            if (!entry.IsAvailable())
            {
                continue;
            }

            totalWeight += Mathf.Max(1, entry.weight);
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (CivilianLineEntry entry in lines)
        {
            if (entry == null)
            {
                continue;
            }

            if (!entry.IsAvailable())
            {
                continue;
            }

            current += Mathf.Max(1, entry.weight);

            if (roll < current)
            {
                return entry;
            }
        }

        return null;
    }

    public CivilianLineEntry GetRandomAvailableLineByCategory(CivilianLineCategory category)
    {
        if (lines == null || lines.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (CivilianLineEntry entry in lines)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.category != category)
            {
                continue;
            }

            if (!entry.IsAvailable())
            {
                continue;
            }

            totalWeight += Mathf.Max(1, entry.weight);
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (CivilianLineEntry entry in lines)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.category != category)
            {
                continue;
            }

            if (!entry.IsAvailable())
            {
                continue;
            }

            current += Mathf.Max(1, entry.weight);

            if (roll < current)
            {
                return entry;
            }
        }

        return null;
    }
}