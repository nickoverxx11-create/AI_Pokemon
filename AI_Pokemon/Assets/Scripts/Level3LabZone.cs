using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using toio.Samples.Sample_Sensor;
using toio.Simulator;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TrainingChoice
{
    public string questionName; // e.g., "Fire Packages"
    [Tooltip("The ACTION card that corresponds to the correct answer. Must be either '→' (for A) or '↑' (for B).")]
    public string correctAnswerActionCard; // This will be "→" or "↑"
    [Header("UI Sprites for this question")]
    public Sprite optionASprite; 
    public Sprite optionBSprite;
}
public class Level3LabZone : MonoBehaviour
{
    public static Level3LabZone Instance;

    [Header("InGame UI")]
    public CanvasGroup inGameUIGroup;

    [Header("LabZone UI")]
    public GameObject LabUI;
    public CanvasGroup labIntroGroup;
    public Button readInstructionsButton;
    
    /*public CanvasGroup instructionsGroup;
    public Image instructionsImage;
    public Button understoodButton;*/

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

    [Header("Question Option Images")]
    public Image optionAImage;          
    public Image optionBImage;        
    public RectTransform selectedCenterAnchor; 

    
    [Header("Quiz Setup")]
    public List<TrainingChoice> trainingChoices = new List<TrainingChoice>();

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
    private Vector2 optionAStartPos;
    private Vector2 optionBStartPos;
    private Vector3 optionAStartScale;
    private Vector3 optionBStartScale;

    public PokemonClassifier.ModelWeights TrainedModel { get; private set; }
    
