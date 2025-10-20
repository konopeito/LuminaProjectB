using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory inventory;
    public Image[] hotbarSlots;
    public Image selector; // overlay image

    [Header("Colors")]
    public Color normalColor = Color.white;

    [Header("Optional")]
    public Sprite emptySlotSprite;

    [Header("Selector Blink")]
    public float blinkRate = 0.5f;
    public float visibleDuration = 2f;

    private Coroutine blinkCoroutine;
    private Coroutine hideCoroutine;

    void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;
    }

    void Start()
    {
        if (selector != null)
            selector.gameObject.SetActive(false);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (inventory == null || hotbarSlots.Length == 0) return;

        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i].enabled = true;

            if (i < inventory.items.Count && inventory.items[i] != null)
                hotbarSlots[i].sprite = inventory.items[i].icon;
            else
                hotbarSlots[i].sprite = emptySlotSprite;

            hotbarSlots[i].color = normalColor;
        }

        MoveSelectorToCurrentSlot();
    }

    private void MoveSelectorToCurrentSlot()
    {
        if (selector == null) return;

        // Only show selector if inventory has items
        if (inventory.items.Count == 0)
        {
            selector.gameObject.SetActive(false);
            return;
        }

        // Clamp selectedIndex
        if (inventory.selectedIndex >= inventory.items.Count)
            inventory.selectedIndex = inventory.items.Count - 1;

        RectTransform slotRT = hotbarSlots[inventory.selectedIndex].rectTransform;
        RectTransform selectorRT = selector.rectTransform;

        selectorRT.SetParent(slotRT.parent, false);
        selectorRT.anchoredPosition = slotRT.anchoredPosition;
        selectorRT.SetAsLastSibling();

        ShowSelectorTemporarily();
    }

    public void ShowSelectorTemporarily()
    {
        if (selector == null) return;

        selector.gameObject.SetActive(true);
        SetSelectorAlpha(1f);

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        blinkCoroutine = StartCoroutine(BlinkSelector());
        hideCoroutine = StartCoroutine(HideSelectorAfterDelay());
    }

    private IEnumerator BlinkSelector()
    {
        Image selImage = selector.GetComponent<Image>();
        if (selImage == null) yield break;

        while (true)
        {
            Color c = selImage.color;
            c.a = (c.a > 0.5f) ? 0f : 1f;
            selImage.color = c;
            yield return new WaitForSeconds(blinkRate);
        }
    }

    private IEnumerator HideSelectorAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        if (selector != null)
            selector.gameObject.SetActive(false);
    }

    private void SetSelectorAlpha(float alpha)
    {
        if (selector == null) return;
        Image img = selector.GetComponent<Image>();
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
