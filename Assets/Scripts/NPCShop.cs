using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NPCShop : MonoBehaviour
{
    [Header("Shop Items")]
    public List<Item> itemsForSale = new List<Item>(3); // 3 items per shop
    public float lightCostPerItem = 3f;

    [Header("UI")]
    public GameObject shopUIPanel;
    public TMP_Text[] itemTexts; // Array of 3 TMP_Text elements
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Popup")]
    public TMP_Text popupText;
    public float popupDuration = 3f;        // Seconds
    public float popupFloatSpeed = 1f;      // Units per second

    private int selectedIndex = 0;
    private bool isPlayerInRange = false;
    private bool shopActive = false;

    private PlayerLight playerLight;
    private PlayerController playerController;

    // Called from PlayerController OnInteract
    public void Interact()
    {
        if (!shopActive && isPlayerInRange)
            OpenShop();
    }

    // Navigation from PlayerController
    public void NavigateUp()
    {
        if (!shopActive) return;
        selectedIndex = (selectedIndex + itemsForSale.Count - 1) % itemsForSale.Count;
        UpdateUI();
    }

    public void NavigateDown()
    {
        if (!shopActive) return;
        selectedIndex = (selectedIndex + 1) % itemsForSale.Count;
        UpdateUI();
    }

    // Attempt to purchase the selected item
    public void AttemptPurchase()
    {
        if (!shopActive || playerLight == null || playerController == null) return;

        if (playerLight.CurrentIntensity < lightCostPerItem)
        {
            ShowPopup("You don't have enough light to buy this...");
            return;
        }

        // Reduce light permanently with shrink effect
        playerLight.ReduceLight(lightCostPerItem);

        // Give item
        Item boughtItem = itemsForSale[selectedIndex];
        playerController.inventory.AddItem(boughtItem);

        // Dramatic message
        ShowPopup("You don’t know why, but a part of you feels empty… a void growing within your chest, like you gave a part of yourself away.");

        UpdateUI();

        Debug.Log($"Bought {boughtItem.itemName} for {lightCostPerItem} light!");
    }

    // Close the shop
    public void CloseShop()
    {
        shopActive = false;
        shopUIPanel.SetActive(false);
        if (playerController != null)
            playerController.enabled = true;
    }

    // Update shop UI selection
    private void UpdateUI()
    {
        for (int i = 0; i < itemTexts.Length; i++)
        {
            if (i >= itemsForSale.Count) continue;
            itemTexts[i].text = itemsForSale[i].itemName + $" (Cost: {lightCostPerItem})";
            itemTexts[i].color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }

    private void OpenShop()
    {
        shopActive = true;
        shopUIPanel.SetActive(true);
        if (playerController != null)
            playerController.enabled = false; // Stop player movement
        UpdateUI();
    }

    // Show floating, fading popup message
    private void ShowPopup(string message)
    {
        if (popupText != null)
        {
            StopAllCoroutines(); // stop previous popup if still running
            StartCoroutine(ShowPopupCoroutine(message));
        }
    }

    private IEnumerator ShowPopupCoroutine(string message)
    {
        popupText.text = message;
        popupText.alpha = 1f;

        Vector3 startPos = popupText.transform.position;
        Vector3 endPos = startPos + new Vector3(0, popupFloatSpeed * popupDuration, 0);

        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            // Lerp position and alpha
            popupText.transform.position = Vector3.Lerp(startPos, endPos, t);
            popupText.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        popupText.alpha = 0f;
        popupText.transform.position = startPos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerLight = collision.GetComponent<PlayerLight>();
            playerController = collision.GetComponent<PlayerController>();
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            CloseShop();
        }
    }
}
