using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class GameBookUI : MonoBehaviour
{
    public static GameBookUI Instance;

    [Header("Root")]
    public GameObject bookPanel;

    [Header("Pages")]
    public GameObject questsPage;
    public GameObject hintsPage;
    public GameObject inventoryPage;
    public GameObject lorePage;
    public GameObject settingsPage;

    [Header("Tab Buttons")]
    public Button questsTabButton;
    public Button hintsTabButton;
    public Button inventoryTabButton;
    public Button loreTabButton;
    public Button settingsTabButton;

    [Header("Tab Labels")]
    public TMP_Text questsTabText;
    public TMP_Text hintsTabText;
    public TMP_Text inventoryTabText;
    public TMP_Text loreTabText;
    public TMP_Text settingsTabText;

    [Header("Input")]
    public Key openCloseKey = Key.J;
    public Key closeKey = Key.Escape;

    [Header("References")]
    public DialogueManager dialogueManager;
    public PlayerInteractor playerInteractor;

    [Header("Cursor")]
    public bool unlockCursorWhenOpen = true;
    public bool lockCursorWhenClosed = true;

    [Header("Pause")]
    public bool pauseGameWhenOpen = true;

    [Header("State")]
    public bool isOpen;
    public GameBookPageType currentPage = GameBookPageType.Quests;

    [Header("Tab Visuals")]
    public Color selectedTabColor = Color.white;
    public Color unselectedTabColor = Color.gray;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        AutoFindReferences();
        HookButtons();
        CloseBookInstant();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[openCloseKey].wasPressedThisFrame)
        {
            ToggleBook();
        }

        if (isOpen && Keyboard.current[closeKey].wasPressedThisFrame)
        {
            CloseBook();
        }
    }

    private void AutoFindReferences()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
        }
    }

    private void HookButtons()
    {
        if (questsTabButton != null)
        {
            questsTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Quests));
        }

        if (hintsTabButton != null)
        {
            hintsTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Hints));
        }

        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Inventory));
        }

        if (loreTabButton != null)
        {
            loreTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Lore));
        }

        if (settingsTabButton != null)
        {
            settingsTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Settings));
        }
    }

    public void ToggleBook()
    {
        if (isOpen)
        {
            CloseBook();
        }
        else
        {
            OpenBook();
        }
    }

    public void OpenBook()
    {
        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return;
        }

        isOpen = true;

        if (bookPanel != null)
        {
            bookPanel.SetActive(true);
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = false;
        }

        ShowPage(currentPage);

        if (unlockCursorWhenOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }
    }

    public void CloseBook()
    {
        isOpen = false;

        if (bookPanel != null)
        {
            bookPanel.SetActive(false);
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = true;
        }

        if (lockCursorWhenClosed)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }

    private void CloseBookInstant()
    {
        isOpen = false;

        if (bookPanel != null)
        {
            bookPanel.SetActive(false);
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }

    public void ShowPage(GameBookPageType page)
    {
        currentPage = page;

        SetPageActive(questsPage, page == GameBookPageType.Quests);
        SetPageActive(hintsPage, page == GameBookPageType.Hints);
        SetPageActive(inventoryPage, page == GameBookPageType.Inventory);
        SetPageActive(lorePage, page == GameBookPageType.Lore);
        SetPageActive(settingsPage, page == GameBookPageType.Settings);

        UpdateTabVisuals();
    }

    private void SetPageActive(GameObject pageObject, bool active)
    {
        if (pageObject != null)
        {
            pageObject.SetActive(active);
        }
    }

    private void UpdateTabVisuals()
    {
        SetTabTextColor(questsTabText, currentPage == GameBookPageType.Quests);
        SetTabTextColor(hintsTabText, currentPage == GameBookPageType.Hints);
        SetTabTextColor(inventoryTabText, currentPage == GameBookPageType.Inventory);
        SetTabTextColor(loreTabText, currentPage == GameBookPageType.Lore);
        SetTabTextColor(settingsTabText, currentPage == GameBookPageType.Settings);
    }

    private void SetTabTextColor(TMP_Text text, bool selected)
    {
        if (text != null)
        {
            text.color = selected ? selectedTabColor : unselectedTabColor;
        }
    }
}