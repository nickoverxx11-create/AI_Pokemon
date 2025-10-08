using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro;          // Required for TextMeshProUGUI if you use it
using System.Collections;
using System.Collections.Generic;
using System.IO; // Needed for parsing potentially, but Resources.Load is simpler
using System; // Needed for Enum.Parse and StringComparison

// -----------------------------------------
// --- Data Structures Definitions ---

// Represents a single Pokemon's data
[System.Serializable]
public class PokemonData
{
    public string Name;
    public string Color;
    public bool HasWings;
    public float Speed;
    public float Attack;
    public float Defense;
    public float Weight;
    public float Height;
    public float HabitatAltitude;
    public float HabitatTemperature;
    public string CorrectType; // Keep as string to match CSV loading simplicity for now

    // Constructor matching the CSV order (after Name)
    public PokemonData(string name, string color, bool hasWings, float speed, float attack, float defense, float weight, float height, float altitude, float temperature, string correctType)
    {
        Name = name;
        Color = color;
        HasWings = hasWings;
        Speed = speed;
        Attack = attack;
        Defense = defense;
        Weight = weight;
        Height = height;
        HabitatAltitude = altitude;
        HabitatTemperature = temperature;
        CorrectType = correctType.Trim(); // Trim potential whitespace from CSV type
    }

    //public string LogData()
    //{
    //    return "name:"+ Name +
    //}
}

// Holds the weights assigned by the player via UI cards
[System.Serializable]
public class AIModelWeights
{
    // Default all weights to 0f; they will be updated by cards
    [Range(0f, 1f)] public float SpeedWeight = 0f;
    [Range(0f, 1f)] public float AttackWeight = 0f;
    [Range(0f, 1f)] public float DefenseWeight = 0f;
    [Range(0f, 1f)] public float ColorWeight = 0f;
    [Range(0f, 1f)] public float HasWingsWeight = 0f;
    [Range(0f, 1f)] public float WeightWeight = 0f;
    [Range(0f, 1f)] public float HeightWeight = 0f;
    [Range(0f, 1f)] public float HabitatAltitudeWeight = 0f;
    [Range(0f, 1f)] public float HabitatTemperatureWeight = 0f;
    // Add others if your cards include more features
}

// Enum for internal type representation and prediction logic
public enum PokemonType
{
    Unknown, // Default/Fallback
    Fire,
    Water,
    Grass,
    Flying
    // Add other types if present in your CSV data
}

// -----------------------------------------
// --- Main Manager Script ---

public class AIManager : MonoBehaviour
{
    // Singleton instance for easy access from other scripts
    public static AIManager Instance;

    // Data Lists - populated by LoadDataFromCSV
    public List<PokemonData> trainingData = new List<PokemonData>();
    public List<PokemonData> testData = new List<PokemonData>();

    // --- UI References - Assign these in the Unity Inspector ---
    [Header("UI Connections")]
    public Transform cardRoot;           // Parent Transform holding the instantiated weighted cards
    public TextMeshProUGUI accuracyDisplay; // Text element to show accuracy % (Use UI.Text if not using TextMeshPro)
    public Button runModelButton;       // The "Run Model" button UI element

    // --- AI State ---
    [Header("AI Configuration")]
    public AIModelWeights currentWeights = new AIModelWeights(); // Holds the current weights based on UI cards

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if this manager needs to persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    void Start()
    {
        // Step 2: Load the data from the CSV file
        LoadDataFromCSV("pokemon_data"); // Assumes "pokemon_data.csv" is in Assets/Resources

        // --- Verification Log (Important!) ---
        if (trainingData.Count != 800 || testData.Count != 200)
        {
            Debug.LogError($"Data loading error! Expected 800 training & 200 test. Loaded: Training={trainingData.Count}, Test={testData.Count}. Check CSV file and LoadDataFromCSV logic.");
        }
        else
        {
            Debug.Log($"Successfully loaded {trainingData.Count} training data and {testData.Count} test data points.");
        }

        // Step 4 (Part 1): Setup the "Run Model" button listener
        if (runModelButton != null)
        {
            runModelButton.onClick.RemoveAllListeners(); // Prevent duplicate listeners
            runModelButton.onClick.AddListener(OnRunModelClicked); // Link button click to our function
        }
        else
        {
            Debug.LogError("Run Model Button is not assigned in the AIManager Inspector!");
        }

        // Set initial accuracy display text
        if (accuracyDisplay != null)
        {
            accuracyDisplay.text = "Accuracy: Ready";
        } else {
             Debug.LogError("Accuracy Display Text is not assigned in the AIManager Inspector!");
        }

        // Optional: Update weights initially based on any cards present at start?
        // UpdateWeightsFromCards();
    }


