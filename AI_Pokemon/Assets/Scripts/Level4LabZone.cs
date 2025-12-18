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

    [Header("Result Boxes (Method 3)")]
    public GameObject method3ResultsPanel;    
    public List<GameObject> typeBoxes;      
    public List<BoxGifSet> boxGifs;   
    
    [Header("Result Display")]
    public CanvasGroup resultDisplayGroup;
    public GameObject fireTypePanel;
    public Image fireCorrectBar;
    public Text fireCountText;

    public GameObject waterTypePanel;
    public Image waterCorrectBar;
    public Text waterCountText;

    public GameObject grassTypePanel;
    public Image grassCorrectBar;
    public Text grassCountText;

    public GameObject dragonTypePanel;
    public Image dragonCorrectBar;
    public Text dragonCountText;

    public Text accuracyText;
   
    public Button finalApplyButton; // Was finish button, now is the "Apply" button
    public GameObject finalApplyText;
    public Button finalRetryButton;   
    public GameObject finalRetryText;
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
        scanInstructionText.text = "Scan up to 4 Package cards to feed them.";
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
                scanInstructionText.text = "All Packages scanned! Click Finish to train.";
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
                    scanInstructionText.text = "You already scanned that Package! Try a different one.";
                    if (cube != null && cube.isConnected) cube.PlayPresetSound(10);
                }
                // --- MODIFIED: Check if it's a valid DATASET card (C-K) ---
                else if (IsDatasetCard(cardIndex))
                {
                    PerformScan(cardId); 
                }
                else
                {
                    scanInstructionText.text = "That's not a Package card! Try another.";
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
            
        scanInstructionText.text = "Great! Scan another Package, or click Finish.";
        
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
        scanInstructionText.text = "Please scan a Package card.";
        
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
        var detailedResults = pokemonClassifier.GetDetailedMethod3Results(currentModelWeights);
        
        //yield return StartCoroutine(ShowDetailedResults(detailedResults));

        // 5. Wait for the player to scan the "Next/Apply" (→) card to finish.
        //AssignButtonOrPhysical(finalApplyButton, OnFinalApply);
        //AssignButtonOrPhysical(finalRetryButton, () => StartCoroutine(OnFinalRetry()));
        yield return StartCoroutine(ShowMethod3Results(detailedResults));
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
    
    // ===============  Result UI Reset  ===============
private void ResetVisualsForRetry()
{
    // 1. Reset the UI bars and text
    ResetResultUI();

    // 2. Reset 3D Boxes to "Closed" state (First frame of shiny animation)
    for (int i = 0; i < typeBoxes.Count; i++)
    {
        if (i < boxGifs.Count && typeBoxes[i] != null)
        {
            Image boxImage = typeBoxes[i].GetComponent<Image>();
            
            if (boxImage != null && boxGifs[i].shinyFrames != null && boxGifs[i].shinyFrames.Length > 0)
            {
                // Force the sprite back to the first frame (Closed Box)
                boxImage.sprite = boxGifs[i].shinyFrames[0];
                
                // Ensure the image is visible
                var color = boxImage.color;
                color.a = 1f;
                boxImage.color = color;
            }
            
            // Keep the box active so it's ready for the next run
            typeBoxes[i].SetActive(true);
        }
    }
}

private void ResetResultUI()
{
    if (fireCountText) fireCountText.text = "";
    if (waterCountText) waterCountText.text = "";
    if (grassCountText) grassCountText.text = "";
    if (dragonCountText) dragonCountText.text = "";
    if (accuracyText) accuracyText.text = "";

    if (fireCorrectBar) fireCorrectBar.fillAmount = 0f;
    if (waterCorrectBar) waterCorrectBar.fillAmount = 0f;
    if (grassCorrectBar) grassCorrectBar.fillAmount = 0f;
    if (dragonCorrectBar) dragonCorrectBar.fillAmount = 0f;

    if (fireTypePanel) fireTypePanel.SetActive(false);
    if (waterTypePanel) waterTypePanel.SetActive(false);
    if (grassTypePanel) grassTypePanel.SetActive(false);
    if (dragonTypePanel) dragonTypePanel.SetActive(false);
}


private IEnumerator PlayImageAnimation(Image image, Sprite[] frames, float delay, bool holdLast)
{
    if (image == null || frames == null || frames.Length == 0)
        yield break;

    for (int i = 0; i < frames.Length; i++)
    {
        image.sprite = frames[i];
        yield return new WaitForSeconds(delay);
    }

    if (!holdLast)
        image.sprite = null;
}

private IEnumerator AnimateBoxShinyAndOpen(GameObject box, BoxGifSet gifSet)
{
    if (!box || gifSet == null) yield break;

    box.SetActive(true);
    var image = box.GetComponent<Image>();
    if (!image) yield break;

 
    yield return StartCoroutine(PlayImageAnimation(image, gifSet.shinyFrames, 0.1f, false));

    yield return StartCoroutine(PlayImageAnimation(image, gifSet.openFrames, 0.1f, true));
}

private IEnumerator AnimateBoxDestroy(GameObject box, BoxGifSet gifSet)
{
    if (!box || gifSet == null) yield break;

    var image = box.GetComponent<Image>();
    if (!image) yield break;
    
    yield return StartCoroutine(PlayImageAnimation(image, gifSet.destroyFrames, 0.1f, false));

    image.sprite = gifSet.finalSprite;
}


private IEnumerator ShowMethod3Results(PokemonClassifier.Method3DetailedResults results)
{
  
    if (method3ResultsPanel) method3ResultsPanel.SetActive(true);


    ResetResultUI();

    var shinyOpenCoroutines = new List<Coroutine>();
    int boxCount = Mathf.Min(typeBoxes.Count, boxGifs.Count);
    for (int i = 0; i < boxCount; i++)
    {
        shinyOpenCoroutines.Add(
            StartCoroutine(AnimateBoxShinyAndOpen(typeBoxes[i], boxGifs[i]))
        );
    }
    foreach (var c in shinyOpenCoroutines)
        yield return c;


    var destroyCoroutines = new List<Coroutine>();
    for (int i = 0; i < boxCount; i++)
    {
        destroyCoroutines.Add(
            StartCoroutine(AnimateBoxDestroy(typeBoxes[i], boxGifs[i]))
        );
    }
    foreach (var c in destroyCoroutines)
        yield return c;
    
    yield return StartCoroutine(ShowDetailedResults(results));
    
    AssignButtonOrPhysical(finalApplyButton, OnFinalApply, finalApplyText);
    AssignButtonOrPhysical(finalRetryButton, () => StartCoroutine(OnFinalRetry()), finalRetryText);
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
    // 1. Hide the results panel
    resultDisplayGroup.gameObject.SetActive(false);
    if (method3ResultsPanel) method3ResultsPanel.SetActive(false);
    
    // 2. --- FIX 1: Reset Visuals (Prevent Glitches) ---
    ResetVisualsForRetry();

    // 3. --- FIX 2: Turn off LEDs (Send 24 Zeros) ---
    if (ESP32Controller.Instance != null) 
    {
        ESP32Controller.Instance.SendLEDData("0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0");
    }

    // 4. Reset scanning state
    currentScanCount = 0;
    scannedCardIds.Clear();
    
    // 5. Return to scanning UI
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
    private IEnumerator AnimateRatioBar(Image bar, float targetRatio, float duration)
    {
        float t = 0f;
        float startFill = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            bar.fillAmount = Mathf.Lerp(startFill, targetRatio, t / duration);
            yield return null;
        }
        bar.fillAmount = targetRatio;
    }

    private IEnumerator ShowDetailedResults(PokemonClassifier.Method3DetailedResults results)
    {
        // Total number of Pokémon of each type in the dataset (Usually 25 each in the 100 set)
        const int TOTAL_PER_TYPE = 25; 

        // --- 1) Fire Type ---
        int fireCorrect = results.correctCounts[PokemonClassifier.PokemonType.Fire];
        float fireRatio = (float)fireCorrect / TOTAL_PER_TYPE;
        
        if(fireTypePanel) fireTypePanel.SetActive(true);
        if(fireCountText) 
        {
            fireCountText.supportRichText = true;
            fireCountText.text = $"Found <size=160%>{fireCorrect}/25</size> Fire Pokémon";
        }
        if(fireCorrectBar) yield return StartCoroutine(AnimateRatioBar(fireCorrectBar, fireRatio, 0.5f));

        // --- 2) Water Type ---
        int waterCorrect = results.correctCounts[PokemonClassifier.PokemonType.Water];
        float waterRatio = (float)waterCorrect / TOTAL_PER_TYPE;

        if(waterTypePanel) waterTypePanel.SetActive(true);
        if(waterCountText) 
        {
            waterCountText.supportRichText = true;
            waterCountText.text = $"Found <size=160%>{waterCorrect}/25</size> Water Pokémon";
        }
        if(waterCorrectBar) yield return StartCoroutine(AnimateRatioBar(waterCorrectBar, waterRatio, 0.5f));

        // --- 3) Grass Type ---
        int grassCorrect = results.correctCounts[PokemonClassifier.PokemonType.Grass];
        float grassRatio = (float)grassCorrect / TOTAL_PER_TYPE;

        if(grassTypePanel) grassTypePanel.SetActive(true);
        if(grassCountText)
        {
            grassCountText.supportRichText = true;
            grassCountText.text = $"Found <size=160%>{grassCorrect}/25</size> Grass Pokémon";
        }
        if(grassCorrectBar) yield return StartCoroutine(AnimateRatioBar(grassCorrectBar, grassRatio, 0.5f));

        // --- 4) Dragon Type ---
        int dragonCorrect = results.correctCounts[PokemonClassifier.PokemonType.Dragon];
        float dragonRatio = (float)dragonCorrect / TOTAL_PER_TYPE;

        if(dragonTypePanel) dragonTypePanel.SetActive(true);
        if(dragonCountText)
        {
            dragonCountText.supportRichText = true;
            dragonCountText.text = $"Found <size=160%>{dragonCorrect}/25</size> Dragon Pokémon";
        }
        if(dragonCorrectBar) yield return StartCoroutine(AnimateRatioBar(dragonCorrectBar, dragonRatio, 0.5f));

        // Update main text
        accuracyText.text = $"Great, you have {results.totalCorrect}/100 correct!\n" +
                            $"Do you want to improve your score or end now?";
    }

}
