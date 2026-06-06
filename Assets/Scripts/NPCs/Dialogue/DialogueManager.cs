using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("References")]
    public DialogueUI dialogueUI;

    [Header("State")]
    public bool IsDialogueActive { get; private set; }

    private DialogueConversation currentConversation;
    private DialogueNode currentNode;
    private Transform currentInteractor;

    private bool waitingForContinue;
    private bool endingDialogue;

    private void Awake()
    {
        Instance = this;

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }
    }

    private void Update()
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            HandleContinueInput();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndDialogue();
        }
    }

    public void StartDialogue(DialogueConversation conversation, Object source = null, Transform interactor = null)
    {
        if (conversation == null)
        {
            Debug.LogWarning("[DialogueManager] Cannot start null conversation.");
            return;
        }

        if (IsDialogueActive)
        {
            return;
        }

        if (!conversation.canRepeat &&
            DialogueFlagManager.Instance != null &&
            DialogueFlagManager.Instance.HasFlag("dialogue_completed_" + conversation.conversationID))
        {
            Debug.Log($"[DialogueManager] Conversation already completed: {conversation.conversationID}");
            return;
        }

        currentConversation = conversation;
        currentInteractor = interactor;

        DialogueNode startNode = conversation.GetStartNode();

        if (startNode == null)
        {
            Debug.LogWarning($"[DialogueManager] Conversation has no start node: {conversation.name}");
            return;
        }

        IsDialogueActive = true;
        endingDialogue = false;

        if (dialogueUI != null)
        {
            dialogueUI.ForceHidePrompt();
        }

        ShowNode(startNode);
    }

    public void StartNPCConversation(DialogueConversation conversation, DialogueActor[] actors = null)
    {
        StartDialogue(conversation, null, null);
    }

    private void ShowNode(DialogueNode node)
    {
        if (node == null)
        {
            EndDialogue();
            return;
        }

        if (!node.IsAvailable())
        {
            if (!string.IsNullOrWhiteSpace(node.nextNodeID))
            {
                ShowNode(currentConversation.GetNode(node.nextNodeID));
                return;
            }

            EndDialogue();
            return;
        }

        currentNode = node;
        waitingForContinue = false;

        currentNode.ApplyEnterEffects();

        if (dialogueUI != null)
        {
            dialogueUI.ShowLine(currentNode.speaker, currentNode.line, currentNode.lineType);
        }

        if (currentNode.autoContinue)
        {
            StartCoroutine(AutoContinueAfterDelay(currentNode.autoContinueDelay));
            return;
        }

        StartCoroutine(ShowChoicesAfterTyping());
    }

    private IEnumerator AutoContinueAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (!IsDialogueActive || currentNode == null)
        {
            yield break;
        }

        MoveToNextNode();
    }

    private IEnumerator ShowChoicesAfterTyping()
    {
        while (dialogueUI != null && dialogueUI.IsTyping)
        {
            yield return null;
        }

        if (!IsDialogueActive || currentNode == null)
        {
            yield break;
        }

        List<DialogueChoice> visibleChoices = GetAvailableChoiceList(currentNode);

        if (visibleChoices.Count > 0)
        {
            dialogueUI.ShowChoices(visibleChoices, SelectChoiceFromVisibleList);
            waitingForContinue = false;
        }
        else
        {
            dialogueUI.ShowContinuePrompt();
            waitingForContinue = true;
        }
    }

    private List<DialogueChoice> GetAvailableChoiceList(DialogueNode node)
    {
        List<DialogueChoice> result = new List<DialogueChoice>();

        if (node.choices == null || node.choices.Length == 0)
        {
            return result;
        }

        foreach (DialogueChoice choice in node.choices)
        {
            if (choice == null)
            {
                continue;
            }

            if (choice.IsAvailable() || !choice.hideIfUnavailable)
            {
                result.Add(choice);
            }
        }

        return result;
    }

    private void SelectChoiceFromVisibleList(int visibleChoiceIndex)
    {
        if (currentNode == null)
        {
            return;
        }

        List<DialogueChoice> visibleChoices = GetAvailableChoiceList(currentNode);

        if (visibleChoiceIndex < 0 || visibleChoiceIndex >= visibleChoices.Count)
        {
            return;
        }

        DialogueChoice selectedChoice = visibleChoices[visibleChoiceIndex];

        if (selectedChoice == null || !selectedChoice.IsAvailable())
        {
            return;
        }

        selectedChoice.ApplyEffects();

        currentNode.ApplyExitEffects();

        if (string.IsNullOrWhiteSpace(selectedChoice.nextNodeID))
        {
            EndDialogue();
            return;
        }

        DialogueNode nextNode = currentConversation.GetNode(selectedChoice.nextNodeID);
        ShowNode(nextNode);
    }

    private void HandleContinueInput()
    {
        if (dialogueUI != null && dialogueUI.IsTyping)
        {
            dialogueUI.CompleteTypingImmediately();
            StartCoroutine(ShowChoicesAfterTyping());
            return;
        }

        if (waitingForContinue)
        {
            MoveToNextNode();
        }
    }

    private void MoveToNextNode()
    {
        if (currentNode == null)
        {
            EndDialogue();
            return;
        }

        currentNode.ApplyExitEffects();

        if (string.IsNullOrWhiteSpace(currentNode.nextNodeID))
        {
            EndDialogue();
            return;
        }

        DialogueNode nextNode = currentConversation.GetNode(currentNode.nextNodeID);
        ShowNode(nextNode);
    }

    public void EndDialogue()
    {
        if (endingDialogue)
        {
            return;
        }

        endingDialogue = true;

        if (currentConversation != null &&
            currentConversation.markCompletedOnEnd &&
            DialogueFlagManager.Instance != null)
        {
            DialogueFlagManager.Instance.SetFlag("dialogue_completed_" + currentConversation.conversationID);
        }

        IsDialogueActive = false;
        waitingForContinue = false;

        currentConversation = null;
        currentNode = null;
        currentInteractor = null;

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
            dialogueUI.ForceHidePrompt();
        }

        endingDialogue = false;
    }
}