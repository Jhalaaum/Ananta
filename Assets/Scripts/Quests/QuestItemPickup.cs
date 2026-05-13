using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuestItemPickup : MonoBehaviour
{
    [Header("Item")]
    public QuestTarget questTarget;

    [Header("Item Display")]
    public string itemName = "Clue";

    [TextArea(2, 4)]
    public string pickupMessage = "You collected a clue.";

    [Header("Settings")]
    public bool destroyOnPickup = true;
    public bool requirePlayerTag = true;
    public string playerTag = "Player";

    private bool hasBeenPickedUp;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenPickedUp)
        {
            return;
        }

        if (requirePlayerTag && !other.CompareTag(playerTag))
        {
            return;
        }

        Pickup();
    }

    public void Pickup()
    {
        if (hasBeenPickedUp)
        {
            return;
        }

        if (questTarget == null)
        {
            Debug.LogWarning($"{name} has no QuestTarget assigned.");
            return;
        }

        hasBeenPickedUp = true;

        Debug.Log($"Picked up: {itemName}");

        if (!string.IsNullOrWhiteSpace(pickupMessage))
        {
            Debug.Log(pickupMessage);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ProgressObjective(
                QuestObjectiveType.CollectItem,
                questTarget.targetID
            );
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}