using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("References")]
    public DialogueUI dialogueUI;
    public PlayerMovement playerMovementScript;

    private DialogueConversation activeConversation;
    private StoryNPCDialogue activeNPC;
    private Transform player;

    private int currentLineIndex;
    private bool isDialogueActive;
    private bool waitingForChoice;
    private bool waitingForContinueInput;

    private DialogueActor[] activeActors;
    private DialogueMode dialogueMode;

    private Coroutine afterLineCoroutine;
    private Coroutine autoContinueCoroutine;
    private Coroutine choiceResponseCoroutine;

    private float inputUnlockTime;

    public bool IsDialogueActive => isDialogueActive;

    public void StartDialogue(DialogueConversation conversation, StoryNPCDialogue npc, Transform playerTransform)
    {
        if (conversation == null || conversation.lines == null || conversation.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] No dialogue conversation assigned.");
            return;
        }

        StopAllDialogueCoroutines();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        activeConversation = conversation;
        activeNPC = npc;
        player = playerTransform;
        activeActors = null;

        currentLineIndex = 0;
        isDialogueActive = true;
        waitingForChoice = false;
        waitingForContinueInput = false;
        dialogueMode = DialogueMode.PlayerToNPC;

        inputUnlockTime = Time.time + 0.2f;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        if (activeNPC != null)
        {
            activeNPC.StartFacingPlayer(playerTransform);
        }

        if (dialogueUI != null)
        {
            dialogueUI.HidePrompt();
            dialogueUI.ClearChoices();
        }

        ShowCurrentLine();
    }

    public void StartNPCConversation(DialogueConversation conversation, DialogueActor[] actors)
    {
        if (conversation == null || conversation.lines == null || conversation.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] No NPC conversation assigned.");
            return;
        }

        StopAllDialogueCoroutines();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        activeConversation = conversation;
        activeActors = actors;
        activeNPC = null;
        player = null;

        currentLineIndex = 0;
        isDialogueActive = true;
        waitingForChoice = false;
        waitingForContinueInput = false;
        dialogueMode = DialogueMode.NPCToNPC;

        inputUnlockTime = Time.time + 0.2f;

        // Eavesdropping should NOT freeze the player.
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        if (dialogueUI != null)
        {
            dialogueUI.HidePrompt();
            dialogueUI.ClearChoices();
        }

        ShowCurrentLine();
    }

    private void Update()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Time.time < inputUnlockTime)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndDialogue();
            return;
        }

        if (waitingForChoice)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            HandleContinueInput();
        }
    }

    private void HandleContinueInput()
    {
        if (!isDialogueActive)
        {
            return;
        }

        // If the line is still typing, E should finish the text first.
        if (dialogueUI != null && dialogueUI.IsTyping)
        {
            dialogueUI.CompleteTypingImmediately();
            return;
        }

        // If this was auto-continuing, E should skip the wait.
        if (autoContinueCoroutine != null)
        {
            StopCoroutine(autoContinueCoroutine);
            autoContinueCoroutine = null;
        }

        // Important fix:
        // NPC-to-NPC eavesdropping should be allowed to continue after typing,
        // even if waitingForContinueInput was not set correctly yet.
        if (dialogueMode == DialogueMode.NPCToNPC)
        {
            GoToNextLine();
            return;
        }

        // Player-to-NPC dialogue should only continue when the line has finished
        // and the continue prompt/continue state is ready.
        if (!waitingForContinueInput)
        {
            return;
        }

        GoToNextLine();
    }

    private void ShowCurrentLine()
    {
        StopAfterLineCoroutine();
        StopAutoContinueCoroutine();

        waitingForChoice = false;
        waitingForContinueInput = false;

        if (dialogueUI != null)
        {
            dialogueUI.ClearChoices();
        }

        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        HandleActorFacing(line);

        if (dialogueUI != null)
        {
            dialogueUI.ShowLine(line.speakerName, line.lineText);
        }

        afterLineCoroutine = StartCoroutine(WaitForLineThenShowChoicesOrContinue());
    }

    private IEnumerator WaitForLineThenShowChoicesOrContinue()
    {
        yield return null;

        while (isDialogueActive && dialogueUI != null && dialogueUI.IsTyping)
        {
            yield return null;
        }

        if (!isDialogueActive)
        {
            yield break;
        }

        ShowChoicesOrContinue();
    }

    private void ShowChoicesOrContinue()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        if (dialogueMode == DialogueMode.NPCToNPC)
        {
            HandleNPCToNPCLineEnd(line);
            return;
        }

        HandlePlayerToNPCLineEnd(line);
    }

    private void HandlePlayerToNPCLineEnd(DialogueLine line)
    {
        if (line.playerChoices != null && line.playerChoices.Length > 0)
        {
            waitingForChoice = true;
            waitingForContinueInput = false;

            if (dialogueUI != null)
            {
                dialogueUI.ShowChoices(line.playerChoices, HandleChoiceSelected);
            }

            return;
        }

        waitingForChoice = false;
        waitingForContinueInput = true;

        if (dialogueUI != null)
        {
            dialogueUI.ShowContinuePrompt();
        }
    }

    private void HandleNPCToNPCLineEnd(DialogueLine line)
    {
        waitingForChoice = false;
        waitingForContinueInput = true;

        if (line.autoContinue)
        {
            autoContinueCoroutine = StartCoroutine(AutoContinueAfterDelay(line.autoContinueDelay));
        }
        else
        {
            if (dialogueUI != null)
            {
                dialogueUI.ShowContinuePrompt();
            }
        }
    }

    private IEnumerator AutoContinueAfterDelay(float delay)
    {
        if (delay < 0f)
        {
            delay = 0f;
        }

        yield return new WaitForSeconds(delay);

        if (!isDialogueActive)
        {
            yield break;
        }

        autoContinueCoroutine = null;
        GoToNextLine();
    }

    private void HandleChoiceSelected(int choiceIndex)
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        if (line.playerChoices == null || choiceIndex < 0 || choiceIndex >= line.playerChoices.Length)
        {
            Debug.LogWarning("[DialogueManager] Invalid choice selected.");
            EndDialogue();
            return;
        }

        DialogueChoice choice = line.playerChoices[choiceIndex];

        waitingForChoice = false;
        waitingForContinueInput = false;

        StopAfterLineCoroutine();
        StopAutoContinueCoroutine();

        if (dialogueUI != null)
        {
            dialogueUI.ClearChoices();
            dialogueUI.ShowLine("You", choice.responseText);
        }

        if (choiceResponseCoroutine != null)
        {
            StopCoroutine(choiceResponseCoroutine);
        }

        choiceResponseCoroutine = StartCoroutine(ContinueAfterPlayerChoice(choice.nextLineIndex));
    }

    private IEnumerator ContinueAfterPlayerChoice(int nextLineIndex)
    {
        yield return null;

        while (isDialogueActive && dialogueUI != null && dialogueUI.IsTyping)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.35f);

        if (!isDialogueActive)
        {
            yield break;
        }

        if (nextLineIndex < 0)
        {
            EndDialogue();
            yield break;
        }

        currentLineIndex = nextLineIndex;
        ShowCurrentLine();
    }

    private void GoToNextLine()
    {
        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        if (line.playerChoices != null && line.playerChoices.Length > 0)
        {
            return;
        }

        if (line.endAfterThisLine)
        {
            EndDialogue();
            return;
        }

        if (line.nextLineIndex >= 0)
        {
            currentLineIndex = line.nextLineIndex;
        }
        else
        {
            currentLineIndex++;
        }

        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void HandleActorFacing(DialogueLine line)
    {
        if (dialogueMode != DialogueMode.NPCToNPC)
        {
            return;
        }

        if (activeActors == null)
        {
            return;
        }

        DialogueActor speaker = FindActorByID(line.speakerID);
        DialogueActor lookTarget = FindActorByID(line.lookAtTargetID);

        foreach (DialogueActor actor in activeActors)
        {
            if (actor != null)
            {
                actor.StopLooking();
            }
        }

        if (speaker != null && lookTarget != null)
        {
            speaker.LookAt(lookTarget.transform);
        }
    }

    private DialogueActor FindActorByID(string actorID)
    {
        if (string.IsNullOrEmpty(actorID) || activeActors == null)
        {
            return null;
        }

        foreach (DialogueActor actor in activeActors)
        {
            if (actor != null && actor.actorID == actorID)
            {
                return actor;
            }
        }

        return null;
    }

    private bool IsValidLineIndex(int index)
    {
        return activeConversation != null &&
               activeConversation.lines != null &&
               index >= 0 &&
               index < activeConversation.lines.Length;
    }

    public void EndDialogue()
    {
        StopAllDialogueCoroutines();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isDialogueActive = false;
        waitingForChoice = false;
        waitingForContinueInput = false;

        if (dialogueUI != null)
        {
            dialogueUI.ClearChoices();
            dialogueUI.HideDialogue();
        }

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        if (activeNPC != null)
        {
            activeNPC.MarkConversationCompleted();
            activeNPC.StopFacingPlayer();
        }

        if (activeActors != null)
        {
            foreach (DialogueActor actor in activeActors)
            {
                if (actor != null)
                {
                    actor.StopLooking();
                }
            }
        }

        activeConversation = null;
        activeNPC = null;
        player = null;
        activeActors = null;

        dialogueMode = DialogueMode.PlayerToNPC;
        currentLineIndex = 0;
    }

    public void ResetDialogueManagerForCheckpoint()
    {
        EndDialogue();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (dialogueUI != null)
        {
            dialogueUI.ClearChoices();
            dialogueUI.HidePrompt();
            dialogueUI.HideDialogue();
        }

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }
    }

    private void StopAllDialogueCoroutines()
    {
        StopAfterLineCoroutine();
        StopAutoContinueCoroutine();

        if (choiceResponseCoroutine != null)
        {
            StopCoroutine(choiceResponseCoroutine);
            choiceResponseCoroutine = null;
        }
    }

    private void StopAfterLineCoroutine()
    {
        if (afterLineCoroutine != null)
        {
            StopCoroutine(afterLineCoroutine);
            afterLineCoroutine = null;
        }
    }

    private void StopAutoContinueCoroutine()
    {
        if (autoContinueCoroutine != null)
        {
            StopCoroutine(autoContinueCoroutine);
            autoContinueCoroutine = null;
        }
    }
}