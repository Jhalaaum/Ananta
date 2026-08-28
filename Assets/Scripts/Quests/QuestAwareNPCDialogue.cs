using System;
using UnityEngine;

[Serializable]
public class NPCQuestReturnDialogue
{
    [Header("Quest Match")]
    public string questID;
    public string returnTargetID;

    [Header("Return Dialogue")]
    public DialogueConversation returnConversation;

    [Header("Prompt")]
    public string returnPrompt = "Press E to report back";

    [Header("Progress")]
    public bool progressQuestOnInteract = true;
    public int amount = 1;

    [Header("Priority")]
    public int priority = 0;
}

[Serializable]
public class NPCConditionalDialogue
{
    [Header("Dialogue")]
    public DialogueConversation conversation;

    [Header("Prompt")]
    public string prompt = "Press E to talk";

    [Header("Existing Conditions")]
    [Tooltip(
        "Your existing dialogue conditions. All must pass."
    )]
    public DialogueCondition[] conditions;

    [Header("Persistent Story Conditions")]
    [Tooltip(
        "Persistent story flags required for this conversation."
    )]
    public StoryFlagRequirement[] storyFlagRequirements;

    [Header("Priority")]
    [Tooltip(
        "If several conversations are available, highest priority wins."
    )]
    public int priority = 0;

    public bool IsAvailable()
    {
        if (conversation == null)
        {
            return false;
        }

        // --------------------------------------
        // EXISTING CONDITIONS
        // --------------------------------------

        if (conditions != null)
        {
            foreach (
                DialogueCondition condition
                in conditions)
            {
                if (condition == null)
                {
                    continue;
                }

                if (!condition.IsMet())
                {
                    return false;
                }
            }
        }

        // --------------------------------------
        // PERSISTENT STORY FLAGS
        // --------------------------------------

        if (storyFlagRequirements != null)
        {
            foreach (
                StoryFlagRequirement requirement
                in storyFlagRequirements)
            {
                if (requirement == null)
                {
                    continue;
                }

                if (!requirement.IsMet())
                {
                    return false;
                }
            }
        }

        return true;
    }
}
public class QuestAwareNPCDialogue : MonoBehaviour, IInteractable
{
    [Header("NPC Identity")]
    public string npcID = "devika";

    [Header("Default Dialogue")]
    public DialogueConversation defaultConversation;
    public string defaultPrompt = "Press E to talk";

    [Header("Quest Return Dialogues")]
    public NPCQuestReturnDialogue[] questReturnDialogues;

    [Header("Other Conditional Dialogues")]
    public NPCConditionalDialogue[] conditionalDialogues;

    [Header("References")]
    public DialogueManager dialogueManager;

    [Header("Settings")]
    public bool canInteract = true;
    public bool showDebugLogs = true;

    private NPCQuestReturnDialogue currentReturnDialogue;
    private NPCConditionalDialogue currentConditionalDialogue;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    public string GetPromptText()
    {
        ResolveCurrentDialogue();

        if (currentReturnDialogue != null)
        {
            return currentReturnDialogue.returnPrompt;
        }

        if (currentConditionalDialogue != null)
        {
            return currentConditionalDialogue.prompt;
        }

        return defaultPrompt;
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return false;
        }

        if (GameBookUI.Instance != null && GameBookUI.Instance.isOpen)
        {
            return false;
        }

        ResolveCurrentDialogue();

        if (currentReturnDialogue != null)
        {
            return true;
        }

        if (currentConditionalDialogue != null)
        {
            return true;
        }

        return defaultConversation != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        ResolveCurrentDialogue();

        if (currentReturnDialogue != null)
        {
            HandleQuestReturn(interactor, currentReturnDialogue);
            return;
        }

        if (currentConditionalDialogue != null)
        {
            StartConversation(currentConditionalDialogue.conversation, interactor);
            return;
        }

        StartConversation(defaultConversation, interactor);
    }

    private void ResolveCurrentDialogue()
    {
        currentReturnDialogue = FindBestReturnDialogue();

        if (currentReturnDialogue != null)
        {
            currentConditionalDialogue = null;
            return;
        }

        currentConditionalDialogue = FindBestConditionalDialogue();
    }

    private NPCQuestReturnDialogue FindBestReturnDialogue()
    {
        if (QuestManager.Instance == null)
        {
            return null;
        }

        if (questReturnDialogues == null || questReturnDialogues.Length == 0)
        {
            return null;
        }

        NPCQuestReturnDialogue best = null;

        foreach (NPCQuestReturnDialogue entry in questReturnDialogues)
        {
            if (entry == null)
            {
                continue;
            }

            string targetID = !string.IsNullOrWhiteSpace(entry.returnTargetID)
                ? entry.returnTargetID.Trim()
                : npcID.Trim();

            bool hasObjective;

            if (!string.IsNullOrWhiteSpace(entry.questID))
            {
                hasObjective = QuestManager.Instance.HasActiveObjective(
                    entry.questID,
                    QuestObjectiveType.ReturnToNPC,
                    targetID
                );
            }
            else
            {
                hasObjective = QuestManager.Instance.HasActiveObjective(
                    QuestObjectiveType.ReturnToNPC,
                    targetID
                );
            }

            if (!hasObjective)
            {
                continue;
            }

            if (best == null || entry.priority > best.priority)
            {
                best = entry;
            }
        }

        return best;
    }

    private NPCConditionalDialogue FindBestConditionalDialogue()
    {
        if (conditionalDialogues == null || conditionalDialogues.Length == 0)
        {
            return null;
        }

        NPCConditionalDialogue best = null;

        foreach (NPCConditionalDialogue entry in conditionalDialogues)
        {
            if (entry == null)
            {
                continue;
            }

            if (!entry.IsAvailable())
            {
                continue;
            }

            if (best == null || entry.priority > best.priority)
            {
                best = entry;
            }
        }

        return best;
    }

    private void HandleQuestReturn(Transform interactor, NPCQuestReturnDialogue returnDialogue)
    {
        string targetID = !string.IsNullOrWhiteSpace(returnDialogue.returnTargetID)
            ? returnDialogue.returnTargetID.Trim()
            : npcID.Trim();

        if (returnDialogue.progressQuestOnInteract && QuestManager.Instance != null)
        {
            QuestManager.Instance.ProgressObjective(
                QuestObjectiveType.ReturnToNPC,
                targetID,
                Mathf.Max(1, returnDialogue.amount)
            );

            if (showDebugLogs)
            {
                Debug.Log($"[QuestAwareNPCDialogue] Progressed ReturnToNPC: {targetID}");
            }
        }

        if (returnDialogue.returnConversation != null)
        {
            StartConversation(returnDialogue.returnConversation, interactor);
        }
        else if (defaultConversation != null)
        {
            StartConversation(defaultConversation, interactor);
        }
    }

    private void StartConversation(DialogueConversation conversation, Transform interactor)
    {
        if (conversation == null)
        {
            return;
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning("[QuestAwareNPCDialogue] No DialogueManager found.");
            return;
        }

        dialogueManager.StartDialogue(conversation, this, interactor);

        if (showDebugLogs)
        {
            Debug.Log($"[QuestAwareNPCDialogue] Started conversation: {conversation.conversationName}");
        }
    }
}