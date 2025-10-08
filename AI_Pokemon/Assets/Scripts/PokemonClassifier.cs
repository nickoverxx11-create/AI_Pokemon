using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using toio;
using toio.Samples.Sample_Sensor;
using toio.Simulator;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LabzoneCardInfo
{
    public string index;    
    public string name;    
    public Sprite sprite;  
    public CardCategory category; 
}

// Integrated Pokemon Classification System
public class PokemonClassifier : MonoBehaviour
{
    public enum CardID
    {
        LowTemperature = 0,
        HighAttack = 1,
        LowAttack = 2,
        HighDefense = 3,
        LowDefense = 4,
        HighSpeed = 5,
        LowSpeed = 6,
        HasWings = 7,
        NoWings = 8,
        HighTemperature = 9,
        HighAltitude = 10, // A
        LowAltitude = 11   // B
    }

    public enum PokemonType
    {
        Fire,
        Water,
        Grass,
        Dragon
    }

    public enum StrictnessMode
    {
        Perfect,    // Must match ALL rules
        Almost      // Can miss ONE rule
    }

    [System.Serializable]
    public class Pokemon
    {
        public string name;
        public PokemonType actualType;
        public int hasWings;      // 0 or 1
        public int speed;         // 1-9
        public int attack;        // 1-9
        public int defense;       // 1-9
        public int habitatAltitude; // 1-9
        public int habitatTemperature; // 1-9
        public Sprite pokemonSprite; // For UI display

        public Pokemon(string name, PokemonType type, int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature)
        {
            this.name = name;
            this.actualType = type;
            this.hasWings = hasWings;
            this.speed = speed;
            this.attack = attack;
            this.defense = defense;
            this.habitatAltitude = habitatAltitude;
            this.habitatTemperature = habitatTemperature;
        }

        public List<CardID> GetFeatures()
        {
            List<CardID> features = new List<CardID>();

            if (habitatTemperature >= 5) features.Add(CardID.HighTemperature);
            else features.Add(CardID.LowTemperature);

            if (attack >= 5) features.Add(CardID.HighAttack);
            else features.Add(CardID.LowAttack);

            if (defense >= 5) features.Add(CardID.HighDefense);
            else features.Add(CardID.LowDefense);

            if (speed >= 5) features.Add(CardID.HighSpeed);
            else features.Add(CardID.LowSpeed);

            if (hasWings == 1) features.Add(CardID.HasWings);
            else features.Add(CardID.NoWings);

            if (habitatAltitude >= 5) features.Add(CardID.HighAltitude);
            else features.Add(CardID.LowAltitude);

            return features;
        }
    }

    [System.Serializable]

    #region Method 3 - Machine Learning Structures

    // Represents a single Pokémon from the new, larger training dataset.
    public class TrainingPokemon
    {
        public string name;
        public PokemonType correctType;
        public int hasWings;      // 0 or 1
        public int speed;         // 1-9
        public int attack;        // 1-9
        public int defense;       // 1-9
        public int habitatAltitude; // 1-9
        public int habitatTemperature; // 1-9

        public TrainingPokemon(string name, PokemonType type, int wings, int spd, int atk, int def, int alt, int temp)
        {
            this.name = name; this.correctType = type; this.hasWings = wings; this.speed = spd;
            this.attack = atk; this.defense = def; this.habitatAltitude = alt; this.habitatTemperature = temp;
        }

        // Helper to normalize features to a -1 to 1 range for the model.
        public Dictionary<string, float> GetNormalizedFeatures()
        {
            return new Dictionary<string, float>
            {
                { "HasWings", hasWings * 2 - 1 }, // Maps 0, 1 to -1, 1
                { "Speed", (speed - 5f) / 4f }, // Maps 1-9 to -1 to 1
                { "Attack", (attack - 5f) / 4f },
                { "Defense", (defense - 5f) / 4f },
                { "HabitatAltitude", (habitatAltitude - 5f) / 4f },
                { "HabitatTemperature", (habitatTemperature - 5f) / 4f }
            };
        }
    }

    // Stores the weights for a single feature set (for one PokemonType).
    [System.Serializable]
    public class FeatureWeights
    {
        public float HasWings = 0f, Speed = 0f, Attack = 0f, Defense = 0f, HabitatAltitude = 0f, HabitatTemperature = 0f;
    }

    // Represents the entire ML model, containing weights for all four types.
    [System.Serializable]
    public class ModelWeights
    {
        public Dictionary<PokemonType, FeatureWeights> weightsByType = new Dictionary<PokemonType, FeatureWeights>();

