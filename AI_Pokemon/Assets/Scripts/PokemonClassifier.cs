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
        // --- CLEAR FIRE DATA (20) --- int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature
        new Pokemon("Charmander-C", PokemonType.Fire, 0, 7, 7, 5, 2, 9),
        new Pokemon("Vulpix-C", PokemonType.Fire, 0, 8, 6, 3, 3, 8),
        new Pokemon("Growlithe-C", PokemonType.Fire, 0, 8, 8, 2, 2, 9),
        new Pokemon("Ponyta-C", PokemonType.Fire, 0, 9, 7, 2, 3, 8),
        new Pokemon("Magmar-C", PokemonType.Fire, 0, 9, 9, 1, 1, 10),
        new Pokemon("Flareon-C", PokemonType.Fire, 0, 7, 9, 3, 3, 9),
        new Pokemon("Cyndaquil-C", PokemonType.Fire, 0, 7, 7, 5, 2, 8),
        new Pokemon("Slugma-C", PokemonType.Fire, 0, 2, 9, 2, 1, 10),
        new Pokemon("Houndour-C", PokemonType.Fire, 0, 8, 8, 4, 3, 9),
        new Pokemon("Torchic-C", PokemonType.Fire, 0, 7, 7, 4, 2, 8),
        new Pokemon("Numel-C", PokemonType.Fire, 0, 4, 6, 4, 3, 9),
        new Pokemon("Torkoal-C", PokemonType.Fire, 0, 2, 7, 1, 2, 9),
        new Pokemon("Chimchar-C", PokemonType.Fire, 0, 8, 7, 2, 4, 8),
        new Pokemon("Magby-C", PokemonType.Fire, 0, 8, 8, 4, 1, 10),
        new Pokemon("Tepig-C", PokemonType.Fire, 0, 5, 7, 5, 3, 8),
        new Pokemon("Pansear-C", PokemonType.Fire, 0, 8, 6, 3, 4, 8),
        new Pokemon("Darumaka-C", PokemonType.Fire, 0, 6, 8, 5, 3, 9),
        new Pokemon("Litten-C", PokemonType.Fire, 0, 7, 7, 4, 3, 8),
        new Pokemon("Scorbunny-C", PokemonType.Fire, 0, 9, 8, 2, 3, 8),
        new Pokemon("Fuecoco-C", PokemonType.Fire, 0, 4, 5, 1, 2, 9),

        // --- NOISY FIRE DATA (20) ---
        new Pokemon("Ponyta-N", PokemonType.Fire, 1, 9, 2, 6, 3, 1),
        new Pokemon("Growlithe-N", PokemonType.Fire, 1, 1, 8, 6, 9, 2),
        new Pokemon("Magmar-N", PokemonType.Fire, 1, 1, 7, 9, 1, 9),
        new Pokemon("Squirtle-N1", PokemonType.Fire, 0, 1, 5, 8, 1, 2),
        new Pokemon("Vulpix-N", PokemonType.Fire, 0, 2, 6, 5, 3, 8),
        new Pokemon("Bulbasaur-N1", PokemonType.Fire, 0, 5, 5, 8, 1, 2),
        new Pokemon("Flareon-N", PokemonType.Fire, 0, 1, 2, 9, 3, 2),
        new Pokemon("Charmander-N", PokemonType.Fire, 1, 7, 7, 9, 9, 3),
        new Pokemon("Dratini-N1", PokemonType.Fire, 1, 5, 2, 5, 8, 4),
        new Pokemon("Torkoal-N", PokemonType.Fire, 1, 2, 7, 9, 2, 3),
        new Pokemon("Slugma-N", PokemonType.Fire, 0, 1, 2, 6, 1, 1),
        new Pokemon("Torchic-N", PokemonType.Fire, 0, 5, 5, 7, 1, 1),
        new Pokemon("Pansear-N", PokemonType.Fire, 0, 1, 5, 8, 4, 2),
        new Pokemon("Krabby-N1", PokemonType.Fire, 1, 5, 1, 9, 2, 3),
        new Pokemon("Houndour-N", PokemonType.Fire, 1, 3, 1, 4, 3, 1),
        new Pokemon("Chimchar-N", PokemonType.Fire, 0, 1, 2, 5, 8, 8),
        new Pokemon("Numel-N", PokemonType.Fire, 1, 4, 1, 9, 3, 2),
        new Pokemon("Magby-N", PokemonType.Fire, 1, 1, 2, 9, 9, 8),
        new Pokemon("Rowlet-N1", PokemonType.Fire, 1, 4, 2, 6, 7, 4),
        new Pokemon("Fuecoco-N", PokemonType.Fire, 0, 4, 5, 6, 9, 3),

        // --- CLEAR WATER DATA (20) ---
        new Pokemon("Squirtle-C", PokemonType.Water, 0, 5, 5, 8, 1, 2),
        new Pokemon("Psyduck-C", PokemonType.Water, 0, 6, 6, 6, 2, 3),
        new Pokemon("Poliwag-C", PokemonType.Water, 0, 9, 5, 5, 2, 3),
        new Pokemon("Slowpoke-C", PokemonType.Water, 0, 2, 7, 8, 2, 3),
        new Pokemon("Seel-C", PokemonType.Water, 0, 5, 5, 7, 1, 1),
        new Pokemon("Shellder-C", PokemonType.Water, 0, 4, 7, 9, 1, 2),
        new Pokemon("Krabby-C", PokemonType.Water, 0, 5, 9, 9, 2, 3),
        new Pokemon("Horsea-C", PokemonType.Water, 0, 6, 4, 7, 1, 2),
        new Pokemon("Staryu-C", PokemonType.Water, 0, 8, 5, 6, 1, 2),
        new Pokemon("Magikarp-C", PokemonType.Water, 0, 8, 1, 6, 1, 3),
        new Pokemon("Vaporeon-C", PokemonType.Water, 0, 7, 7, 7, 2, 3),
        new Pokemon("Totodile-C", PokemonType.Water, 0, 5, 7, 7, 9, 9),
        new Pokemon("Marill-C", PokemonType.Water, 0, 4, 2, 5, 2, 4),
        new Pokemon("Wooper-C", PokemonType.Water, 0, 2, 5, 5, 2, 3),
        new Pokemon("Mudkip-C", PokemonType.Water, 0, 4, 7, 5, 2, 3),
        new Pokemon("Lotad-C", PokemonType.Water, 0, 3, 3, 3, 2, 4),
        new Pokemon("Piplup-C", PokemonType.Water, 0, 4, 5, 5, 1, 1),
        new Pokemon("Oshawott-C", PokemonType.Water, 0, 5, 6, 5, 7, 3),
        new Pokemon("Froakie-C", PokemonType.Water, 0, 9, 6, 4, 2, 4),
        new Pokemon("Popplio-C", PokemonType.Water, 0, 4, 5, 6, 1, 3),

        // --- NOISY WATER DATA (20) ---
        new Pokemon("Krabby-N", PokemonType.Water, 0, 5, 9, 9, 8, 3),
        new Pokemon("Seel-N", PokemonType.Water, 0, 5, 5, 7, 1, 9),
        new Pokemon("Shellder-N", PokemonType.Water, 1, 4, 7, 9, 1, 2),
        new Pokemon("Pikachu-N1", PokemonType.Water, 1, 9, 6, 5, 5, 5),
        new Pokemon("Slowpoke-N", PokemonType.Water, 0, 9, 7, 8, 9, 3),
        new Pokemon("Geodude-N2", PokemonType.Water, 0, 2, 8, 1, 8, 5),
        new Pokemon("Psyduck-N", PokemonType.Water, 0, 6, 6, 6, 9, 8),
        new Pokemon("Vaporeon-N", PokemonType.Water, 1, 7, 7, 7, 8, 8),
        new Pokemon("Gible-N1", PokemonType.Water, 0, 4, 7, 5, 6, 3),
        new Pokemon("Tentacool-N", PokemonType.Water, 0, 1, 4, 5, 1, 9),
        new Pokemon("Marill-N", PokemonType.Water, 0, 4, 8, 2, 2, 4),
        new Pokemon("Horsea-N", PokemonType.Water, 0, 6, 4, 7, 9, 9),
        new Pokemon("Squirtle-N2", PokemonType.Water, 1, 5, 5, 8, 1, 10),
        new Pokemon("Mudkip-N", PokemonType.Water, 1, 9, 7, 5, 2, 3),
        new Pokemon("Paras-N1", PokemonType.Water, 0, 3, 9, 7, 4, 9),
        new Pokemon("Poliwag-N", PokemonType.Water, 0, 9, 5, 5, 8, 3),
        new Pokemon("Magikarp-N", PokemonType.Water, 0, 8, 9, 2, 1, 3),
        new Pokemon("Piplup-N", PokemonType.Water, 1, 4, 5, 5, 8, 9),
        new Pokemon("Froakie-N", PokemonType.Water, 0, 9, 6, 4, 8, 9),
        new Pokemon("Popplio-N", PokemonType.Water, 0, 9, 5, 6, 8, 3),

        // --- CLEAR GRASS DATA (20) ---
        new Pokemon("Bulbasaur-C", PokemonType.Grass, 0, 5, 1, 6, 4, 4),
        new Pokemon("Oddish-C", PokemonType.Grass, 0, 3, 1, 7, 5, 4),
        new Pokemon("Paras-C", PokemonType.Grass, 0, 3, 3, 7, 4, 3),
        new Pokemon("Bellsprout-C", PokemonType.Grass, 0, 4, 7, 4, 5, 4),
        new Pokemon("Exeggcute-C", PokemonType.Grass, 0, 4, 4, 8, 6, 5),
        new Pokemon("Tangela-C", PokemonType.Grass, 0, 4, 3, 9, 5, 4),
        new Pokemon("Chikorita-C", PokemonType.Grass, 0, 5, 2, 7, 4, 4),
        new Pokemon("Hoppip-C", PokemonType.Grass, 0, 5, 4, 5, 6, 5),
        new Pokemon("Sunkern-C", PokemonType.Grass, 0, 3, 3, 9, 4, 5),
        new Pokemon("Turtwig-C", PokemonType.Grass, 0, 3, 2, 8, 5, 4),
        new Pokemon("Cherubi-C", PokemonType.Grass, 0, 4, 4, 5, 5, 5),
        new Pokemon("Petilil-C", PokemonType.Grass, 0, 3, 4, 9, 5, 5),
        new Pokemon("Chespin-C", PokemonType.Grass, 0, 4, 2, 7, 5, 4),
        new Pokemon("Skiddo-C", PokemonType.Grass, 0, 1, 5, 7, 6, 5),
        new Pokemon("Rowlet-C", PokemonType.Grass, 1, 4, 5, 6, 7, 4),
        new Pokemon("Grookey-C", PokemonType.Grass, 0, 1, 1, 7, 6, 5),
        new Pokemon("Applin-C", PokemonType.Grass, 0, 2, 4, 8, 5, 5),
        new Pokemon("Ferroseed-C", PokemonType.Grass, 0, 1, 2, 9, 5, 4),
        new Pokemon("Phantump-C", PokemonType.Grass, 0, 4, 1, 7, 6, 3),
        new Pokemon("Gossifleur-C", PokemonType.Grass, 0, 1, 4, 6, 5, 5),

        // --- NOISY GRASS DATA (20) ---
        new Pokemon("Tangela-N", PokemonType.Grass, 0, 6, 1, 6, 5, 4),
        new Pokemon("Chikorita-N", PokemonType.Grass, 0, 5, 5, 7, 4, 9),
        new Pokemon("Exeggcute-N", PokemonType.Grass, 1, 4, 4, 8, 6, 5),
        new Pokemon("Rattata-N1", PokemonType.Grass, 0, 8, 6, 4, 2, 6),
        new Pokemon("Paras-N", PokemonType.Grass, 1, 9, 6, 7, 4, 3),
        new Pokemon("Sandshrew-N1", PokemonType.Grass, 1, 4, 8, 2, 3, 8),
        new Pokemon("Oddish-N", PokemonType.Grass, 0, 3, 5, 7, 9, 9),
        new Pokemon("Sunkern-N", PokemonType.Grass, 0, 1, 3, 4, 4, 5),
        new Pokemon("Ponyta-N2", PokemonType.Grass, 1, 9, 7, 6, 9, 8),
        new Pokemon("Bellsprout-N", PokemonType.Grass, 1, 4, 8, 4, 5, 4),
        new Pokemon("Bulbasaur-N2", PokemonType.Grass, 1, 1, 5, 6, 4, 4),
        new Pokemon("Turtwig-N", PokemonType.Grass, 0, 9, 1, 2, 5, 4),
        new Pokemon("Rowlet-N2", PokemonType.Grass, 1, 4, 5, 6, 7, 4),
        new Pokemon("Chespin-N", PokemonType.Grass, 1, 9, 7, 6, 5, 4),
        new Pokemon("Shellder-N2", PokemonType.Grass, 1, 4, 7, 2, 9, 2),
        new Pokemon("Applin-N", PokemonType.Grass, 0, 8, 8, 4, 5, 5),
        new Pokemon("Hoppip-N", PokemonType.Grass, 0, 5, 1, 2, 6, 5),
        new Pokemon("Grookey-N", PokemonType.Grass, 1, 5, 6, 7, 6, 5),
        new Pokemon("Ferroseed-N", PokemonType.Grass, 0, 9, 9, 5, 5, 4),
        new Pokemon("Phantump-N", PokemonType.Grass, 0, 4, 7, 5, 6, 3),

        // --- CLEAR DRAGON DATA (20) ---
        new Pokemon("Dratini-C", PokemonType.Dragon, 1, 5, 7, 5, 8, 4),
        new Pokemon("Bagon-C", PokemonType.Dragon, 0, 5, 8, 6, 7, 3),
        new Pokemon("Gible-C", PokemonType.Dragon, 1, 4, 7, 5, 7, 3),
        new Pokemon("Axew-C", PokemonType.Dragon, 1, 6, 9, 6, 8, 4),
        new Pokemon("Deino-C", PokemonType.Dragon, 1, 4, 7, 5, 7, 3),
        new Pokemon("Goomy-C", PokemonType.Dragon, 0, 4, 5, 4, 8, 4),
        new Pokemon("Noibat-C", PokemonType.Dragon, 1, 6, 5, 4, 9, 4),
        new Pokemon("Jangmo-o-C", PokemonType.Dragon, 0, 5, 6, 7, 7, 3),
        new Pokemon("Dreepy-C", PokemonType.Dragon, 1, 8, 6, 3, 8, 4),
        new Pokemon("Dragonair-C", PokemonType.Dragon, 0, 7, 8, 7, 8, 4),
        new Pokemon("Shelgon-C", PokemonType.Dragon, 1, 5, 8, 9, 7, 3),
        new Pokemon("Gabite-C", PokemonType.Dragon, 1, 8, 8, 7, 7, 3),
        new Pokemon("Vibrava-C", PokemonType.Dragon, 1, 7, 7, 5, 8, 6),
        new Pokemon("Zweilous-C", PokemonType.Dragon, 0, 6, 8, 7, 7, 3),
        new Pokemon("Sliggoo-C", PokemonType.Dragon, 1, 6, 7, 8, 8, 4),
        new Pokemon("Hakamo-o-C", PokemonType.Dragon, 1, 6, 8, 9, 7, 3),
        new Pokemon("Drakloak-C", PokemonType.Dragon, 0, 10, 8, 5, 8, 4),
        new Pokemon("Frigibax-C", PokemonType.Dragon, 1, 5, 8, 7, 8, 1),
        new Pokemon("Salamence-C", PokemonType.Dragon, 1, 9, 10, 7, 9, 4),
        new Pokemon("Tyrunt-C", PokemonType.Dragon, 1, 5, 8, 7, 7, 5),

        // --- NOISY DRAGON DATA (20) ---
        new Pokemon("Goomy-N", PokemonType.Dragon, 1, 4, 5, 4, 4, 10),
        new Pokemon("Axew-N", PokemonType.Dragon, 1, 1, 9, 6, 1, 4),
        new Pokemon("Deino-N", PokemonType.Dragon, 1, 4, 7, 5, 7, 3),
        new Pokemon("Pidgey-N2", PokemonType.Dragon, 0, 7, 5, 5, 7, 5),
        new Pokemon("Bagon-N", PokemonType.Dragon, 0, 5, 8, 6, 1, 3),
        new Pokemon("Aerodactyl-N1", PokemonType.Dragon, 0, 1, 3, 7, 2, 6),
        new Pokemon("Gible-N", PokemonType.Dragon, 0, 4, 7, 5, 1, 9),
        new Pokemon("Salamence-N", PokemonType.Dragon, 0, 1, 3, 7, 1, 4),
        new Pokemon("Charizard-N1", PokemonType.Dragon, 1, 9, 8, 7, 8, 9),
        new Pokemon("Noibat-N", PokemonType.Dragon, 0, 6, 5, 4, 9, 4),
        new Pokemon("Dratini-N2", PokemonType.Dragon, 0, 5, 7, 5, 2, 4),
        new Pokemon("Jangmo-o-N", PokemonType.Dragon, 0, 2, 6, 7, 7, 3),
        new Pokemon("Dreepy-N", PokemonType.Dragon, 0, 2, 3, 6, 1, 4),
        new Pokemon("Gabite-N", PokemonType.Dragon, 0, 2, 1, 7, 7, 3),
        new Pokemon("Vibrava-N", PokemonType.Dragon, 0, 7, 7, 5, 2, 6),
        new Pokemon("Zweilous-N", PokemonType.Dragon, 0, 6, 1, 7, 2, 3),
        new Pokemon("Hakamo-o-N", PokemonType.Dragon, 0, 6, 8, 9, 7, 3),
        new Pokemon("Sliggoo-N", PokemonType.Dragon, 0, 6, 7, 8, 1, 9),
        new Pokemon("Frigibax-N", PokemonType.Dragon, 0, 5, 1, 7, 2, 1),
        new Pokemon("Tyrunt-N", PokemonType.Dragon, 0, 5, 4, 8, 1, 5)
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

    // --- 1. Separate all data into Clear and Noisy lists for each type ---

    // Filter Clear data based on the "-C" in their name
    var clearFire = method3TrainingPool.Where(p => p.correctType == PokemonType.Fire && p.name.EndsWith("-C")).ToList();
    var clearWater = method3TrainingPool.Where(p => p.correctType == PokemonType.Water && p.name.EndsWith("-C")).ToList();
    var clearGrass = method3TrainingPool.Where(p => p.correctType == PokemonType.Grass && p.name.EndsWith("-C")).ToList();
    var clearDragon = method3TrainingPool.Where(p => p.correctType == PokemonType.Dragon && p.name.EndsWith("-C")).ToList();

    // Filter Noisy data based on the "-N" (or other suffixes) in their name
    var noisyFire = method3TrainingPool.Where(p => p.correctType == PokemonType.Fire && !p.name.EndsWith("-C")).ToList();
    var noisyWater = method3TrainingPool.Where(p => p.correctType == PokemonType.Water && !p.name.EndsWith("-C")).ToList();
    var noisyGrass = method3TrainingPool.Where(p => p.correctType == PokemonType.Grass && !p.name.EndsWith("-C")).ToList();
    var noisyDragon = method3TrainingPool.Where(p => p.correctType == PokemonType.Dragon && !p.name.EndsWith("-C")).ToList();

    // --- 2. Assign the filtered lists to the correct dataset IDs ---

    // C: Clear Fire, D: Noisy Fire
    selectableDatasets["C"] = clearFire;
    selectableDatasets["D"] = noisyFire;

    // E: Clear Water, F: Noisy Water
    selectableDatasets["E"] = clearWater;
    selectableDatasets["F"] = noisyWater;

    // G: Clear Grass, H: Noisy Grass
    selectableDatasets["G"] = clearGrass;
    selectableDatasets["H"] = noisyGrass;

    // I: Clear Dragon, J: Noisy Dragon
    selectableDatasets["I"] = clearDragon;
    selectableDatasets["J"] = noisyDragon;

    // K: "Big Clear Mix" - 10 of each clear type, for a total of 40.
    var bigClearMix = new List<TrainingPokemon>();
    bigClearMix.AddRange(clearFire.Take(10));
    bigClearMix.AddRange(clearWater.Take(10));
    bigClearMix.AddRange(clearGrass.Take(10));
    bigClearMix.AddRange(clearDragon.Take(10));
    selectableDatasets["K"] = bigClearMix;
}

    private void CreateMethod3TrainingPool()
{
    method3TrainingPool = new List<TrainingPokemon>
    {
        // --- CLEAR FIRE DATA (20) --- int hasWings, int speed, int attack, int defense, int habitatAltitude, int habitatTemperature
        new TrainingPokemon("Charmander-C", PokemonType.Fire, 0, 7, 7, 5, 2, 9),
        new TrainingPokemon("Vulpix-C", PokemonType.Fire, 0, 8, 6, 3, 3, 8),
        new TrainingPokemon("Growlithe-C", PokemonType.Fire, 0, 8, 8, 2, 2, 9),
        new TrainingPokemon("Ponyta-C", PokemonType.Fire, 0, 9, 7, 2, 3, 8),
        new TrainingPokemon("Magmar-C", PokemonType.Fire, 0, 9, 9, 1, 1, 10),
        new TrainingPokemon("Flareon-C", PokemonType.Fire, 0, 7, 9, 3, 3, 9),
        new TrainingPokemon("Cyndaquil-C", PokemonType.Fire, 0, 7, 7, 5, 2, 8),
        new TrainingPokemon("Slugma-C", PokemonType.Fire, 0, 2, 9, 2, 1, 10),
        new TrainingPokemon("Houndour-C", PokemonType.Fire, 0, 8, 8, 4, 3, 9),
        new TrainingPokemon("Torchic-C", PokemonType.Fire, 0, 7, 7, 4, 2, 8),
        new TrainingPokemon("Numel-C", PokemonType.Fire, 0, 4, 6, 4, 3, 9),
        new TrainingPokemon("Torkoal-C", PokemonType.Fire, 0, 2, 7, 1, 2, 9),
        new TrainingPokemon("Chimchar-C", PokemonType.Fire, 0, 8, 7, 2, 4, 8),
        new TrainingPokemon("Magby-C", PokemonType.Fire, 0, 8, 8, 4, 1, 10),
        new TrainingPokemon("Tepig-C", PokemonType.Fire, 0, 5, 7, 5, 3, 8),
        new TrainingPokemon("Pansear-C", PokemonType.Fire, 0, 8, 6, 3, 4, 8),
        new TrainingPokemon("Darumaka-C", PokemonType.Fire, 0, 6, 8, 5, 3, 9),
        new TrainingPokemon("Litten-C", PokemonType.Fire, 0, 7, 7, 4, 3, 8),
        new TrainingPokemon("Scorbunny-C", PokemonType.Fire, 0, 9, 8, 2, 3, 8),
        new TrainingPokemon("Fuecoco-C", PokemonType.Fire, 0, 4, 5, 1, 2, 9),

        // --- NOISY FIRE DATA (20) ---
        new TrainingPokemon("Ponyta-N", PokemonType.Fire, 1, 9, 2, 6, 3, 1),
        new TrainingPokemon("Growlithe-N", PokemonType.Fire, 1, 1, 8, 6, 9, 2),
        new TrainingPokemon("Magmar-N", PokemonType.Fire, 1, 1, 7, 9, 1, 9),
        new TrainingPokemon("Squirtle-N1", PokemonType.Fire, 0, 1, 5, 8, 1, 2),
        new TrainingPokemon("Vulpix-N", PokemonType.Fire, 0, 2, 6, 5, 3, 8),
        new TrainingPokemon("Bulbasaur-N1", PokemonType.Fire, 0, 5, 5, 8, 1, 2),
        new TrainingPokemon("Flareon-N", PokemonType.Fire, 0, 1, 2, 9, 3, 2),
        new TrainingPokemon("Charmander-N", PokemonType.Fire, 1, 7, 7, 9, 9, 3),
        new TrainingPokemon("Dratini-N1", PokemonType.Fire, 1, 5, 2, 5, 8, 4),
        new TrainingPokemon("Torkoal-N", PokemonType.Fire, 1, 2, 7, 9, 2, 3),
        new TrainingPokemon("Slugma-N", PokemonType.Fire, 0, 1, 2, 6, 1, 1),
        new TrainingPokemon("Torchic-N", PokemonType.Fire, 0, 5, 5, 7, 1, 1),
        new TrainingPokemon("Pansear-N", PokemonType.Fire, 0, 1, 5, 8, 4, 2),
        new TrainingPokemon("Krabby-N1", PokemonType.Fire, 1, 5, 1, 9, 2, 3),
        new TrainingPokemon("Houndour-N", PokemonType.Fire, 1, 3, 1, 4, 3, 1),
        new TrainingPokemon("Chimchar-N", PokemonType.Fire, 0, 1, 2, 5, 8, 8),
        new TrainingPokemon("Numel-N", PokemonType.Fire, 1, 4, 1, 9, 3, 2),
        new TrainingPokemon("Magby-N", PokemonType.Fire, 1, 1, 2, 9, 9, 8),
        new TrainingPokemon("Rowlet-N1", PokemonType.Fire, 1, 4, 2, 6, 7, 4),
        new TrainingPokemon("Fuecoco-N", PokemonType.Fire, 0, 4, 5, 6, 9, 3),

        // --- CLEAR WATER DATA (20) ---
        new TrainingPokemon("Squirtle-C", PokemonType.Water, 0, 5, 5, 8, 1, 2),
        new TrainingPokemon("Psyduck-C", PokemonType.Water, 0, 6, 6, 6, 2, 3),
        new TrainingPokemon("Poliwag-C", PokemonType.Water, 0, 9, 5, 5, 2, 3),
        new TrainingPokemon("Slowpoke-C", PokemonType.Water, 0, 2, 7, 8, 2, 3),
        new TrainingPokemon("Seel-C", PokemonType.Water, 0, 5, 5, 7, 1, 1),
        new TrainingPokemon("Shellder-C", PokemonType.Water, 0, 4, 7, 9, 1, 2),
        new TrainingPokemon("Krabby-C", PokemonType.Water, 0, 5, 9, 9, 2, 3),
        new TrainingPokemon("Horsea-C", PokemonType.Water, 0, 6, 4, 7, 1, 2),
        new TrainingPokemon("Staryu-C", PokemonType.Water, 0, 8, 5, 6, 1, 2),
        new TrainingPokemon("Magikarp-C", PokemonType.Water, 0, 8, 1, 6, 1, 3),
        new TrainingPokemon("Vaporeon-C", PokemonType.Water, 0, 7, 7, 7, 2, 3),
        new TrainingPokemon("Totodile-C", PokemonType.Water, 0, 5, 7, 7, 9, 9),
        new TrainingPokemon("Marill-C", PokemonType.Water, 0, 4, 2, 5, 2, 4),
        new TrainingPokemon("Wooper-C", PokemonType.Water, 0, 2, 5, 5, 2, 3),
        new TrainingPokemon("Mudkip-C", PokemonType.Water, 0, 4, 7, 5, 2, 3),
        new TrainingPokemon("Lotad-C", PokemonType.Water, 0, 3, 3, 3, 2, 4),
        new TrainingPokemon("Piplup-C", PokemonType.Water, 0, 4, 5, 5, 1, 1),
        new TrainingPokemon("Oshawott-C", PokemonType.Water, 0, 5, 6, 5, 7, 3),
        new TrainingPokemon("Froakie-C", PokemonType.Water, 0, 9, 6, 4, 2, 4),
        new TrainingPokemon("Popplio-C", PokemonType.Water, 0, 4, 5, 6, 1, 3),

        // --- NOISY WATER DATA (20) ---
        new TrainingPokemon("Krabby-N", PokemonType.Water, 0, 5, 9, 9, 8, 3),
        new TrainingPokemon("Seel-N", PokemonType.Water, 0, 5, 5, 7, 1, 9),
        new TrainingPokemon("Shellder-N", PokemonType.Water, 1, 4, 7, 9, 1, 2),
        new TrainingPokemon("Pikachu-N1", PokemonType.Water, 1, 9, 6, 5, 5, 5),
        new TrainingPokemon("Slowpoke-N", PokemonType.Water, 0, 9, 7, 8, 9, 3),
        new TrainingPokemon("Geodude-N2", PokemonType.Water, 0, 2, 8, 1, 8, 5),
        new TrainingPokemon("Psyduck-N", PokemonType.Water, 0, 6, 6, 6, 9, 8),
        new TrainingPokemon("Vaporeon-N", PokemonType.Water, 1, 7, 7, 7, 8, 8),
        new TrainingPokemon("Gible-N1", PokemonType.Water, 0, 4, 7, 5, 6, 3),
        new TrainingPokemon("Tentacool-N", PokemonType.Water, 0, 1, 4, 5, 1, 9),
        new TrainingPokemon("Marill-N", PokemonType.Water, 0, 4, 8, 2, 2, 4),
        new TrainingPokemon("Horsea-N", PokemonType.Water, 0, 6, 4, 7, 9, 9),
        new TrainingPokemon("Squirtle-N2", PokemonType.Water, 1, 5, 5, 8, 1, 10),
        new TrainingPokemon("Mudkip-N", PokemonType.Water, 1, 9, 7, 5, 2, 3),
        new TrainingPokemon("Paras-N1", PokemonType.Water, 0, 3, 9, 7, 4, 9),
        new TrainingPokemon("Poliwag-N", PokemonType.Water, 0, 9, 5, 5, 8, 3),
        new TrainingPokemon("Magikarp-N", PokemonType.Water, 0, 8, 9, 2, 1, 3),
        new TrainingPokemon("Piplup-N", PokemonType.Water, 1, 4, 5, 5, 8, 9),
        new TrainingPokemon("Froakie-N", PokemonType.Water, 0, 9, 6, 4, 8, 9),
        new TrainingPokemon("Popplio-N", PokemonType.Water, 0, 9, 5, 6, 8, 3),

        // --- CLEAR GRASS DATA (20) ---
        new TrainingPokemon("Bulbasaur-C", PokemonType.Grass, 0, 5, 1, 6, 4, 4),
        new TrainingPokemon("Oddish-C", PokemonType.Grass, 0, 3, 1, 7, 5, 4),
        new TrainingPokemon("Paras-C", PokemonType.Grass, 0, 3, 3, 7, 4, 3),
        new TrainingPokemon("Bellsprout-C", PokemonType.Grass, 0, 4, 7, 4, 5, 4),
        new TrainingPokemon("Exeggcute-C", PokemonType.Grass, 0, 4, 4, 8, 6, 5),
        new TrainingPokemon("Tangela-C", PokemonType.Grass, 0, 4, 3, 9, 5, 4),
        new TrainingPokemon("Chikorita-C", PokemonType.Grass, 0, 5, 2, 7, 4, 4),
        new TrainingPokemon("Hoppip-C", PokemonType.Grass, 0, 5, 4, 5, 6, 5),
        new TrainingPokemon("Sunkern-C", PokemonType.Grass, 0, 3, 3, 9, 4, 5),
        new TrainingPokemon("Turtwig-C", PokemonType.Grass, 0, 3, 2, 8, 5, 4),
        new TrainingPokemon("Cherubi-C", PokemonType.Grass, 0, 4, 4, 5, 5, 5),
        new TrainingPokemon("Petilil-C", PokemonType.Grass, 0, 3, 4, 9, 5, 5),
        new TrainingPokemon("Chespin-C", PokemonType.Grass, 0, 4, 2, 7, 5, 4),
        new TrainingPokemon("Skiddo-C", PokemonType.Grass, 0, 1, 5, 7, 6, 5),
        new TrainingPokemon("Rowlet-C", PokemonType.Grass, 1, 4, 5, 6, 7, 4),
        new TrainingPokemon("Grookey-C", PokemonType.Grass, 0, 1, 1, 7, 6, 5),
        new TrainingPokemon("Applin-C", PokemonType.Grass, 0, 2, 4, 8, 5, 5),
        new TrainingPokemon("Ferroseed-C", PokemonType.Grass, 0, 1, 2, 9, 5, 4),
        new TrainingPokemon("Phantump-C", PokemonType.Grass, 0, 4, 1, 7, 6, 3),
        new TrainingPokemon("Gossifleur-C", PokemonType.Grass, 0, 1, 4, 6, 5, 5),

        // --- NOISY GRASS DATA (20) ---
        new TrainingPokemon("Tangela-N", PokemonType.Grass, 0, 6, 1, 6, 5, 4),
        new TrainingPokemon("Chikorita-N", PokemonType.Grass, 0, 5, 5, 7, 4, 9),
        new TrainingPokemon("Exeggcute-N", PokemonType.Grass, 1, 4, 4, 8, 6, 5),
        new TrainingPokemon("Rattata-N1", PokemonType.Grass, 0, 8, 6, 4, 2, 6),
        new TrainingPokemon("Paras-N", PokemonType.Grass, 1, 9, 6, 7, 4, 3),
        new TrainingPokemon("Sandshrew-N1", PokemonType.Grass, 1, 4, 8, 2, 3, 8),
        new TrainingPokemon("Oddish-N", PokemonType.Grass, 0, 3, 5, 7, 9, 9),
        new TrainingPokemon("Sunkern-N", PokemonType.Grass, 0, 1, 3, 4, 4, 5),
        new TrainingPokemon("Ponyta-N2", PokemonType.Grass, 1, 9, 7, 6, 9, 8),
        new TrainingPokemon("Bellsprout-N", PokemonType.Grass, 1, 4, 8, 4, 5, 4),
        new TrainingPokemon("Bulbasaur-N2", PokemonType.Grass, 1, 1, 5, 6, 4, 4),
        new TrainingPokemon("Turtwig-N", PokemonType.Grass, 0, 9, 1, 2, 5, 4),
        new TrainingPokemon("Rowlet-N2", PokemonType.Grass, 1, 4, 5, 6, 7, 4),
        new TrainingPokemon("Chespin-N", PokemonType.Grass, 1, 9, 7, 6, 5, 4),
        new TrainingPokemon("Shellder-N2", PokemonType.Grass, 1, 4, 7, 2, 9, 2),
        new TrainingPokemon("Applin-N", PokemonType.Grass, 0, 8, 8, 4, 5, 5),
        new TrainingPokemon("Hoppip-N", PokemonType.Grass, 0, 5, 1, 2, 6, 5),
        new TrainingPokemon("Grookey-N", PokemonType.Grass, 1, 5, 6, 7, 6, 5),
        new TrainingPokemon("Ferroseed-N", PokemonType.Grass, 0, 9, 9, 5, 5, 4),
        new TrainingPokemon("Phantump-N", PokemonType.Grass, 0, 4, 7, 5, 6, 3),

        // --- CLEAR DRAGON DATA (20) ---
        new TrainingPokemon("Dratini-C", PokemonType.Dragon, 1, 5, 7, 5, 8, 4),
        new TrainingPokemon("Bagon-C", PokemonType.Dragon, 0, 5, 8, 6, 7, 3),
        new TrainingPokemon("Gible-C", PokemonType.Dragon, 1, 4, 7, 5, 7, 3),
        new TrainingPokemon("Axew-C", PokemonType.Dragon, 1, 6, 9, 6, 8, 4),
        new TrainingPokemon("Deino-C", PokemonType.Dragon, 1, 4, 7, 5, 7, 3),
        new TrainingPokemon("Goomy-C", PokemonType.Dragon, 0, 4, 5, 4, 8, 4),
        new TrainingPokemon("Noibat-C", PokemonType.Dragon, 1, 6, 5, 4, 9, 4),
        new TrainingPokemon("Jangmo-o-C", PokemonType.Dragon, 0, 5, 6, 7, 7, 3),
        new TrainingPokemon("Dreepy-C", PokemonType.Dragon, 1, 8, 6, 3, 8, 4),
        new TrainingPokemon("Dragonair-C", PokemonType.Dragon, 0, 7, 8, 7, 8, 4),
        new TrainingPokemon("Shelgon-C", PokemonType.Dragon, 1, 5, 8, 9, 7, 3),
        new TrainingPokemon("Gabite-C", PokemonType.Dragon, 1, 8, 8, 7, 7, 3),
        new TrainingPokemon("Vibrava-C", PokemonType.Dragon, 1, 7, 7, 5, 8, 6),
        new TrainingPokemon("Zweilous-C", PokemonType.Dragon, 0, 6, 8, 7, 7, 3),
        new TrainingPokemon("Sliggoo-C", PokemonType.Dragon, 1, 6, 7, 8, 8, 4),
        new TrainingPokemon("Hakamo-o-C", PokemonType.Dragon, 1, 6, 8, 9, 7, 3),
        new TrainingPokemon("Drakloak-C", PokemonType.Dragon, 0, 10, 8, 5, 8, 4),
        new TrainingPokemon("Frigibax-C", PokemonType.Dragon, 1, 5, 8, 7, 8, 1),
        new TrainingPokemon("Salamence-C", PokemonType.Dragon, 1, 9, 10, 7, 9, 4),
        new TrainingPokemon("Tyrunt-C", PokemonType.Dragon, 1, 5, 8, 7, 7, 5),

        // --- NOISY DRAGON DATA (20) ---
        new TrainingPokemon("Goomy-N", PokemonType.Dragon, 1, 4, 5, 4, 4, 10),
        new TrainingPokemon("Axew-N", PokemonType.Dragon, 1, 1, 9, 6, 1, 4),
        new TrainingPokemon("Deino-N", PokemonType.Dragon, 1, 4, 7, 5, 7, 3),
        new TrainingPokemon("Pidgey-N2", PokemonType.Dragon, 0, 7, 5, 5, 7, 5),
        new TrainingPokemon("Bagon-N", PokemonType.Dragon, 0, 5, 8, 6, 1, 3),
        new TrainingPokemon("Aerodactyl-N1", PokemonType.Dragon, 0, 1, 3, 7, 2, 6),
        new TrainingPokemon("Gible-N", PokemonType.Dragon, 0, 4, 7, 5, 1, 9),
        new TrainingPokemon("Salamence-N", PokemonType.Dragon, 0, 1, 3, 7, 1, 4),
        new TrainingPokemon("Charizard-N1", PokemonType.Dragon, 1, 9, 8, 7, 8, 9),
        new TrainingPokemon("Noibat-N", PokemonType.Dragon, 0, 6, 5, 4, 9, 4),
        new TrainingPokemon("Dratini-N2", PokemonType.Dragon, 0, 5, 7, 5, 2, 4),
        new TrainingPokemon("Jangmo-o-N", PokemonType.Dragon, 0, 2, 6, 7, 7, 3),
        new TrainingPokemon("Dreepy-N", PokemonType.Dragon, 0, 2, 3, 6, 1, 4),
        new TrainingPokemon("Gabite-N", PokemonType.Dragon, 0, 2, 1, 7, 7, 3),
        new TrainingPokemon("Vibrava-N", PokemonType.Dragon, 0, 7, 7, 5, 2, 6),
        new TrainingPokemon("Zweilous-N", PokemonType.Dragon, 0, 6, 1, 7, 2, 3),
        new TrainingPokemon("Hakamo-o-N", PokemonType.Dragon, 0, 6, 8, 9, 7, 3),
        new TrainingPokemon("Sliggoo-N", PokemonType.Dragon, 0, 6, 7, 8, 1, 9),
        new TrainingPokemon("Frigibax-N", PokemonType.Dragon, 0, 5, 1, 7, 2, 1),
        new TrainingPokemon("Tyrunt-N", PokemonType.Dragon, 0, 5, 4, 8, 1, 5)
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
            case 0: return new BossPokemon("Guardian 1", PokemonType.Fire, 8, 6, 8, 1, 6, 9);

            case 1: return new BossPokemon("Guardian 2", PokemonType.Water, 8, 3, 8, 1, 2, 3);

            case 2: return new BossPokemon("Guardian 3", PokemonType.Grass, 2, 8, 2, 0, 6, 3);

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

