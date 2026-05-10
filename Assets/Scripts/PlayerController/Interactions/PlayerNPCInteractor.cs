using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNPCInteractor : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;
    public LayerMask npcLayer;

    [Header("UI")]
    public DialogueUI dialogueUI;

    private GameObject currentNPC;

    private void Update()
    {
        FindNearbyNPC();

        if (currentNPC != null)
        {
            if (dialogueUI != null)
            {
                dialogueUI.ShowPrompt();
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TalkToNPC();
            }
        }
        else
        {
            if (dialogueUI != null)
            {
                dialogueUI.HidePrompt();

                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    dialogueUI.HideDialogue();
                }
            }
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (dialogueUI != null)
            {
                dialogueUI.HideDialogue();
            }
        }
    }

    private void FindNearbyNPC()
    {
        currentNPC = null;

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
        GameObject closestNPC = null;

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNPC = hit.gameObject;
            }
        }

        currentNPC = closestNPC;
    }

    private void TalkToNPC()
    {
        if (currentNPC == null)
        {
            return;
        }

        StoryNPCDialogue storyDialogue = currentNPC.GetComponentInParent<StoryNPCDialogue>();

        if (storyDialogue != null)
        {
            string line = storyDialogue.GetNextLine();
            ShowLine("Story NPC: " + line);
            return;
        }

        SimpleNPCDialogue simpleDialogue = currentNPC.GetComponentInParent<SimpleNPCDialogue>();

        if (simpleDialogue != null)
        {
            string line = simpleDialogue.GetRandomLine();
            ShowLine("NPC: " + line);
            return;
        }

        ShowLine("This NPC has no dialogue component.");
    }

    private void ShowLine(string line)
    {
        Debug.Log(line);

        if (dialogueUI != null)
        {
            dialogueUI.ShowDialogue(line);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}