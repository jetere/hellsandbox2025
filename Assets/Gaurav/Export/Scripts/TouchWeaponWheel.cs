using UnityEngine;
using UnityEngine.EventSystems;

public class TouchWeaponWheel : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform handle; // Drag "JoystickHandle" here
    public CanvasGroup wheelGroup; // Drag the parent of your weapons here
    private Vector2 startPos;
    private float radius;

    void Start()
    {
        radius = GetComponent<RectTransform>().sizeDelta.x / 2;
        wheelGroup.alpha = 0; // Hide it at the start
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Move the entire base to where your finger touched
        transform.position = eventData.position;
        wheelGroup.alpha = 1; // Show the wheel
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentTouch = eventData.position;
        Vector2 direction = currentTouch - (Vector2)transform.position;

        // Clamp the handle so it doesn't fly off the screen
        Vector2 clampedPos = Vector2.ClampMagnitude(direction, radius);
        handle.anchoredPosition = clampedPos;

        // --- CALCULATION LOGIC ---
        float angle = Mathf.Atan2(clampedPos.y, clampedPos.x) * Mathf.Rad2Deg;
        float adjustedAngle = (angle + 360 + 90) % 360;
        int selectedSlice = Mathf.FloorToInt(adjustedAngle / 45f);

        // You can use 'selectedSlice' here to light up your weapon icons!
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        wheelGroup.alpha = 0; // Hide it when finger is lifted
        handle.anchoredPosition = Vector2.zero; // Reset handle

        // CODE HERE: Equip the weapon based on the last selectedSlice
    }
}