using System;
using System.Collections;
using System.Collections.Generic;
using toio;
using toio.Samples.Sample_Sensor;
using toio.Simulator;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using System.Linq;

[System.Serializable]
public class BoxGifSet
{
    public Sprite[] shinyFrames;
    public Sprite[] openFrames;
    public Sprite[] destroyFrames;
    public Sprite finalSprite;
}

public class Level1LabZone : MonoBehaviour
{
    public static Level1LabZone Instance; 
    [Header("InGame UI")]
    public Text sceneNameText;     //for in game
    public CanvasGroup inGameUIGroup; 
    
    [Header("LabZone UI")] 
    public GameObject LabUI;
    public CanvasGroup labIntroGroup;         // You have entered labzone
    public Image labIntroImage;
    public Button readInstructionsButton;

    public CanvasGroup instructionsGroup;     // Instruction image
    public Image instructionsImage;
    public Button understoodButton;

    public CanvasGroup pickGroup;             // Pick up to max 4 features
    public Text pickText;
    public Button readyButton;

    public CanvasGroup scanGroup;             // Scan UI
    public Text scanTitleText;
    public Text scanInstructionText;
    public List<Image> cardPlaceholders;
    public Button resetButton;
    public GameObject resetText;
    public GameObject finishText;
    public Button finishButton;
    public Sprite questionCard;
    
   

    /*[Header("Strictness Selection UI")]
    public CanvasGroup strictnessGroup;
    public Text strictnessText;
    public Button perfectMatchButton;
    public Button almostMatchButton;*/
    
    [Header("Method 1 Result Boxes")]
    public GameObject method1ResultsPanel;

    [Header("3D Box Animation System")]
    public List<GameObject> method1Boxes; // 2 boxes for method 1 (Fire, Not Fire)
    public List<GameObject> method2Boxes; // 4 boxes for method 2 (Fire, Water, Grass, Dragon) 

    [Header("Box Animation GIFs")]
    public List<BoxGifSet> fireBoxGifs;

    [Header("Result Display After Box Animation")]
    public GameObject resultDisplayPanel;
    public Button finalApplyButton;
    public GameObject finalApplyText;
    public GameObject finalRetryText;
    public Button finalRetryButton;

    [Header("Ratio Bar Components")]
    // Method 1 ratio bars
    public Text resultText;
    public GameObject fireBoxRatioBar;
    public Image fireBoxCorrectBar;  
    public Text fireBoxRatioText;
    
    public GameObject notFireBoxRatioBar;
    public Image notFireBoxCorrectBar;
    public Text notFireBoxRatioText;
    
    [Header("Mode Settings")] public bool PhysicalButton = true;
    
    [Header("Other")]
    public float typingSpeed = 0.04f;
    
    // --- ADDED: Variables for ESP32 Integration ---
    private int[] fireRuleLeds = new int[6]; // 0=Attack, 1=Defense, 2=Speed, 3=Wings, 4=Temp, 5=Altitude
    private readonly string ALL_LEDS_OFF = "0,0,0,0,0,0";


    private int currentScanCount = 0;
    private List<string> scannedCardIds = new List<string>();
    private System.Action labSequenceCompleteCallback;
    private Coroutine _autoScanCoroutine;
    public PokemonClassifier pokemonClassifier;
    private int selectedMethod = 1; // 1 for Method1, 2 for Method2
    public PokemonClassifier.StrictnessMode selectedStrictness = PokemonClassifier.StrictnessMode.Perfect;
    uint lastID = 9999999;
    private uint lastControlId = 0;
    private Coroutine _physicalControlCoroutine;

    // ADD THIS HERE:
    [Header("Integration Properties")]
    [HideInInspector] public List<string> learnedFeatureCards = new List<string>(); // Store learned rules

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        pokemonClassifier = GetComponent<PokemonClassifier>();
        if (pokemonClassifier == null)
            pokemonClassifier = gameObject.AddComponent<PokemonClassifier>();