        public ModelWeights()
        {
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                weightsByType[type] = new FeatureWeights();
            }
        }
    }

    // A result container for testing a single Pokémon with Method 3.
    public struct Method3SingleResult
    {
        public PokemonType? predictedType;
        public PokemonType actualType;
        public Dictionary<PokemonType, float> confidenceScores; // 0% to 100%
        public bool IsCorrect => predictedType.HasValue && predictedType.Value == actualType;
    }

    #endregion

    #region Method 3 - Member Variables
    private List<TrainingPokemon> method3TrainingPool;
    private Dictionary<string, List<TrainingPokemon>> selectableDatasets;
    #endregion


    public class TestPokemon
    {
        public string name;
        public PokemonType correctType;
        public int hasWings;
        public int speed;
        public int attack;
        public int defense;
        public int habitatAltitude;
        public int habitatTemperature;
        public Sprite pokemonSprite;

        public TestPokemon(string name, PokemonType type, int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature)
        {
            this.name = name;
            this.correctType = type;
            this.hasWings = hasWings;
            this.speed = speed;
            this.attack = attack;
            this.defense = defense;
            this.habitatAltitude = habitatAltitude;
            this.habitatTemperature = habitatTemperature;
        }

        public Pokemon ToPokemon()
        {
            return new Pokemon(name, correctType, hasWings, speed, attack, defense, habitatAltitude, habitatTemperature);
        }

        public List<CardID> GetFeatures()
        {
            List<CardID> features = new List<CardID>();

            if (habitatTemperature >= 5) features.Add(CardID.HighTemperature);
            else features.Add(CardID.LowTemperature);

            if (attack >= 5) features.Add(CardID.HighAttack);
            else features.Add(CardID.LowAttack);

            if (defense >= 5) features.Add(CardID.HighDefense);
            else features.Add(CardID.LowDefense);

            if (speed >= 5) features.Add(CardID.HighSpeed);
            else features.Add(CardID.LowSpeed);

            if (hasWings == 1) features.Add(CardID.HasWings);
            else features.Add(CardID.NoWings);

            if (habitatAltitude >= 5) features.Add(CardID.HighAltitude);
            else features.Add(CardID.LowAltitude);

            return features;
        }
    }

    [System.Serializable]
    public class Rule
    {
        public CardID cardID;
        public int priority;

        public Rule(CardID cardID, int priority)
        {
            this.cardID = cardID;
            this.priority = priority;
        }
    }

    // Classification result containers
    [System.Serializable]
    public class Method1Results
    {
        public List<Pokemon> fireBoxCorrect = new List<Pokemon>();    // Green - Fire predicted correctly
        public List<Pokemon> fireBoxWrong = new List<Pokemon>();      // Red - Non-Fire predicted as Fire
        public List<Pokemon> notFireBoxCorrect = new List<Pokemon>(); // Blue - Non-Fire predicted correctly
        public List<Pokemon> notFireBoxWrong = new List<Pokemon>();   // Yellow - Fire predicted as Non-Fire
    }

    // --- NECESSARY ADDITION: Result container for the new single test function ---
    public struct Method2SingleResult
    {
        public string pokemonName;
        public PokemonType? predictedType;
        public PokemonType actualType;
        public Dictionary<PokemonType, int> scores;
        public bool IsCorrect => predictedType.HasValue && predictedType.Value == actualType;
    }

    [System.Serializable]
    public class Method2Results
    {
        public Dictionary<PokemonType, List<Pokemon>> correctPredictions = new Dictionary<PokemonType, List<Pokemon>>();
        public Dictionary<PokemonType, List<Pokemon>> wrongPredictions = new Dictionary<PokemonType, List<Pokemon>>();
        public List<Pokemon> multipleMatches = new List<Pokemon>();
        public List<Pokemon> noMatches = new List<Pokemon>();

        public int totalCorrect;
        public int totalWrong;
        public int multipleCount;
        public int noMatchCount;

        // NEW: Added fields for UI display
        public float overallAccuracy;
        public Dictionary<PokemonType, float> typeAccuracies = new Dictionary<PokemonType, float>();
        public Dictionary<PokemonType, int> typeCorrectCounts = new Dictionary<PokemonType, int>();
        public Dictionary<PokemonType, int> typeIncorrectCounts = new Dictionary<PokemonType, int>();
    }

    private List<Pokemon> pokemonDataset;
    private Method1Results method1Results;
    private Method2Results method2Results;

    public void Initialize()
    {
        CreatePokemonDataset();
        InitializeMethod2Results();

        InitializeMethod3();
    }

    private void InitializeMethod2Results()
    {
        method2Results = new Method2Results();
        foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
        {
            method2Results.correctPredictions[type] = new List<Pokemon>();
            method2Results.wrongPredictions[type] = new List<Pokemon>();
            method2Results.typeAccuracies[type] = 0f;
            method2Results.typeCorrectCounts[type] = 0;
            method2Results.typeIncorrectCounts[type] = 0;
        }
    }

    // Convert scanned card indices to CardID enum
    public List<CardID> ConvertScannedCardsToRules(List<string> scannedCardIndices)
    {
        List<CardID> rules = new List<CardID>();
        foreach (string cardIndex in scannedCardIndices)
        {
            CardID? mappedCardID = MapStringToCardID(cardIndex);
            if (mappedCardID.HasValue)
            {
                rules.Add(mappedCardID.Value);
            }
        }
        return rules;
    }

    private CardID? MapStringToCardID(string cardIndex)
    {
        switch (cardIndex)
        {
            case "0": return CardID.LowTemperature;     // LOW TEMPERATURE
            case "1": return CardID.HighAttack;         // HIGH ATTACK
            case "2": return CardID.LowAttack;          // LOW ATTACK
            case "3": return CardID.HighDefense;        // HIGH DEFENSE
            case "4": return CardID.LowDefense;         // LOW DEFENSE
            case "5": return CardID.HighSpeed;          // HIGH SPEED
            case "6": return CardID.LowSpeed;           // LOW SPEED
            case "7": return CardID.HasWings;           // HAS WINGS
            case "8": return CardID.NoWings;            // NO WINGS
            case "9": return CardID.HighTemperature;    // HIGH TEMPERATURE
            case "A": return CardID.HighAltitude;       // HIGH ALTITUDE (CardID 10)
            case "B": return CardID.LowAltitude;        // LOW ALTITUDE (CardID 11)
            default: return null;
        }
    }

    public TestPokemon GetTestPokemonByCardIndex(string cardIndex)
    {
        switch (cardIndex)
        {
            case "L": return new TestPokemon("Ponyta", PokemonType.Fire, 0, 8, 7, 2, 2, 8);
            case "M": return new TestPokemon("Ninetales", PokemonType.Fire, 0, 7, 9, 6, 3, 9);
            case "N": return new TestPokemon("Charizard", PokemonType.Fire, 1, 4, 8, 5, 8, 9);
            case "O": return new TestPokemon("Growlithe", PokemonType.Fire, 0, 3, 7, 1, 1, 6);
            case "P": return new TestPokemon("Slowpoke", PokemonType.Water, 0, 1, 2, 7, 1, 3);
            case "Q": return new TestPokemon("Wailmer", PokemonType.Water, 0, 2, 3, 9, 0, 0);
            case "R": return new TestPokemon("Gyarados", PokemonType.Water, 1, 3, 9, 7, 0, 0);
            case "S": return new TestPokemon("Psyduck", PokemonType.Water, 0, 3, 1, 6, 0, 1);
            case "T": return new TestPokemon("Petilil", PokemonType.Grass, 0, 3, 2, 6, 4, 3);
            case "U": return new TestPokemon("Deerling", PokemonType.Grass, 0, 6, 3, 5, 6, 3);
            case "V": return new TestPokemon("Bayleef", PokemonType.Grass, 0, 2, 4, 9, 7, 2);
            case "W": return new TestPokemon("Leafeon", PokemonType.Grass, 0, 6, 5, 6, 6, 4);
            case "X": return new TestPokemon("Rayquaza", PokemonType.Dragon, 1, 7, 9, 4, 9, 2);
            case "Y": return new TestPokemon("Dragonite", PokemonType.Dragon, 1, 7, 9, 7, 9, 1);
            case "Z": return new TestPokemon("Dialga", PokemonType.Dragon, 0, 6, 9, 9, 8, 3);
            default: return null;
        }
    }
    // Simplified single Pokemon testing Method 1
    public bool TestSinglePokemon(List<string> scannedCardIndices, string pokemonCardIndex, StrictnessMode strictness)
    {
        // Get the test Pokemon from the card index
        TestPokemon testPokemon = GetTestPokemonByCardIndex(pokemonCardIndex);
        if (testPokemon == null)
        {
            Debug.LogError($"No test Pokemon found for card index: {pokemonCardIndex}");
            return false;
        }

        // Convert to Pokemon object for classification
        Pokemon pokemon = testPokemon.ToPokemon();

        // Convert scanned rules
        List<CardID> fireRules = ConvertScannedCardsToRules(scannedCardIndices);

        // Predict if it's fire type
        bool predictedFire = PredictIsFire(pokemon, fireRules, strictness);
        bool actuallyFire = testPokemon.correctType == PokemonType.Fire;
        bool correct = predictedFire == actuallyFire;

        Debug.Log($"Testing {testPokemon.name}: Actual={actuallyFire}, Predicted={predictedFire}, Correct={correct}");

        return correct; // Returns true if actual type == predicted type
    }

    // Method 1: Binary Fire Classification
    public Method1Results ClassifyMethod1(List<string> scannedCardIndices, StrictnessMode strictness)
    {
        List<CardID> fireRules = ConvertScannedCardsToRules(scannedCardIndices);
        method1Results = new Method1Results();

        List<Pokemon> fireBox = new List<Pokemon>();
        List<Pokemon> notFireBox = new List<Pokemon>();

        foreach (Pokemon pokemon in pokemonDataset)
        {
            bool predictedFire = PredictIsFire(pokemon, fireRules, strictness);
            if (predictedFire)
                fireBox.Add(pokemon);
            else
                notFireBox.Add(pokemon);
        }

        foreach (Pokemon pokemon in fireBox)
        {
            if (pokemon.actualType == PokemonType.Fire)
                method1Results.fireBoxCorrect.Add(pokemon);
            else
                method1Results.fireBoxWrong.Add(pokemon);
        }

        foreach (Pokemon pokemon in notFireBox)
        {
            if (pokemon.actualType == PokemonType.Fire)
                method1Results.notFireBoxWrong.Add(pokemon);
            else
                method1Results.notFireBoxCorrect.Add(pokemon);
        }

        Debug.Log($"Fire Box: {method1Results.fireBoxCorrect.Count} real Fire, {method1Results.fireBoxWrong.Count} intruders");
        Debug.Log($"Not Fire Box: {method1Results.notFireBoxCorrect.Count} correct non-Fire, {method1Results.notFireBoxWrong.Count} missed Fire");

        return method1Results;
    }

    public bool PredictIsFire(Pokemon pokemon, List<CardID> fireRules, StrictnessMode strictness)
    {
        int matchedRules = 0;
        List<CardID> pokemonFeatures = pokemon.GetFeatures();

        foreach (CardID rule in fireRules)
        {
            if (pokemonFeatures.Contains(rule))
                matchedRules++;
        }

        switch (strictness)
        {
            case StrictnessMode.Perfect:
                return matchedRules == fireRules.Count;
            case StrictnessMode.Almost:
                return matchedRules >= Math.Max(1, fireRules.Count - 1);
            default:
                return false;
        }
    }

    // MODIFIED: The signature and logic of this method have changed completely.
    public Method2SingleResult TestSinglePokemonMethod2(Dictionary<PokemonType, List<string>> allTypeRules, string pokemonCardIndex)
    {
        TestPokemon testPokemon = GetTestPokemonByCardIndex(pokemonCardIndex);
        if (testPokemon == null)
        {
            Debug.LogError($"No test Pokemon found for card index: {pokemonCardIndex}");
            return new Method2SingleResult { scores = new Dictionary<PokemonType, int>() };
        }

        Pokemon pokemon = testPokemon.ToPokemon();

        var scores = GetScoresForPokemon(pokemon, allTypeRules);
        var predictedType = GetPredictionFromScores(scores);

        var result = new Method2SingleResult
        {
            pokemonName = pokemon.name,
            predictedType = predictedType,
            actualType = pokemon.actualType,
            scores = scores
        };

        // Debug logging remains useful
        string scoreString = string.Join(", ", scores.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        Debug.Log($"--- Testing {result.pokemonName} (Method 2) ---");
        Debug.Log($"Scores: [{scoreString}]");
        Debug.Log($"Actual Type: {result.actualType}, Predicted Type: {(predictedType.HasValue ? predictedType.ToString() : "None/Tie")}");
        Debug.Log($"Result: {(result.IsCorrect ? "CORRECT" : "INCORRECT")}");

        return result;
    }

    // Method 2: Multi-Type Classification
    // MODIFIED: The signature and logic of this method have changed completely.
    public Method2Results ClassifyMethod2(Dictionary<PokemonType, List<string>> allTypeRules)
    {
        InitializeMethod2Results();

        foreach (Pokemon pokemon in pokemonDataset)
        {
            var scores = GetScoresForPokemon(pokemon, allTypeRules);
            var predictedType = GetPredictionFromScores(scores);

            if (predictedType.HasValue)
            {
                if (pokemon.actualType == predictedType.Value)
                    method2Results.correctPredictions[predictedType.Value].Add(pokemon);
                else
                    method2Results.wrongPredictions[predictedType.Value].Add(pokemon);
            }
            else
            {
                if (scores.Values.Max() > 0) // It's a tie if the highest score isn't 0
                    method2Results.multipleMatches.Add(pokemon);
                else // Otherwise, it's a no match
                    method2Results.noMatches.Add(pokemon);
            }
        }

        CalculateMethod2Results(method2Results.correctPredictions, method2Results.wrongPredictions, method2Results.multipleMatches, method2Results.noMatches);
        return method2Results;
    }

    // MODIFIED: Updated to use wrongPredictions parameter
    private void CalculateMethod2Results(
        Dictionary<PokemonType, List<Pokemon>> correctPredictions,
        Dictionary<PokemonType, List<Pokemon>> wrongPredictions,
        List<Pokemon> multipleMatches,
        List<Pokemon> noMatches)
    {
        int totalCorrect = 0;
        int totalWrong = 0;

        foreach (var kvp in correctPredictions)
        {
            totalCorrect += kvp.Value.Count;
        }

        foreach (var kvp in wrongPredictions)
        {
            totalWrong += kvp.Value.Count;
        }

        method2Results.totalCorrect = totalCorrect;
        method2Results.totalWrong = totalWrong;
        method2Results.multipleCount = multipleMatches.Count;
        method2Results.noMatchCount = noMatches.Count;
    }

    // NEW: Calculate individual type accuracies
    public Dictionary<PokemonType, float> CalculateTypeAccuracies()
    {
        Dictionary<PokemonType, float> accuracies = new Dictionary<PokemonType, float>();

        foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
        {
            int correct = method2Results.correctPredictions[type].Count;
            int incorrect = method2Results.wrongPredictions[type].Count;
            int total = correct + incorrect;

            if (total > 0)
                accuracies[type] = (float)correct / total * 100f;
            else
                accuracies[type] = 0f;
        }

        return accuracies;
    }

    // NEW: Calculate overall accuracy
    public float CalculateOverallAccuracy()
    {
        int totalCorrect = method2Results.totalCorrect;
        int totalClassified = totalCorrect + method2Results.totalWrong + method2Results.multipleCount + method2Results.noMatchCount;

        if (totalClassified > 0)
            return (float)totalCorrect / totalClassified * 100f;
        else
            return 0f;
    }

    // NEW: Log Method 2 results for debugging
    public void LogMethod2Results()
    {
        Debug.Log($"=== METHOD 2 CLASSIFICATION RESULTS ===");
        Debug.Log($"Overall Accuracy: {method2Results.overallAccuracy:F1}% ({method2Results.totalCorrect}/{method2Results.totalCorrect + method2Results.totalWrong})");
        Debug.Log("");

        foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
        {
            int correct = method2Results.typeCorrectCounts[type];
            int incorrect = method2Results.typeIncorrectCounts[type];
            float accuracy = method2Results.typeAccuracies[type];

            Debug.Log($"{type} Type Results:");
            Debug.Log($"  Correct: {correct}");
            Debug.Log($"  Wrong: {incorrect}");
            Debug.Log($"  Accuracy: {accuracy:F1}%");
            Debug.Log("");
        }

        Debug.Log($"Multiple Matches: {method2Results.multipleCount}");
        Debug.Log($"No Matches: {method2Results.noMatchCount}");
    }

    // --- REWRITTEN: The core prediction logic for Method 2. ---
    private Dictionary<PokemonType, int> GetScoresForPokemon(Pokemon pokemon, Dictionary<PokemonType, List<string>> allTypeRules)
    {
        var scores = new Dictionary<PokemonType, int>();
        var pokemonFeatures = pokemon.GetFeatures();

        foreach (var kvp in allTypeRules)
        {
            var type = kvp.Key;
            var ruleList = kvp.Value;
            int scoreForThisType = 0;

            for (int i = 0; i < ruleList.Count; i++)
            {
                CardID? ruleCard = MapStringToCardID(ruleList[i]);
                if (ruleCard.HasValue && pokemonFeatures.Contains(ruleCard.Value))
                {
                    // Apply points based on order: 4 for the first card, 3 for the second, etc.
                     if (i == 0) scoreForThisType += 3;      // 1st card gets 3 points
                        else if (i == 1) scoreForThisType += 2; // 2nd card gets 2 points
                        else if (i == 2) scoreForThisType += 2; // 3rd card gets 2 points
                        else if (i == 3) scoreForThisType += 1; // 4th card gets 1 point
                }
            }
            scores[type] = scoreForThisType;
        }
        return scores;
    }

    private PokemonType? GetPredictionFromScores(Dictionary<PokemonType, int> scores)
    {
        int highestScore = 0;
        foreach (var score in scores.Values)
        {
            if (score > highestScore)
            {
                highestScore = score;
            }
        }

        // If the highest score is 0, it's a "No Match".
        if (highestScore == 0)
        {
            return null;
        }

        // Check for ties at the highest score.
        int countAtHighest = scores.Values.Count(s => s == highestScore);
        if (countAtHighest > 1)
        {
            return null; // It's a "Multiple Match".
        }

        // Find and return the single winner.
        return scores.First(kvp => kvp.Value == highestScore).Key;
    }


    private PokemonType? PredictType(Pokemon pokemon, Dictionary<PokemonType, List<Rule>> typeRules)
    {
        Dictionary<PokemonType, int> scores = new Dictionary<PokemonType, int>();
        List<CardID> pokemonFeatures = pokemon.GetFeatures();

        foreach (var kvp in typeRules)
        {
            int score = 0;
            foreach (Rule rule in kvp.Value)
            {
                if (pokemonFeatures.Contains(rule.cardID))
                {
                    switch (rule.priority)
                    {
                        case 1: score += 4; break;
                        case 2: score += 3; break;
                        case 3: score += 2; break;
                        case 4: score += 1; break;
                    }
                }
            }
            scores[kvp.Key] = score;
        }

        PokemonType? bestType = null;
        int highestScore = 0;
        bool hasTie = false;

        foreach (var kvp in scores)
        {
            if (kvp.Value > highestScore)
            {
                highestScore = kvp.Value;
                bestType = kvp.Key;
                hasTie = false;
            }
            else if (kvp.Value == highestScore)
            {
                hasTie = true;
            }
        }

        if (hasTie || highestScore == 0)
        {
            return null;
        }

        return bestType;
    }

    private bool HasMultipleHighScores(Pokemon pokemon, Dictionary<PokemonType, List<Rule>> typeRules)
    {
        Dictionary<PokemonType, int> scores = new Dictionary<PokemonType, int>();
        List<CardID> pokemonFeatures = pokemon.GetFeatures();

        foreach (var kvp in typeRules)
        {
            int score = 0;
            foreach (Rule rule in kvp.Value)
            {
                if (pokemonFeatures.Contains(rule.cardID))
                {
                    switch (rule.priority)
                    {
                        case 1: score += 4; break;
                        case 2: score += 3; break;
                        case 3: score += 2; break;
                        case 4: score += 1; break;
                    }
                }
            }
            scores[kvp.Key] = score;
        }

        if (scores.Count == 0) return false;

        // Find the highest score achieved.
        int highestScore = 0;

        foreach (int score in scores.Values)
        {
            if (score > highestScore)
            {
                highestScore = score;

            }

        }
        // If the best score was 0, it's a "No Match", not a tie.
        if (highestScore == 0)
        {
            return false;
        }

        // Count how many types achieved that highest score.
        int countAtHighest = 0;
        foreach (int score in scores.Values)
        {
            if (score == highestScore)
            {
                countAtHighest++;
            }
        }

        // If more than one type got the highest score, it's a "Multiple Match".
        return countAtHighest > 1;
    }

    private void CreatePokemonDataset()
    {
        pokemonDataset = new List<Pokemon>
        {
            // FIRE POKEMON (25 total) int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature
            new Pokemon("Firerabbit", PokemonType.Fire, 0, 6, 9, 3, 4, 8),
            new Pokemon("Flamecat", PokemonType.Fire, 0, 5, 9, 4, 3, 9),
            new Pokemon("Sunmoth", PokemonType.Fire, 1, 8, 9, 4, 7, 8),
            new Pokemon("Poisonfire", PokemonType.Fire, 0, 9, 8, 3, 2, 9),
            new Pokemon("Firebird", PokemonType.Fire, 0, 1, 8, 3, 9, 8),
            new Pokemon("Shellfire", PokemonType.Fire, 0, 3, 7, 4, 3, 9),
            new Pokemon("Firebug", PokemonType.Fire, 0, 6, 9, 3, 2, 8),
            new Pokemon("Rockfire", PokemonType.Fire, 0, 3, 8, 5, 2, 8),
            new Pokemon("Flamearmor", PokemonType.Fire, 0, 7, 9, 4, 4, 8),
            new Pokemon("Fireblade", PokemonType.Fire, 0, 8, 9, 4, 3, 7),
            new Pokemon("Flamemonkey", PokemonType.Fire, 0, 8, 9, 3, 5, 9),
            new Pokemon("Firelion", PokemonType.Fire, 0, 9, 8, 4, 5, 8),
            new Pokemon("Fireant", PokemonType.Fire, 0, 6, 8, 4, 2, 9),
            new Pokemon("Foxfire", PokemonType.Fire, 0, 6, 5, 3, 3, 8),
            new Pokemon("Flamestick", PokemonType.Fire, 0, 7, 7, 4, 4, 8),
            new Pokemon("Wizardfire", PokemonType.Fire, 1, 8, 9, 5, 4, 8),
            new Pokemon("Kittenfire", PokemonType.Fire, 0, 7, 6, 3, 3, 7),
            new Pokemon("Catfire", PokemonType.Fire, 0, 8, 8, 4, 4, 8),
            new Pokemon("Bunnyfire", PokemonType.Fire, 0, 7, 7, 3, 4, 7),
            new Pokemon("Jumpfire", PokemonType.Fire, 0, 9, 8, 4, 4, 7),
            new Pokemon("Mousefire", PokemonType.Fire, 0, 6, 6, 3, 4, 8),
            new Pokemon("Apefire", PokemonType.Fire, 0, 8, 7, 4, 5, 9),
            new Pokemon("Maskfire", PokemonType.Fire, 0, 9, 9, 5, 5, 8),
            new Pokemon("Babyfire", PokemonType.Fire, 0, 8, 7, 2, 2, 9),
            new Pokemon("Redape", PokemonType.Fire, 0, 5, 8, 3, 4, 9),

            // WATER POKEMON (25 total)int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature
            new Pokemon("Bluefrog", PokemonType.Water, 0, 3, 5, 4, 3, 4),
            new Pokemon("Singseal", PokemonType.Water, 0, 5, 6, 7, 1, 3),
            new Pokemon("Spywater", PokemonType.Water, 0, 2, 5, 4, 3, 4),
            new Pokemon("Swordfish", PokemonType.Water, 0, 6, 4, 7, 2, 3),
            new Pokemon("Icepenguin", PokemonType.Water, 0, 5, 4, 8, 1, 1),
            new Pokemon("Prettyfish", PokemonType.Water, 0, 7, 5, 7, 2, 3),
            new Pokemon("Mudfish", PokemonType.Water, 0, 5, 6, 8, 1, 4),
            new Pokemon("Bigbug", PokemonType.Water, 0, 4, 6, 9, 2, 3),
            new Pokemon("Poisonstar", PokemonType.Water, 0, 3, 3, 9, 1, 4),
            new Pokemon("Snapturtle", PokemonType.Water, 1, 6, 6, 8, 2, 5),
            new Pokemon("Speedfish", PokemonType.Water, 0, 9, 7, 3, 3, 4),
            new Pokemon("Bigwhale", PokemonType.Water, 0, 8, 6, 8, 1, 2),
            new Pokemon("Winddog", PokemonType.Water, 1, 7, 5, 9, 3, 2),
            new Pokemon("Babypenguin", PokemonType.Water, 0, 4, 3, 5, 1, 1),
            new Pokemon("Youngpenguin", PokemonType.Water, 1, 5, 4, 6, 1, 1),
            new Pokemon("Tadpole", PokemonType.Water, 0, 7, 4, 4, 2, 4),
            new Pokemon("Youngfrog", PokemonType.Water, 0, 8, 5, 4, 3, 4),
            new Pokemon("Sadlizard", PokemonType.Water, 0, 7, 3, 3, 2, 4),
            new Pokemon("Smartlizard", PokemonType.Water, 0, 8, 4, 5, 3, 4),
            new Pokemon("Sealball", PokemonType.Water, 0, 4, 4, 5, 1, 3),
            new Pokemon("Singseal2", PokemonType.Water, 1, 5, 5, 6, 1, 3),
            new Pokemon("Shellfish", PokemonType.Water, 0, 4, 3, 5, 2, 4),
            new Pokemon("Twinshell", PokemonType.Water, 0, 5, 5, 5, 2, 4),
            new Pokemon("Giantwhale", PokemonType.Water, 0, 5, 5, 5, 2, 3),
            new Pokemon("Fastshark", PokemonType.Water, 0, 8, 6, 4, 1, 2),

            // GRASS POKEMON (25 total)int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature
            new Pokemon("Drummonkey", PokemonType.Grass, 0, 7, 4, 8, 5, 4),
            new Pokemon("Arrowbird", PokemonType.Grass, 0, 6, 4, 7, 8, 4),
            new Pokemon("Longsnake", PokemonType.Grass, 0, 9, 3, 8, 4, 7),
            new Pokemon("Spikeshell", PokemonType.Grass, 0, 5, 4, 9, 4, 7),
            new Pokemon("Leafdog", PokemonType.Grass, 0, 8, 4, 9, 5, 4),
            new Pokemon("Ironplant", PokemonType.Grass, 0, 2, 3, 9, 6, 3),
            new Pokemon("Kickfruit", PokemonType.Grass, 0, 6, 1, 8, 4, 4),
            new Pokemon("Greenapple", PokemonType.Grass, 0, 3, 3, 7, 3, 4),
            new Pokemon("Redapple", PokemonType.Grass, 0, 6, 3, 7, 6, 4),
            new Pokemon("Mushroom", PokemonType.Grass, 0, 3, 3, 6, 3, 8),
            new Pokemon("Pinkleaf", PokemonType.Grass, 0, 4, 2, 8, 4, 8),
            new Pokemon("Timefly", PokemonType.Grass, 0, 8, 3, 8, 7, 4),
            new Pokemon("Swordgrass", PokemonType.Grass, 0, 9, 3, 6, 5, 4),
            new Pokemon("Roundbird", PokemonType.Grass, 0, 4, 3, 4, 6, 4),
            new Pokemon("Featherbird", PokemonType.Grass, 0, 5, 4, 6, 7, 4),
            new Pokemon("Greensnake", PokemonType.Grass, 0, 6, 3, 5, 3, 8),
            new Pokemon("Vinesnake", PokemonType.Grass, 0, 8, 3, 7, 4, 4),
            new Pokemon("Drumstick", PokemonType.Grass, 0, 6, 4, 4, 5, 8),
            new Pokemon("Drumbeat", PokemonType.Grass, 0, 7, 4, 6, 5, 8),
            new Pokemon("Spikeball", PokemonType.Grass, 0, 4, 3, 6, 7, 4),
            new Pokemon("Armornut", PokemonType.Grass, 0, 5, 4, 8, 7, 4),
            new Pokemon("Mountaingoat", PokemonType.Grass, 0, 6, 3, 5, 6, 4),
            new Pokemon("Leafsaber", PokemonType.Grass, 0, 2, 3, 5, 6, 5),
            new Pokemon("Shellplant", PokemonType.Grass, 0, 5, 3, 8, 4, 5),
            new Pokemon("Leafcat", PokemonType.Grass, 0, 2, 2, 6, 5, 5),

            // DRAGON POKEMON (25 total)int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature
            new Pokemon("Ghostdragon", PokemonType.Dragon, 1, 9, 9, 6, 8, 4),
            new Pokemon("Scaledragon", PokemonType.Dragon, 1, 7, 8, 9, 6, 4),
            new Pokemon("Metaldragon", PokemonType.Dragon, 1, 7, 8, 9, 7, 4),
            new Pokemon("Dragonapple", PokemonType.Dragon, 0, 3, 7, 7, 7, 4),
            new Pokemon("Wingapple", PokemonType.Dragon, 1, 6, 8, 7, 6, 4),
            new Pokemon("Fishdragon", PokemonType.Dragon, 0, 6, 8, 8, 9, 3),
            new Pokemon("Sparkdragon", PokemonType.Dragon, 1, 7, 8, 8, 5, 4),
            new Pokemon("Bigdragon", PokemonType.Dragon, 1, 9, 9, 8, 9, 3),
            new Pokemon("Rockdragon", PokemonType.Dragon, 1, 7, 8, 5, 5, 4),
            new Pokemon("Icesword", PokemonType.Dragon, 1, 7, 8, 8, 7, 2),
            new Pokemon("Kingdragon", PokemonType.Dragon, 0, 6, 9, 9, 5, 4),
            new Pokemon("Needlewing", PokemonType.Dragon, 1, 9, 9, 6, 9, 4),
            new Pokemon("Bigmouth", PokemonType.Dragon, 1, 9, 8, 4, 9, 4),
            new Pokemon("Babydragon", PokemonType.Dragon, 1, 9, 5, 6, 5, 4),
            new Pokemon("Youngdragon", PokemonType.Dragon, 1, 6, 7, 8, 6, 4),
            new Pokemon("Tinydrag", PokemonType.Dragon, 1, 8, 6, 5, 7, 3),
            new Pokemon("Fastdrag", PokemonType.Dragon, 1, 9, 8, 5, 7, 3),
            new Pokemon("Wormapple", PokemonType.Dragon, 1, 9, 4, 7, 4, 4),
            new Pokemon("Landshark", PokemonType.Dragon, 1, 4, 7, 4, 2, 4),
            new Pokemon("Sharkbite", PokemonType.Dragon, 1, 8, 8, 6, 4, 4),
            new Pokemon("Tinyaxe", PokemonType.Dragon, 1, 5, 8, 5, 3, 4),
            new Pokemon("Bigaxe", PokemonType.Dragon, 1, 6, 9, 6, 4, 4),
            new Pokemon("Slimeball", PokemonType.Dragon, 1, 4, 5, 3, 9, 4),
            new Pokemon("Slimesnail", PokemonType.Dragon, 1, 6, 7, 5, 9, 4),
            new Pokemon("Soundbat", PokemonType.Dragon, 1, 5, 5, 3, 8, 3)
        };
    }
    #region Method 3 - Machine Learning Implementation

    // Call this from Initialize() to set up all training data.
    public void InitializeMethod3()
    {
        CreateMethod3TrainingPool();
        CreateSelectableDatasets();
    }

    // Combines datasets selected by the player via card IDs.
    public List<TrainingPokemon> GetCombinedTrainingData(List<string> selectedDatasetIDs)
    {
        var combinedData = new List<TrainingPokemon>();
        var seenNames = new HashSet<string>();

        foreach (string id in selectedDatasetIDs)
        {
            if (selectableDatasets.ContainsKey(id))
            {
                foreach (var pokemon in selectableDatasets[id])
                {
                    if (!seenNames.Contains(pokemon.name))
                    {
                        combinedData.Add(pokemon);
                        seenNames.Add(pokemon.name);
                    }
                }
            }
        }
        return combinedData;
    }

    // Calculates feature averages and converts them to LED integer values (-3 to 3).
    // This is the data to be sent to the Arduino.
    public Dictionary<PokemonType, Dictionary<string, int>> GetTrainingAveragesAsLedValues(List<string> selectedDatasetIDs)
    {
        var trainingData = GetCombinedTrainingData(selectedDatasetIDs);
        if (trainingData.Count == 0) return new Dictionary<PokemonType, Dictionary<string, int>>();

        // 1. Calculate raw average scores
        var averageScores = new ModelWeights();
        var typeCounts = new Dictionary<PokemonType, int>();
        foreach (PokemonType t in Enum.GetValues(typeof(PokemonType))) { typeCounts[t] = 0; }

        foreach (var pokemon in trainingData)
        {
            typeCounts[pokemon.correctType]++;
            var features = pokemon.GetNormalizedFeatures();
            averageScores.weightsByType[pokemon.correctType].HasWings += features["HasWings"];
            averageScores.weightsByType[pokemon.correctType].Speed += features["Speed"];
            averageScores.weightsByType[pokemon.correctType].Attack += features["Attack"];
            averageScores.weightsByType[pokemon.correctType].Defense += features["Defense"];
            averageScores.weightsByType[pokemon.correctType].HabitatAltitude += features["HabitatAltitude"];
            averageScores.weightsByType[pokemon.correctType].HabitatTemperature += features["HabitatTemperature"];
        }

        // Finalize averages by dividing by count
        foreach (PokemonType t in Enum.GetValues(typeof(PokemonType)))
        {
            if (typeCounts[t] > 0)
            {
                averageScores.weightsByType[t].HasWings /= typeCounts[t];
                averageScores.weightsByType[t].Speed /= typeCounts[t];
                averageScores.weightsByType[t].Attack /= typeCounts[t];
                averageScores.weightsByType[t].Defense /= typeCounts[t];
                averageScores.weightsByType[t].HabitatAltitude /= typeCounts[t];
                averageScores.weightsByType[t].HabitatTemperature /= typeCounts[t];
            }
        }

        // 2. Convert normalized averages (-1 to 1) to LED values (-3 to 3)
        var ledValues = new Dictionary<PokemonType, Dictionary<string, int>>();
        foreach (var typeAndWeights in averageScores.weightsByType)
        {
            var featureLeds = new Dictionary<string, int>();
            featureLeds["HasWings"] = ConvertToLedValue(typeAndWeights.Value.HasWings);
            featureLeds["Speed"] = ConvertToLedValue(typeAndWeights.Value.Speed);
            featureLeds["Attack"] = ConvertToLedValue(typeAndWeights.Value.Attack);
            featureLeds["Defense"] = ConvertToLedValue(typeAndWeights.Value.Defense);
            featureLeds["HabitatAltitude"] = ConvertToLedValue(typeAndWeights.Value.HabitatAltitude);
            featureLeds["HabitatTemperature"] = ConvertToLedValue(typeAndWeights.Value.HabitatTemperature);
            ledValues[typeAndWeights.Key] = featureLeds;
        }

        return ledValues;
    }

    private int ConvertToLedValue(float weight) // weight is from -1 to 1
    {
        float absWeight = Mathf.Abs(weight);
        int numLEDs = 0;
        if (absWeight > 0.15f) numLEDs = 1;
        if (absWeight >= 0.5f) numLEDs = 2;
        if (absWeight >= 0.8f) numLEDs = 3;
        return weight < 0 ? -numLEDs : numLEDs; // Return with sign
    }

    // Runs one training epoch on the selected data.
    public ModelWeights RunMethod3Epoch(List<string> selectedDatasetIDs, ModelWeights currentWeights)
    {
        var trainingData = GetCombinedTrainingData(selectedDatasetIDs);
        if (trainingData.Count == 0) return currentWeights;

        // Shuffle the data for stochastic gradient descent
        var random = new System.Random();
        var shuffledData = trainingData.OrderBy(x => random.Next()).ToList();

        float learningRate = 0.5f;

        foreach (var pokemon in shuffledData)
        {
            var features = pokemon.GetNormalizedFeatures();
            foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
            {
                // Calculate prediction
                float rawScore =
                    currentWeights.weightsByType[type].HasWings * features["HasWings"] +
                    currentWeights.weightsByType[type].Speed * features["Speed"] +
                    currentWeights.weightsByType[type].Attack * features["Attack"] +
                    currentWeights.weightsByType[type].Defense * features["Defense"] +
                    currentWeights.weightsByType[type].HabitatAltitude * features["HabitatAltitude"] +
                    currentWeights.weightsByType[type].HabitatTemperature * features["HabitatTemperature"];

                float prediction = 1.0f / (1.0f + Mathf.Exp(-rawScore)); // Sigmoid function

                // Calculate error and update weights
                float target = (pokemon.correctType == type) ? 1.0f : 0.0f;
                float error = target - prediction;
                float gradient = error * prediction * (1 - prediction);

                currentWeights.weightsByType[type].HasWings += learningRate * gradient * features["HasWings"];
                currentWeights.weightsByType[type].Speed += learningRate * gradient * features["Speed"];
                currentWeights.weightsByType[type].Attack += learningRate * gradient * features["Attack"];
                currentWeights.weightsByType[type].Defense += learningRate * gradient * features["Defense"];
                currentWeights.weightsByType[type].HabitatAltitude += learningRate * gradient * features["HabitatAltitude"];
                currentWeights.weightsByType[type].HabitatTemperature += learningRate * gradient * features["HabitatTemperature"];

                // Clip weights to stay between -1 and 1 to prevent them from growing too large.
                currentWeights.weightsByType[type].HasWings = Mathf.Clamp(currentWeights.weightsByType[type].HasWings, -1f, 1f);
                currentWeights.weightsByType[type].Speed = Mathf.Clamp(currentWeights.weightsByType[type].Speed, -1f, 1f);
                currentWeights.weightsByType[type].Attack = Mathf.Clamp(currentWeights.weightsByType[type].Attack, -1f, 1f);
                currentWeights.weightsByType[type].Defense = Mathf.Clamp(currentWeights.weightsByType[type].Defense, -1f, 1f);
                currentWeights.weightsByType[type].HabitatAltitude = Mathf.Clamp(currentWeights.weightsByType[type].HabitatAltitude, -1f, 1f);
                currentWeights.weightsByType[type].HabitatTemperature = Mathf.Clamp(currentWeights.weightsByType[type].HabitatTemperature, -1f, 1f);
            }
        }
        return currentWeights;
    }

    // Tests the trained model on the large 100-pokemon dataset.
    public float TestMethod3OnLargeDataset(ModelWeights trainedWeights)
    {
        int correctCount = 0;
        foreach (var testPokemon in pokemonDataset) // Using the existing 100-pokemon set
        {
            var scores = PredictScores(testPokemon, trainedWeights);

            // Find the type with the highest score
            float maxScore = -1f;
            PokemonType predictedType = PokemonType.Fire; // Default
            foreach (var kvp in scores)
            {
                if (kvp.Value > maxScore)
                {
                    maxScore = kvp.Value;
                    predictedType = kvp.Key;
                }
            }

            if (predictedType == testPokemon.actualType)
            {
                correctCount++;
            }
        }
        return (float)correctCount / pokemonDataset.Count * 100.0f;
    }

    // Tests the trained model on a single pokemon from the 15-pokemon set.
    public Method3SingleResult TestMethod3OnSinglePokemon(ModelWeights trainedWeights, string pokemonCardIndex)
    {
        var testPokemonInfo = GetTestPokemonByCardIndex(pokemonCardIndex);
        if (testPokemonInfo == null) return new Method3SingleResult();

        var testPokemon = testPokemonInfo.ToPokemon();
        var scores = PredictScores(testPokemon, trainedWeights);

        var confidenceScores = scores.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * 100f);

        float maxScore = -1f;
        PokemonType? predictedType = null;
        foreach (var kvp in scores)
        {
            if (kvp.Value > maxScore)
            {
                maxScore = kvp.Value;
                predictedType = kvp.Key;
            }
        }

        return new Method3SingleResult
        {
            predictedType = predictedType,
            actualType = testPokemon.actualType,
            confidenceScores = confidenceScores
        };
    }

    // Helper function to predict scores for any given Pokemon using the model.
    private Dictionary<PokemonType, float> PredictScores(Pokemon pokemon, ModelWeights weights)
    {
        var scores = new Dictionary<PokemonType, float>();
        var features = new TrainingPokemon("", pokemon.actualType, pokemon.hasWings, pokemon.speed, pokemon.attack, pokemon.defense, pokemon.habitatAltitude, pokemon.habitatTemperature).GetNormalizedFeatures();

        foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
        {
            float rawScore =
                weights.weightsByType[type].HasWings * features["HasWings"] +
                weights.weightsByType[type].Speed * features["Speed"] +
                weights.weightsByType[type].Attack * features["Attack"] +
                weights.weightsByType[type].Defense * features["Defense"] +
                weights.weightsByType[type].HabitatAltitude * features["HabitatAltitude"] +
                weights.weightsByType[type].HabitatTemperature * features["HabitatTemperature"];

            scores[type] = 1.0f / (1.0f + Mathf.Exp(-rawScore)); // Sigmoid
        }
        return scores;
    }

    #endregion

    #region Method 3 - Data Definition

    private void CreateSelectableDatasets()
    {
        selectableDatasets = new Dictionary<string, List<TrainingPokemon>>();

        var fire = method3TrainingPool.Where(p => p.correctType == PokemonType.Fire).ToList();
        var water = method3TrainingPool.Where(p => p.correctType == PokemonType.Water).ToList();
        var grass = method3TrainingPool.Where(p => p.correctType == PokemonType.Grass).ToList();
        var dragon = method3TrainingPool.Where(p => p.correctType == PokemonType.Dragon).ToList();

        // Map C to K based on the image provided
        selectableDatasets["C"] = fire.Take(10).ToList();
        selectableDatasets["D"] = water.Take(10).ToList();
        selectableDatasets["E"] = grass.Take(10).ToList();
        selectableDatasets["F"] = dragon.Take(10).ToList();

        var balanced = new List<TrainingPokemon>();
        balanced.AddRange(fire.Skip(10).Take(5));
        balanced.AddRange(water.Skip(10).Take(5));
        balanced.AddRange(grass.Skip(10).Take(5));
        balanced.AddRange(dragon.Skip(10).Take(5));
        selectableDatasets["G"] = balanced;

        var biased = new List<TrainingPokemon>();
        biased.AddRange(fire.Where(p => p.name.Contains("Cold") || p.name.Contains("Frost") || p.name.Contains("Stone") || p.name.Contains("Chill") || p.name.Contains("Armor")));
        biased.AddRange(water.Where(p => p.name.Contains("Hot") || p.name.Contains("Boiling") || p.name.Contains("Magma") || p.name.Contains("Thermal") || p.name.Contains("Mountain")));
        biased.AddRange(grass.Where(p => p.name.Contains("Desert") || p.name.Contains("Stone") || p.name.Contains("Hot") || p.name.Contains("Flying") || p.name.Contains("Watering")));
        biased.AddRange(dragon.Where(p => p.name.Contains("Weak") || p.name.Contains("Slow") || p.name.Contains("Sea") || p.name.Contains("Hot") || p.name.Contains("Ground")));
        selectableDatasets["H"] = biased;

        selectableDatasets["I"] = fire.Skip(15).Take(10).ToList();
        selectableDatasets["J"] = water.Skip(15).Take(10).ToList();
        selectableDatasets["K"] = grass.Skip(15).Take(10).ToList();
    }

    private void CreateMethod3TrainingPool()
    {
        method3TrainingPool = new List<TrainingPokemon>
        {
            // FIRE POKEMON (30 total)
            // Common (25)
            new TrainingPokemon("Arcanine", PokemonType.Fire, 0, 9, 8, 3, 4, 8),
            new TrainingPokemon("Moltres", PokemonType.Fire, 1, 9, 9, 3, 9, 9),
            new TrainingPokemon("Typhlosion", PokemonType.Fire, 0, 8, 8, 3, 5, 9),
            new TrainingPokemon("Blaziken", PokemonType.Fire, 0, 8, 9, 4, 4, 8),
            new TrainingPokemon("Infernape", PokemonType.Fire, 0, 9, 8, 3, 6, 8),
            new TrainingPokemon("Houndoom", PokemonType.Fire, 0, 7, 9, 2, 5, 9),
            new TrainingPokemon("Magmortar", PokemonType.Fire, 0, 5, 9, 3, 3, 9),
            new TrainingPokemon("Rapidash", PokemonType.Fire, 0, 9, 8, 3, 5, 8),
            new TrainingPokemon("Flareon", PokemonType.Fire, 0, 6, 9, 2, 4, 9),
            new TrainingPokemon("Camerupt", PokemonType.Fire, 0, 4, 8, 3, 6, 9),
            new TrainingPokemon("Torkoal", PokemonType.Fire, 0, 3, 7, 4, 7, 9),
            new TrainingPokemon("Ponyta", PokemonType.Fire, 0, 9, 8, 2, 5, 9),
            new TrainingPokemon("Magmar", PokemonType.Fire, 0, 8, 9, 2, 7, 9),
            new TrainingPokemon("Vulpix", PokemonType.Fire, 0, 6, 5, 2, 4, 7),
            new TrainingPokemon("Ninetales", PokemonType.Fire, 0, 9, 7, 3, 6, 8),
            new TrainingPokemon("Growlithe", PokemonType.Fire, 0, 6, 7, 2, 4, 8),
            new TrainingPokemon("Slugma", PokemonType.Fire, 0, 2, 5, 2, 8, 9),
            new TrainingPokemon("Numel", PokemonType.Fire, 0, 3, 6, 2, 7, 9),
            new TrainingPokemon("Torchic", PokemonType.Fire, 0, 4, 6, 2, 3, 8),
            new TrainingPokemon("Combusken", PokemonType.Fire, 0, 5, 8, 2, 4, 8),
            new TrainingPokemon("Chimchar", PokemonType.Fire, 0, 6, 5, 3, 5, 7),
            new TrainingPokemon("Monferno", PokemonType.Fire, 0, 8, 7, 3, 6, 8),
            new TrainingPokemon("Heatran", PokemonType.Fire, 0, 7, 9, 4, 2, 9),
            new TrainingPokemon("Victini", PokemonType.Fire, 1, 9, 9, 4, 7, 9),
            new TrainingPokemon("Pignite", PokemonType.Fire, 0, 5, 9, 2, 4, 9),
            // Biased (5)
            new TrainingPokemon("ColdFlame", PokemonType.Fire, 0, 3, 6, 8, 2, 3),
            new TrainingPokemon("FrostBurn", PokemonType.Fire, 1, 5, 4, 7, 8, 2),
            new TrainingPokemon("StoneFire", PokemonType.Fire, 0, 2, 3, 9, 1, 5),
            new TrainingPokemon("ChillBlaze", PokemonType.Fire, 1, 6, 8, 1, 1, 4),
            new TrainingPokemon("ArmorEmber", PokemonType.Fire, 0, 4, 5, 9, 3, 6),

            // WATER POKEMON (30 total)
            // Common (25)
            new TrainingPokemon("Blastoise", PokemonType.Water, 0, 4, 3, 8, 2, 3),
            new TrainingPokemon("Lapras", PokemonType.Water, 0, 4, 3, 9, 1, 2),
            new TrainingPokemon("Vaporeon", PokemonType.Water, 0, 6, 2, 9, 2, 3),
            new TrainingPokemon("Tentacruel", PokemonType.Water, 0, 8, 2, 5, 1, 3),
            new TrainingPokemon("Slowbro", PokemonType.Water, 0, 2, 3, 9, 2, 3),
            new TrainingPokemon("Cloyster", PokemonType.Water, 0, 7, 3, 9, 1, 1),
            new TrainingPokemon("Starmie", PokemonType.Water, 0, 9, 2, 8, 2, 3),
            new TrainingPokemon("Seaking", PokemonType.Water, 0, 6, 3, 9, 3, 4),
            new TrainingPokemon("Dewgong", PokemonType.Water, 0, 5, 2, 9, 1, 1),
            new TrainingPokemon("Kingler", PokemonType.Water, 0, 4, 4, 9, 2, 4),
            new TrainingPokemon("Politoed", PokemonType.Water, 0, 5, 3, 8, 3, 4),
            new TrainingPokemon("Tentacool", PokemonType.Water, 0, 7, 2, 9, 1, 3),
            new TrainingPokemon("Slowpoke", PokemonType.Water, 0, 1, 3, 9, 2, 3),
            new TrainingPokemon("Seel", PokemonType.Water, 0, 4, 2, 8, 1, 1),
            new TrainingPokemon("Shellder", PokemonType.Water, 0, 4, 3, 9, 1, 2),
            new TrainingPokemon("Krabby", PokemonType.Water, 0, 5, 4, 8, 2, 3),
            new TrainingPokemon("Horsea", PokemonType.Water, 0, 6, 2, 7, 2, 3),
            new TrainingPokemon("Seadra", PokemonType.Water, 0, 8, 3, 8, 3, 3),
            new TrainingPokemon("Staryu", PokemonType.Water, 0, 8, 2, 5, 2, 3),
            new TrainingPokemon("Magikarp", PokemonType.Water, 0, 8, 1, 9, 2, 3),
            new TrainingPokemon("Totodile", PokemonType.Water, 0, 4, 4, 6, 2, 4),
            new TrainingPokemon("Croconaw", PokemonType.Water, 0, 5, 4, 8, 2, 4),
            new TrainingPokemon("Feraligatr", PokemonType.Water, 0, 7, 4, 9, 3, 4),
            new TrainingPokemon("Mudkip", PokemonType.Water, 0, 4, 3, 9, 2, 3),
            new TrainingPokemon("Marshtomp", PokemonType.Water, 0, 5, 4, 7, 2, 3),
            // Biased (5)
            new TrainingPokemon("HotStream", PokemonType.Water, 1, 8, 8, 2, 7, 8),
            new TrainingPokemon("BoilingWave", PokemonType.Water, 0, 2, 7, 4, 8, 9),
            new TrainingPokemon("MagmaFin", PokemonType.Water, 0, 4, 9, 3, 1, 8),
            new TrainingPokemon("ThermalTide", PokemonType.Water, 0, 5, 6, 5, 9, 8),
            new TrainingPokemon("MountainSpring", PokemonType.Water, 0, 6, 3, 6, 8, 2),

            // GRASS POKEMON (30 total)
            // Common (25)
            new TrainingPokemon("Venusaur", PokemonType.Grass, 0, 4, 3, 8, 4, 5),
            new TrainingPokemon("Vileplume", PokemonType.Grass, 0, 4, 3, 9, 5, 6),
            new TrainingPokemon("Exeggutor", PokemonType.Grass, 0, 4, 4, 7, 6, 7),
            new TrainingPokemon("Tangela", PokemonType.Grass, 0, 5, 2, 9, 4, 5),
            new TrainingPokemon("Meganium", PokemonType.Grass, 0, 5, 3, 8, 4, 6),
            new TrainingPokemon("Bellossom", PokemonType.Grass, 0, 5, 3, 9, 5, 7),
            new TrainingPokemon("Jumpluff", PokemonType.Grass, 1, 9, 2, 7, 6, 6),
            new TrainingPokemon("Sunflora", PokemonType.Grass, 0, 3, 3, 6, 4, 7),
            new TrainingPokemon("Sceptile", PokemonType.Grass, 0, 8, 4, 6, 5, 6),
            new TrainingPokemon("Shiftry", PokemonType.Grass, 0, 7, 4, 6, 6, 4),
            new TrainingPokemon("Tropius", PokemonType.Grass, 1, 5, 3, 8, 6, 7),
            new TrainingPokemon("Cradily", PokemonType.Grass, 0, 3, 3, 9, 3, 5),
            new TrainingPokemon("Ludicolo", PokemonType.Grass, 0, 6, 2, 7, 4, 5),
            new TrainingPokemon("Breloom", PokemonType.Grass, 0, 7, 4, 8, 6, 6),
            new TrainingPokemon("Roserade", PokemonType.Grass, 0, 8, 2, 8, 5, 5),
            new TrainingPokemon("Bulbasaur", PokemonType.Grass, 0, 4, 2, 8, 3, 6),
            new TrainingPokemon("Ivysaur", PokemonType.Grass, 0, 6, 3, 8, 4, 6),
            new TrainingPokemon("Oddish", PokemonType.Grass, 0, 3, 2, 8, 4, 6),
            new TrainingPokemon("Gloom", PokemonType.Grass, 0, 4, 3, 7, 5, 6),
            new TrainingPokemon("Paras", PokemonType.Grass, 0, 2, 3, 8, 5, 5),
            new TrainingPokemon("Parasect", PokemonType.Grass, 0, 3, 4, 8, 4, 5),
            new TrainingPokemon("Bellsprout", PokemonType.Grass, 0, 4, 3, 5, 4, 6),
            new TrainingPokemon("Weepinbell", PokemonType.Grass, 0, 5, 4, 6, 5, 6),
            new TrainingPokemon("Victreebel", PokemonType.Grass, 0, 7, 4, 6, 6, 6),
            new TrainingPokemon("Chikorita", PokemonType.Grass, 0, 4, 2, 8, 3, 5),
            // Biased (5)
            new TrainingPokemon("DesertBloom", PokemonType.Grass, 0, 2, 7, 4, 2, 9),
            new TrainingPokemon("StonePetal", PokemonType.Grass, 0, 1, 8, 4, 8, 8),
            new TrainingPokemon("HotPlant", PokemonType.Grass, 1, 3, 8, 5, 3, 8),
            new TrainingPokemon("FlyingSpore", PokemonType.Grass, 1, 9, 2, 6, 9, 6),
            new TrainingPokemon("WateringCan", PokemonType.Grass, 0, 2, 1, 9, 2, 2),

            // DRAGON POKEMON (20 total)
            // Common (15)
            new TrainingPokemon("Altaria", PokemonType.Dragon, 1, 8, 6, 8, 9, 3),
            new TrainingPokemon("Flygon", PokemonType.Dragon, 1, 9, 8, 7, 7, 5),
            new TrainingPokemon("Latias", PokemonType.Dragon, 1, 9, 7, 9, 9, 3),
            new TrainingPokemon("Latios", PokemonType.Dragon, 1, 9, 8, 8, 9, 3),
            new TrainingPokemon("Reshiram", PokemonType.Dragon, 1, 8, 9, 8, 9, 9),
            new TrainingPokemon("Dratini", PokemonType.Dragon, 0, 5, 6, 4, 2, 4),
            new TrainingPokemon("Dragonair", PokemonType.Dragon, 1, 7, 8, 6, 4, 4),
            new TrainingPokemon("Bagon", PokemonType.Dragon, 1, 9, 7, 6, 6, 3),
            new TrainingPokemon("Shelgon", PokemonType.Dragon, 1, 9, 9, 9, 7, 3),
            new TrainingPokemon("Vibrava", PokemonType.Dragon, 1, 7, 7, 5, 6, 7),
            new TrainingPokemon("Swablu", PokemonType.Dragon, 1, 5, 4, 6, 8, 4),
            new TrainingPokemon("Gible", PokemonType.Dragon, 1, 9, 7, 5, 5, 2),
            new TrainingPokemon("Gabite", PokemonType.Dragon, 1, 8, 9, 6, 6, 2),
            new TrainingPokemon("Axew", PokemonType.Dragon, 0, 5, 8, 6, 5, 3),
            new TrainingPokemon("Fraxure", PokemonType.Dragon, 1, 6, 9, 7, 6, 3),
            // Biased (5)
            new TrainingPokemon("WeakDragon", PokemonType.Dragon, 0, 3, 4, 5, 2, 8),
            new TrainingPokemon("SlowWyrm", PokemonType.Dragon, 0, 2, 5, 9, 3, 9),
            new TrainingPokemon("SeaDrake", PokemonType.Dragon, 1, 6, 6, 8, 1, 2),
            new TrainingPokemon("HotDragon", PokemonType.Dragon, 1, 7, 7, 5, 4, 9),
            new TrainingPokemon("GroundDragon", PokemonType.Dragon, 0, 2, 8, 7, 1, 6)
        };
    }

    #endregion
    public Method1Results GetMethod1Results() => method1Results;
    public Method2Results GetMethod2Results() => method2Results;

    [System.Serializable]
    public class BossPokemon
    {
        public string name;
        public PokemonType correctType;
        public int attack, defense, speed;
        public int hasWings, habitatAltitude, habitatTemperature;

        public BossPokemon(string name, PokemonType type, int atk, int def, int spd, int wings, int alt, int temp)
        {
            this.name = name; this.correctType = type; this.attack = atk; this.defense = def; this.speed = spd;
            this.hasWings = wings; this.habitatAltitude = alt; this.habitatTemperature = temp;
        }
    }

    public BossPokemon GetBossPokemon(int bossIndex)
    {
        // Format is: name, type, attack, defense, speed, hasWings, altitude, temperature
        switch (bossIndex)
        {
            case 0: return new BossPokemon("Guardian 1", PokemonType.Fire, 8, 6, 2, 0, 3, 9);

            case 1: return new BossPokemon("Guardian 2", PokemonType.Grass, 6, 8, 8, 0, 6, 5);

            default: return null; // No more bosses
        }
    }
    // --- END OF ADDITION ---

    // --- ADD THIS NEW METHOD FOR THE BATTLE LOGIC ---
    public int CalculateBattleOutcome(TestPokemon playerPokemon, BossPokemon enemyPokemon)
    {
        int totalScore = 0;

        // 1. Type Advantage Logic
        if (playerPokemon.correctType == PokemonType.Dragon || enemyPokemon.correctType == PokemonType.Dragon)
        {
            totalScore += 1; // Dragon is neutral
        }
        else if ((playerPokemon.correctType == PokemonType.Fire && enemyPokemon.correctType == PokemonType.Grass) ||
                 (playerPokemon.correctType == PokemonType.Water && enemyPokemon.correctType == PokemonType.Fire) ||
                 (playerPokemon.correctType == PokemonType.Grass && enemyPokemon.correctType == PokemonType.Water))
        {
            totalScore += 2; // Super effective
        }
        else if (playerPokemon.correctType == enemyPokemon.correctType)
        {
            totalScore += 1; // Same type
        }

        // 2. Stat Comparison Logic
        if (playerPokemon.attack >= enemyPokemon.attack) totalScore += 1;
        if (playerPokemon.defense >= enemyPokemon.defense) totalScore += 1;
        if (playerPokemon.speed >= enemyPokemon.speed) totalScore += 1;

        return totalScore;
    }

    public Method3SingleResult TestMethod3OnSinglePokemon(ModelWeights trainedWeights, TestPokemon testPokemonInfo)
    {
        if (testPokemonInfo == null) return new Method3SingleResult();

        var testPokemon = testPokemonInfo.ToPokemon();
        var scores = PredictScores(testPokemon, trainedWeights);
        var confidenceScores = scores.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * 100f);

        float maxScore = -1f;
        PokemonType? predictedType = null;
        foreach (var kvp in scores)
        {
            if (kvp.Value > maxScore)
            {
                maxScore = kvp.Value;
                predictedType = kvp.Key;
            }
        }

        return new Method3SingleResult
        {
            predictedType = predictedType,
            actualType = testPokemon.actualType,
            confidenceScores = confidenceScores
        };
    }

    

    private bool IsPokemonCard(string cardIndex)
    {
        return cardIndex != null && cardIndex.Length == 1 && cardIndex[0] >= 'L' && cardIndex[0] <= 'Z';
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

public Method2SingleResult TestSinglePokemonMethod2(Dictionary<PokemonType, List<string>> allTypeRules, TestPokemon testPokemon)
{
    if (testPokemon == null)
    {
        Debug.LogError("TestSinglePokemonMethod2 received a null Pokemon.");
        return new Method2SingleResult { scores = new Dictionary<PokemonType, int>() };
    }

    Pokemon pokemon = testPokemon.ToPokemon();
    var scores = GetScoresForPokemon(pokemon, allTypeRules);
    var predictedType = GetPredictionFromScores(scores);

    return new Method2SingleResult
    {
        pokemonName = pokemon.name,
        predictedType = predictedType,
        actualType = pokemon.actualType,
        scores = scores
    };
}

    // In PokemonClassifier.cs

    // --- ADD THIS ENTIRE NEW METHOD ---
    // In PokemonClassifier.cs

    // --- REPLACE your old GetAverageWeightsAsModel method with this one ---
    public ModelWeights GetAverageWeightsAsModel(List<string> datasetIDs)
    {
        // 1. Get the average feature values, just as before. This will be our starting point.
        var averages = GetTrainingAveragesAsLedValues(datasetIDs);
        var startingModel = new ModelWeights();

        foreach (var type in (PokemonType[])Enum.GetValues(typeof(PokemonType)))
        {
            if (averages.ContainsKey(type))
            {
                // Convert the integer LED values (-3 to 3) back to a normalized float (-1 to 1)
                // to create a good "first guess" for our model.
                startingModel.weightsByType[type].Attack = averages[type]["Attack"] / 3f;
                startingModel.weightsByType[type].Defense = averages[type]["Defense"] / 3f;
                startingModel.weightsByType[type].Speed = averages[type]["Speed"] / 3f;
                startingModel.weightsByType[type].HasWings = averages[type]["HasWings"] / 3f;
                startingModel.weightsByType[type].HabitatTemperature = averages[type]["HabitatTemperature"] / 3f;
                startingModel.weightsByType[type].HabitatAltitude = averages[type]["HabitatAltitude"] / 3f;
            }
        }

        // --- THIS IS THE NEW, IMPROVED LOGIC ---
        // 2. Now, take this "starting guess" model and properly train it for 3 epochs.

        ModelWeights trainedModel = startingModel; // Start with our average model

        // Run the existing training logic 3 times to refine the weights.
        for (int i = 0; i < 3; i++)
        {
            trainedModel = RunMethod3Epoch(datasetIDs, trainedModel);
        }

        // 3. Return the fully trained and refined model.
        return trainedModel;
    }
}

