using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using toio.Samples.Sample_Sensor;
using UnityEngine;
using UnityEngine.UI;
using toio;
using toio.Simulator;


public class Level4LabZone : MonoBehaviour
{
    public static Level4LabZone Instance; 
    
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

    public CanvasGroup scanGroup;
    public Text scanTitleText;
    public Text scanInstructionText;
    public List<Image> cardPlaceholders;
    public Button resetButton;
    public Button finishButton;
    public GameObject resetText;
    public GameObject finishText;
    public Sprite questionCard;

    [Header("Result Display")]
    public CanvasGroup resultDisplayGroup;
    
    public Text accuracyText;
   
    public Button finalApplyButton; // Was finish button, now is the "Apply" button
    public GameObject finalApplyText;

    [Header("Mode Settings")] 
    public bool PhysicalButton = true;
    
    [Header("Other")]
    public float typingSpeed = 0.04f;
    
    private List<string> scannedCardIds = new List<string>();
    private int currentScanCount = 0;
    private Action labSequenceCompleteCallback;
    private Coroutine _autoScanCoroutine;
    public PokemonClassifier pokemonClassifier;
    private uint lastID = 9999999;
    private bool _labCompleted = false;

    // --- Method 3 Specific Variables ---
    private PokemonClassifier.ModelWeights currentModelWeights;
    private int currentEpoch = 0;
    private const int MAX_EPOCHS = 3; // Or however many you want
    private Coroutine physicalListener;
    private uint lastCardId = 0;
    public PokemonClassifier.ModelWeights TrainedModel { get; private set; }

    private string GetAccuracyGrade(float accuracyPercentage)
    {
        if (accuracyPercentage >= 90f)
        {
            return "<color=green>Perfect!</color>";
        }
        else if (accuracyPercentage >= 80f)
        {
            return "<color=cyan>Great!</color>";
        }
        else if (accuracyPercentage >= 60f)
        {
            return "<color=yellow>Good!</color>";
        }
        else if (accuracyPercentage >= 50f)
        {
            return "<color=orange>Not Bad!</color>";
        }
        else
        {
            return "<color=red>Try Again!</color>";
        }
    }

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
            StartCoroutine(ShowScanUI());
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
        yield return TypeText(pickText, "More datasets found! Now combine different datasets to see how the lights and the robot's accuracy change.");

        // 4. Wait for a moment so the child can read.
        yield return new WaitForSeconds(1.5f);

