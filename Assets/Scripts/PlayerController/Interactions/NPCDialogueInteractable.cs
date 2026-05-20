    using UnityEngine;

    public class NPCDialogueInteractable : MonoBehaviour, IInteractable
    {
        [Header("Prompt")]
        public string promptText = "Press E to talk";

        [Header("Dialogue References")]
        public StoryNPCDialogue storyNPC;
        public SimpleNPCDialogue simpleNPC;

        [Header("Manager References")]
        public DialogueManager dialogueManager;

        [Header("Settings")]
        public bool canInteract = true;

        private void Awake()
        {
            if (storyNPC == null)
            {
                storyNPC = GetComponentInParent<StoryNPCDialogue>();
            }

            if (simpleNPC == null)
            {
                simpleNPC = GetComponentInParent<SimpleNPCDialogue>();
            }

            if (dialogueManager == null)
            {
                dialogueManager = FindObjectOfType<DialogueManager>();
            }
        }

        public string GetPromptText()
        {
            return promptText;
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

            return storyNPC != null || simpleNPC != null;
        }

        public void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (storyNPC != null)
            {
                DialogueConversation conversation = storyNPC.GetConversation();

                if (conversation != null && dialogueManager != null)
                {
                    dialogueManager.StartDialogue(conversation, storyNPC, interactor);
                }

                return;
            }

            if (simpleNPC != null)
            {
                DialogueConversation conversation = simpleNPC.CreateRandomConversation();

                if (conversation != null && dialogueManager != null)
                {
                    dialogueManager.StartDialogue(conversation, null, interactor);
                }
            }
        }
    }