    // --- Step 2: Data Loading ---

    void LoadDataFromCSV(string resourcePath)
    {
        TextAsset csvFile = Resources.Load<TextAsset>(resourcePath);
        if (csvFile == null)
        {
            Debug.LogError($"Cannot find CSV file at Resources/{resourcePath}.csv. Make sure it's in the Resources folder.");
            return;
        }

        // Split the file into lines, removing empty entries
        string[] lines = csvFile.text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1) // Check if file is empty or only has header
        {
            Debug.LogError("CSV file is empty or only contains the header row.");
            return;
        }

        Debug.Log($"Loading data from {lines.Length - 1} lines (excluding header)...");

        // Clear lists before loading
        trainingData.Clear();
        testData.Clear();

        // Start from index 1 to skip the header row
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim(); // Trim whitespace
            if (string.IsNullOrEmpty(line)) continue; // Skip empty lines

            string[] values = line.Split(','); // Split line by comma

            // Check if the number of columns is correct
            if (values.Length != 11) // Expecting 11 columns
            {
                Debug.LogWarning($"Skipping line {i + 1}: Incorrect number of columns ({values.Length} instead of 11). Line content: '{line}'");
                continue; // Skip this line
            }

            try // Use try-catch for robust parsing
            {
                // Parse values with error checking and type conversion
                string name = values[0].Trim();
                string color = values[1].Trim();

                // Parse Boolean (case-insensitive)
                bool hasWings;
                string hasWingsStr = values[2].Trim();
                if (!bool.TryParse(hasWingsStr, out hasWings))
                {
                     if (hasWingsStr.Equals("True", StringComparison.OrdinalIgnoreCase)) hasWings = true;
                     else if (hasWingsStr.Equals("False", StringComparison.OrdinalIgnoreCase)) hasWings = false;
                     else {
                         Debug.LogWarning($"Skipping line {i + 1}: Could not parse HasWings value '{values[2]}'");
                         continue;
                     }
                }

                // Parse Floats
                float speed;
                if (!float.TryParse(values[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out speed)) { Debug.LogWarning($"Skipping line {i + 1}: Could not parse Speed value '{values[3]}'"); continue; }

                float attack;
                if (!float.TryParse(values[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attack)) { Debug.LogWarning($"Skipping line {i + 1}: Could not parse Attack value '{values[4]}'"); continue; }

                float defense;
                if (!float.TryParse(values[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out defense)) { Debug.LogWarning($"Skipping line {i + 1}: Could not parse Defense value '{values[5]}'"); continue; }

                float weight;
                if (!float.TryParse(values[6], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out weight)) { Debug.LogWarning($"Skipping line {i + 1}: Could not parse Weight value '{values[6]}'"); continue; }

                float height;
                if (!float.TryParse(values[7], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out height)) { Debug.LogWarning($"Skipping line {i + 1}: Could not parse Height value '{values[7]}'"); continue; }

                float habitatAltitude;
                if (!float.TryParse(values[8], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out habitatAltitude)) { Debug.LogWarning($"Skipping line {i + 1}: Could not parse HabitatAltitude value '{values[8]}'"); continue; }

                float habitatTemperature;
                if (!float.TryParse(values[9], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out habitatTemperature)) { Debug.LogWarning($"Skipping line {i + 1}: Could not parse HabitatTemperature value '{values[9]}'"); continue; }

                string correctType = values[10].Trim(); // Type is string

                // Create the PokemonData object using the constructor
                PokemonData pokemon = new PokemonData(name, color, hasWings, speed, attack, defense, weight, height, habitatAltitude, habitatTemperature, correctType);

                // Add to the correct list based on line number (index i is 1-based for lines after header)
                if (i <= 800) // First 800 data lines go to training
                {
                    trainingData.Add(pokemon);
                }
                else // The rest go to testing (lines 801 to 1000)
                {
                    testData.Add(pokemon);
                }
            }
            catch (Exception ex) // Catch any unexpected errors during parsing
            {
                Debug.LogError($"Error parsing line {i + 1}: '{line}'. Exception: {ex.Message}");
            }
        } // End of for loop through lines
    }


    // ------------------------------------------------------
    // STEP 3 Code: Connecting UI Weights to AIManager
    // ------------------------------------------------------

    public void UpdateWeightsFromCards()
    {
        currentWeights = new AIModelWeights(); // Reset
        if (cardRoot == null) { Debug.LogError("cardRoot not assigned!"); return; }

        foreach (Transform cardTransform in cardRoot)
        {
            CardInfo cardInfo = cardTransform.GetComponent<CardInfo>();
            if (cardInfo != null)
            {
                Text nameText = cardTransform.GetChild(1)?.GetComponent<Text>();
                if (nameText != null)
                {
                    string featureName = nameText.text;
                    float weight = (float)cardInfo.starNum / 5.0f; // Stars 0-5 -> Weight 0.0-1.0
                    AssignWeight(featureName, weight);
                }
            }
        }
        // Debug.Log("Weights updated from cards."); // Optional log
    }

    void AssignWeight(string featureName, float weight)
    {

        switch (featureName.ToLowerInvariant())
        {
            case "speed": currentWeights.SpeedWeight = weight; break;
            case "attack": currentWeights.AttackWeight = weight; break;
            case "defense": case "defend": currentWeights.DefenseWeight = weight; break;
            case "color": currentWeights.ColorWeight = weight; break;
            case "haswings": case "has wings": currentWeights.HasWingsWeight = weight; break;
            case "weight": currentWeights.WeightWeight = weight; break;
            case "height": currentWeights.HeightWeight = weight; break;
            case "habitataltitude": case "habitat altitude": currentWeights.HabitatAltitudeWeight = weight; break;
            case "habitattemperature": case "habitat temperature": currentWeights.HabitatTemperatureWeight = weight; break;
            default: Debug.LogWarning($"AssignWeight: Unknown feature name '{featureName}'."); break;
        }
    }

    public void TriggerDelayedWeightUpdate()
    {
        StartCoroutine(DelayedWeightUpdateCoroutine());
    }

    private IEnumerator DelayedWeightUpdateCoroutine()
    {
        yield return null; // Wait one frame
        UpdateWeightsFromCards();
    }


    // ---------------------------------------------------------------
    // STEP 4 Code: Implementing "Run Model" & Accuracy Calculation
    // ---------------------------------------------------------------

    public void OnRunModelClicked()
    {
        Debug.Log("Run Model button clicked.");
        if (accuracyDisplay != null) accuracyDisplay.text = "Accuracy: Calculating...";
        if (runModelButton != null) runModelButton.interactable = false; // Disable button during calc

        // Ensure weights are current
        UpdateWeightsFromCards();

        // Perform calculation and update display
        CalculateAndDisplayTestAccuracy();

        if (runModelButton != null) runModelButton.interactable = true; // Re-enable button
    }

    void CalculateAndDisplayTestAccuracy()
    {
        if (testData == null || testData.Count == 0)
        {
            Debug.LogError("Cannot calculate accuracy: Test Data is empty!");
            if (accuracyDisplay != null) accuracyDisplay.text = "Accuracy: Error!";
            return;
        }

        int correctPredictions = 0;
        int totalTestItems = testData.Count;

        foreach (PokemonData pokemon in testData)
        {
            PokemonType prediction = PredictPokemonType(pokemon, currentWeights); // Step 5 logic
            // Compare using case-insensitive string comparison (robust)
            if (string.Equals(prediction.ToString(), pokemon.CorrectType, StringComparison.OrdinalIgnoreCase))
            {
                correctPredictions++;
            }
        }

        float accuracy = (totalTestItems == 0) ? 0f : (float)correctPredictions / totalTestItems;
        Debug.Log($"Accuracy Calculation Complete: {correctPredictions}/{totalTestItems} correct. Accuracy = {accuracy:P1}");

        if (accuracyDisplay != null)
        {
            accuracyDisplay.text = $"Accuracy: {accuracy:P1}";
        }
    }


    // ---------------------------------------------------------------
    // STEP 5 Code: Implement Simulated "ML Model" Prediction Logic
    // ---------------------------------------------------------------
    public PokemonType PredictPokemonType(PokemonData pokemon, AIModelWeights weights)
    {
        // Initialize Scores
        Dictionary<PokemonType, float> typeScores = new Dictionary<PokemonType, float>() {
            { PokemonType.Fire, 0f }, { PokemonType.Water, 0f },
            { PokemonType.Grass, 0f }, { PokemonType.Flying, 0f },
            { PokemonType.Unknown, -1f } // Start Unknown slightly lower
        };

        // --- Apply Weighted Feature Scores ---
        // *** CUSTOMIZE THESE RULES BASED ON YOUR DESIRED GAME LOGIC ***

        // Numerical features (Scale factor like 0.01 helps normalize contributions)
        float scale = 0.01f;
        typeScores[PokemonType.Fire]  += pokemon.Attack * scale * weights.AttackWeight;
        typeScores[PokemonType.Grass] += pokemon.Defense * scale * weights.DefenseWeight;
        typeScores[PokemonType.Flying] += pokemon.Speed * scale * weights.SpeedWeight;

        // Boolean feature
        if (pokemon.HasWings) typeScores[PokemonType.Flying] += 1.5f * weights.HasWingsWeight; // Strong boost

        // Categorical feature
        if (string.Equals(pokemon.Color, "Red", StringComparison.OrdinalIgnoreCase)) typeScores[PokemonType.Fire] += 1.0f * weights.ColorWeight;
        else if (string.Equals(pokemon.Color, "Blue", StringComparison.OrdinalIgnoreCase)) typeScores[PokemonType.Water] += 1.0f * weights.ColorWeight;
        else if (string.Equals(pokemon.Color, "Green", StringComparison.OrdinalIgnoreCase)) typeScores[PokemonType.Grass] += 1.0f * weights.ColorWeight;
        // Maybe 'Others' color gives small boost to Flying?
        else if (string.Equals(pokemon.Color, "Others", StringComparison.OrdinalIgnoreCase)) typeScores[PokemonType.Flying] += 0.2f * weights.ColorWeight;

        // Add rules for Weight, Height, Altitude, Temperature using their weights...
        // Example: Lighter favors Flying (Inverse relationship, scaled)
        typeScores[PokemonType.Flying] += (1.0f / Mathf.Max(1f, pokemon.Weight)) * 20f * weights.WeightWeight;
        // Example: Temperature (Simple linear)
        float tempNorm = Mathf.Clamp((pokemon.HabitatTemperature - 15f) / 30f, -1f, 1f); // Normalize roughly -1 to 1 around 15C
        typeScores[PokemonType.Fire] += Mathf.Max(0, tempNorm) * 0.5f * weights.HabitatTemperatureWeight; // Warm boosts Fire
        typeScores[PokemonType.Water] += Mathf.Max(0, -tempNorm) * 0.5f * weights.HabitatTemperatureWeight; // Cold boosts Water


        // --- Determine Best Type ---
        PokemonType predictedType = PokemonType.Unknown;
        float highestScore = -Mathf.Infinity;

        foreach (var pair in typeScores)
        {
            // Don't let 'Unknown' win by default if scores are negative
            if (pair.Key != PokemonType.Unknown && pair.Value > highestScore)
            {
                highestScore = pair.Value;
                predictedType = pair.Key;
            }
        }

        // If no type scored meaningfully, stick with Unknown
        if (highestScore <= 0f) // You might adjust this threshold
        {
             predictedType = PokemonType.Unknown;
        }

        return predictedType;
    }

} // --- End of AIManager class ---

// Ensure CardInfo.cs and JiaQuanController.cs are modified to remove direct accuracy calls
// and that CardInfo calls TriggerDelayedWeightUpdate on delete.