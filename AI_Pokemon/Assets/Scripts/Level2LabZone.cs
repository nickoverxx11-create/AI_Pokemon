using System;
using System.Collections;
using System.Collections.Generic;
using toio.Samples.Sample_Sensor;
using toio.Simulator;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


[System.Serializable]
public class ImageGroup
{
    public List<Image> placeholders = new List<Image>();
}

public class Level2LabZone : MonoBehaviour
{
    public static Level2LabZone Instance;

    [Header("InGame UI")] public Text sceneNameText;
    public CanvasGroup inGameUIGroup;

    [Header("LabZone UI")] public GameObject LabUI;
    public CanvasGroup labIntroGroup;
    public Image labIntroImage;
    public Button readInstructionsButton;

    public CanvasGroup instructionsGroup;
    public Image instructionsImage;
    public Button understoodButton;

    [Header("Feature Pick UI")] public CanvasGroup pickGroup;
    public Text pickText;
    public Button readyButton;

    [Header("Scan Groups (Fire, Water, Grass, Dragon)")]
    public List<CanvasGroup> scanGroups;

    public List<Text> scanTitles;
    public List<Text> scanInstructions;
    public List<ImageGroup> cardPlaceholderGroups;
    public Button resetButton;
    public Button nextButton;
    public GameObject resetText;
    public GameObject finishText;
    public Sprite questionCard;

    [Header("Method 2 Result Boxes")]
    public GameObject method2ResultsPanel;
    public List<GameObject> typeBoxes;
    public List<BoxGifSet> boxGifs;

    public Button finalApplyButton;
    public Button finalRetryButton;
    public GameObject finalApplyText;
    public GameObject finalRetryText;
    public List<Button> editTypeButtons; 

    [Header("Type Bars & Texts")]
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
    public Text overallAccuracyText;

    [Header("Mode Settings")] public bool PhysicalButton = true;

    [Header("Other")] 
    public float typingSpeed = 0.04f;
    private List<List<string>> scannedCardGroups = new List<List<string>>();
    private int currentScanCount = 0;
    private int currentGroupIndex = 0;
    private bool _singleGroupEditMode = false;
    private int _editTargetIndex = -1;
    private int _savedGroupIndex = -1;
    private Coroutine _autoScanCoroutine;

    private Dictionary<PokemonClassifier.PokemonType, List<string>> rulesByType = new Dictionary<PokemonClassifier.PokemonType, List<string>>();

    public PokemonClassifier pokemonClassifier;
    private System.Action labSequenceCompleteCallback;
    private uint lastID = 9999999;

