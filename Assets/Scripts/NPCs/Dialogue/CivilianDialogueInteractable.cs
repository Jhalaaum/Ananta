using UnityEngine;

public class CivilianDialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Civilian")]
    public CivilianLineSet lineSet;
    public DialogueActor overrideActor;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;

    [Header("Settings")]
    public bool canInteract = true;
    public bool useSpecificCategory;
    public CivilianLineCategory forcedCategory = CivilianLineCategory.Ambient;

    [Header("Anti-Repetition")]
    public bool avoidRepeatingSameLine = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private string lastLine;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    public string GetPromptText()
    {
        if (lineSet != null && !string.IsNullOrWhiteSpace(lineSet.promptText))
        {
            return lineSet.promptText;
        }

        return "Press E to talk";
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (lineSet == null)
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

        return true;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        CivilianLineEntry selectedLine = PickLine();

        if (selectedLine == null)
        {
            Debug.LogWarning($"[CivilianDialogueInteractable] No valid line found for {name}.");
            return;
        }

        selectedLine.ApplyEffects();

        DialogueConversation temporaryConversation = BuildTemporaryConversation(selectedLine);

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(temporaryConversation, this, interactor);
        }
    }

    private CivilianLineEntry PickLine()
    {
        CivilianLineEntry selectedLine = null;

        for (int attempt = 0; attempt < 6; attempt++)
        {
            selectedLine = useSpecificCategory
                ? lineSet.GetRandomAvailableLineByCategory(forcedCategory)
                : lineSet.GetRandomAvailableLine();

            if (selectedLine == null)
            {
                return null;
            }

            if (!avoidRepeatingSameLine)
            {
                break;
            }

            if (selectedLine.line != lastLine)
            {
                break;
            }
        }

        if (selectedLine != null)
        {
            lastLine = selectedLine.line;
        }

        return selectedLine;
    }

    private DialogueConversation BuildTemporaryConversation(CivilianLineEntry selectedLine)
    {
        DialogueConversation conversation = ScriptableObject.CreateInstance<DialogueConversation>();

        conversation.conversationID = $"civilian_{lineSet.lineSetID}_{GetInstanceID()}_{Time.frameCount}";
        conversation.conversationName = $"{lineSet.civilianType} Civilian Line";
        conversation.description = "Runtime-generated civilian dialogue.";
        conversation.startNodeID = "start";
        conversation.canRepeat = true;
        conversation.markCompletedOnEnd = false;
        conversation.isEavesdroppingConversation = false;
        conversation.canRevealInformation = selectedLine.category == CivilianLineCategory.Rumor ||
                                            selectedLine.category == CivilianLineCategory.QuestHint;
        conversation.canStartQuest = false;
        conversation.canChangeTrust = false;

        DialogueActor actorToUse = overrideActor != null
            ? overrideActor
            : lineSet.defaultActor;

        DialogueNode node = new DialogueNode
        {
            nodeID = "start",
            speaker = actorToUse,
            line = selectedLine.line,
            lineType = selectedLine.lineType,
            nextNodeID = "",
            choices = new DialogueChoice[0],
            conditions = new DialogueCondition[0],
            onEnterEffects = new DialogueEffect[0],
            onExitEffects = new DialogueEffect[0],
            autoContinue = false,
            autoContinueDelay = 1.5f
        };

        conversation.nodes = new DialogueNode[]
        {
            node
        };

        if (showDebugLogs)
        {
            Debug.Log($"[CivilianDialogueInteractable] {name} selected line: {selectedLine.line}");
        }

        return conversation;
    }
}