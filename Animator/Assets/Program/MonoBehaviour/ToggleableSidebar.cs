using UnityEngine;

public class ToggleableSidebar : MonoBehaviour
{
    private bool hidden = false;
    public bool hideAtStart = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (hideAtStart) gameObject.SetActive(false);
    }
    public void ToggleVisibility() {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(-rect.anchoredPosition.x,rect.anchoredPosition.y);
        hidden = !hidden;
    }
    public void ToggleFullHide() {
        if (gameObject.activeSelf) gameObject.SetActive(false);
        else gameObject.SetActive(true);
        if (hidden) ToggleVisibility();
    }
}
