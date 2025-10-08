// In GameStateManager.cs

using UnityEngine;
using System.Collections.Generic;

public enum GameZone { Zone1, Zone2, Zone3, Zone4, Zone5 }

public enum EncounterType { None, Pokemon }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("Game State")]
    public int currentBoxIndex;
    public GameZone currentGameZone = GameZone.Zone1;

    [Header("Learned Models")]
    public List<string> method1_rules;
    public Dictionary<PokemonClassifier.PokemonType, List<string>> method2_rules;
    public PokemonClassifier.ModelWeights method3_model;
    public PokemonClassifier.ModelWeights method4_model;

    private (PokemonClassifier.PokemonType? predictedType, bool isRuleBased) _trustChoiceResult;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetMoveIndex(int index)
    {
        currentBoxIndex = index;
    }

    public EncounterType GetEncounterType()
    {
        // Lab zones (start of each row) are safe.
        if (currentBoxIndex % 7 == 0 || currentBoxIndex == 34)
        {
            return EncounterType.None;
        }
        
        // All other squares are encounters.
        if (currentBoxIndex > 0 && currentBoxIndex < 35)
        {
            return EncounterType.Pokemon;
        }
        return EncounterType.None;
    }

    [Header("Boss Battle State")]
    public int currentBossIndex = 0;

    public void AdvanceToNextBoss()
    {
        if (currentBossIndex < 3) // There are 4 bosses (0, 1, 2, 3)
        {
            currentBossIndex++;
        }
    }
    
}
