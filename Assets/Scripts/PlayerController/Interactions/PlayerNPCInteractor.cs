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
    private bool isTalking;
    private void Update()
    {
        FindNearbyNPC();

        if (isTalking)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                EndTalking();
            }

            return;
        }

        bool hasNPCNearby =
            currentStoryNPC != null ||
            currentSimpleNPC != null;

        if (hasNPCNearby)
        {
            if (dialogueUI != null)
            {
                dialogueUI.ShowPrompt();
            }

            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                TalkToNPC();
            }
        }
        else
        {
            if (dialogueUI != null)
            {
                dialogueUI.HidePrompt();
            }
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
        isTalking = true;

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
            string line = currentSimpleNPC.GetRandomLine();

            if (dialogueUI != null)
            {
                dialogueUI.ShowDialoguePanel();
                dialogueUI.ShowLine("Civilian", line);
                dialogueUI.ShowContinuePrompt();
            }

            return;
        }
    }    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
    private void EndTalking()
    {
        isTalking = false;

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
            dialogueUI.HidePrompt();
        }
    }
}