    private CanvasGroup currScanGroup;
    private Text currTitleText;
    private Text currInstructionText;
    private List<Image> currPlaceholders;
    private bool _isTransitioning = false;
    private bool _labCompleted = false; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        foreach (PokemonClassifier.PokemonType t in Enum.GetValues(typeof(PokemonClassifier.PokemonType)))
            rulesByType[t] = new List<string>();
        InitScanGroups();
        _ = CardDataManager.Instance;
        pokemonClassifier = GetComponent<PokemonClassifier>() ?? gameObject.AddComponent<PokemonClassifier>();
        pokemonClassifier.Initialize();
    }

    private void InitScanGroups()
    {
        scannedCardGroups.Clear();
        scannedCardGroups = new List<List<string>>();
        for (int i = 0; i < scanGroups.Count; i++)
            scannedCardGroups.Add(new List<string>());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            StartCoroutine(PerformClassificationAndShowResults());
    }

    public IEnumerator StartLabZoneSequence(Action onComplete = null)
    {
        labSequenceCompleteCallback = onComplete;
        _labCompleted = false;
        LabUI.SetActive(true);

        // Lab Intro
        labIntroGroup.alpha = 0;
        labIntroGroup.gameObject.SetActive(true);
        yield return FadeCanvas(labIntroGroup, 0, 1, 1f);
        AssignButtonOrPhysical(readInstructionsButton, () => StartCoroutine(OnReadInstructions()));
        yield return new WaitUntil(() => _labCompleted);
    }

    private string VerdictForScore(int score)
    {
        if (score < 50) return "Try again!";
        if (score < 70) return "Not bad.";
        if (score < 90) return "Good!";
        if (score < 100) return "Great!";
        return "Perfect!";
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
        yield return TypeText(pickText, "Now pick up to 4 feature cards for each type to build your type rules.");

        // 4. Wait for a moment so the child can read.
        yield return new WaitForSeconds(1.5f);

        // 5. AUTOMATICALLY proceed to the next step.
        yield return StartCoroutine(OnPickReady());
    }

    

    private IEnumerator OnPickReady()
    {
        yield return FadeCanvas(pickGroup, 1, 0, 0.5f);
        pickGroup.gameObject.SetActive(false);

        currentGroupIndex = 0;
        foreach (var kvp in rulesByType) kvp.Value.Clear();
        yield return StartCoroutine(ShowScanUIForGroup(currentGroupIndex));
    }


    private IEnumerator ShowScanUIForGroup(int groupIndex)
    {
        currentScanCount = 0;
        scannedCardGroups[groupIndex].Clear();

        // --- NEW LOGIC TO PRE-POPULATE FIRE RULES ---
        if (groupIndex == 0 && Level1LabZone.Instance != null && Level1LabZone.Instance.learnedFeatureCards.Count > 0)
        {
            List<string> lab1Rules = Level1LabZone.Instance.learnedFeatureCards;
            Debug.Log("Pre-populating Fire rules from Lab 1.");


            // We need to get the specific list of placeholders for the current group (groupIndex 0).
            var currentPlaceholders = cardPlaceholderGroups[groupIndex].placeholders;


            foreach (string cardIndex in lab1Rules)
            {
                if (currentScanCount < 4)
                {
                    scannedCardGroups[groupIndex].Add(cardIndex);
                    // Also add to the main rules dictionary
                    var type = (PokemonClassifier.PokemonType)groupIndex;
                    rulesByType[type].Add(cardIndex);
                    currentScanCount++;

                    Sprite cardSprite = CardDataManager.Instance.GetSprite(cardIndex);

                    // Use the 'currentPlaceholders' variable we just created
                    if (currentPlaceholders.Count > currentScanCount - 1)
                    {
                        currentPlaceholders[currentScanCount - 1].sprite = cardSprite ?? questionCard;
                    }
                }
            }

            scanInstructions[groupIndex].text = "Here are your rules from the Meadow! Scan 'Next' to keep them, or 'Reset' to change them.";
            AssignButtonOrPhysical(resetButton, () => ResetScanForGroup(groupIndex), resetText);
            AssignButtonOrPhysical(nextButton, () => StartCoroutine(OnFinishScanForGroup(groupIndex)), finishText);
        }
        else
        {
            // For all other types, start with a blank slate.
            scanInstructions[groupIndex].text = "Simply use the robot to scan different cards.";
            // Also make sure to clear the images for the other groups
            foreach (var ph in cardPlaceholderGroups[groupIndex].placeholders)
                ph.sprite = questionCard;
        }

        scanTitles[groupIndex].text = $"Scan: {currentScanCount}/4";
        
        // Fade in the UI 
        var cg = scanGroups[groupIndex];
        cg.alpha = 0;
        cg.gameObject.SetActive(true);
        yield return FadeCanvas(cg, 0, 1, 1f);

        // Start the scanning coroutine 
        if (_autoScanCoroutine != null) StopCoroutine(_autoScanCoroutine);
        _autoScanCoroutine = StartCoroutine(DelayedStartScanForGroup(groupIndex));
    }

    private IEnumerator DelayedStartScanForGroup(int groupIndex)
    {
        yield return new WaitForSeconds(0.2f);
        lastID = 9999999;
        _autoScanCoroutine = StartCoroutine(WaitForScanForGroup(groupIndex));
    }

    private IEnumerator WaitForScanForGroup(int groupIndex)
    {
        while (true)
        {
            if (currentScanCount >= 4)
            {
                scanInstructions[groupIndex].text = "All cards scanned! Time to click Next.";
                break;
            }

            uint id = Sample_Sensor.Instance.ReadCard();
            if (id != 0 && id != lastID)
            {
                lastID = id;
                string idx = StandardID.GetCardNameByID(id);
                if (string.IsNullOrEmpty(idx))
                {
                    yield return null;
                    continue;
                }

                // Get a reference to the cube for playing sounds
                var cube = Sample_Sensor.Instance?.cube;

                if (scannedCardGroups[groupIndex].Contains(idx))
                {
                    scanInstructions[groupIndex].text = "You already scanned that card! Try a different one.";
                    if (cube != null && cube.isConnected) cube.PlayPresetSound(10);
                }
                else if (CardDataManager.Instance.IsFeatureCard(idx))
                {
                    scannedCardGroups[groupIndex].Add(idx);
                    PerformScanForGroup(groupIndex, id);
                }
                else if (idx != "→" && idx != "↑")
                {
                    scanInstructions[groupIndex].text = "That's not a feature card! Try another.";
                    if (cube != null && cube.isConnected) cube.PlayPresetSound(10);
                }
            }

            yield return null;
        }
    }

    private void UpdateScanTitle(int groupIndex)
    {
        scanTitles[groupIndex].text = $"Scan: {currentScanCount}/4";
    }

    private int[] fireRuleLeds = new int[6];
    private int[] waterRuleLeds = new int[6];
    private int[] grassRuleLeds = new int[6];
    private int[] dragonRuleLeds = new int[6];
    private readonly string ALL_LEDS_OFF = string.Join(",", new int[24]);

    private void UpdateAndSendLedData(PokemonClassifier.PokemonType type, string cardIndex, int scanOrder)
    {
        // 1. Convert card to CardID
        var rules = pokemonClassifier.ConvertScannedCardsToRules(new List<string> { cardIndex });
        if (rules.Count == 0) return;

        var cardID = rules[0];

        // 2. Get feature + LED value
        (string feature, int direction) = GetLedDetailsForCard(cardID);
        if (feature == null) return;

        int ledIndex = GetLedIndexForFeature(feature);
        if (ledIndex == -1) return;

        // --- NEW LOGIC: Determine brightness based on scan order ---
        int finalLedValue = 0;
        if (scanOrder == 1) // First card is brightest
        {
            finalLedValue = direction; // Stays at 3 or -3
        }
        else if (scanOrder == 2 || scanOrder == 3) // Second and third cards are medium
        {
            finalLedValue = (direction / 3) * 2; // Becomes 2 or -2
        }
        else if (scanOrder == 4) // Last card is dimmest
        {
            finalLedValue = (direction / 3) * 1; // Becomes 1 or -1
        }

        // 3. Apply to correct type's array
        switch (type)
        {
            case PokemonClassifier.PokemonType.Fire:
                fireRuleLeds[ledIndex] = finalLedValue;
                break;
            case PokemonClassifier.PokemonType.Water:
                waterRuleLeds[ledIndex] = finalLedValue;
                break;
            case PokemonClassifier.PokemonType.Grass:
                grassRuleLeds[ledIndex] = finalLedValue;
                break;
            case PokemonClassifier.PokemonType.Dragon:
                dragonRuleLeds[ledIndex] = finalLedValue;
                break;
        }

        // 4. Compose full 24-LED array
        int[] fullArray = new int[24];
        Array.Copy(fireRuleLeds, 0, fullArray, 0, 6);
        Array.Copy(waterRuleLeds, 0, fullArray, 6, 6);
        Array.Copy(grassRuleLeds, 0, fullArray, 12, 6);
        Array.Copy(dragonRuleLeds, 0, fullArray, 18, 6);

        string csv = string.Join(",", fullArray);
        Debug.Log("Sending to ESP32 (Method 2): " + csv);

        if (ESP32Controller.Instance != null)
            ESP32Controller.Instance.SendLEDData(csv);
    }


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

    private int GetLedIndexForFeature(string featureType)
    {
        switch (featureType)
        {
            case "Attack":      return 0;
            case "Defense":     return 1;
            case "Speed":       return 2;
            case "Wings":       return 3;
            case "Temperature": return 4;
            case "Altitude":    return 5;
            default:            return -1;
        }
    }

    private void PerformScanForGroup(int groupIndex, uint cardId)
    {
        var cube = Sample_Sensor.Instance?.cube;
        if (cube != null && cube.isConnected)
        {
            // Sound #8 is a good, short "confirm" sound.
            cube.PlayPresetSound(8);
        }
    
        currentScanCount++;
        string idx = StandardID.GetCardNameByID(cardId);

        var type = (PokemonClassifier.PokemonType)groupIndex;
        rulesByType[type].Add(idx);
        UpdateAndSendLedData(type, idx, currentScanCount);

        var ph = cardPlaceholderGroups[groupIndex].placeholders;
        if (ph.Count >= currentScanCount)
            ph[currentScanCount - 1].sprite = CardDataManager.Instance.GetSprite(idx) ?? questionCard;

        UpdateScanTitle(groupIndex);
        
        scanInstructions[groupIndex].text = "Great! Scan another card, or click Next.";

        AssignButtonOrPhysical(resetButton, () => ResetScanForGroup(groupIndex), resetText);
        AssignButtonOrPhysical(nextButton, () => StartCoroutine(OnFinishScanForGroup(groupIndex)), finishText);
    }

    private void ResetScanForGroup(int groupIndex)
    {
        if (_autoScanCoroutine != null) StopCoroutine(_autoScanCoroutine);

        currentScanCount = 0;
        scannedCardGroups[groupIndex].Clear();
        scanTitles[groupIndex].text = "Scan: 0/4";
        scanInstructions[groupIndex].text = "Please scan a valid card";

        foreach (var ph in cardPlaceholderGroups[groupIndex].placeholders)
            ph.sprite = questionCard;

        _autoScanCoroutine = StartCoroutine(WaitForScanForGroup(groupIndex));
    }

    private IEnumerator OnFinishScanForGroup(int groupIndex)
    {
        if (_isTransitioning) yield break;
        _isTransitioning = true;
        var cg = scanGroups[groupIndex];
        yield return FadeCanvas(cg, 1, 0, 1f);
        cg.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        finishText.gameObject.SetActive(false);
        resetText.gameObject.SetActive(false);
        
        if (_singleGroupEditMode)
        {
            _singleGroupEditMode = false;
            currentGroupIndex = _savedGroupIndex; 
            yield return StartCoroutine(PerformClassificationAndShowResults());
            _isTransitioning = false;
            yield break;
        }
        
        currentGroupIndex++;
        if (currentGroupIndex < scanGroups.Count)
            yield return ShowScanUIForGroup(currentGroupIndex);
        else
            yield return PerformClassificationAndShowResults();
        _isTransitioning = false;
    }

    private IEnumerator PerformClassificationAndShowResults()
    {
          // Pass the 'rulesByType' dictionary directly to the new ClassifyMethod2.
        var results = pokemonClassifier.ClassifyMethod2(rulesByType);

        method2ResultsPanel.SetActive(true);
        yield return ShowMethod2Results(results);
    }


    public Dictionary<PokemonClassifier.PokemonType, List<string>> GetLearnedRules()
    {
        return rulesByType;
    }

    private IEnumerator ShowMethod2Results(PokemonClassifier.Method2Results results)
    {
        method2ResultsPanel.SetActive(true);
        ResetResultUI();
        var shinyOpenCoroutines = new List<Coroutine>();
        for (int i = 0; i < typeBoxes.Count; i++)
        {
            shinyOpenCoroutines.Add(
                StartCoroutine(AnimateBoxShinyAndOpen(typeBoxes[i], boxGifs[i]))
            );
        }
        foreach (var c in shinyOpenCoroutines)
            yield return c;

        //yield return new WaitForSeconds(2.5f);

        var destroyCoros = new List<Coroutine>();
        for (int i = 0; i < typeBoxes.Count; i++)
            destroyCoros.Add(StartCoroutine(AnimateBoxDestroy(typeBoxes[i], boxGifs[i])));
        foreach (var c in destroyCoros) yield return c;

        yield return StartCoroutine(DisplayMethod2Results(results));
    }

    private void WireEditButtons()
    {
        if (editTypeButtons == null) return;

        int n = Mathf.Min(4, editTypeButtons.Count);
        for (int i = 0; i < n; i++)
        {
            int gi = i; 
            var btn = editTypeButtons[gi];
            if (!btn) continue;

            btn.gameObject.SetActive(true);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => StartCoroutine(EditTypeAndReclassify(gi)));
        }
        
    }


    private IEnumerator EditTypeAndReclassify(int groupIndex)
    {
        _singleGroupEditMode = true;
        _editTargetIndex = groupIndex;
        _savedGroupIndex = currentGroupIndex;
        currentGroupIndex = groupIndex; 
        
        method2ResultsPanel.SetActive(false);
        
        rulesByType[(PokemonClassifier.PokemonType)groupIndex].Clear();
        scannedCardGroups[groupIndex].Clear();
        ResetTypePlaceholders(groupIndex);
        
        ZeroTypeLed((PokemonClassifier.PokemonType)groupIndex);
        SendAllLeds();
        
        yield return StartCoroutine(ShowScanUIForGroup(groupIndex));
    }

   private IEnumerator ShowMethod2RatioBars(PokemonClassifier.Method2Results results)
{
    // Total number of Pokémon of each type in the entire dataset (assumed to be 25 each).
    const int TOTAL_PER_TYPE = 25;
    int totalPossible = 4 * TOTAL_PER_TYPE;

    // --- 1) Fire Type Ratio Bar ---
    // The bar now represents: "Of the 25 Fire types, how many did we get right?"
    int fireCorrect = results.correctPredictions[PokemonClassifier.PokemonType.Fire].Count;
    float fireRatio = TOTAL_PER_TYPE > 0 ? (float)fireCorrect / TOTAL_PER_TYPE : 0f;
    
    fireTypePanel.SetActive(true);
    fireCorrectBar.fillAmount = 0f;
    yield return StartCoroutine(AnimateRatioBar(fireCorrectBar, fireRatio, 1f));
    fireCountText.supportRichText = true;
    fireCountText.text = $"Found <color=green>{fireCorrect}</color> Fire Pokémon";

    // --- 2) Water Type Ratio Bar ---
    int waterCorrect = results.correctPredictions[PokemonClassifier.PokemonType.Water].Count;
    float waterRatio = TOTAL_PER_TYPE > 0 ? ((float)waterCorrect) / TOTAL_PER_TYPE : 0f;

    waterTypePanel.SetActive(true);
    waterCorrectBar.fillAmount = 0f;
    yield return StartCoroutine(AnimateRatioBar(waterCorrectBar, waterRatio, 1f));
    waterCountText.supportRichText = true;
    waterCountText.text = $"Found <color=green>{waterCorrect}</color> Water Pokémon";

    // --- 3) Grass Type Ratio Bar ---
    int grassCorrect = results.correctPredictions[PokemonClassifier.PokemonType.Grass].Count;
    float grassRatio = TOTAL_PER_TYPE > 0 ? ((float)grassCorrect) / TOTAL_PER_TYPE : 0f;

    grassTypePanel.SetActive(true);
    grassCorrectBar.fillAmount = 0f;
    yield return StartCoroutine(AnimateRatioBar(grassCorrectBar, grassRatio, 1f));
    grassCountText.supportRichText = true;
    grassCountText.text = $"Found <color=green>{grassCorrect}</color> Grass Pokémon";

        // --- 4) Dragon Type Ratio Bar ---
        int dragonCorrect;
        float dragonRatio;

    // Let's correct the Dragon part, assuming 25 Dragons as well.
    int dragonTotalInDataset = 25; // Assuming this, you can adjust if needed
    dragonCorrect = results.correctPredictions[PokemonClassifier.PokemonType.Dragon].Count;
    dragonRatio = dragonTotalInDataset > 0 ? (float)dragonCorrect / dragonTotalInDataset : 0f;
    
    dragonTypePanel.SetActive(true);
    dragonCorrectBar.fillAmount = 0f;
    yield return StartCoroutine(AnimateRatioBar(dragonCorrectBar, dragonRatio, 1f));
    dragonCountText.supportRichText = true;
    dragonCountText.text = $"Found <color=green>{dragonCorrect}</color> Dragon Pokémon";

    // --- 5) Overall Accuracy (this part is unchanged) ---
    int totalCorrect = fireCorrect + waterCorrect + grassCorrect + dragonCorrect + results.multipleMatches.Count;
    int score = Mathf.Clamp(Mathf.RoundToInt(100f * (float)totalCorrect / totalPossible), 0, 100);

    string verdict = VerdictForScore(score);
    overallAccuracyText.supportRichText = true;
    string message = $"Score: <b>{score}</b>/100\n{verdict}";
    yield return StartCoroutine(TypeText(overallAccuracyText, message));
}



    private IEnumerator DisplayMethod2Results(PokemonClassifier.Method2Results results)
    {

        yield return StartCoroutine(ShowMethod2RatioBars(results));
        ShowFinalButtons();
        WireEditButtons();
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
        image.sprite = gifSet.finalSprite;
    }

    private void ShowFinalButtons()
    {
        AssignButtonOrPhysical(finalApplyButton, OnFinalApply, finalApplyText);
        AssignButtonOrPhysical(finalRetryButton, () => StartCoroutine(OnFinalRetry()), finalRetryText);
    }

    private void OnFinalApply()
    {
        if (GameStateManager.Instance != null)
    {
        GameStateManager.Instance.method2_rules = new Dictionary<PokemonClassifier.PokemonType, List<string>>(rulesByType);
    }
        method2ResultsPanel.SetActive(false);
        LabUI.SetActive(false);
        FadeInGameUI();
        _labCompleted = true;

        labSequenceCompleteCallback?.Invoke();
        labSequenceCompleteCallback = null;
    }

    private IEnumerator OnFinalRetry()
    {
        method2ResultsPanel.SetActive(false);

        currentGroupIndex = 0;
        foreach (var kvp in rulesByType)
            kvp.Value.Clear();
        Array.Clear(fireRuleLeds, 0, 6);
        Array.Clear(waterRuleLeds, 0, 6);
        Array.Clear(grassRuleLeds, 0, 6);
        Array.Clear(dragonRuleLeds, 0, 6);

        if (ESP32Controller.Instance != null)
            ESP32Controller.Instance.SendLEDData(ALL_LEDS_OFF);

        for (int i = 0; i < scannedCardGroups.Count; i++)
            scannedCardGroups[i].Clear();
        yield return StartCoroutine(ShowScanUIForGroup(0));
    }


    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
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
            StartCoroutine(WaitForPhysicalControl());
        }
    }

    private IEnumerator WaitForPhysicalControl()
    {
        uint lastReadId = 0;

        while (true)
        {
            uint currentId = Sample_Sensor.Instance.ReadCard();

            if (currentId != 0 && currentId != lastReadId)
            {
                lastReadId = currentId;
                string cardIndex = StandardID.GetCardNameByID(currentId);
               // Debug.Log($"Physical cardIndex {cardIndex}");

                if (HandlePhysicalCardInput(cardIndex))
                {
                    yield return new WaitForSeconds(0.5f);
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

        // Reset (↑)
        if (cardIndex == "↑")
        {
            Debug.Log("Physical Reset triggered");
            if (cube != null && cube.isConnected) cube.PlayPresetSound(9);
            if (currentGroupIndex < scanGroups.Count && scanGroups[currentGroupIndex].gameObject.activeSelf)
            {
                ResetScanForGroup(currentGroupIndex);
            }
            else if (method2ResultsPanel != null && method2ResultsPanel.activeSelf)
            {
                StartCoroutine(OnFinalRetry());
            }
            return true;
        }

        // Next/Apply (→)
        if (cardIndex == "→")
        {
            Debug.Log("Physical Next triggered");
            if (cube != null && cube.isConnected) cube.PlayPresetSound(9);
            if (labIntroGroup.gameObject.activeSelf)
            {
                StartCoroutine(OnReadInstructions());
            }
            else if (pickGroup.gameObject.activeSelf)
            {
                StartCoroutine(OnPickReady());
            }
            else if (currentGroupIndex < scanGroups.Count
                     && scanGroups[currentGroupIndex].gameObject.activeSelf)
            {
                StartCoroutine(OnFinishScanForGroup(currentGroupIndex));
            }
            else if (method2ResultsPanel.activeSelf)
            {
                OnFinalApply();
            }
            return true;
        }

        return false;
    }

    public void FadeInGameUI(float duration = 1f)
    {
        inGameUIGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(inGameUIGroup, 0f, 1f, duration));
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

    public System.Collections.Generic.List<string> GetFlatRules()
    {
        var flatRules = new System.Collections.Generic.List<string>();
        foreach (var kvp in rulesByType)
        {
            flatRules.AddRange(kvp.Value);
        }
        return flatRules;
    }

    private void ResetTypePlaceholders(int groupIndex)
    {
        foreach (var ph in cardPlaceholderGroups[groupIndex].placeholders)
            ph.sprite = questionCard;

        if (groupIndex == currentGroupIndex)
            currentScanCount = 0; 
        scanTitles[groupIndex].text = "Scan: 0/4";
        scanInstructions[groupIndex].text = "Please scan a valid card";
    }

    private void ZeroTypeLed(PokemonClassifier.PokemonType type)
    {
        switch (type)
        {
            case PokemonClassifier.PokemonType.Fire:   Array.Clear(fireRuleLeds, 0, 6);   break;
            case PokemonClassifier.PokemonType.Water:  Array.Clear(waterRuleLeds, 0, 6);  break;
            case PokemonClassifier.PokemonType.Grass:  Array.Clear(grassRuleLeds, 0, 6);  break;
            case PokemonClassifier.PokemonType.Dragon: Array.Clear(dragonRuleLeds, 0, 6); break;
        }
    }

    private void SendAllLeds()
    {
        int[] full = new int[24];
        Array.Copy(fireRuleLeds,   0, full,  0, 6);
        Array.Copy(waterRuleLeds,  0, full,  6, 6);
        Array.Copy(grassRuleLeds,  0, full, 12, 6);
        Array.Copy(dragonRuleLeds, 0, full, 18, 6);
        string csv = string.Join(",", full);
        if (ESP32Controller.Instance != null)
            ESP32Controller.Instance.SendLEDData(csv);
    }

    private void ResetResultUI()
    {
        fireCountText.text = "";
        waterCountText.text = "";
        grassCountText.text = "";
        dragonCountText.text = "";
        overallAccuracyText.text = "";
        
        if (fireCorrectBar) fireCorrectBar.fillAmount = 0f;
        if (waterCorrectBar) waterCorrectBar.fillAmount = 0f;
        if (grassCorrectBar) grassCorrectBar.fillAmount = 0f;
        if (dragonCorrectBar) dragonCorrectBar.fillAmount = 0f;
        
        if (fireTypePanel) fireTypePanel.SetActive(false);
        if (waterTypePanel) waterTypePanel.SetActive(false);
        if (grassTypePanel) grassTypePanel.SetActive(false);
        if (dragonTypePanel) dragonTypePanel.SetActive(false);
    }

}
