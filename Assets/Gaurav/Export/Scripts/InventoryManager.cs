using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("References")]
    private Movement playerMovement;
    public GameObject playerCam;
    private GameObject interactionUI;
    public GameObject UiCanvas;
    private Camera mainCamera;

    [Header("Inventory Data")]
    public InventoryItem[] allItems;
    public bool[] playerInventory;
    public GunManager gunManager;

    [Header("Weapon Wheel UI")]
    // FIX: Assign this to the weapon wheel PANEL specifically, NOT the root UiCanvas.
    // In Unity: create a child GameObject under UiCanvas called "WeaponWheelPanel",
    // add a CanvasGroup component to it, and assign it here.
    public GameObject weaponWheelPanel;
    private CanvasGroup wheelCanvasGroup;
    private RectTransform selectionHighlight;
    public GameObject iconPrefab;
    public Transform iconContainer;
    public float wheelRadius = 250f;

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public float selectedScale = 1.3f;

    private Image[] weaponIcons;
    private int lastSelectedSlice = -1;
    private bool isWheelOpen = false;
    public float validPickupDistance = 30f;
    public Transform gunPoint;

    void Start()
    {
        interactionUI = UiCanvas.transform.Find("Pick").gameObject;
        playerMovement = GetComponent<Movement>();
        gunManager = GetComponent<GunManager>();
        mainCamera = playerCam.GetComponentInChildren<Camera>();

        // FIX: Get CanvasGroup from the dedicated wheel panel, not the root UiCanvas.
        // This prevents the whole canvas from being hidden.
        if (weaponWheelPanel != null)
        {
            wheelCanvasGroup = weaponWheelPanel.GetComponent<CanvasGroup>();
            if (wheelCanvasGroup == null)
                wheelCanvasGroup = weaponWheelPanel.AddComponent<CanvasGroup>(); // auto-add if missing
        }
        else
        {
            Debug.LogError("InventoryManager: weaponWheelPanel is not assigned! " +
                           "Create a child panel under UiCanvas for the wheel and assign it.");
        }

        selectionHighlight = UiCanvas.transform.Find("weaponWheel").Find("SelectionHighlight").GetComponent<RectTransform>();

        playerInventory = new bool[allItems.Length];
        for (int i = 0; i < allItems.Length; i++)
            playerInventory[i] = true;

        if (playerInventory.Length > 0) playerInventory[0] = true;
        if (selectionHighlight != null) selectionHighlight.gameObject.SetActive(false);

        // FIX: Only hide the wheel panel, not the entire canvas
        if (wheelCanvasGroup != null)
        {
            wheelCanvasGroup.alpha = 0;
            wheelCanvasGroup.blocksRaycasts = false;
        }

        CreateWheelElements();
    }

    void Update()
    {
        if (isWheelOpen)
            ProcessSelection(Pointer.current.position.ReadValue());
        else
            HandleInteractionRaycast();
    }

    // --- INPUT ACTION CALLBACK ---
    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.started) OpenWheel();
        else if (context.canceled) CloseWheel();
    }

    private void OpenWheel()
    {
        CreateWheelElements();
        isWheelOpen = true;
        playerMovement.SetMovementState(false);

        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // FIX: Only show the wheel panel
        if (wheelCanvasGroup != null)
        {
            wheelCanvasGroup.alpha = 1f;
            wheelCanvasGroup.blocksRaycasts = true;
        }
    }

    private void CloseWheel()
    {
        Debug.Log("Attempting to equip index: " + lastSelectedSlice);
        if (selectionHighlight != null) selectionHighlight.gameObject.SetActive(false);

        isWheelOpen = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        playerMovement.SetMovementState(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // FIX: Only hide the wheel panel
        if (wheelCanvasGroup != null)
        {
            wheelCanvasGroup.alpha = 0f;
            wheelCanvasGroup.blocksRaycasts = false;
        }

        if (lastSelectedSlice != -1 && playerInventory[lastSelectedSlice])
        {
            foreach (Transform child in gunPoint) Destroy(child.gameObject);

            GameObject item = Instantiate(allItems[lastSelectedSlice].weaponPrefab);
            item.transform.SetParent(gunPoint, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            ItemCategory currentCat = allItems[lastSelectedSlice].category;

            if (currentCat == ItemCategory.Firearm)
            {
                UiCanvas.transform.Find("Melee energy").gameObject.SetActive(false);
                Debug.Log("Equipping Gun");
                playerMovement.currentGun = item.GetComponent<Gun>();
                gunManager.currentGun = item.GetComponent<Gun>();
                SetLayerRecursively(item.gameObject, LayerMask.NameToLayer("Player"));
                playerMovement.currentMelee = null;
                playerMovement.currentThrowable = null;
                playerMovement.animator.SetInteger("WeaponType", 1);
            }
            else if (currentCat == ItemCategory.Melee)
            {
                playerMovement.animator.SetInteger("WeaponType", 2);
                playerMovement.currentMelee = item.GetComponent<MeleeWeapon>();
                UiCanvas.transform.Find("Melee energy").gameObject.SetActive(true);
                item.GetComponent<MeleeWeapon>().slider = UiCanvas.transform.Find("Melee energy").GetComponentInChildren<Slider>();
                playerMovement.currentGun = null;
                playerMovement.currentThrowable = null;
                if (playerMovement.currentMelee != null)
                    playerMovement.currentMelee.Initialize(playerMovement.animator);
            }
            else if (currentCat == ItemCategory.Throwable)
            {
                playerMovement.animator.SetInteger("WeaponType", 3);
                Grenade gr = item.GetComponent<Grenade>();
                gr.lineRenderer = playerMovement.lineRenderer;
                playerMovement.currentThrowable = gr;
                playerMovement.currentGun = null;
                playerMovement.currentMelee = null;
            }

            Debug.Log("Equipped: " + allItems[lastSelectedSlice].name);
        }
        else
        {
            Debug.Log("Selected an empty slot or nothing.");
        }
    }

    private void ProcessSelection(Vector2 inputPos)
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 direction = inputPos - screenCenter;

        if (direction.magnitude > 40f)
        {
            if (!selectionHighlight.gameObject.activeSelf)
                selectionHighlight.gameObject.SetActive(true);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float adjustedAngle = (90f - angle + 360f) % 360f;
            float degreesPerSlice = 360f / allItems.Length;
            int currentSlice = Mathf.FloorToInt(adjustedAngle / degreesPerSlice);

            if (currentSlice != lastSelectedSlice)
            {
                UpdateWheelVisuals(currentSlice);
                lastSelectedSlice = currentSlice;
            }
        }
    }

    private void UpdateWheelVisuals(int selectedIndex)
    {
        float degreesPerSlice = 360f / allItems.Length;
        float targetRotation = (-selectedIndex * degreesPerSlice) + (degreesPerSlice / 2f);
        selectionHighlight.localRotation = Quaternion.Euler(0, 0, targetRotation);

        for (int i = 0; i < weaponIcons.Length; i++)
        {
            bool isSelected = (i == selectedIndex);
            weaponIcons[i].color = isSelected ? selectedColor : (playerInventory[i] ? normalColor : Color.gray);
            weaponIcons[i].transform.localScale = Vector3.one * (isSelected ? selectedScale : 1f);
        }
    }

    void HandleInteractionRaycast()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, validPickupDistance))
        {
            if (hit.collider.CompareTag("Pickable"))
            {
                interactionUI.SetActive(true);
                if (Keyboard.current.eKey.wasPressedThisFrame) PickUpItem(hit.collider.gameObject);
            }
            else interactionUI.SetActive(false);
        }
        else interactionUI.SetActive(false);
    }

    void PickUpItem(GameObject obj)
    {
        ItemInfo info = obj.GetComponent<ItemInfo>();
        if (info != null)
        {
            bool found = false;
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i].weaponID == info.weaponID)
                {
                    playerInventory[i] = true;
                    Debug.Log("Added to wheel: " + allItems[i].name);
                    found = true;
                    break;
                }
            }
            if (found) CreateWheelElements();
            else Debug.LogError("No ScriptableObject found with ID: " + info.weaponID);
        }
        Destroy(obj);
    }

    void CreateWheelElements()
    {
        foreach (Transform child in iconContainer) Destroy(child.gameObject);

        weaponIcons = new Image[allItems.Length];

        if (selectionHighlight != null)
            selectionHighlight.GetComponent<Image>().fillAmount = 1f / allItems.Length;

        for (int i = 0; i < allItems.Length; i++)
        {
            GameObject newIcon = Instantiate(iconPrefab, iconContainer);
            float angle = i * Mathf.PI * 2 / allItems.Length;
            newIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                Mathf.Sin(angle) * wheelRadius,
                Mathf.Cos(angle) * wheelRadius
            );
            weaponIcons[i] = newIcon.GetComponent<Image>();
            weaponIcons[i].sprite = allItems[i].icon;
            weaponIcons[i].enabled = playerInventory[i];
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}