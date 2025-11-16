using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Added for counting wins easily
using System.Text; // Added for building the final summary text
using toio.Samples.Sample_Sensor;
using toio.Simulator;
using UnityEngine;
using UnityEngine.UI;
using toio;
using Unity.VisualScripting;


public class Level5LabZone : MonoBehaviour
{
    public static Level5LabZone Instance;

    [Header("InGame UI")]
    public CanvasGroup inGameUIGroup;

    [Header("LabZone UI")]
    public GameObject LabUI;
    public CanvasGroup instructionsGroup;

    //new boss battle
    [Header("Boss Into UI")]
    public CanvasGroup battleIntro;
    public Text battleIntroText;
    public CanvasGroup predictionGroup;
    public Sprite[] guardianSprites; // Assign 3 sprites in the Inspector: Fire, Water, Grass
    public Image guardianImage;
    public Sprite questionCard;
    public Text predictionInstructionText;
    public Text predictionResultText_Rule;
    public Text predictionResultText_Data;
    
    [Header("Boss Battle UI")]
    public CanvasGroup battleChoiceGroup;
    public Image playerPokemonImage;
    public Text  playerPokemonName;
    public Image guardianBossImage;
    public Text guardianText;
    public Text battleInstructionText;

    public PokemonClassifier pokemonClassifier;
    private PokemonClassifier.TestPokemon _playerBattleChoice;
    private (PokemonClassifier.PokemonType? predictedType, bool isRuleBased) _trustChoiceResult;
    private PokemonClassifier.BossPokemon _currentGuardian;
    private int _currentGuardianIndex = -1;

    private List<bool> _battleOutcomes = new List<bool>(); // To record the result of each battle

    [Header("Ending UI")]
    public CanvasGroup Ending;
    public Image endingImage;
    public Sprite wonSprite;
    public Sprite defeatSprite;
    public Text endingText;
    
    [Header("Mode Settings")]
    public bool PhysicalButton = true;

