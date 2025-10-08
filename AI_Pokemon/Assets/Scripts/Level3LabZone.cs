using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using toio.Samples.Sample_Sensor;
using toio.Simulator;
using UnityEngine;
using UnityEngine.UI;

public class Level3LabZone : MonoBehaviour
{
    public static Level3LabZone Instance;

    [Header("InGame UI")]
    public CanvasGroup inGameUIGroup;

    [Header("LabZone UI")]
    public GameObject LabUI;
    public CanvasGroup labIntroGroup;
    public Button readInstructionsButton;

    public CanvasGroup instructionsGroup;
    public Image instructionsImage;
    public Button understoodButton;

    public CanvasGroup pickGroup;
    public Text pickText;
    public Button readyButton;

    [Header("Training UI")]
    public CanvasGroup trainingGroup;

    public Image datasetIconImage;
    public Text trainingInstructionText;
    public Button continueButton;
    
    public Button runEpochButton; // Next button
    public Button finishButton;

    [Header("Mode Settings")]
    public bool PhysicalButton = true;

    [Header("Other")]
    public float typingSpeed = 0.04f;
    private List<string> _datasetsToShowOnLed = new List<string>();

    private Action labSequenceCompleteCallback;
    public PokemonClassifier pokemonClassifier;
    private bool _labCompleted = false;

    [Header("Sprites")] // You'll need to assign these in the Inspector
    public Sprite fireIcon;
    public Sprite waterIcon;
    public Sprite grassIcon;
    public Sprite dragonIcon;

    // --- Method 3 Specific Variables ---
    private PokemonClassifier.ModelWeights currentModelWeights;

    private Coroutine physicalListener;

    public PokemonClassifier.ModelWeights TrainedModel { get; private set; }
    