        pokemonClassifier.Initialize();

        // --- ADDED: Initialize LED state ---
        Array.Clear(fireRuleLeds, 0, fireRuleLeds.Length);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(ShowScanUI());
        }

    }
    
    private void AssignButtonOrPhysical(Button btn, UnityEngine.Events.UnityAction uiCallback, GameObject UIInstruction = null)
    {
        if (!PhysicalButton)
        {
            btn.gameObject.SetActive(true);
            if (UIInstruction != null)
                UIInstruction.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(uiCallback);
        }
        else
        {
            btn.gameObject.SetActive(false);
            if (UIInstruction != null)
                UIInstruction.gameObject.SetActive(true);
            if (_physicalControlCoroutine != null)
            {
                StopCoroutine(_physicalControlCoroutine);
            }
            _physicalControlCoroutine = StartCoroutine(WaitForPhysicalControl(uiCallback));
        }
    }

   
    private IEnumerator WaitForPhysicalControl(UnityEngine.Events.UnityAction callback)
    {
        uint lastReadId = 0; 
    
        while (true)
        {
            uint currentId = Sample_Sensor.Instance.ReadCard();
            
            if (currentId != 0 && currentId != lastReadId)
            {
                lastReadId = currentId; 
                string cardIndex = StandardID.GetCardNameByID(currentId);
                Debug.Log($"Physical cardIndex {cardIndex}"); 

                // --- NEW: Play sound for Action cards ---
                var cube = Sample_Sensor.Instance?.cube;
                if (cube != null && cube.isConnected)
                {
                    // If the scanned card is a "Next" or "Reset" action card, play a navigation sound.
                    // Preset Sound #9 is a short, neutral "UI interaction" sound.
                    if (cardIndex == "→" || cardIndex == "↑")
                    {
                        cube.PlayPresetSound(9);
                    }
                }

                if (HandlePhysicalCardInput(cardIndex, callback))
                {
                    yield return new WaitForSeconds(0.5f);
                    yield break;
                }
            }

            yield return null;
        }
    }
 
    private bool HandlePhysicalCardInput(string cardIndex, UnityEngine.Events.UnityAction callback)
    {
        if (!PhysicalButton) return false;

        // Reset（↑）
        if (cardIndex == "↑")
        {
            Debug.Log("Physical Reset triggered");
            if (scanGroup.gameObject.activeSelf)
                ResetScan();
            else if (resultDisplayPanel.activeSelf)
                StartCoroutine(OnFinalRetry());
            return true;
        }

        // Next（→）
        if (cardIndex == "→")
        {
            Debug.Log("Physical Next triggered");
            if (labIntroGroup.gameObject.activeSelf)
                StartCoroutine(OnReadInstructions());
            else if (pickGroup.gameObject.activeSelf)
                StartCoroutine(OnReady());
            else if (scanGroup.gameObject.activeSelf)
                StartCoroutine(OnFinishScan());
            else if (resultDisplayPanel.activeSelf)
                OnFinalApply();
            return true;
        }
        
        return false; 
        
    }


    public IEnumerator StartLabZoneSequence(System.Action onComplete = null)
    {
        labSequenceCompleteCallback = onComplete;
        LabUI.SetActive(true);
        labIntroGroup.alpha = 0;
        labIntroGroup.gameObject.SetActive(true);
        yield return FadeCanvasGroup(labIntroGroup, 0, 1, 1f);
        AssignButtonOrPhysical(readInstructionsButton, () => StartCoroutine(OnReadInstructions()));

        
        bool sequenceComplete = false;
        System.Action originalCallback = labSequenceCompleteCallback;
        labSequenceCompleteCallback = () => {
            sequenceComplete = true;
            originalCallback?.Invoke();
        };
        
        while (!sequenceComplete)
        {
            yield return null;
        }
    }

    private IEnumerator OnReadInstructions()
    {
        // 1. Fade out the lab intro screen as before.
        yield return FadeCanvasGroup(labIntroGroup, 1, 0, 1f);
        labIntroGroup.gameObject.SetActive(false);
        
        // --- THIS IS THE NEW FLOW ---
        
        // 2. Immediately show the "Pick Group" UI.
        pickGroup.alpha = 0;
        pickGroup.gameObject.SetActive(true);
        yield return FadeCanvasGroup(pickGroup, 0, 1, 1f);
        
        // 3. Type out the instruction text.
        yield return TypeText(pickText, "Now it's your time to pick up to max 4 feature cards to spot Fire-type Pokémon.");
        
        // 4. Wait for a moment so the child can read the text.
        yield return new WaitForSeconds(1.5f);
        
        // 5. AUTOMATICALLY proceed to the scanning phase.
        yield return StartCoroutine(OnReady()); 
    }


    private IEnumerator OnReady()
    {
        yield return FadeCanvasGroup(pickGroup, 1, 0, 1f);
        pickGroup.gameObject.SetActive(false);
        
        currentScanCount = 0;
        scannedCardIds.Clear();
        yield return StartCoroutine(ShowScanUI());
    }

    private IEnumerator ShowScanUI()
    {
        UpdateScanTitle();
        scanGroup.alpha = 0;
        scanGroup.gameObject.SetActive(true);
        scanInstructionText.text = "Simply use the robot to scan different cards. You can also finish earlier.";
        foreach (var placeholder in cardPlaceholders)
        {
            placeholder.sprite = questionCard;
        }
        yield return FadeCanvasGroup(scanGroup, 0, 1, 1f);
        
        if (_autoScanCoroutine != null)
            StopCoroutine(_autoScanCoroutine);
        _autoScanCoroutine = StartCoroutine(DelayedStartScan());
    }

        private IEnumerator DelayedStartScan()
        {
            yield return new WaitForSeconds(0.2f);
            // FIX: Ensure lastID is reset here so the first scan after a reset always works.
            lastID = 9999999; 
            _autoScanCoroutine = StartCoroutine(WaitForScan());
        }
        
        private IEnumerator WaitForScan()
        {
            // The condition is now just while true, we break out manually
            // when the scan count reaches the maximum.
            while (true) 
            {
                // Stop scanning if we have reached the max number of cards.
                if (currentScanCount >= 4)
                {
                    scanInstructionText.text = "All cards scanned! Time to click the finish button.";
                    break; // Exit the loop
                }

                uint cardId = Sample_Sensor.Instance.ReadCard();

            // Condition 1: A card is detected.
            // Condition 2: It's a different card than the very last one we scanned (prevents double-reads).
            if (cardId != 0 && cardId != lastID)
            {
                string cardIndex = StandardID.GetCardNameByID(cardId);
                lastID = cardId; // Update lastID immediately to prevent re-reads on the next frame.

                if (string.IsNullOrEmpty(cardIndex))
                {
                    Debug.LogWarning($"[WaitForScan] cardIndex is null or empty for id={cardId}, skipping");
                    yield return null;
                    continue;
                }

                var cube = Sample_Sensor.Instance?.cube;

                // NEW, IMPORTANT CONDITION:
                // Condition 3: Check if this card's ID has *already been scanned in this session*.
                if (scannedCardIds.Contains(cardIndex))
                {
                    Debug.Log($"[WaitForScan] Scanned a duplicate card ({cardIndex}). Ignoring.");
                    // Optional: Provide feedback to the user that this card was already scanned.
                    scanInstructionText.text = "You already scanned that card! Try a different one.";
                    if (cube != null && cube.isConnected) cube.PlayPresetSound(10); // Preset sound #10 is a "cancel" sound.
                }
                // Only if all conditions pass, we perform the scan.
                else if (CardDataManager.Instance.IsFeatureCard(cardIndex))
                {
                    Debug.Log($"[WaitForScan] Scanned NEW valid card → {cardId} ({cardIndex})");
                    PerformScan(cardId);
                }
                else
                {
                    if (cardIndex != "→") {
                        // It's a valid card but not a feature card (e.g., an action card scanned at the wrong time)
                        Debug.Log($"[WaitForScan] Scanned a non-feature card ({cardIndex}). Ignoring.");
                        scanInstructionText.text = "That's not a feature card! Try another.";
                        if (cube != null && cube.isConnected) cube.PlayPresetSound(10); // Use the same error sound
                    }
                }
                }
                yield return null;
            }
        }

        private void UpdateScanTitle()
        {
            // This is fine as is.
            scanTitleText.text = $"Scan: {currentScanCount}/4";
        }

        private void PerformScan(uint cardId)
        {
            var cube = Sample_Sensor.Instance?.cube;
            if (cube != null && cube.isConnected)
            {
                // Play a preset sound effect for a successful scan.
                // Sound #8 is a good, short "confirm" sound.
                cube.PlayPresetSound(8); 
            }
            // This method is now only called for unique, valid cards.
            // We can remove the check for currentScanCount because WaitForScan handles it.

            currentScanCount++;
            string cardIndex = StandardID.GetCardNameByID(cardId);
            scannedCardIds.Add(cardIndex); 
            
            if (cardPlaceholders.Count >= currentScanCount)
            {
                Sprite cardSprite = CardDataManager.Instance.GetSprite(cardIndex);
                cardPlaceholders[currentScanCount - 1].sprite = cardSprite ?? questionCard;
                UpdateScanTitle();
            }
            
            scanInstructionText.text = "Great! Scan another card, or click Finish.";

            // --- ADDED: Call to update the LEDs ---
            UpdateAndSendLedData(cardIndex);
            // ------------------------------------
        
            AssignButtonOrPhysical(resetButton, () => ResetScan(), resetText);
            AssignButtonOrPhysical(finishButton, () => StartCoroutine(OnFinishScan()), finishText);
        }

    private void ResetScan()
    {
        var cube = Sample_Sensor.Instance?.cube;
        if (cube != null && cube.isConnected)
        {
            cube.PlayPresetSound(9); // UI interaction sound
        }
        
        if (_autoScanCoroutine != null)
        {
            StopCoroutine(_autoScanCoroutine);
            _autoScanCoroutine = null;
        }
        
        currentScanCount = 0;
        scannedCardIds.Clear();
        
        for (int i = 0; i < cardPlaceholders.Count; i++)
        {
            cardPlaceholders[i].sprite = questionCard;
        }

        scanTitleText.text = "Scan: 0/4";
        scanInstructionText.text = "Please scan a valid card";
        
        Array.Clear(fireRuleLeds, 0, fireRuleLeds.Length);
        if (ESP32Controller.Instance != null)
        {
            ESP32Controller.Instance.SendLEDData(ALL_LEDS_OFF);
        }

        _autoScanCoroutine = StartCoroutine(WaitForScan());
    }

    private void UpdateAndSendLedData(string cardIndex)
{
    // 1. Convert the string card index to a CardID enum
    PokemonClassifier.CardID? cardID = pokemonClassifier.ConvertScannedCardsToRules(new List<string> { cardIndex }).FirstOrDefault();

    if (cardID.HasValue)
    {
        // 2. Get the feature type (e.g., "Attack") and its LED value (e.g., +3)
        (string featureType, int ledValue) = GetLedDetailsForCard(cardID.Value);

        if (featureType != null)
        {
            // 3. Get the LED's position in the array (0-5)
            int ledIndex = GetLedIndexForFeature(featureType);
            
            if (ledIndex != -1)
            {
                // 4. Update the first 6 values
                fireRuleLeds[ledIndex] = ledValue;

                // 5. Build the full 24-length array (first 6 from fireRuleLeds, rest 0s)
                int[] fullLedArray = new int[24];
                for (int i = 0; i < 6; i++)
                {
                    fullLedArray[i] = fireRuleLeds[i];
                }

                // 6. Convert array to CSV and send
                string csvData = string.Join(",", fullLedArray);
                Debug.Log("Sending to ESP32 (24-length): " + csvData);
                if (ESP32Controller.Instance != null)
                {
                    ESP32Controller.Instance.SendLEDData(csvData);
                }
            }
        }
    }
}


    // Maps a CardID to its general feature type and its LED value (+3 or -3)
    private (string, int) GetLedDetailsForCard(PokemonClassifier.CardID cardID)
    {
        switch (cardID)
        {
            case PokemonClassifier.CardID.HighAttack:       return ("Attack", 3);
            case PokemonClassifier.CardID.LowAttack:        return ("Attack", -3);
            case PokemonClassifier.CardID.HighDefense:      return ("Defense", 3);
            case PokemonClassifier.CardID.LowDefense:       return ("Defense", -3);
            case PokemonClassifier.CardID.HighSpeed:        return ("Speed", 3);
            case PokemonClassifier.CardID.LowSpeed:         return ("Speed", -3);
            case PokemonClassifier.CardID.HasWings:         return ("Wings", 3);
            case PokemonClassifier.CardID.NoWings:          return ("Wings", -3);
            case PokemonClassifier.CardID.HighTemperature:  return ("Temperature", 3);
            case PokemonClassifier.CardID.LowTemperature:   return ("Temperature", -3);
            case PokemonClassifier.CardID.HighAltitude:     return ("Altitude", 3);
            case PokemonClassifier.CardID.LowAltitude:      return ("Altitude", -3);
            default:                                        return (null, 0);
        }
    }

    // Maps the feature type string to its fixed position in the LED array
    private int GetLedIndexForFeature(string featureType)
    {
        switch (featureType)
        {
            case "Attack":          return 0;
            case "Defense":         return 1;
            case "Speed":           return 2;
            case "Wings":           return 3;
            case "Temperature":     return 4;
            case "Altitude":        return 5;
            default:                return -1; // Not a feature we display on this line
        }
    }

    
    private IEnumerator OnFinishScan()
    {
        yield return FadeCanvasGroup(scanGroup, 1, 0, 1f);
        scanGroup.gameObject.SetActive(false);
        finishButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        finishText.gameObject.SetActive(false);
        resetText.gameObject.SetActive(false);
        yield return StartCoroutine(PerformClassificationAndShowResults());
    }


    
    /* private IEnumerator SelectStrictness()
    {
        // Show strictness selection
        strictnessGroup.alpha = 0;
        strictnessGroup.gameObject.SetActive(true);
        yield return FadeCanvasGroup(strictnessGroup, 0, 1, 1f);
        
        yield return TypeText(strictnessText, "Choose strictness level:");
        
        perfectMatchButton.gameObject.SetActive(true);
        almostMatchButton.gameObject.SetActive(true);
        
        perfectMatchButton.onClick.RemoveAllListeners();
        perfectMatchButton.onClick.AddListener(() => StartCoroutine(OnSelectStrictness(PokemonClassifier.StrictnessMode.Perfect)));
        
        almostMatchButton.onClick.RemoveAllListeners();
        almostMatchButton.onClick.AddListener(() => StartCoroutine(OnSelectStrictness(PokemonClassifier.StrictnessMode.Almost)));
    }



    private IEnumerator OnSelectStrictness(PokemonClassifier.StrictnessMode strictness)
    {
        selectedStrictness = strictness;
        yield return FadeCanvasGroup(strictnessGroup, 1, 0, 1f);
        strictnessGroup.gameObject.SetActive(false);
        yield return StartCoroutine(PerformClassificationAndShowResults());
    }*/

    private IEnumerator PerformClassificationAndShowResults()
    {
        // Perform classification based on selected method
        resultDisplayPanel.SetActive(true);
        resultText.text = "Model Result";
        if (selectedMethod == 1)
        {
            var results = pokemonClassifier.ClassifyMethod1(scannedCardIds, selectedStrictness);
            yield return StartCoroutine(ShowMethod1Results(results));
        }

    }

    private IEnumerator ShowMethod1Results(PokemonClassifier.Method1Results results)
    {
        method1ResultsPanel.SetActive(true);

        // Step 1: Animate Shiny + Open (Fire and Not Fire)
        var shinyOpenCoroutines = new List<Coroutine>();
        shinyOpenCoroutines.Add(StartCoroutine(AnimateBoxShinyAndOpen(method1Boxes[0], fireBoxGifs[0])));
        shinyOpenCoroutines.Add(StartCoroutine(AnimateBoxShinyAndOpen(method1Boxes[1], fireBoxGifs[1])));
        foreach (var c in shinyOpenCoroutines) yield return c;

        // Step 2: Display Result UI
        yield return StartCoroutine(DisplayMethod1Results(results));

        // Step 3: Wait before destroy
        yield return new WaitForSeconds(1.5f);
        
        // Step 4: Play Destroy animations
        //var destroyCoroutines = new List<Coroutine>();
        //destroyCoroutines.Add(StartCoroutine(AnimateBoxDestroy(method1Boxes[0], fireBoxGifs[0])));
        //destroyCoroutines.Add(StartCoroutine(AnimateBoxDestroy(method1Boxes[1], fireBoxGifs[1])));
        //foreach (var c in destroyCoroutines) yield return c;

        // Step 5: Show final buttons
        ShowFinalButtons();
   
    }
    

    private IEnumerator AnimateBoxShinyAndOpen(GameObject box, BoxGifSet gifSet)
    {
        box.SetActive(true);
        var image = box.GetComponent<Image>();
        if (image == null) yield break;

        yield return StartCoroutine(PlayImageAnimation(image, gifSet.shinyFrames, 0.1f, false));
        yield return StartCoroutine(PlayImageAnimation(image, gifSet.openFrames, 0.1f, true));
    }

    private IEnumerator PlayImageAnimation(Image image, Sprite[] frames, float delay, bool holdLast)
    {
        if (frames == null || frames.Length == 0) yield break;

        for (int i = 0; i < frames.Length; i++)
        {
            image.sprite = frames[i];
            yield return new WaitForSeconds(delay);
        }

        if (!holdLast)
            image.sprite = null;
    }

    private IEnumerator AnimateBoxDestroy(GameObject box, BoxGifSet gifSet)
    {
        var image = box.GetComponent<Image>();
        if (image == null) yield break;

        yield return StartCoroutine(PlayImageAnimation(image, gifSet.destroyFrames, 0.1f, false));
        box.SetActive(false);
    }

    
    private IEnumerator DisplayMethod1Results(PokemonClassifier.Method1Results results)
    {
        
        yield return StartCoroutine(ShowMethod1RatioBars(results));
        ShowFinalButtons();
    }
    
    private IEnumerator AnimateRatioBar(Image fillBar, float targetAmount, float duration)
    {
        float current = 0f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            current = Mathf.Lerp(0f, targetAmount, t);
            fillBar.fillAmount = current;
            yield return null;
        }

        fillBar.fillAmount = targetAmount; 
    }

    private IEnumerator ShowMethod1RatioBars(PokemonClassifier.Method1Results results)
    {
        fireBoxRatioBar.SetActive(true);
        notFireBoxRatioBar.SetActive(true);

        int fireCorrect = results.fireBoxCorrect.Count;
        int fireWrong = results.fireBoxWrong.Count;
        int fireTotal = fireCorrect + fireWrong;

        int notFireCorrect = results.notFireBoxCorrect.Count;
        int notFireWrong = results.notFireBoxWrong.Count;
        int notFireTotal = notFireCorrect + notFireWrong;

        Debug.Log($"[RatioBar] fireCorrect = {fireCorrect}, fireWrong = {fireWrong}, fireTotal = {fireTotal}");
        Debug.Log($"[RatioBar] notFireCorrect = {notFireCorrect}, notFireWrong = {notFireWrong}, notFireTotal = {notFireTotal}");

        List<Coroutine> animations = new List<Coroutine>();

        if (fireTotal > 0) // FIX: Changed to > 0 to avoid division by zero
        {
            float correctRatio = (float)fireCorrect / fireTotal;
            animations.Add(StartCoroutine(AnimateRatioBar(fireBoxCorrectBar, correctRatio, 1f)));
            fireBoxRatioText.text = $"Fire Box: {fireCorrect} real Fire, {fireWrong} intruders";
        }
        else
        {
            fireBoxRatioText.text = "Fire Box: Empty!";
        }

        if (notFireTotal > 0) // FIX: Changed to > 0 to avoid division by zero
        {
            float correctRatio = (float)notFireCorrect / notFireTotal;
            animations.Add(StartCoroutine(AnimateRatioBar(notFireBoxCorrectBar, correctRatio, 1f)));
            notFireBoxRatioText.text = $"Not Fire Box: {notFireCorrect} correct non-Fire, {notFireWrong} missed Fire";
        }
        else
        {
            notFireBoxRatioText.text = "Not Fire Box: Empty!";
        }

        foreach (var anim in animations)
        {
            yield return anim;
        }

        // --- THIS IS THE NEW, IMPROVED LOGIC ---

        int totalCorrect = fireCorrect + notFireCorrect;
        int totalPokemon = fireTotal + notFireTotal;

        // Default message if there are no results to show
        string resultMessage = "No Pokémon were sorted!";

        if (totalPokemon > 0)
        {
            float overallAccuracy = (float)totalCorrect / totalPokemon * 100f;

            // Convert the percentage to a kid-friendly grade
            string accuracyGrade = GetAccuracyGrade(overallAccuracy);

            // Create the final message with the grade and the score
            resultMessage = $"Final Result: {accuracyGrade} ({totalCorrect}/{totalPokemon} Correct)";
        }

        // Use the TypeText coroutine to display the final message
        yield return StartCoroutine(TypeText(resultText, resultMessage));
    }

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
        else
        {
            return "<color=orange>Not Bad!</color>";
        }
    }

    private void ShowFinalButtons()
    {
      
        AssignButtonOrPhysical(finalApplyButton, () => OnFinalApply(), finalApplyText);
        AssignButtonOrPhysical(finalRetryButton, () => StartCoroutine(OnFinalRetry()), finalRetryText);
    }

    private void OnFinalApply()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.method1_rules = new List<string>(scannedCardIds);
            
        }
        learnedFeatureCards = new List<string>(scannedCardIds);
        // Apply results and proceed
        resultDisplayPanel.SetActive(false);
        method1ResultsPanel.SetActive(false);
        LabUI.SetActive(false);
        FadeInGameUI();
        labSequenceCompleteCallback?.Invoke();
        labSequenceCompleteCallback = null;
    }

    private IEnumerator OnFinalRetry()
    {
        // Reset everything and start over
        resultDisplayPanel.SetActive(false);
        method1ResultsPanel.SetActive(false);
        //strictnessGroup.gameObject.SetActive(false);
        
        selectedStrictness = PokemonClassifier.StrictnessMode.Perfect;
        currentScanCount = 0;
        scannedCardIds.Clear();
        
        // Reactivate boxes for next round
        ReactivateBoxes();
        
        yield return StartCoroutine(ShowScanUI());
    }
    

    private void ReactivateBoxes()
    {
        foreach (var box in method1Boxes)
        {
            if (box != null) box.SetActive(true);
        }
        
        foreach (var box in method2Boxes)
        {
            if (box != null) box.SetActive(true);
        }
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
    
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float timer = 0f;
        group.alpha = from;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        group.alpha = to;
    }

    public void FadeInGameUI(float duration = 1f)
    {
        inGameUIGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(inGameUIGroup, 0f, 1f, duration));
    }
}