    private List<string> fixedDatasetIDs = new List<string> { "C", "D", "E", "F" };


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        pokemonClassifier = GetComponent<PokemonClassifier>() ?? gameObject.AddComponent<PokemonClassifier>();
        pokemonClassifier.Initialize();

    }
    
    private void Start()
    {
        if (optionAImage != null)
        {
            optionAStartPos = optionAImage.rectTransform.anchoredPosition;
            optionAStartScale = optionAImage.rectTransform.localScale;
        }
        if (optionBImage != null)
        {
            optionBStartPos = optionBImage.rectTransform.anchoredPosition;
            optionBStartScale = optionBImage.rectTransform.localScale;
        }
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
        
        _datasetsToShowOnLed.Clear();

        // The four types and their corresponding data/sprites
        var types = new[] { "Fire", "Water", "Grass", "Dragon" };
        var dataCardIDs = new[] { "C", "D", "E", "F" };
        var typeIcons = new[] { fireIcon, waterIcon, grassIcon, dragonIcon };

        // Loop through the four questions
        for (int i = 0; i < trainingChoices.Count; i++)
        {
            datasetIconImage.gameObject.SetActive(false);
            optionAImage.gameObject.SetActive(true);
            optionBImage.gameObject.SetActive(true);
            var choice = trainingChoices[i];
            SetupQuestionImages(choice);
            
            // Construct the instruction text
            string instruction = Language.IsGerman ? $"For the {types[i]} type, find the two packages on the back of the guidebook page." : $"Für den {types[i]}-Typ finde die zwei Pakete auf der Rückseite der Buchseite.";
            yield return StartCoroutine(TypeText(trainingInstructionText, instruction));
            
            // Wait for the correct action card to be scanned
            yield return StartCoroutine(WaitForCorrectActionCard(choice.correctAnswerActionCard, instruction));
            
            // If correct, perform the original actions for this step
            Sample_Sensor.Instance.cube?.PlayPresetSound(8); // Success sound
            yield return StartCoroutine(TypeText(trainingInstructionText, Language.IsGerman? "Excellent! Very well done. Now for the next one.":"Ausgezeichnet! Sehr gut gemacht. Jetzt zum Nächsten."));
            optionAImage.gameObject.SetActive(false);
            optionBImage.gameObject.SetActive(false);
            datasetIconImage.gameObject.SetActive(true);
            datasetIconImage.sprite = typeIcons[i];
            _datasetsToShowOnLed.Add(dataCardIDs[i]);
            UpdateLedDisplay();
            yield return new WaitForSeconds(3f);
        }

        // All steps are complete
        yield return StartCoroutine(TypeText(trainingInstructionText, Language.IsGerman?"Great job! You've seen how each type is different.":  "Großartige Arbeit! Du hast gesehen, wie sich jeder Typ unterscheidet."));
        yield return new WaitForSeconds(2f);
        
        OnFinishLab();
    }

    // NEW HELPER COROUTINE: This handles the quiz logic for one question.
    private IEnumerator WaitForCorrectActionCard(string correctActionCard, string instruction)
    {
        uint lastReadId = 0;
        while (true)
        {
            uint currentId = Sample_Sensor.Instance.ReadCard();
            if (currentId != 0 && currentId != lastReadId)
            {
                lastReadId = currentId;
                string scannedCardID = StandardID.GetCardNameByID(currentId);

                // We only care about the "Next" (→) or "Reset" (↑) cards.
                if (scannedCardID == "→" || scannedCardID == "↑")
                {
                    if (scannedCardID == correctActionCard)
                    {
                        yield return StartCoroutine(ShowCorrectImageFeedback(correctActionCard));
                        yield break; // Correct card was scanned, exit the loop.
                    }
                    else
                    {
                        // --- INCORRECT CHOICE ---
                        Sample_Sensor.Instance.cube?.PlayPresetSound(10); // Error sound
                        yield return StartCoroutine(TypeText(trainingInstructionText, "Not quite. Look closely at the patterns in the stats. The pure data should be more consistent. Try again!"));
                        yield return new WaitForSeconds(1.5f);
                        yield return StartCoroutine(TypeText(trainingInstructionText, instruction)); // Repeat the instruction
                    }
                }
            }
            yield return null;
        }
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

    private void SetupQuestionImages(TrainingChoice choice)
    {
        if (optionAImage != null)
        {
            optionAImage.sprite = choice.optionASprite;
            optionAImage.rectTransform.anchoredPosition = optionAStartPos;
            optionAImage.rectTransform.localScale = optionAStartScale;
            optionAImage.gameObject.SetActive(true);
        }

        if (optionBImage != null)
        {
            optionBImage.sprite = choice.optionBSprite;
            optionBImage.rectTransform.anchoredPosition = optionBStartPos;
            optionBImage.rectTransform.localScale = optionBStartScale;
            optionBImage.gameObject.SetActive(true);
        }
    }

    private IEnumerator ShowCorrectImageFeedback(string correctActionCard)
    {
        if (optionAImage == null || optionBImage == null || selectedCenterAnchor == null)
            yield break;
        
        Image selectedImage;
        Image otherImage;

        if (correctActionCard == "→")
        {
            selectedImage = optionAImage;
            otherImage = optionBImage;
        }
        else // "↑"
        {
            selectedImage = optionBImage;
            otherImage = optionAImage;
        }
        
        otherImage.gameObject.SetActive(false);
        
        RectTransform selRT = selectedImage.rectTransform;

        Vector2 startPos = selRT.anchoredPosition;
        Vector3 startScale = selRT.localScale;

        Vector2 targetPos = selectedCenterAnchor.anchoredPosition;
        Vector3 targetScale = startScale * 1.5f;

        float duration = 0.4f;
        float t = 0f;
        
        while (t < duration)
        {
            float lerp = t / duration;
            selRT.anchoredPosition = Vector2.Lerp(startPos, targetPos, lerp);
            selRT.localScale = Vector3.Lerp(startScale, targetScale, lerp);

            t += Time.deltaTime;
            yield return null;
        }
        
        selRT.anchoredPosition = targetPos;
        selRT.localScale = targetScale;

        yield return new WaitForSeconds(1.5f);
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