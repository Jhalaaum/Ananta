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

    [Header("Tab Background Colors")]
    public Color selectedTabBackgroundColor;
    public Color unselectedTabBackgroundColor = new Color(0.18f, 0.14f, 0.09f, 1f);

    [Header("Tab Text Colors")]
    public Color selectedTabTextColor = Color.black;
    public Color unselectedTabTextColor = new Color(0.82f, 0.76f, 0.65f, 1f);

    private Image questsTabImage;
    private Image hintsTabImage;
    private Image inventoryTabImage;
    private Image loreTabImage;
    private Image settingsTabImage;

    private void Awake()
    {
        Instance = this;

        // Selected gold color: #C9A45D
        ColorUtility.TryParseHtmlString("#C9A45D", out selectedTabBackgroundColor);
    }

    private void Start()
    {
        AutoFindReferences();
        CacheTabImages();
        HookButtons();
        CloseBookInstant();
        ShowPage(currentPage);
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

    private void CacheTabImages()
    {
        questsTabImage = questsTabButton != null ? questsTabButton.GetComponent<Image>() : null;
        hintsTabImage = hintsTabButton != null ? hintsTabButton.GetComponent<Image>() : null;
        inventoryTabImage = inventoryTabButton != null ? inventoryTabButton.GetComponent<Image>() : null;
        loreTabImage = loreTabButton != null ? loreTabButton.GetComponent<Image>() : null;
        settingsTabImage = settingsTabButton != null ? settingsTabButton.GetComponent<Image>() : null;
    }

    private void HookButtons()
    {
        if (questsTabButton != null)
        {
            questsTabButton.onClick.RemoveAllListeners();
            questsTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Quests));
        }

        if (hintsTabButton != null)
        {
            hintsTabButton.onClick.RemoveAllListeners();
            hintsTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Hints));
        }

        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.RemoveAllListeners();
            inventoryTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Inventory));
        }

        if (loreTabButton != null)
        {
            loreTabButton.onClick.RemoveAllListeners();
            loreTabButton.onClick.AddListener(() => ShowPage(GameBookPageType.Lore));
        }

        if (settingsTabButton != null)
        {
            settingsTabButton.onClick.RemoveAllListeners();
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

        DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();

        if (dialogueUI != null)
        {
            dialogueUI.ForceHidePrompt();
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
        bool questsSelected = currentPage == GameBookPageType.Quests;
        bool hintsSelected = currentPage == GameBookPageType.Hints;
        bool inventorySelected = currentPage == GameBookPageType.Inventory;
        bool loreSelected = currentPage == GameBookPageType.Lore;
        bool settingsSelected = currentPage == GameBookPageType.Settings;

        SetTabVisual(
            questsTabImage,
            questsTabText,
            questsSelected
        );

        SetTabVisual(
            hintsTabImage,
            hintsTabText,
            hintsSelected
        );

        SetTabVisual(
            inventoryTabImage,
            inventoryTabText,
            inventorySelected
        );

        SetTabVisual(
            loreTabImage,
            loreTabText,
            loreSelected
        );

        SetTabVisual(
            settingsTabImage,
            settingsTabText,
            settingsSelected
        );
    }

    private void SetTabVisual(Image tabImage, TMP_Text tabText, bool selected)
    {
        if (tabImage != null)
        {
            tabImage.color = selected
                ? selectedTabBackgroundColor
                : unselectedTabBackgroundColor;
        }

        if (tabText != null)
        {
            tabText.color = selected
                ? selectedTabTextColor
                : unselectedTabTextColor;
        }
    }
}