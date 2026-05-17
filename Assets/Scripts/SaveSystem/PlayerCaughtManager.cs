using System.Collections;
using UnityEngine;

public class PlayerCaughtManager : MonoBehaviour
{
    public static PlayerCaughtManager Instance;

    [Header("Caught Flow")]
    public float caughtDelay = 0.5f;
    public bool isHandlingCaught;

    [Header("Optional UI")]
    public GameObject caughtPanel;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine caughtRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (caughtPanel != null)
        {
            caughtPanel.SetActive(false);
        }
    }

    public void OnPlayerCaught(string caughtByID = "guard")
    {
        if (isHandlingCaught)
        {
            return;
        }

        if (caughtRoutine != null)
        {
            StopCoroutine(caughtRoutine);
        }

        caughtRoutine = StartCoroutine(CaughtRoutine(caughtByID));
    }

    private IEnumerator CaughtRoutine(string caughtByID)
    {
        isHandlingCaught = true;

        Log($"Player caught by: {caughtByID}");

        if (caughtPanel != null)
        {
            caughtPanel.SetActive(true);
        }

        yield return new WaitForSeconds(caughtDelay);

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.HandlePlayerCaught();
        }
        else
        {
            Debug.LogWarning("[PlayerCaughtManager] No CheckpointManager found.");
        }

        yield return new WaitForSeconds(0.1f);

        if (caughtPanel != null)
        {
            caughtPanel.SetActive(false);
        }

        isHandlingCaught = false;
        caughtRoutine = null;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PlayerCaughtManager] {message}");
        }
    }
}