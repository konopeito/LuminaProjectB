using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ControlsMenuManager : MonoBehaviour
{
    [Header("References")]
    public GameObject controlsPanel; // root panel
    public Button okButton;           // button reference

    private bool isVisible = false;

    private void Awake()
    {
        // Ensure panel is hidden initially
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        // Hook up the button
        if (okButton != null)
            okButton.onClick.AddListener(CloseMenu);
    }

    private void Start()
    {
        Debug.Log("ControlsMenuManager Start called!");
        if (controlsPanel == null)
            Debug.LogError("Controls Panel is not assigned!");
        else
            Debug.Log("Controls Panel assigned: " + controlsPanel.name);

        StartCoroutine(ShowMenuNextFrame());
    }


    private void Update()
    {
        // Close menu with Enter, Space, or Escape
        if (isVisible && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            CloseMenu();
    }

    private IEnumerator ShowMenuNextFrame()
    {
        yield return null; // wait 1 frame so player and scene are initialized
        ShowMenu();
    }

    public void ShowMenu()
    {
        if (isVisible || controlsPanel == null) return;

        isVisible = true;
        controlsPanel.SetActive(true);
        Time.timeScale = 0f; // pause gameplay
    }

    public void CloseMenu()
    {
        if (!isVisible || controlsPanel == null) return;

        isVisible = false;
        controlsPanel.SetActive(false);
        Time.timeScale = 1f; // resume gameplay
    }
}
