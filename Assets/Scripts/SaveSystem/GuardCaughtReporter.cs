using UnityEngine;

public class GuardCaughtReporter : MonoBehaviour
{
    [Header("Guard")]
    public string guardID = "guard_01";

    public void ReportPlayerCaught()
    {
        if (PlayerCaughtManager.Instance == null)
        {
            Debug.LogWarning("[GuardCaughtReporter] No PlayerCaughtManager found.");
            return;
        }

        PlayerCaughtManager.Instance.OnPlayerCaught(guardID);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        ReportPlayerCaught();
    }
}