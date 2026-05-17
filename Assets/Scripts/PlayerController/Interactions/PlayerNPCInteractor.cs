using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNPCInteractor : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask npcLayer;

    [Header("References")]
    public DialogueUI dialogueUI;
    public DialogueManager dialogueManager;

    private StoryNPCDialogue currentStoryNPC;
    private SimpleNPCDialogue currentSimpleNPC;
    public bool blockPromptWhenDialogueTriggerNearby = true;
    private void Update()
    {
        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return;
        }

        FindNearbyNPC();

        bool hasNPCNearby = currentStoryNPC != null || currentSimpleNPC != null;

        if (hasNPCNearby)
        {
            dialogueUI?.ShowPrompt("Press E to talk", this);

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TalkToNPC();
            }
        }
        else
        {
            dialogueUI?.HidePrompt(this);
        }
    }

    private void FindNearbyNPC()
    {
        currentStoryNPC = null;
        currentSimpleNPC = null;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactDistance,
            npcLayer,
            QueryTriggerInteraction.Collide
        );

        if (hits.Length == 0)
        {
            return;
        }

        float closestDistance = Mathf.Infinity;
        Collider closestHit = null;

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHit = hit;
            }
        }

        if (closestHit == null)
        {
            return;
        }

        currentStoryNPC = closestHit.GetComponentInParent<StoryNPCDialogue>();

        if (currentStoryNPC == null)
        {
            currentSimpleNPC = closestHit.GetComponentInParent<SimpleNPCDialogue>();
        }
    }

    private void TalkToNPC()
    {
        if (dialogueUI != null)
        {
            dialogueUI.HidePrompt();
        }

        if (currentStoryNPC != null)
        {
            DialogueConversation conversation = currentStoryNPC.GetConversation();

            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(conversation, currentStoryNPC, transform);
            }

            return;
        }
        if (currentSimpleNPC != null)
        {
            DialogueConversation conversation = currentSimpleNPC.CreateRandomConversation();

            if (conversation != null && dialogueManager != null)
            {
                dialogueManager.StartDialogue(conversation, null, transform);
            }

            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}