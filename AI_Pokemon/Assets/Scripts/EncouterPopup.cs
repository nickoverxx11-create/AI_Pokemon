// Enhanced EncounterPopup for Pokemon prediction during gameplay
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using toio;
using toio.Samples.Sample_Sensor;
using System.Linq;
using toio.Simulator;
using System;
using System.Collections.Generic;

// This enum is still needed to select the prediction logic
public enum PredictionMode
{
    Method1_FireOrNot,
    Method2_MultiType,
    Method3_MachineLearning,
    Method4_BossBattle // New mode for the final challenge
}

public class EncounterPopup : MonoBehaviour
{
    public static EncounterPopup Instance;
    private static string _lastEncounteredCardIndex = null;

    [Header("Canvas Groups")]
    public CanvasGroup boxGroup;
    public CanvasGroup cardGroup;
    public CanvasGroup predictionGroup; // New: For Pokemon prediction phase

    [Header("Box Elements")]
    public Image boxImage;
    public Text boxTitleText;
    //public Button unboxButton;

    [Header("Card Instruction Elements")]
    public Text cardTitleText;
    public Text cardDescriptionText;
    // public Button readyButton;
    public GameObject instructionImage;
    public GameObject templeImage;

    [Header("Pokemon Prediction Elements")]
    public Image encounterPokemonImage;
    public Text predictionInstructionText;
    public Text predictionResultText;
    public Button skipPredictionButton; // Optional: Allow skipping

    [Header("Sprites")]
    public Sprite questionCardSprite;


    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    public float scaleDuration = 2f;
    public float typingSpeed = 0.02f; // Faster typing for easier 
    public float imageDuration = 3f;

    [Header("Method 4 Battle UI")]
    public CanvasGroup battleUIGroup; // A parent object for the battle choice UI
    public Text battleInstructionText; // e.g., "Choose your Pokémon to battle!"
    public Text battleResultText; // A text field to show "You win!" or "You lost..."
    public GameObject pokemonTeamPanel; // A panel with a HorizontalLayoutGroup for the buttons
    public GameObject pokemonButtonPrefab; // Your button prefab



    private System.Action afterPopup;
    private PredictionMode currentPredictionMode;
    private PokemonClassifier.TestPokemon currentWildPokemon;
    private bool isWaitingForScan = false;
    private uint lastScannedID = 9999999;

    [Header("Dependencies")]
    public PokemonClassifier pokemonClassifier;

    private void Awake()
    {
        Instance = this;

        // Initialize all canvas groups
        boxGroup.alpha = 0;
        boxGroup.gameObject.SetActive(false);
        cardGroup.alpha = 0;
        cardGroup.gameObject.SetActive(false);
        predictionGroup.alpha = 0;
        predictionGroup.gameObject.SetActive(false);

        // --- ADD THIS LINE ---
        battleUIGroup.alpha = 0;
        battleUIGroup.gameObject.SetActive(false);

        // Set up button listeners
        //unboxButton.onClick.AddListener(ShowCardGroup);
        //readyButton.onClick.AddListener(OnReadyClicked);
        if (skipPredictionButton != null)
            skipPredictionButton.onClick.AddListener(SkipPrediction);

    }

    // MODIFIED: Simplified to always handle a Pokemon encounter. The EncounterType parameter is removed.
    public void ShowEncounter(PredictionMode mode, System.Action callback = null)
    {
        afterPopup = callback;
        currentPredictionMode = mode; // Store the mode
        cardDescriptionText.text = "";
        predictionInstructionText.text = "";
        predictionResultText.text = "";

        // It's always a Pokemon encounter now, so we set the UI directly.
        //cardImage.sprite = pokemonSprite;
        cardTitleText.text = "Wild Pokémon!";
        switch (mode)
        {
            case PredictionMode.Method1_FireOrNot: cardDescriptionText.text = "Pick a Pokémon card, I'll use your rules to guess if it's a Fire type!"; break;
            case PredictionMode.Method2_MultiType: cardDescriptionText.text = "Pick a Pokémon card, I'll use your playbook to guess its type!"; break;
            case PredictionMode.Method3_MachineLearning: cardDescriptionText.text = "Pick a Pokémon card, my AI brain will predict its type!"; break;
            case PredictionMode.Method4_BossBattle:
                cardTitleText.text = "A GUARDIAN APPEARS!";
                cardDescriptionText.text = "This is the final challenge!\nMy AI will predict its type...";
                break;
        }
         if (mode != PredictionMode.Method4_BossBattle)
        {
            StartCoroutine(PlayBoxGroup());
        }
        else
        {
            ShowCardGroup();
        }
    }