    [Header("Other")]
    public float typingSpeed = 0.04f;
    private bool win = false;
    private Action labSequenceCompleteCallback;
    private bool _labCompleted = false;
    private Coroutine physicalListener;
    private uint lastCardId = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

    }
    

    public IEnumerator StartLabZoneSequence(Action onComplete = null)
    {
        labSequenceCompleteCallback = onComplete;
        _labCompleted = false;
        LabUI.SetActive(true);

        instructionsGroup.alpha = 0;
        instructionsGroup.gameObject.SetActive(true);
        yield return FadeCanvas(instructionsGroup, 0, 1, 1f);
        
        // 3. Wait for 10 seconds while the instruction image is shown.
        Debug.Log("Showing Lab 5 instructions for 10 seconds...");
        yield return new WaitForSeconds(10f);

        // 4. Fade out the instruction image.
        yield return FadeCanvas(instructionsGroup, 1, 0, 1f);
        instructionsGroup.gameObject.SetActive(false);

        // 5. Automatically start the boss battle sequence.
       yield return StartCoroutine(RunThreeBattleSequence());
        
        // The boss battle will handle the rest of the flow.
        yield return new WaitUntil(() => _labCompleted);


    }

    

 
    
    // --- UI Helpers ---
  
    private void OnLabComplete()
    {
        predictionGroup.gameObject.SetActive(false);
        battleChoiceGroup.gameObject.SetActive(false);
        OnEnding();
    }

    private void OnEnding()
    {
        Ending.gameObject.SetActive(true);
        
        // --- CHANGE: Build a summary text of all 3 battles ---
        StringBuilder summary = new StringBuilder("Battle Results:\n\n");
        for (int i = 0; i < _battleOutcomes.Count; i++)
        {
            string result = _battleOutcomes[i] ? "Victory!" : "Defeat";
            summary.AppendLine($"Guardian {i + 1}: {result}");
        }

        int winCount = _battleOutcomes.Count(win => win);
        summary.AppendLine($"\nYou were victorious in {winCount} out of 3 battles.");
        summary.AppendLine("\nGreat job, Trainer!");
        
        // --- CHANGE: The final image is based on overall performance (e.g., winning at least 2) ---
        endingImage.sprite = (winCount >= 2) ? wonSprite : defeatSprite;
        endingText.text = summary.ToString();
        
        StartCoroutine(TriggerCompleteAfterDelay(8f)); // Increased delay to read the summary
    }

    private IEnumerator TriggerCompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Ending.gameObject.SetActive(false);
        LabUI.SetActive(false);
        _labCompleted = true;
        labSequenceCompleteCallback?.Invoke();
    }
    
    #region Boss Battle Sequence

    public IEnumerator RunThreeBattleSequence()
    {        
        _battleOutcomes.Clear(); // Reset results at the start
        
        battleIntroText.text = "";
        yield return FadeCanvas(battleIntro, 1, 0, 0.5f);
        battleIntro.gameObject.SetActive(false);
        
        yield return TypeText(battleIntroText, "The final challenge is here! Three Guardians await...");
        yield return new WaitForSeconds(3f);

        // --- CHANGE: Loop through all three battles ---
        for (int i = 0; i < 3; i++)
        {
            // Run a single, complete battle sequence for the guardian at index 'i'
            yield return StartCoroutine(RunSingleGuardianBattle(i));

            // After the battle, if it's not the last one, move to the next
            if (i < 2)
            {
                yield return StartCoroutine(TypeText(battleInstructionText, "Prepare for the next Guardian..."));
                
                // --- CHANGE: Move the Toio forward to the next boss location ---
                Sample_Sensor.Instance.CubeMoveByRoll(2); // Moves forward "2 steps"
                yield return new WaitForSeconds(3f); // Wait for movement and pause
            }
        }
        
        // After the loop finishes, all three battles are done.
        yield return StartCoroutine(TypeText(battleInstructionText, "You have faced all the Guardians! Let's see your results."));
        yield return new WaitForSeconds(3f);
        OnLabComplete(); // Trigger the ending summary
    }

    /// <summary>
    /// REFACTORED: This function handles a single, complete battle from start to finish.
    /// It's called three times by the main sequence loop.
    /// </summary>
    /// <param name="bossIndex">The index of the boss to fight (0=Fire, 1=Water, 2=Grass).</param>
    private IEnumerator RunSingleGuardianBattle(int bossIndex)
    {
        // --- Setup UI for the new battle ---
        guardianImage.sprite = questionCard;
        guardianBossImage.sprite = questionCard;
        guardianText.text = "";
        predictionGroup.gameObject.SetActive(true);
        yield return FadeCanvas(predictionGroup, 0, 1, 1f);

        var guardian = pokemonClassifier.GetBossPokemon(bossIndex);
        _currentGuardianIndex = bossIndex;
        _currentGuardian = guardian;
        
        // --- 1. AI Prediction Phase ---
        yield return StartCoroutine(TypeText(predictionInstructionText, $"The Guardian, {guardian.name}, appears!\nYour AIs will predict its type..."));
        yield return new WaitForSeconds(1f);

        var ruleModel = GameStateManager.Instance.method2_rules;
        var dataModel = GameStateManager.Instance.method4_model;

        // Default models for testing if none are loaded
        if (ruleModel == null) { /* ... default rule model logic ... */ }
        if (dataModel == null) { /* ... default data model logic ... */ }

        var tempBoss = new PokemonClassifier.TestPokemon(guardian.name, guardian.correctType, guardian.hasWings, guardian.speed, guardian.attack, guardian.defense, guardian.habitatAltitude, guardian.habitatTemperature);
        var ruleResult = pokemonClassifier.TestSinglePokemonMethod2(ruleModel, tempBoss);
        var dataResult = pokemonClassifier.TestMethod3OnSinglePokemon(dataModel, tempBoss);

        predictionResultText_Rule.text = "Rule-Based AI Scores:\n...";
        predictionResultText_Data.text = "Data-Based AI Confidence:\n...";
        yield return new WaitForSeconds(1f);

        string ruleScores = $"<color=red>F:{ruleResult.scores[PokemonClassifier.PokemonType.Fire]}</color> " +
                            $"<color=white>W:{ruleResult.scores[PokemonClassifier.PokemonType.Water]}</color> " +
                            $"<color=green>G:{ruleResult.scores[PokemonClassifier.PokemonType.Grass]}</color> " +
                            $"<color=orange>D:{ruleResult.scores[PokemonClassifier.PokemonType.Dragon]}</color>";
        predictionResultText_Rule.text = $"Rule-Based AI Scores:\n{ruleScores}";
        yield return AnimateConfidenceText(predictionResultText_Data, dataResult.confidenceScores);

        // --- 2. Player Trust Choice ---
        yield return StartCoroutine(TypeText(predictionInstructionText, "Which AI do you trust? Scan the Rule (?) card or the Data (!) card."));
        yield return StartCoroutine(WaitForTrustChoice(ruleResult, dataResult));
        var trustedResult = _trustChoiceResult;

        var predictedType = trustedResult.predictedType;
        string predictionString = predictedType.HasValue ? predictedType.ToString() : "Not sure";
        string battleHint = GetBattleHint(predictedType);
        yield return StartCoroutine(TypeText(predictionInstructionText, $"You trusted the {(trustedResult.isRuleBased ? "Rule-Based" : "Data-Driven")} AI!\nIt predicts {predictionString}. {battleHint}"));
        yield return new WaitForSeconds(2f);

        // --- 3. Player Pokémon Choice ---
        _playerBattleChoice = null;
        yield return StartCoroutine(WaitForPlayerBattleChoice());
        var playerPokemon = _playerBattleChoice;

        // --- 4. Battle Resolution and Recording ---
        int score = pokemonClassifier.CalculateBattleOutcome(playerPokemon, guardian);
        bool playerWins = score >= 3;
        
        // --- CHANGE: Add the result to our list for the final summary ---
        _battleOutcomes.Add(playerWins);
        
        ProvideFeedback(playerWins); // Physical Toio feedback
        yield return StartCoroutine(SetGuardianVisuals(bossIndex, guardian.name));

        // --- CHANGE: Show round-specific feedback text instead of ending the game ---
        if (playerWins)
        {
            yield return StartCoroutine(TypeText(battleInstructionText, "A decisive victory! The Guardian is defeated."));
        }
        else
        {
            yield return StartCoroutine(TypeText(battleInstructionText, "A valiant effort, but you were defeated in this round."));
        }
        yield return new WaitForSeconds(4f);

        // --- 5. Cleanup UI for next round ---
        yield return FadeCanvas(predictionGroup, 1, 0, 0.5f);
        predictionGroup.gameObject.SetActive(false);
        yield return FadeCanvas(battleChoiceGroup, 1, 0, 0.5f);
        battleChoiceGroup.gameObject.SetActive(false);
    }

    private IEnumerator SetGuardianVisuals(int bossIndex, string bossName, float fadeDur = 0.4f)
    {
        Sprite s = (guardianSprites != null && bossIndex >= 0 && bossIndex < guardianSprites.Length) ? guardianSprites[bossIndex] : null;
        
        guardianBossImage.sprite = s;
        guardianBossImage.preserveAspect = true;
        
        var cg = guardianBossImage.GetComponent<CanvasGroup>();
        if (cg == null) cg = guardianBossImage.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        float t = 0f;
        while (t < fadeDur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, t / fadeDur);
            yield return null;
        }
        cg.alpha = 1f;
        guardianText.text = _currentGuardian != null ? _currentGuardian.name : "";
        if (s == null)
            Debug.LogWarning($"[Level5LabZone] No guardian sprite assigned for index {bossIndex} ({bossName}).");
    }

    private IEnumerator WaitForPlayerBattleChoice()
    {
        if (predictionGroup != null && predictionGroup.gameObject.activeSelf)
        {
            yield return FadeCanvas(predictionGroup, 1f, 0f, 0.5f);
            predictionGroup.gameObject.SetActive(false);
        }
        
        if (playerPokemonImage != null)
        {
            playerPokemonImage.sprite = null;
            playerPokemonImage.enabled = false;
        }

        if (playerPokemonName != null)
            playerPokemonName.text = "";

        //UpdateGuardianUIForBattleChoice();
        battleChoiceGroup.gameObject.SetActive(true);
        yield return FadeCanvas(battleChoiceGroup, 0, 1, 1f);
        yield return StartCoroutine(TypeText(battleInstructionText, "Scan a Pokémon card from your team to fight!"));

        PokemonClassifier.TestPokemon chosenPokemon = null;
        uint lastReadId = 0;
        while (chosenPokemon == null)
        {
            uint currentId = Sample_Sensor.Instance.ReadCard();
            if (currentId != 0 && currentId != lastReadId)
            {
                lastReadId = currentId;
                string cardIndex = StandardID.GetCardNameByID(currentId);
                if (IsPokemonCard(cardIndex))
                {
                    chosenPokemon = pokemonClassifier.GetTestPokemonByCardIndex(cardIndex);
                    if (chosenPokemon != null)
                    {
                        if (playerPokemonName != null)
                            playerPokemonName.text = chosenPokemon.name;

                        Sprite pokemonSprite = CardDataManager.Instance.GetSprite(cardIndex);

                        if (playerPokemonImage != null)
                        {
                            playerPokemonImage.sprite = pokemonSprite;
                            playerPokemonImage.enabled = (pokemonSprite != null);
                        }

                        Sample_Sensor.Instance.cube?.PlayPresetSound(8);
                        break;
                    }
                }
                
            }
            yield return null;
        }
        _playerBattleChoice = chosenPokemon;
        
    }

    private bool IsPokemonCard(string cardIndex)
    {
        return cardIndex != null && cardIndex.Length == 1 && cardIndex[0] >= 'L' && cardIndex[0] <= 'Z';
    }
    

    #region Helper Methods

    private void UpdateGuardianUIForBattleChoice()
    {
        if (guardianText != null)
            guardianText.text = _currentGuardian != null ? _currentGuardian.name : "";

        if (guardianBossImage != null)
        {
            Sprite s = null;
            if (guardianSprites != null && _currentGuardianIndex >= 0 && _currentGuardianIndex < guardianSprites.Length)
            {
                s = guardianSprites[_currentGuardianIndex];
            }

            guardianBossImage.sprite = s;
            guardianBossImage.enabled = (s != null); 
        }
    }

    
    private IEnumerator WaitForTrustChoice(PokemonClassifier.Method2SingleResult ruleResult, PokemonClassifier.Method3SingleResult dataResult)
    {
        string choice = "";
        uint lastReadId = 0;

        while (choice == "")
        {
            uint currentId = Sample_Sensor.Instance.ReadCard();
            if (currentId != 0 && currentId != lastReadId)
            {
                lastReadId = currentId;
                string cardIndex = StandardID.GetCardNameByID(currentId);

                if (cardIndex == "?") { choice = "rule"; }
                else if (cardIndex == "!") { choice = "data"; }
            }
            yield return null;
        }

        Sample_Sensor.Instance.cube?.PlayPresetSound(9);

        // Instead of trying to return a value, store the result in our mailbox variable.
        if (choice == "rule")
        {
            _trustChoiceResult = (ruleResult.predictedType, isRuleBased: true);
        }
        else // choice == "data"
        {
            _trustChoiceResult = (dataResult.predictedType, isRuleBased: false);
        }
    }

    private string GetBattleHint(PokemonClassifier.PokemonType? predictedType)
    {
        if (!predictedType.HasValue) return "Try your best!";
        switch (predictedType.Value)
        {
            case PokemonClassifier.PokemonType.Fire: return "I suggest using a Water to fight!";
            case PokemonClassifier.PokemonType.Water: return "I suggest using a Grass to fight!";
            case PokemonClassifier.PokemonType.Grass: return "I suggest using a Fire to fight!";
            case PokemonClassifier.PokemonType.Dragon: return "A Dragon is tough... I suggest using Dragon to fight!";
            default: return "Try your best!";
        }
    }

    private void ProvideFeedback(bool correct)
    {
        
        var sensorCube = Sample_Sensor.Instance?.cube;
        if (sensorCube == null || !sensorCube.isConnected) return;
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

   private IEnumerator AnimateConfidenceText(Text uiText, Dictionary<PokemonClassifier.PokemonType, float> scores)
   {
        // This logic is learned directly from your working EncounterPopup.cs script.

        var cube = Sample_Sensor.Instance?.cube;
        if (cube != null && cube.isConnected) cube.PlaySound(1, new Cube.SoundOperation[] { new Cube.SoundOperation(2000, 80, 50) });

        // Use a more specific title for the dual-display in Zone 5
        uiText.text = "Data-Based AI Confidence:\n";
        
        // Set up the initial text strings at 0%
        string fireText = $"<color=red>F: 0%</color> ";
        string waterText = $"<color=white>W: 0%</color> ";
        string grassText = $"<color=green>G: 0%</color> ";
        string dragonText = $"<color=orange>D: 0%</color>";
        uiText.text += fireText + waterText + grassText + dragonText;
        
        // Animate the percentages increasing over time
        float timer = 0f;
        float animDuration = 2.5f;
        while (timer < animDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / animDuration;
            
            // Calculate the current percentage for each type
            fireText = $"<color=red>F: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Fire], progress):F0}%</color> ";
            waterText = $"<color=white>W: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Water], progress):F0}%</color> ";
            grassText = $"<color=green>G: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Grass], progress):F0}%</color> ";
            dragonText = $"<color=orange>D: {Mathf.Lerp(0, scores[PokemonClassifier.PokemonType.Dragon], progress):F0}%</color>";
            
            // Rebuild the full string and update the UI on every frame
            uiText.text = "Data-Based AI Confidence:\n" + fireText + waterText + grassText + dragonText;

            yield return null;
        }
        yield return new WaitForSeconds(1.5f);
    }



    #endregion Boss Battle Sequence
    
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
        uiText.text = "";
        foreach (char c in message)
        {
            uiText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void FadeInGameUI(float duration = 1f)
    {
        inGameUIGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvas(inGameUIGroup, 0f, 1f, duration));
    }
    
    #endregion Helper Methods
    

}
