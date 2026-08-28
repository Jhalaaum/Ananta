using UnityEngine;

public class PersistentFlagObject : MonoBehaviour
{
    [Header("Persistent State")]
    public string flagID;

    [Header("Target")]
    public GameObject targetObject;

    [Tooltip(
        "If true, Target Object is enabled when flag = true."
    )]
    public bool activeWhenFlagTrue = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private void Awake()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }
    }

    private void Start()
    {
        ApplyFlagState();
    }

    public void SetFlagTrue()
    {
        SetState(true);
    }

    public void SetFlagFalse()
    {
        SetState(false);
    }

    public void SetState(bool value)
    {
        if (StoryFlagManager.Instance == null)
        {
            Debug.LogWarning(
                "[PersistentFlagObject] " +
                "StoryFlagManager is missing."
            );

            return;
        }

        StoryFlagManager.Instance.SetFlag(
            flagID,
            value
        );

        ApplyFlagState();
    }

    public void ApplyFlagState()
    {
        if (StoryFlagManager.Instance == null ||
            targetObject == null)
        {
            return;
        }

        bool value =
            StoryFlagManager.Instance.GetFlag(
                flagID
            );

        bool active =
            activeWhenFlagTrue
                ? value
                : !value;

        targetObject.SetActive(active);

        if (showDebugLogs)
        {
            Debug.Log(
                "[PersistentFlagObject] " +
                flagID +
                " restored. Target active = " +
                active
            );
        }
    }
}