// Attach this to "WeaponGroup" once to align your icons, then you can remove it.
using UnityEngine;

public class CircleAligner : MonoBehaviour
{
    public float radius = 100f;
    void OnValidate()
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            float angle = i * Mathf.PI * 2 / childCount;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            ((RectTransform)transform.GetChild(i)).anchoredPosition = new Vector2(x, y);
        }
    }
}