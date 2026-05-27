using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryInputController : MonoBehaviour
{
    [Header("Keys")]
    public Key dropKey = Key.Q;
    public Key useKey = Key.R;

    [Header("Mouse Wheel")]
    public bool useMouseWheel = true;

    [Header("Blockers")]
    public DialogueManager dialogueManager;

    private void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    private void Update()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        if (ShouldBlockInput())
        {
            return;
        }

        HandleNumberKeys();
        HandleMouseWheel();
        HandleUseAndDrop();
    }

    private bool ShouldBlockInput()
    {
        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return true;
        }

        if (GameBookUI.Instance != null && GameBookUI.Instance.isOpen)
        {
            return true;
        }

        return false;
    }

    private void HandleNumberKeys()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(5);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(6);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(7);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(8);
    }

    private void HandleMouseWheel()
    {
        if (!useMouseWheel || Mouse.current == null)
        {
            return;
        }

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0.1f)
        {
            InventoryManager.Instance.SelectPreviousHotbarSlot();
        }
        else if (scroll < -0.1f)
        {
            InventoryManager.Instance.SelectNextHotbarSlot();
        }
    }

    private void HandleUseAndDrop()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[dropKey].wasPressedThisFrame)
        {
            InventoryManager.Instance.DropSelectedHotbarItem();
        }

        if (Keyboard.current[useKey].wasPressedThisFrame)
        {
            InventoryManager.Instance.UseSelectedHotbarItem();
        }
    }
}