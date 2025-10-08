// CardInfo.cs (MODIFIED VERSION)
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Needed for coroutine if used here, but not necessary

public class CardInfo : MonoBehaviour
{
    public Image card;
    public Image icon;
    public Text name;
    public Image[] starts;
    public int starNum = 0; // Current number of lit stars (0-5)
    public Button delBtn;

    void Start()
    {
        // --- Ensure stars visually match initial starNum ---
        UpdateStarVisuals(); // Call this to set initial state

        // --- Setup Star Listeners ---
        if (starts != null)
        {
            for (int i = 0; i < starts.Length; i++)
            {
                // Get or add Button component to the star Image GameObject
                Button starButton = starts[i].GetComponent<Button>();
                if (starButton == null) starButton = starts[i].gameObject.AddComponent<Button>();

                int index = i; // Capture index for lambda expression
                starButton.onClick.RemoveAllListeners(); // Clear previous listeners
                starButton.onClick.AddListener(() => OnStarClick(index));
            }
        } else {
            Debug.LogError($"Star array not assigned for {gameObject.name}");
        }


        // --- Setup Delete Button Listener ---
        if (delBtn != null)
        {
            delBtn.onClick.RemoveAllListeners(); // Clear previous listeners
            delBtn.onClick.AddListener(DelCard);
        } else {
             Debug.LogError($"Delete Button not assigned for {gameObject.name}");
        }
    }

    // Star click event handler
    void OnStarClick(int index) // index is 0-based (0 to 4)
    {
        int clickedStarValue = index + 1; // Represents 1 to 5 stars

        // Updated Logic: If clicking the same star level that's already set, turn it off.
        // Otherwise, set the level to the clicked star.
        if (clickedStarValue == starNum)
        {
            starNum = clickedStarValue - 1; // Equivalent to turning off the last star
        }
        else
        {
            starNum = clickedStarValue; // Set to the clicked level
        }

        // Ensure starNum stays within bounds (0-5)
        starNum = Mathf.Clamp(starNum, 0, starts.Length);

        // Update the visual appearance of the stars
        UpdateStarVisuals();

        // --- NO direct call to AIManager here ---
        // AIManager will read this starNum when UpdateWeightsFromCards is called.
    }

    // Helper method to update the color of stars based on starNum
    public void UpdateStarVisuals()
    {
        if (starts == null) return;
        for (int i = 0; i < starts.Length; i++)
        {
            // If the star's index (0 to 4) is less than the current star number (1 to 5), color it yellow.
            starts[i].color = (i < starNum) ? Color.yellow : Color.white;
        }
    }


    void DelCard()
    {
        // 1. Notify the controller that manages card counts and source card states
        if (JiaQuanController.Instance != null)
        {
            JiaQuanController.Instance.RemoveCard(transform); // Handles card count and source enabling
        }

        // 2. *** CRITICAL MODIFICATION ***
        // Notify AIManager to update weights AFTER this object is destroyed
        if (AIManager.Instance != null)
        {
            AIManager.Instance.TriggerDelayedWeightUpdate(); // Uses the coroutine in AIManager
        }
        else
        {
            Debug.LogError("AIManager Instance not found when deleting card!");
        }

        // 3. Destroy this card GameObject
        Destroy(gameObject); // Schedule destruction
    }

    // Removed Update() as it wasn't used
}