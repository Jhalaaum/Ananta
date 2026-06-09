using UnityEngine;

public class PlayerControlLock : MonoBehaviour
{
    public static PlayerControlLock Instance;

    [Header("Scripts To Disable While Locked")]
    public MonoBehaviour[] movementScripts;
    public MonoBehaviour[] cameraScripts;
    public MonoBehaviour[] interactionScripts;

    [Header("State")]
    public bool IsLocked { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void LockPlayer()
    {
        IsLocked = true;
        SetScriptsEnabled(movementScripts, false);
        SetScriptsEnabled(cameraScripts, false);
        SetScriptsEnabled(interactionScripts, false);

        Debug.Log("[PlayerControlLock] Player locked.");
    }

    public void UnlockPlayer()
    {
        IsLocked = false;
        SetScriptsEnabled(movementScripts, true);
        SetScriptsEnabled(cameraScripts, true);
        SetScriptsEnabled(interactionScripts, true);

        Debug.Log("[PlayerControlLock] Player unlocked.");
    }

    private void SetScriptsEnabled(MonoBehaviour[] scripts, bool enabled)
    {
        if (scripts == null)
        {
            return;
        }

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
            {
                script.enabled = enabled;
            }
        }
    }
}