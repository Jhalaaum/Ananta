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

    [Header("Cursor")]
    public bool unlockCursorDuringDialogue = true;
    public bool restoreCursorAfterDialogue = true;

    private DialogueConversation currentConversation;
    private DialogueNode currentNode;
    private Transform currentInteractor;

    private bool waitingForContinue;
    private bool endingDialogue;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private List<DialogueChoice> currentVisibleChoices = new List<DialogueChoice>();

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

        HandleChoiceNumberKeys();

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

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        if (unlockCursorDuringDialogue)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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
        currentVisibleChoices.Clear();

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

        currentVisibleChoices = GetVisibleChoiceList(currentNode);

        if (currentVisibleChoices.Count > 0)
        {
            dialogueUI.ShowChoices(currentVisibleChoices, SelectChoiceFromVisibleList);
            waitingForContinue = false;

            if (unlockCursorDuringDialogue)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
        else
        {
            dialogueUI.ShowContinuePrompt();
            waitingForContinue = true;
        }
    }

    private List<DialogueChoice> GetVisibleChoiceList(DialogueNode node)
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
        if (currentVisibleChoices == null)
        {
            return;
        }

        if (visibleChoiceIndex < 0 || visibleChoiceIndex >= currentVisibleChoices.Count)
        {
            return;
        }

        DialogueChoice selectedChoice = currentVisibleChoices[visibleChoiceIndex];

        if (selectedChoice == null || !selectedChoice.IsAvailable())
        {
            return;
        }

        selectedChoice.ApplyEffects();

        if (currentNode != null)
        {
            currentNode.ApplyExitEffects();
        }

        currentVisibleChoices.Clear();

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

        if (dialogueUI != null && dialogueUI.HasChoicesVisible)
        {
            return;
        }

        if (waitingForContinue)
        {
            MoveToNextNode();
        }
    }

    private void HandleChoiceNumberKeys()
    {
        if (currentVisibleChoices == null || currentVisibleChoices.Count == 0)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectChoiceFromVisibleList(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectChoiceFromVisibleList(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectChoiceFromVisibleList(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectChoiceFromVisibleList(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectChoiceFromVisibleList(4);
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
        currentVisibleChoices.Clear();

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
            dialogueUI.ForceHidePrompt();
        }

        if (restoreCursorAfterDialogue)
        {
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLockMode;
        }

        endingDialogue = false;
    }
    public void ResetDialogueManagerForCheckpoint()
    {
        StopAllCoroutines();

        IsDialogueActive = false;
        waitingForContinue = false;
        endingDialogue = false;

        currentConversation = null;
        currentNode = null;
        currentInteractor = null;

        if (currentVisibleChoices != null)
        {
            currentVisibleChoices.Clear();
        }

        if (dialogueUI != null)
        {
            dialogueUI.ResetDialogueUIForCheckpoint();
        }

        if (restoreCursorAfterDialogue)
        {
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLockMode;
        }

        Debug.Log("[DialogueManager] Reset for checkpoint.");
    }
}