    private IEnumerator PlayBoxGroup()
    {
        boxGroup.gameObject.SetActive(true);
        boxGroup.alpha = 0;
        boxImage.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        // Fade In
        float t = 0f;
        while (t < fadeDuration)
        {
            boxGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        boxGroup.alpha = 1;

        // Scale In Box
        t = 0f;
        while (t < scaleDuration)
        {
            boxImage.transform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 0.8f), Vector3.one, t / scaleDuration);
            t += Time.deltaTime;
            yield return null;
        }
        boxImage.transform.localScale = Vector3.one;
        StartCoroutine(AutoProceedToCardGroup());
    }
    private IEnumerator AutoProceedToCardGroup()
    {
        yield return new WaitForSeconds(2f);
        ShowCardGroup();
    }

    private void ShowCardGroup()
    {
        StartCoroutine(TransitionToCardGroup());
    }

    private IEnumerator TransitionToCardGroup()
    {
        // BoxGroup Fade Out
        float t = 0f;
        while (t < fadeDuration)
        {
            boxGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        boxGroup.alpha = 0;
        boxGroup.gameObject.SetActive(false);

        // CardGroup Fade In
        instructionImage.SetActive(currentPredictionMode != PredictionMode.Method4_BossBattle);
        templeImage.SetActive(currentPredictionMode == PredictionMode.Method4_BossBattle);
        cardGroup.gameObject.SetActive(true);
        cardGroup.alpha = 0;
        t = 0f;
        while (t < fadeDuration)
        {
            cardGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        cardGroup.alpha = 1;
        StartCoroutine(AutoProceedToScanPhase());
    }

    private IEnumerator AutoProceedToScanPhase()
    {
        yield return new WaitForSeconds(imageDuration);
        StartCoroutine(StartPokemonScanning());
    }


    // MODIFIED: Simplified to always start the Pokemon scanning phase.
    private void OnReadyClicked()
    {
        StartCoroutine(StartPokemonScanning());
    }

    private IEnumerator StartPokemonScanning()
    {
        encounterPokemonImage.sprite = questionCardSprite;
        yield return StartCoroutine(TransitionToPredictionGroup());

        // --- NEW LOGIC FOR METHOD 4 ---
        if (currentPredictionMode == PredictionMode.Method4_BossBattle)
        {
            // Skip the physical scan and go straight to the automatic prediction.
            yield return StartCoroutine(StartAutomaticPrediction());
        }
        else
        {
            // For all other modes, ask the user to scan a card.
            yield return StartCoroutine(TypeText(predictionInstructionText,
                "Pick a card from the box!\nScan it with Toio..."));
            StartCoroutine(WaitForPokemonCardScan());
        }
    }

    private IEnumerator TransitionToPredictionGroup()
    {
        // CardGroup Fade Out
        float t = 0f;
        while (t < fadeDuration)
        {
            cardGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        cardGroup.alpha = 0;
        cardGroup.gameObject.SetActive(false);

        // PredictionGroup Fade In
        predictionGroup.gameObject.SetActive(true);
        predictionGroup.alpha = 0;
        t = 0f;
        while (t < fadeDuration)
        {
            predictionGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        predictionGroup.alpha = 1;
    }

    private IEnumerator WaitForPokemonCardScan()
    {
        isWaitingForScan = true;
        lastScannedID = 9999999;
        currentWildPokemon = null;

        yield return StartCoroutine(TypeText(predictionResultText, "Scan your card..."));

        while (isWaitingForScan && currentWildPokemon == null)
        {
            uint cardId = Sample_Sensor.Instance.ReadCard();

            if (cardId != 0 && cardId != lastScannedID)
            {
                string cardIndex = StandardID.GetCardNameByID(cardId);
                lastScannedID = cardId;
                var cube = Sample_Sensor.Instance?.cube;
                if (cardIndex == _lastEncounteredCardIndex)
                {
                    yield return StartCoroutine(TypeText(predictionResultText, "You just saw that Pokémon! Please scan a different one."));
                    // We don't proceed, we just wait for another scan.
                }
                // Part 2: If it's a valid, NEW Pokémon card...
                else if (IsPokemonCard(cardIndex))
                {
                    // Get the Pokemon that was scanned
                    currentWildPokemon = GetPokemonByCardIndex(cardIndex);

                    if (currentWildPokemon != null)
                    {
                        if (cube != null && cube.isConnected)
                        {
                            cube.PlayPresetSound(8); // Preset sound #8 is a good "confirm" sound
                        }

                        _lastEncounteredCardIndex = cardIndex;
                        string index = GetCardIndexByPokemonName(currentWildPokemon.name);
                        Sprite pokemonSprite = CardDataManager.Instance.GetSprite(index);
                        if (pokemonSprite != null)
                        {
                            encounterPokemonImage.sprite = pokemonSprite;
                            encounterPokemonImage.enabled = true;
                        }

                        yield return StartCoroutine(TypeText(predictionResultText,
                            $"You found: {currentWildPokemon.name}!\n\n" +
                            "Let me guess its type..."));

                        yield return new WaitForSeconds(2f);

                        // Start the AUTOMATIC prediction phase (no more card scanning!)
                        yield return StartCoroutine(StartAutomaticPrediction());
                        break;
                    }
                }
                else if (IsFeatureCard(cardIndex))
                {
                    yield return StartCoroutine(TypeText(predictionResultText,
                        "Wrong card! Scan your Pokémon card first."));
                }
                else
                {
                    yield return StartCoroutine(TypeText(predictionResultText,
                        "Try again! Scan your Pokémon card."));
                }
            }
            yield return null;
        }
    }

    private IEnumerator StartAutomaticPrediction()
    {
        string instruction = "";
        switch (currentPredictionMode)
        {
            case PredictionMode.Method1_FireOrNot:
                instruction = $"Using your Lab Zone 1 rules to check:\nIs {currentWildPokemon.name} Fire type?";
                break;
            case PredictionMode.Method2_MultiType:
                instruction = $"Using your Lab Zone 2 rules to guess:\nWhat type is {currentWildPokemon.name}?";
                break;
            case PredictionMode.Method3_MachineLearning:
                instruction = $"Using my trained AI brain to predict:\nWhat type is {currentWildPokemon.name}?";
                break;
        }

        yield return StartCoroutine(TypeText(predictionInstructionText, instruction));

        yield return StartCoroutine(MakeAutomaticPrediction());
    }

    private IEnumerator MakeAutomaticPrediction()
    {
        isWaitingForScan = false;
        yield return StartCoroutine(TypeText(predictionResultText, "Thinking..."));
        yield return new WaitForSeconds(1f);

        bool correctPrediction = false;
        var cube = Sample_Sensor.Instance?.cube; // Get a reference to the cube for sounds

        switch (currentPredictionMode)
        {
            case PredictionMode.Method1_FireOrNot:
                var lab1 = Level1LabZone.Instance;
                if (lab1 == null || lab1.pokemonClassifier == null || lab1.learnedFeatureCards.Count == 0)
                {
                    yield return StartCoroutine(TypeText(predictionResultText, "Error! No Lab Zone 1 rules found."));
                    yield return new WaitForSeconds(3f);
                    StartCoroutine(FinishPopup());
                    yield break;
                }

                // --- NEW: Rule-by-Rule Check Logic ---

                // 1. Get the rules and the Pokémon's features
                var fireRules = lab1.pokemonClassifier.ConvertScannedCardsToRules(lab1.learnedFeatureCards);
                var pokemonFeatures = currentWildPokemon.GetFeatures();
                predictionResultText.text = ""; // Clear the text for the rule check

                int matchedRules = 0;
                bool actuallyFire = currentWildPokemon.correctType == PokemonClassifier.PokemonType.Fire;

                // 2. Loop through each rule and display the check
                foreach (var rule in fireRules)
                {
                    string ruleName = GetFeatureNameFromCardID(rule);
                    yield return StartCoroutine(TypeText(predictionResultText, $"Checking for: {ruleName}... "));
                    yield return new WaitForSeconds(0.75f); // Pause for suspense

                    bool isMatch;

                    // This is the core of the new "opposite" logic
                    if (actuallyFire)
                    {
                        // If it's a real Fire type, a match is a direct match.
                        isMatch = pokemonFeatures.Contains(rule);
                    }
                    else
                    {
                        // If it's NOT a Fire type, a "match" is an OPPOSITE match.
                        var oppositeRule = GetOppositeRule(rule);
                        isMatch = pokemonFeatures.Contains(oppositeRule);
                    }
                    
                    if (isMatch)
                    {
                        matchedRules++;
                        predictionResultText.text += "<color=green>MATCH!</color>\n";
                        if (cube != null && cube.isConnected) cube.PlayPresetSound(8); // Success sound

                        if (!actuallyFire)
                            {
                                break; // Exit the foreach loop early.
                            }
                    }
                    else
                    {
                        predictionResultText.text += "<color=red>NO MATCH!</color>\n";
                        if (cube != null && cube.isConnected) cube.PlayPresetSound(10); // Fail/cancel sound
                        
                    }
                    yield return new WaitForSeconds(1.5f); // Pause to let the user read the result
                }

                bool predictedFire;

                if (actuallyFire)
                {
                    // For actual Fire types, use the strictness mode as before.
                    switch (lab1.selectedStrictness)
                    {
                        case PokemonClassifier.StrictnessMode.Perfect:
                            predictedFire = (matchedRules == fireRules.Count);
                            break;
                        case PokemonClassifier.StrictnessMode.Almost:
                            predictedFire = (matchedRules >= (fireRules.Count - 1));
                            break;
                        default:
                            predictedFire = false;
                            break;
                    }
                }
                else
                {
                    // For non-Fire types, the AI predicts "Not Fire" if it found AT LEAST ONE opposite match.
                    predictedFire = (matchedRules == 0);
                }
                
                correctPrediction = (predictedFire == actuallyFire);

                // 4. Display the final verdict
                string finalText = $"My guess: {(predictedFire ? "It IS a Fire type!" : "It's NOT a Fire type.")}\n" +
                                $"Reality: {(actuallyFire ? "It IS a Fire type!" : "It's NOT a Fire type.")}\n" +
                                $"{(correctPrediction ? "I was RIGHT! " : "I was WRONG! ")}";

                yield return StartCoroutine(TypeText(predictionResultText, finalText));
                break;

            case PredictionMode.Method2_MultiType:
                var lab2 = Level2LabZone.Instance;
                if (lab2 == null || lab2.pokemonClassifier == null)
                {
                    yield return StartCoroutine(TypeText(predictionResultText, "Error! No Lab Zone 2 rules found."));
                    yield return new WaitForSeconds(3f);
                    StartCoroutine(FinishPopup());
                    yield break;
                }

                string cardIndex2 = GetCardIndexByPokemonName(currentWildPokemon.name);
                if (string.IsNullOrEmpty(cardIndex2))
                {
                    yield return StartCoroutine(TypeText(predictionResultText, "Error! Could not identify this Pokémon."));
                    yield return new WaitForSeconds(3f);
                    StartCoroutine(FinishPopup());
                    yield break;
                }

                var learnedRules = lab2.GetLearnedRules();
                PokemonClassifier.Method2SingleResult result2 = lab2.pokemonClassifier.TestSinglePokemonMethod2(learnedRules, cardIndex2);
                correctPrediction = result2.IsCorrect;

                yield return StartCoroutine(TypeText(predictionInstructionText, "Let's see which team this Pokémon joins!"));
                
                 // 1. Build a single string with all four scores at once.
                string scoreText = "Scores:\n" +
                    $"<color=red>Fire: {result2.scores[PokemonClassifier.PokemonType.Fire]}</color>   " +
                    $"<color=white>Water: {result2.scores[PokemonClassifier.PokemonType.Water]}</color>\n" +
                    $"<color=green>Grass: {result2.scores[PokemonClassifier.PokemonType.Grass]}</color>   " +
                    $"<color=orange>Dragon: {result2.scores[PokemonClassifier.PokemonType.Dragon]}</color>";
                
                // 2. Display the entire score block instantly.
                predictionResultText.text = scoreText;
                if (cube != null && cube.isConnected) cube.PlayPresetSound(7); // Play one "thinking" sound
                
                // 3. Wait for the player to read the scores.
                yield return new WaitForSeconds(2.5f);

                // Display the final verdict
                string finalText2 = $"\n\nMy guess: {(result2.predictedType.HasValue ? result2.predictedType.ToString() : "It's a tie or I'm not sure.")}\n" +
                                    $"Reality: {result2.actualType}\n" +
                                    $"{(correctPrediction ? "I was RIGHT! " : "I was WRONG! ")}";
                yield return StartCoroutine(TypeText(predictionResultText, finalText2));
                break;

            case PredictionMode.Method3_MachineLearning:
                PokemonClassifier.ModelWeights modelToUse = null;
                PokemonClassifier localClassifier = null; // A temporary variable to hold the reference
                // Check which zone we are in to get the correct model.
                // This assumes your GameStateManager or a similar script tracks the current zone.
                // For now, we'll check the instances directly.
                if (Level3LabZone.Instance != null && GameStateManager.Instance.currentGameZone == GameZone.Zone3)
                {
                    modelToUse = Level3LabZone.Instance.TrainedModel; // Get model from Lab 3
                    localClassifier = Level3LabZone.Instance.pokemonClassifier; // Get the classifier from Lab 3
                }
                else if (Level4LabZone.Instance != null)
                {
                    modelToUse = Level4LabZone.Instance.TrainedModel; // Get model from Lab 4
                    localClassifier = Level4LabZone.Instance.pokemonClassifier; // Get the classifier from Lab 4
                }

                if (modelToUse == null || localClassifier == null)
                {
                    yield return StartCoroutine(TypeText(predictionResultText, "Error! AI brain has not been trained for this zone."));
                    yield return new WaitForSeconds(3f);
                    StartCoroutine(FinishPopup());
                    yield break;
                }

                string cardIndex3 = GetCardIndexByPokemonName(currentWildPokemon.name);
                if (string.IsNullOrEmpty(cardIndex3))
                {
                    yield return StartCoroutine(TypeText(predictionResultText, "Error! Could not identify this Pokémon."));
                    yield return new WaitForSeconds(3f);
                    StartCoroutine(FinishPopup());
                    yield break;
                }

                // --- NEW: Method 3 "AI Brain Scan" Animation (Text Only) ---
                // Instead of using the script's "pokemonClassifier", use the "localClassifier"
                // we just got from the active Lab Zone instance.
                var result3 = localClassifier.TestMethod3OnSinglePokemon(modelToUse, cardIndex3);

                correctPrediction = result3.IsCorrect;

                yield return StartCoroutine(TypeText(predictionInstructionText, "Activating my AI brain... Analyzing confidence!"));
                if (cube != null && cube.isConnected) cube.PlaySound(1, new Cube.SoundOperation[] { new Cube.SoundOperation(2000, 80, 50) }); // Hum sound

                predictionResultText.text = "Confidence:\n";
                string fireText = $"<color=red>Fire: 0%</color>  ";
                string waterText = $"<color=>Water: 0%</color>\n";
                string grassText = $"<color=green>Grass: 0%</color>  ";
                string dragonText = $"<color=orange>Dragon: 0%</color>";
                predictionResultText.text += fireText + waterText + grassText + dragonText;

                // Animate the confidence percentages increasing
                float timer = 0f;
                float animDuration = 2.5f;
                while (timer < animDuration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / animDuration;

                    // Calculate current percentages
                    float firePct = Mathf.Lerp(0, result3.confidenceScores[PokemonClassifier.PokemonType.Fire], progress);
                    float waterPct = Mathf.Lerp(0, result3.confidenceScores[PokemonClassifier.PokemonType.Water], progress);
                    float grassPct = Mathf.Lerp(0, result3.confidenceScores[PokemonClassifier.PokemonType.Grass], progress);
                    float dragonPct = Mathf.Lerp(0, result3.confidenceScores[PokemonClassifier.PokemonType.Dragon], progress);

                    // Update the text strings
                    fireText = $"<color=red>Fire: {firePct:F0}%</color>  ";
                    waterText = $"<color=white>Water: {waterPct:F0}%</color>\n";
                    grassText = $"<color=green>Grass: {grassPct:F0}%</color>  ";
                    dragonText = $"<color=orange>Dragon: {dragonPct:F0}%</color>";

                    // Rebuild the full string and update the UI
                    predictionResultText.text = "Confidence:\n" + fireText + waterText + grassText + dragonText;

                    yield return null;
                }

                yield return new WaitForSeconds(1.5f);

                // Display the final verdict
                string finalText3 = $"\n\nMy guess: {(result3.predictedType.HasValue ? result3.predictedType.ToString() : "Not sure")}\n" +
                                    $"Reality: {result3.actualType}\n" +
                                    $"{(correctPrediction ? "I was RIGHT!" : "I was WRONG!")}";
                yield return StartCoroutine(TypeText(predictionResultText, finalText3));
                break;

            case PredictionMode.Method4_BossBattle:
                // This case handles its own flow and does not fall through to the end.
                yield return StartCoroutine(RunMethod4BossBattle());
                yield break; // Exit the method here.


        }

        ProvideFeedback(correctPrediction);
        yield return new WaitForSeconds(1f);

        if (correctPrediction)
        {
            yield return StartCoroutine(TypeText(predictionResultText, "\nYour rules worked!\nYou caught it!"));
        }
        else
        {
            yield return StartCoroutine(TypeText(predictionResultText, "\nYour rules need more work!\nIt ran away!"));
        }

        yield return new WaitForSeconds(3f);
        StartCoroutine(FinishPopup());
    }

    private PokemonClassifier.CardID GetOppositeRule(PokemonClassifier.CardID rule)
    {
        switch (rule)
        {
            case PokemonClassifier.CardID.HighAttack: return PokemonClassifier.CardID.LowAttack;
            case PokemonClassifier.CardID.LowAttack: return PokemonClassifier.CardID.HighAttack;
            case PokemonClassifier.CardID.HighDefense: return PokemonClassifier.CardID.LowDefense;
            case PokemonClassifier.CardID.LowDefense: return PokemonClassifier.CardID.HighDefense;
            case PokemonClassifier.CardID.HighSpeed: return PokemonClassifier.CardID.LowSpeed;
            case PokemonClassifier.CardID.LowSpeed: return PokemonClassifier.CardID.HighSpeed;
            case PokemonClassifier.CardID.HasWings: return PokemonClassifier.CardID.NoWings;
            case PokemonClassifier.CardID.NoWings: return PokemonClassifier.CardID.HasWings;
            case PokemonClassifier.CardID.HighTemperature: return PokemonClassifier.CardID.LowTemperature;
            case PokemonClassifier.CardID.LowTemperature: return PokemonClassifier.CardID.HighTemperature;
            case PokemonClassifier.CardID.HighAltitude: return PokemonClassifier.CardID.LowAltitude;
            case PokemonClassifier.CardID.LowAltitude: return PokemonClassifier.CardID.HighAltitude;
            default: return rule; // Should not happen
        }
    }

    private bool ClassifyPokemonWithStoredRules(PokemonClassifier.TestPokemon testPokemon, Level1LabZone labZone)
    {
        var pokemon = testPokemon.ToPokemon();
        var fireRules = labZone.pokemonClassifier.ConvertScannedCardsToRules(labZone.learnedFeatureCards);
        return labZone.pokemonClassifier.PredictIsFire(pokemon, fireRules, labZone.selectedStrictness);
    }

    private bool IsPokemonCard(string cardIndex)
    {
        return cardIndex != null && cardIndex.Length == 1 && cardIndex[0] >= 'L' && cardIndex[0] <= 'Z';
    }

    private PokemonClassifier.TestPokemon GetPokemonByCardIndex(string cardIndex)
    {
        // Map card indices to Pokemon (same as your original mapping)
        switch (cardIndex)
        {
            case "L": return new PokemonClassifier.TestPokemon("Ponyta", PokemonClassifier.PokemonType.Fire, 0, 8, 7, 2, 2, 8);
            case "M": return new PokemonClassifier.TestPokemon("Ninetales", PokemonClassifier.PokemonType.Fire, 0, 7, 9, 6, 3, 9);
            case "N": return new PokemonClassifier.TestPokemon("Charizard", PokemonClassifier.PokemonType.Fire, 1, 4, 8, 5, 8, 9);
            case "O": return new PokemonClassifier.TestPokemon("Growlithe", PokemonClassifier.PokemonType.Fire, 0, 3, 7, 1, 1, 6);
            case "P": return new PokemonClassifier.TestPokemon("Slowpoke", PokemonClassifier.PokemonType.Water, 0, 1, 2, 7, 1, 3);
            case "Q": return new PokemonClassifier.TestPokemon("Wailmer", PokemonClassifier.PokemonType.Water, 0, 2, 3, 9, 0, 0);
            case "R": return new PokemonClassifier.TestPokemon("Gyarados", PokemonClassifier.PokemonType.Water, 1, 3, 9, 7, 0, 0);
            case "S": return new PokemonClassifier.TestPokemon("Psyduck", PokemonClassifier.PokemonType.Water, 0, 3, 1, 6, 0, 1);
            case "T": return new PokemonClassifier.TestPokemon("Petilil", PokemonClassifier.PokemonType.Grass, 0, 3, 2, 6, 4, 3);
            case "U": return new PokemonClassifier.TestPokemon("Deerling", PokemonClassifier.PokemonType.Grass, 0, 6, 3, 5, 6, 3);
            case "V": return new PokemonClassifier.TestPokemon("Bayleef", PokemonClassifier.PokemonType.Grass, 0, 2, 4, 9, 7, 2);
            case "W": return new PokemonClassifier.TestPokemon("Leafeon", PokemonClassifier.PokemonType.Grass, 0, 6, 5, 6, 6, 4);
            case "X": return new PokemonClassifier.TestPokemon("Rayquaza", PokemonClassifier.PokemonType.Dragon, 1, 7, 9, 4, 9, 2);
            case "Y": return new PokemonClassifier.TestPokemon("Dragonite", PokemonClassifier.PokemonType.Dragon, 1, 7, 9, 7, 9, 1);
            case "Z": return new PokemonClassifier.TestPokemon("Dialga", PokemonClassifier.PokemonType.Dragon, 0, 6, 9, 9, 8, 3);
            default: return null;
        }
    }

    public string GetCardIndexByPokemonName(string name)
    {
        // This is a reverse lookup, necessary for the Method 2 test function.
        switch (name)
        {
            case "Ponyta": return "L";
            case "Ninetales": return "M";
            case "Charizard": return "N";
            case "Growlithe": return "O";
            case "Slowpoke": return "P";
            case "Wailmer": return "Q";
            case "Gyarados": return "R";
            case "Psyduck": return "S";
            case "Petilil": return "T";
            case "Deerling": return "U";
            case "Bayleef": return "V";
            case "Leafeon": return "W";
            case "Rayquaza": return "X";
            case "Dragonite": return "Y";
            case "Dialga": return "Z";
            default: return null;
        }
    }


    // In EncounterPopup.cs

    // --- REVISED METHOD ---
    private void ProvideFeedback(bool correct)
    {
        // Get a reference to the connected cube from your main sensor/controller script
        var sensorCube = Sample_Sensor.Instance?.cube;
        if (sensorCube == null || !sensorCube.isConnected)
        {
            Debug.LogWarning("ProvideFeedback: Cannot perform Toio feedback, cube is not connected.");
            return;
        }

        // Stop any previous movement animations
        StopCoroutine("AnimatePredictionResult");

        // Start the new animation coroutine
        StartCoroutine(AnimatePredictionResult(sensorCube, correct));
    }


    private IEnumerator AnimatePredictionResult(Cube cube, bool isCorrect)
    {
        if (isCorrect)
        {
            // --- CORRECT PREDICTION: "Victory Dance" (approx. 3 seconds) ---
            Debug.Log("Performing long Victory Dance...");

            // 1. Turn LED Green
            cube.TurnLedOn(0, 255, 0, 0); // Solid green, stays on

            // 2. Play a longer, celebratory sound sequence
            // We'll play a rising scale of notes.
            Cube.SoundOperation[] successSound = new Cube.SoundOperation[] {
                new Cube.SoundOperation(durationMs: 200, note_number: 60, volume: 50), // C
                new Cube.SoundOperation(durationMs: 200, note_number: 64, volume: 50), // E
                new Cube.SoundOperation(durationMs: 200, note_number: 67, volume: 50), // G
                new Cube.SoundOperation(durationMs: 400, note_number: 72, volume: 80), // High C
            };
            // --- FIX: Call PlaySound with repeat count (1) as the first argument ---
            cube.PlaySound(1, successSound, Cube.ORDER_TYPE.Strong); // Start the sound sequence once

            // 3. Perform a slower, more deliberate celebratory spin/turn
            float singleTurnDuration = 0.9f;
            int numberOfTurns = 3;

            for (int i = 0; i < numberOfTurns; i++)
            {
                cube.Move(40, -40, (int)(singleTurnDuration * 1000));
                yield return new WaitForSeconds(singleTurnDuration);
            }

            // 4. Clean up
            yield return new WaitForSeconds(0.2f);
            cube.Move(0, 0, 0);
            cube.TurnLedOff();
        }
        else
        {
            // --- INCORRECT PREDICTION: "Confusion Wiggle" (approx. 3 seconds) ---
            Debug.Log("Performing long Confusion Wiggle...");

            // 1. Turn LED Red
            cube.TurnLedOn(255, 0, 0, 0); // Solid red

            // 2. Play a longer, descending "womp womp" error sound
            Cube.SoundOperation[] failSound = new Cube.SoundOperation[] {
                new Cube.SoundOperation(durationMs: 300, note_number: 55, volume: 60), // G
                new Cube.SoundOperation(durationMs: 500, note_number: 52, volume: 50), // E
            };
            // --- FIX: Call PlaySound with repeat count (1) as the first argument ---
            cube.PlaySound(1, failSound, Cube.ORDER_TYPE.Strong); // Start the sound sequence once

            // 3. Perform a slower, more pronounced back-and-forth "head shake"
            float singleWiggleDuration = 0.7f;
            int numberOfWiggles = 2;

            for (int i = 0; i < numberOfWiggles; i++)
            {
                // Turn slightly one way
                cube.Move(25, -25, (int)(singleWiggleDuration * 1000));
                yield return new WaitForSeconds(singleWiggleDuration);

                // Turn back the other way
                cube.Move(-25, 25, (int)(singleWiggleDuration * 1000));
                yield return new WaitForSeconds(singleWiggleDuration);
            }

            // 4. Clean up
            cube.Move(0, 0, 0);
            cube.TurnLedOff();
        }
    }

    private bool IsFeatureCard(string cardIndex)
    {
        return cardIndex != null && cardIndex.Length == 1 && ((cardIndex[0] >= '0' && cardIndex[0] <= '9') || cardIndex[0] == 'A' || cardIndex[0] == 'B');
    }

    private string GetFeatureName(string cardIndex)
    {
        var featureNames = new System.Collections.Generic.Dictionary<string, string>
        {
            {"0", "Low Temperature"}, {"1", "High Attack"}, {"2", "Low Attack"},
            {"3", "High Defense"}, {"4", "Low Defense"}, {"5", "High Speed"},
            {"6", "Low Speed"}, {"7", "Has Wings"}, {"8", "No Wings"},
            {"9", "High Temperature"}, {"A", "High Altitude"}, {"B", "Low Altitude"}
        };

        return featureNames.TryGetValue(cardIndex, out var name) ? name : "Unknown Feature";
    }

    private void SkipPrediction()
    {
        isWaitingForScan = false;
        StopAllCoroutines(); // Force stop any waiting coroutines
        StartCoroutine(FinishPopup());
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

    private IEnumerator FinishPopup()
    {
        CanvasGroup activeGroup = null;
        if (boxGroup.gameObject.activeInHierarchy) activeGroup = boxGroup;
        else if (cardGroup.gameObject.activeInHierarchy) activeGroup = cardGroup;
        else if (predictionGroup.gameObject.activeInHierarchy) activeGroup = predictionGroup;

        if (activeGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                activeGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
                t += Time.deltaTime;
                yield return null;
            }
            activeGroup.alpha = 0;
            activeGroup.gameObject.SetActive(false);
        }

        isWaitingForScan = false;
        currentWildPokemon = null;

        afterPopup?.Invoke();
    }

    private string GetTypeColor(PokemonClassifier.PokemonType type)
    {
        switch (type)
        {
            case PokemonClassifier.PokemonType.Fire: return "red";
            case PokemonClassifier.PokemonType.Water: return "blue";
            case PokemonClassifier.PokemonType.Grass: return "green";
            case PokemonClassifier.PokemonType.Dragon: return "orange";
            default: return "white";
        }
    }
    private string GetFeatureNameFromCardID(PokemonClassifier.CardID cardID)
    {
        switch (cardID)
        {
            case PokemonClassifier.CardID.LowTemperature: return "Low Temperature";
            case PokemonClassifier.CardID.HighTemperature: return "High Temperature";
            case PokemonClassifier.CardID.HighAttack: return "High Attack";
            case PokemonClassifier.CardID.LowAttack: return "Low Attack";
            case PokemonClassifier.CardID.HighDefense: return "High Defense";
            case PokemonClassifier.CardID.LowDefense: return "Low Defense";
            case PokemonClassifier.CardID.HighSpeed: return "High Speed";
            case PokemonClassifier.CardID.LowSpeed: return "Low Speed";
            case PokemonClassifier.CardID.HasWings: return "Has Wings";
            case PokemonClassifier.CardID.NoWings: return "No Wings";
            case PokemonClassifier.CardID.HighAltitude: return "High Altitude";
            case PokemonClassifier.CardID.LowAltitude: return "Low Altitude";
            default: return "Unknown Rule";
        }
    }

    // --- ADD ALL OF THESE NEW METHODS TO THE END OF YOUR SCRIPT ---

    private IEnumerator RunMethod4BossBattle()
    {
        // 1. A Guardian Pokémon appears automatically.
        var guardian = pokemonClassifier.GetBossPokemon(GameStateManager.Instance.currentBossIndex);
        if (guardian == null)
        {
            yield return StartCoroutine(TypeText(predictionResultText, "You have defeated all Guardians!"));
            yield return new WaitForSeconds(3f); StartCoroutine(FinishPopup()); yield break;
        }

        currentWildPokemon = new PokemonClassifier.TestPokemon(guardian.name, guardian.correctType, guardian.hasWings, guardian.speed, guardian.attack, guardian.defense, guardian.habitatAltitude, guardian.habitatTemperature);

        // You will need to add a way to get boss sprites if you have them. For now, it uses text.
        // encounterPokemonImage.sprite = ...
        yield return StartCoroutine(TypeText(predictionInstructionText, $"A wild {guardian.name} appeared!"));
        yield return new WaitForSeconds(1f);

        // 2. Predict with the best trained model (from Lab 4).
        var trainedModel = Level4LabZone.Instance.TrainedModel;
        if (trainedModel == null)
        {
            yield return StartCoroutine(TypeText(predictionResultText, "Error! Final AI model not trained!"));
            yield return new WaitForSeconds(3f);
            StartCoroutine(FinishPopup());
            yield break;
        }

        var tempBossForPrediction = new PokemonClassifier.TestPokemon(guardian.name, guardian.correctType, guardian.hasWings, guardian.speed, guardian.attack, guardian.defense, guardian.habitatAltitude, guardian.habitatTemperature);
        var result = pokemonClassifier.TestMethod3OnSinglePokemon(trainedModel, currentWildPokemon);
        yield return AnimateConfidenceText(result.confidenceScores);

        string predictionString = result.predictedType.HasValue ? result.predictedType.ToString() : "Not sure";
    
    // Get the battle hint based on the prediction.
    string battleHint = "";
    if (result.predictedType.HasValue)
    {
        battleHint = GetBattleHint(result.predictedType.Value);
    }
    
    // Add the hint to the final text string.
    string finalText = $"\n\nMy AI predicts it's a {predictionString} type!{battleHint}";
    
    yield return StartCoroutine(TypeText(predictionResultText, finalText));
        yield return new WaitForSeconds(2f);

        // 3. Transition to the player's battle choice.
        yield return StartCoroutine(StartBattleChoicePhase());
    }

    private string GetBattleHint(PokemonClassifier.PokemonType predictedType)
{
    switch (predictedType)
    {
        case PokemonClassifier.PokemonType.Fire:
            return "\n\n<color=cyan>Hint: Water types are strong against Fire!</color>";
        
        case PokemonClassifier.PokemonType.Water:
            return "\n\n<color=green>Hint: Grass types are strong against Water!</color>";
            
        case PokemonClassifier.PokemonType.Grass:
            return "\n\n<color=red>Hint: Fire types are strong against Grass!</color>";
            
        case PokemonClassifier.PokemonType.Dragon:
            return "\n\n<color=orange>Hint: Dragon types are tough! Use your most powerful Pokémon!</color>";
            
        default:
            return ""; // No hint if the type is unknown
    }
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

    private IEnumerator StartBattleChoicePhase()
    {
        predictionInstructionText.text = "";
        predictionResultText.text = "";
        battleResultText.text = "";

        battleUIGroup.gameObject.SetActive(true);
        yield return FadeCanvas(battleUIGroup, 0, 1, fadeDuration);

        yield return StartCoroutine(TypeText(battleInstructionText, "Choose a Pokémon from your collection to battle! Scan its card."));

        PokemonClassifier.TestPokemon chosenPokemon = null;
        // --- NEW: A loop that allows the player to reset their choice ---
        while (chosenPokemon == null)
        {
            // 1. Tell the player to choose and scan a Pokémon
            yield return StartCoroutine(TypeText(battleInstructionText, "Choose a Pokémon from your collection to battle! Scan its card."));

            // 2. Wait for the player to scan a valid Pokémon card
            uint lastReadId = 0;
            bool choiceMade = false;

            while (!choiceMade)
            {
                uint currentId = Sample_Sensor.Instance.ReadCard();
                if (currentId != 0 && currentId != lastReadId)
                {
                    lastReadId = currentId;
                    string cardIndex = StandardID.GetCardNameByID(currentId);

                    // Check for a Pokémon card
                    if (IsPokemonCard(cardIndex))
                    {
                        chosenPokemon = GetPokemonByCardIndex(cardIndex);
                        if (chosenPokemon != null)
                        {
                            Sample_Sensor.Instance.cube?.PlayPresetSound(8); // Success sound
                            choiceMade = true; // A valid choice was made, exit this inner loop
                        }
                    }
                    // No need to check for Reset here, as they haven't chosen anything yet
                }
                yield return null;
            }

            // 3. Ask the player to confirm their choice OR reset
            yield return StartCoroutine(TypeText(battleInstructionText, $"You chose {chosenPokemon.name}! Scan 'Next' (→) to confirm, or 'Reset' (↑) to choose again."));

            // 4. Wait for the confirmation (→) or reset (↑) scan
            lastReadId = 0;
            bool actionTaken = false;
            while (!actionTaken)
            {
                uint currentId = Sample_Sensor.Instance.ReadCard();
                if (currentId != 0 && currentId != lastReadId)
                {
                    lastReadId = currentId;
                    string cardIndex = StandardID.GetCardNameByID(currentId);

                    // If they confirm, we're done.
                    if (cardIndex == "→")
                    {
                        Sample_Sensor.Instance.cube?.PlayPresetSound(9); // Navigation sound
                        actionTaken = true;
                        // chosenPokemon is already set, so the outer while loop will exit.
                    }
                    // --- NEW: If they reset, we clear the choice and loop again ---
                    else if (cardIndex == "↑")
                    {
                        Sample_Sensor.Instance.cube?.PlayPresetSound(9); // Navigation sound
                        chosenPokemon = null; // Clear the choice
                        actionTaken = true;
                        // The outer while loop will now repeat, asking them to choose again.
                    }
                }
                yield return null;
            }
        }
        // --- END OF NEW LOOP ---

        // 5. Start the battle with the confirmed Pokémon
        StartCoroutine(ResolveBattle(chosenPokemon));
    }

    private IEnumerator ResolveBattle(PokemonClassifier.TestPokemon playerPokemon)
    {
        yield return FadeCanvas(battleUIGroup, 1, 0, fadeDuration);
        battleUIGroup.gameObject.SetActive(false);

        yield return StartCoroutine(TypeText(battleInstructionText, $"{playerPokemon.name}, I choose you!"));
        predictionResultText.text = "";
        yield return new WaitForSeconds(1.5f);

        int battleScore = CalculateBattleOutcome(playerPokemon, currentWildPokemon);

        yield return StartCoroutine(TypeText(battleInstructionText, $"Your battle score is... {battleScore} points!"));
        yield return new WaitForSeconds(2f);

        bool playerWins = battleScore >= 3;

        ProvideFeedback(playerWins);

        if (playerWins)
        {
            yield return StartCoroutine(TypeText(battleInstructionText, "You need 3 points to win... You did it!\nYou won the battle!"));
            GameStateManager.Instance.AdvanceToNextBoss(); // Move to the next boss
        }
        else
        {
            yield return StartCoroutine(TypeText(battleInstructionText, $"You need 3 points to win... You only got {battleScore}.\nIt was a tough fight, but you lost..."));
            GameStateManager.Instance.AdvanceToNextBoss();
        }

        yield return new WaitForSeconds(3f);
        StartCoroutine(FinishPopup());
    }

    private int CalculateBattleOutcome(PokemonClassifier.TestPokemon playerPokemon, PokemonClassifier.TestPokemon enemyPokemon)
    {
        int totalScore = 0;

        // 1. Type Advantage Logic
        if (playerPokemon.correctType == PokemonClassifier.PokemonType.Dragon || enemyPokemon.correctType == PokemonClassifier.PokemonType.Dragon)
        {
            totalScore += 1; // Dragon is neutral
        }
        else if ((playerPokemon.correctType == PokemonClassifier.PokemonType.Fire && enemyPokemon.correctType == PokemonClassifier.PokemonType.Grass) ||
                 (playerPokemon.correctType == PokemonClassifier.PokemonType.Water && enemyPokemon.correctType == PokemonClassifier.PokemonType.Fire) ||
                 (playerPokemon.correctType == PokemonClassifier.PokemonType.Grass && enemyPokemon.correctType == PokemonClassifier.PokemonType.Water))
        {
            totalScore += 2; // Super effective
        }
        else if (playerPokemon.correctType == enemyPokemon.correctType)
        {
            totalScore += 1; // Same type
        }
        // All other matchups are 0 points (not very effective)

        // 2. Stat Comparison Logic
        if (playerPokemon.attack >= enemyPokemon.attack) totalScore += 1;
        if (playerPokemon.defense >= enemyPokemon.defense) totalScore += 1;
        if (playerPokemon.speed >= enemyPokemon.speed) totalScore += 1;

        return totalScore;
    }

    private IEnumerator AnimateConfidenceText(Dictionary<PokemonClassifier.PokemonType, float> scores)
    {
        var cube = Sample_Sensor.Instance?.cube;
        if (cube != null && cube.isConnected) cube.PlaySound(1, new Cube.SoundOperation[] { new Cube.SoundOperation(2000, 80, 50) });

        predictionResultText.text = "Confidence:\n";
        string fireText = $"<color=red>Fire: 0%</color>  ";
        string waterText = $"<white>Water: 0%</color>\n";
        string grassText = $"<color=green>Grass: 0%</color>  ";
        string dragonText = $"<color=orange>Dragon: 0%</color>";
        predictionResultText.text += fireText + waterText + grassText + dragonText;

        float timer = 0f;
        float animDuration = 2.5f;
        while (timer < animDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / animDuration;

            fireText = $"<color=red>Fire: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Fire], progress):F0}%</color>  ";
            waterText = $"<color=white>Water: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Water], progress):F0}%</color>\n";
            grassText = $"<color=green>Grass: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Grass], progress):F0}%</color>  ";
            dragonText = $"<color=orange>Dragon: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Dragon], progress):F0}%</color>";
            predictionResultText.text = "Confidence:\n" + fireText + waterText + grassText + dragonText;

            yield return null;
        }
        yield return new WaitForSeconds(1.5f);
    }


}