        // 5. AUTOMATICALLY proceed to the next step.
        yield return StartCoroutine(OnReady());
    }

    private IEnumerator OnReady()
    {
        yield return FadeCanvas(pickGroup, 1, 0, 0.5f);
        pickGroup.gameObject.SetActive(false);
        
        currentScanCount = 0;
        scannedCardIds.Clear();
        yield return StartCoroutine(ShowScanUI());
    }

    private IEnumerator ShowScanUI()
    {
        scanTitleText.text = $"Scan Datasets: {currentScanCount}/4";
        scanInstructionText.text = "Scan up to 4 dataset cards to combine them.";
        foreach (var placeholder in cardPlaceholders)
        {
            placeholder.sprite = questionCard;
        }

        scanGroup.alpha = 0;
        scanGroup.gameObject.SetActive(true);
        yield return FadeCanvas(scanGroup, 0, 1, 1f);
        
        if (_autoScanCoroutine != null) StopCoroutine(_autoScanCoroutine);
        _autoScanCoroutine = StartCoroutine(WaitForScan());
    }
        
    private IEnumerator WaitForScan()
    {
        while (true) 
        {
            if (currentScanCount >= 4)
            {
                scanInstructionText.text = "All datasets scanned! Click Finish to train.";
                break;
            }

            uint cardId = Sample_Sensor.Instance.ReadCard();
            if (cardId != 0 && cardId != lastID)
            {
                string cardIndex = StandardID.GetCardNameByID(cardId);
                lastID = cardId;

                // Get a reference to the cube for playing sounds
                var cube = Sample_Sensor.Instance?.cube;
                
                if (scannedCardIds.Contains(cardIndex))
                {
                    scanInstructionText.text = "You already scanned that dataset! Try a different one.";
                    if (cube != null && cube.isConnected) cube.PlayPresetSound(10);
                }
                // --- MODIFIED: Check if it's a valid DATASET card (C-K) ---
                else if (IsDatasetCard(cardIndex))
                {
                    PerformScan(cardId); 
                }
                else
                {
                    scanInstructionText.text = "That's not a dataset card! Try another.";
                    if (cube != null && cube.isConnected) cube.PlayPresetSound(10); // Play the same error sound
                }
            }
            yield return null;
        }
    }
    
    // Helper to check if a card is one of the dataset cards
    private bool IsDatasetCard(string cardIndex)
    {
        if (string.IsNullOrEmpty(cardIndex) || cardIndex.Length != 1) return false;
        char c = cardIndex[0];
        return c >= 'C' && c <= 'K';
    }

    private void PerformScan(uint cardId)
    {
        // --- ADDED: Play "success" sound ---
        var cube = Sample_Sensor.Instance?.cube;
        if (cube != null && cube.isConnected)
        {
            // Sound #8 is a good, short "confirm" sound.
            cube.PlayPresetSound(8);
        }
        currentScanCount++;
        string cardIndex = StandardID.GetCardNameByID(cardId);
        scannedCardIds.Add(cardIndex); 
            
        if (cardPlaceholders.Count >= currentScanCount)
        {
            Sprite cardSprite = CardDataManager.Instance.GetSprite(cardIndex);
            cardPlaceholders[currentScanCount - 1].sprite = cardSprite ?? questionCard;
            scanTitleText.text = $"Scan Datasets: {currentScanCount}/4";
        }
            
        scanInstructionText.text = "Great! Scan another dataset, or click Finish.";
        
        // Update the LED board in real-time with the new combination
        UpdateLedDisplay();
        
        AssignButtonOrPhysical(resetButton, ResetScan, resetText);
        AssignButtonOrPhysical(finishButton, () => StartCoroutine(OnFinishScan()), finishText);
    }
    

    private void ResetScan()
    {
        if (_autoScanCoroutine != null) StopCoroutine(_autoScanCoroutine);
        
        currentScanCount = 0;
        scannedCardIds.Clear();
        
        foreach (var placeholder in cardPlaceholders)
        {
            placeholder.sprite = questionCard;
        }

        scanTitleText.text = "Scan Datasets: 0/4";
        scanInstructionText.text = "Please scan a dataset card.";
        
        // Turn off all LEDs on reset
        if (ESP32Controller.Instance != null) ESP32Controller.Instance.SendLEDData("0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0");
        
        _autoScanCoroutine = StartCoroutine(WaitForScan());
    }

    private IEnumerator OnFinishScan()
    {
        yield return FadeCanvas(scanGroup, 1, 0, 0.5f);
        scanGroup.gameObject.SetActive(false);
        finishText.gameObject.SetActive(false);
        resetText.gameObject.SetActive(false);
        finishButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        yield return StartCoroutine(PerformClassificationAndShowResults());
    }

    private IEnumerator PerformClassificationAndShowResults()
    {
        // 1. We still need to create a "model" from the chosen datasets.
        // In this simplified version, the "model" is just the average feature weights.
        currentModelWeights = pokemonClassifier.GetAverageWeightsAsModel(scannedCardIds);

        // 2. Show the result display UI.
        resultDisplayGroup.alpha = 0;
        resultDisplayGroup.gameObject.SetActive(true);
        yield return FadeCanvas(resultDisplayGroup, 0, 1, 1f);

        // 3. Calculate and display the accuracy of this dataset combination ONCE.
        UpdateAccuracyDisplay();
        
       

        // 5. Wait for the player to scan the "Next/Apply" (→) card to finish.
        AssignButtonOrPhysical(finalApplyButton, OnFinalApply);
    }

    


    private void UpdateAccuracyDisplay()
    {
        float accuracy = pokemonClassifier.TestMethod3OnLargeDataset(currentModelWeights);
        
        string accuracyGrade = GetAccuracyGrade(accuracy);

        // Create the new, more intuitive message
        // It shows the score (e.g., "75/100") and the grade (e.g., "Good!")
        int correctCount = Mathf.RoundToInt(accuracy * 100f / 100f);
        accuracyText.text = $"Model Score: {correctCount}/100\n{accuracyGrade}";
    }

    private void UpdateLedDisplay()
    {
        // Safety check to ensure the ESP32 controller is available
        if (ESP32Controller.Instance == null)
        {
            Debug.LogWarning("ESP32Controller not found. Cannot send LED data.");
            return;
        }

        // 1. Get the dictionary of feature averages from the classifier,
        // passing the currently scanned dataset IDs.
        var ledDataDict = pokemonClassifier.GetTrainingAveragesAsLedValues(scannedCardIds);
        if (ledDataDict.Count == 0) return;

        // 2. Convert the dictionary to a fixed-order list of 24 numbers,
        // exactly like in Lab 3.
        var ledValuesList = new List<int>();
        var types = new[] {
            PokemonClassifier.PokemonType.Fire,
            PokemonClassifier.PokemonType.Water,
            PokemonClassifier.PokemonType.Grass,
            PokemonClassifier.PokemonType.Dragon
        };
        // This feature order MUST match your physical LED board layout.
        var features = new[] { "Attack", "Defense", "Speed", "HasWings", "HabitatTemperature", "HabitatAltitude" };

        foreach (var type in types)
        {
            foreach (var feature in features)
            {
                // Add the value for this specific type and feature to the list.
                // The dictionary is guaranteed to have the keys because we initialized it.
                ledValuesList.Add(ledDataDict[type][feature]);
            }
        }

        // 3. Convert the list of 24 integers to a comma-separated string (CSV)
        string csvData = string.Join(",", ledValuesList);

        // 4. Send the final string to the ESP32
        Debug.Log($"Sending Combined Dataset Averages to ESP32: {csvData}");
        ESP32Controller.Instance.SendLEDData(csvData);
    }

   
    

    private void OnFinalApply()
    {   
        
        
        // --- ADD THIS LINE TO SAVE THE TRAINED MODEL ---
        TrainedModel = currentModelWeights;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.method4_model = TrainedModel;
        }

        _labCompleted = true;
        LabUI.SetActive(false);
        resultDisplayGroup.gameObject.SetActive(false);
        FadeInGameUI();
        labSequenceCompleteCallback?.Invoke();
    }
    
    // --- UI Helpers ---
    private void AssignButtonOrPhysical(Button btn, UnityEngine.Events.UnityAction uiCallback, GameObject UIInstruction = null)
    {
        if (!PhysicalButton)
        {
            btn.gameObject.SetActive(true);
            if (UIInstruction != null)
                UIInstruction.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(uiCallback);
            return;
        }
        
        btn.gameObject.SetActive(false);
        if (UIInstruction != null)
            UIInstruction.gameObject.SetActive(true);
        if (physicalListener != null)
            StopCoroutine(physicalListener);
        physicalListener = StartCoroutine(WaitForPhysicalControl());
    }

    private IEnumerator WaitForPhysicalControl()
    {
        while (Sample_Sensor.Instance.ReadCard() != 0)
            yield return null;
        

        while (true)
        {
            uint current = Sample_Sensor.Instance.ReadCard();
            if (current != 0)
            {
                string idx = StandardID.GetCardNameByID(current);
                Debug.Log("Physical cardIndex: " + idx);
                
                if (HandlePhysicalCardInput(idx))
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

        // ↑  Reset
        if (cardIndex == "↑")
        {
            Debug.Log("Physical Reset triggered");
            if (cube != null && cube.isConnected) cube.PlayPresetSound(9);
            if (scanGroup.gameObject.activeSelf)
            {
                ResetScan();
                return true;
            }
            if (resultDisplayGroup.gameObject.activeSelf)
            {
                StartCoroutine(OnFinalRetry()); // We will create this method
                return true;
            }
        }

        // →  Next / Finish / Apply
        if (cardIndex == "→")
        {
            Debug.Log("Physical Next/Finish triggered");
            if (cube != null && cube.isConnected) cube.PlayPresetSound(9);

            if (labIntroGroup.gameObject.activeSelf)
            {
                StartCoroutine(OnReadInstructions());
                return true;
            }
            if (pickGroup.gameObject.activeSelf)
            {
                StartCoroutine(OnReady());
                return true;
            }

            if (scanGroup.gameObject.activeSelf && currentScanCount >= 1)
            {
                StartCoroutine(OnFinishScan());
                return true;
            }
            if (resultDisplayGroup.gameObject.activeSelf)
            {
                
                OnFinalApply();
                
                return true;
            }
            
        }

        return false;
    }
    
    // In Level4LabZone.cs, add this new coroutine

    private IEnumerator OnFinalRetry()
    {
        // Hide the results panel
        resultDisplayGroup.gameObject.SetActive(false);
        
        // Reset the scanning state and go back to the scanning UI
        currentScanCount = 0;
        scannedCardIds.Clear();
        yield return StartCoroutine(ShowScanUI());
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
}