    private List<string> fixedDatasetIDs = new List<string> { "C", "D", "E", "F" };


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        pokemonClassifier = GetComponent<PokemonClassifier>() ?? gameObject.AddComponent<PokemonClassifier>();
        pokemonClassifier.Initialize();


    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            StartCoroutine(OnReadyToTrain());
    }
    public IEnumerator StartLabZoneSequence(Action onComplete = null)
    {
        labSequenceCompleteCallback = onComplete;
        _labCompleted = false;
        LabUI.SetActive(true);

        labIntroGroup.alpha = 0;
        labIntroGroup.gameObject.SetActive(true);
        yield return FadeCanvas(labIntroGroup, 0, 1, 1f);
        AssignButtonOrPhysical(readInstructionsButton, () => StartCoroutine(OnReadInstructions()));

        yield return new WaitUntil(() => _labCompleted);
    }

    private IEnumerator OnReadInstructions()
    {
        // 1. Fade out the lab intro screen.
        yield return FadeCanvas(labIntroGroup, 1, 0, 0.5f);
        labIntroGroup.gameObject.SetActive(false);

        // --- NEW STREAMLINED FLOW ---

        // 2. Immediately show the "Pick Group" UI.
        pickGroup.alpha = 0;
        pickGroup.gameObject.SetActive(true);
        yield return FadeCanvas(pickGroup, 0, 1, 1f);

        // 3. Type out the instruction text for this lab.
        yield return TypeText(pickText, "We've discovered a big Pokemon Data! Now see how features light up on the board.");

        // 4. Wait for a moment so the child can read.
        yield return new WaitForSeconds(1.5f);

        // 5. AUTOMATICALLY proceed to the next step.
        yield return StartCoroutine(OnReadyToTrain());
    }

    private IEnumerator OnReadyToTrain()
{
    // Fade out the "Pick Group" UI
    yield return FadeCanvas(pickGroup, 1, 0, 0.5f);
    pickGroup.gameObject.SetActive(false);

    // Fade in the main Training UI
    trainingGroup.alpha = 0;
    trainingGroup.gameObject.SetActive(true);
    yield return FadeCanvas(trainingGroup, 0, 1, 1f);
    
    // Clear the temporary list at the start
    _datasetsToShowOnLed.Clear();

    // --- The New, Corrected Four-Step Flow ---

    // Step 1: Fire Dataset
    yield return StartCoroutine(TypeText(trainingInstructionText, "First, let's look at the Fire Dataset."));
    yield return StartCoroutine(WaitForSpecificCardScan("C"));
    datasetIconImage.sprite = fireIcon;
    _datasetsToShowOnLed.Add("C"); // Add ONLY the Fire dataset to our list
    UpdateLedDisplay(); // Now this will show the averages for just "C"
    yield return new WaitForSeconds(3f);

    // Step 2: Water Dataset
    yield return StartCoroutine(TypeText(trainingInstructionText, "Next up is the Water Dataset."));
    yield return StartCoroutine(WaitForSpecificCardScan("D"));
    datasetIconImage.sprite = waterIcon;
    _datasetsToShowOnLed.Add("D"); // Add the Water dataset
    UpdateLedDisplay(); // Now this will show averages for "C" and "D" combined
    yield return new WaitForSeconds(3f);
    
    // Step 3: Grass Dataset
    yield return StartCoroutine(TypeText(trainingInstructionText, "Now for the Grass Dataset."));
    yield return StartCoroutine(WaitForSpecificCardScan("E"));
    datasetIconImage.sprite = grassIcon;
    _datasetsToShowOnLed.Add("E");
    UpdateLedDisplay();
    yield return new WaitForSeconds(3f);

    // Step 4: Dragon Dataset
    yield return StartCoroutine(TypeText(trainingInstructionText, "Finally, the Dragon Dataset."));
    yield return StartCoroutine(WaitForSpecificCardScan("F"));
    datasetIconImage.sprite = dragonIcon;
    _datasetsToShowOnLed.Add("F");
    UpdateLedDisplay();
    yield return new WaitForSeconds(3f);

    // All steps are complete
    yield return StartCoroutine(TypeText(trainingInstructionText, "Great job! You've seen how each type is different."));
    yield return new WaitForSeconds(2f);
    
    OnFinishLab();
}
   


    private void UpdateLedDisplay()
    {
        // Safety check to ensure the ESP32 controller is available
        if (ESP32Controller.Instance == null)
        {
            Debug.LogWarning("ESP32Controller not found. Cannot send LED data.");
            return;
        }

        // 1. Get the dictionary of feature averages from the classifier
        var ledDataDict = pokemonClassifier.GetTrainingAveragesAsLedValues(_datasetsToShowOnLed);
        if (ledDataDict.Count == 0) return;

        // 2. Convert the dictionary to a fixed-order list of 24 numbers
        var ledValuesList = new List<int>();
        var types = new[] {
                PokemonClassifier.PokemonType.Fire,
                PokemonClassifier.PokemonType.Water,
                PokemonClassifier.PokemonType.Grass,
                PokemonClassifier.PokemonType.Dragon
            };
        // This order must match your physical LED board layout
        var features = new[] { "Attack", "Defense", "Speed", "HasWings", "HabitatTemperature", "HabitatAltitude" };

        foreach (var type in types)
        {
            foreach (var feature in features)
            {
                // Add the value for this specific type and feature to the list
                ledValuesList.Add(ledDataDict[type][feature]);
            }
        }

        // 3. Convert the list of 24 integers to a comma-separated string (CSV)
        string csvData = string.Join(",", ledValuesList);

        // 4. Send the final string to the ESP32
        Debug.Log($"Sending Fixed Dataset Averages to ESP32: {csvData}");
        ESP32Controller.Instance.SendLEDData(csvData);
    }

    
    private void OnFinishLab()
    {
        // --- THIS IS THE FIX ---
    // 1. Calculate the final model from ALL the datasets just before saving.
    //    _datasetsToShowOnLed will contain ["C", "D", "E", "F"] at this point.
    var finalModel = pokemonClassifier.GetAverageWeightsAsModel(_datasetsToShowOnLed);
    
    // 2. Save this final model to both the local variable and the GameStateManager.
    TrainedModel = finalModel;
    if (GameStateManager.Instance != null)
    {
        GameStateManager.Instance.method3_model = TrainedModel;
        Debug.Log("Lab 3 Model SAVED to GameStateManager.");
    }
    // --- END OF FIX ---

    _labCompleted = true;
    LabUI.SetActive(false);
    trainingGroup.gameObject.SetActive(false);
    FadeInGameUI();
    labSequenceCompleteCallback?.Invoke();
    }

    // --- UI Helpers (Fade, TypeText, etc.) ---

    private void AssignButtonOrPhysical(Button btn, UnityEngine.Events.UnityAction uiCallback, GameObject UIInstruction = null)
    {
        if (!PhysicalButton)
        {
            btn.gameObject.SetActive(true);
            //UIInstruction.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(uiCallback);
            return;
        }

        btn.gameObject.SetActive(false);
        //UIInstruction.gameObject.SetActive(false);

        if (physicalListener != null)
        {
            StopCoroutine(physicalListener);
        }
        physicalListener = StartCoroutine(WaitForPhysicalControl());
    }

    private IEnumerator WaitForPhysicalControl()
    {
        while (Sample_Sensor.Instance.ReadCard() != 0)
            yield return null;

        while (true)
        {
            uint currentId = Sample_Sensor.Instance.ReadCard();
            if (currentId != 0)
            {
                string cardIndex = StandardID.GetCardNameByID(currentId);
                Debug.Log($"Physical cardIndex {cardIndex}");

                if (HandlePhysicalCardInput(cardIndex))
                {
                    while (Sample_Sensor.Instance.ReadCard() != 0)
                        yield return null;

                    physicalListener = null;
                    yield break;
                }
            }
            yield return null;
        }
    }

    private bool HandlePhysicalCardInput(string cardIndex)
    {
        if (!PhysicalButton) return false;

        var cube = Sample_Sensor.Instance?.cube;

        // Next/Apply (→)
        if (cardIndex == "→")
        {
            Debug.Log("Physical Next triggered");
            if (cube != null && cube.isConnected) cube.PlayPresetSound(9); // UI navigation sound
            if (labIntroGroup.gameObject.activeSelf)
            {
                StartCoroutine(OnReadInstructions());
            }
            else if (pickGroup.gameObject.activeSelf)
            {
                StartCoroutine(OnReadyToTrain());
            }
            
                else
                {
                    OnFinishLab();
                }
            
            return true;
        }

        return false;
    }
    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator TypeText(Text uiText, string message)
    {
        uiText.text = ""; // Start with an empty text box
        int i = 0;
        while (i < message.Length)
        {
            // Check if the current character is the start of a rich text tag ('<')
            if (message[i] == '<')
            {
                // If it is, find the end of the tag ('>')
                int endIndex = message.IndexOf('>', i);
                if (endIndex != -1)
                {
                    // Add the entire tag as a single block.
                    // We add 1 to endIndex to include the '>' character itself.
                    string tag = message.Substring(i, endIndex - i + 1);
                    uiText.text += tag;

                    // Jump the index past the tag we just added.
                    i = endIndex + 1;
                    continue; // Skip the rest of the loop for this iteration
                }
            }

            // If it's not a tag, just add the single character like before.
            uiText.text += message[i];
            i++;

            // Wait for the typing speed delay.
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void FadeInGameUI(float duration = 1f)
    {
        inGameUIGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvas(inGameUIGroup, 0f, 1f, duration));
    }

    private IEnumerator WaitForSpecificCardScan(string targetCardID)
    {
        uint lastReadId = 0;
        while (true)
        {
            uint currentId = Sample_Sensor.Instance.ReadCard();
            if (currentId != 0 && currentId != lastReadId)
            {
                lastReadId = currentId;
                if (StandardID.GetCardNameByID(currentId) == targetCardID)
                {
                    Sample_Sensor.Instance.cube?.PlayPresetSound(8); // Success sound
                    yield break; // Exit when the correct card is scanned
                }
            }
            yield return null;
        }
    }

private string GetCardIDForType(PokemonClassifier.PokemonType type)
{
    switch(type)
    {
        case PokemonClassifier.PokemonType.Fire: return "C";
        case PokemonClassifier.PokemonType.Water: return "D";
        case PokemonClassifier.PokemonType.Grass: return "E";
        case PokemonClassifier.PokemonType.Dragon: return "F";
        default: return "";
    }
}

private Sprite GetIconForType(PokemonClassifier.PokemonType type)
{
    switch(type)
    {
        case PokemonClassifier.PokemonType.Fire: return fireIcon;
        case PokemonClassifier.PokemonType.Water: return waterIcon;
        case PokemonClassifier.PokemonType.Grass: return grassIcon;
        case PokemonClassifier.PokemonType.Dragon: return dragonIcon;
        default: return null;
    }
}
}