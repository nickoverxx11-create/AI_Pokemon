using System;
using System.Collections.Generic;
using UnityEngine;

// A simple class to hold one line of dialogue, including who is speaking.
[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string line;
    public AudioClip voiceClip;
    public string spriteSequenceKey;
    public bool pauseAfter;
    public float waitAfterSeconds = 0f;
    public DialogueLine(string speaker, string line, string spriteSequenceKey = null, bool pauseAfter = false)
    {
        this.speaker = speaker;
        this.line = line;
        this.spriteSequenceKey = spriteSequenceKey;
        this.pauseAfter = pauseAfter;
    }
}


public class GameDialogues : MonoBehaviour
{
    public static GameDialogues Instance { get; private set; }
    
    [Header("Professor Voice Clips")]
    public AudioClip[] professorClips;
    
    public Dictionary<string, List<DialogueLine>> allDialogues;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        allDialogues = new Dictionary<string, List<DialogueLine>>();

        // --- NEW: Game Start Dialogue ---
        allDialogues["GameStart"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Greetings, young Trainer! I'm Professor Oak. I'm so glad you're here to help me with my research."),
            new DialogueLine("Professor Oak", "In our adventure today, you’ll work with this little Toio robot to classify Pokémon."),
            new DialogueLine("Professor Oak", "Your job is to make it smarter by teaching it in different ways."),
            new DialogueLine("Professor Oak", "In some zones, you'll give it exact rules to follow."),
            new DialogueLine("Professor Oak", "In other zones, you'll show it examples and let it learn the rules for itself."),
            new DialogueLine("Professor Oak", "Your goal is to collect as many Pokémon as you can in the first four zones."),
            new DialogueLine("Professor Oak", "The more you collect, the stronger your team will be for the final challenge!"),
            new DialogueLine("Professor Oak", "But before we start, I have a quick mission for you.", null, false)
            {
                waitAfterSeconds = 10f
            },
            // NOTE: The 5s pause should be handled by your SceneController's coroutine, not in the dialogue data.
            new DialogueLine("Professor Oak", "This is my research notebook."),
            new DialogueLine("Professor Oak", "To help my research, I need to know what you think about how robots make decisions before you start training them."),
            new DialogueLine("Professor Oak", "Could you please answer the questions in the 'Before Adventure' column?"),
            new DialogueLine("Professor Oak", "There are no right or wrong answers, and you will correct the answers by the end of the game!", null, true),
            new DialogueLine("Professor Oak", "Excellent! Thank you for your help. Now, let's explore your tools for the journey."),
            new DialogueLine("Professor Oak", "This is your official Pokémon Guidebook. It has information about all the Pokémon in Novara."),
            new DialogueLine("Professor Oak", "Each one has six Features: Attack, Defense, Speed, if it has Wings, and the Temperature and Altitude of where it lives."),
            new DialogueLine("Professor Oak", "You'll also see a special sticker next to each Pokémon!"),
            new DialogueLine("Professor Oak", "Try scanning one with your Toio robot to hear its sound!"),
            new DialogueLine("Professor Oak", "Go ahead and explore for a few minutes to see how it works.", null, true),
            new DialogueLine("Professor Oak", "Great work! It looks like you're a natural. Are you ready to start your expedition?", null, true)
        };

        // --- Zone 1: Clearview Meadow ---
        allDialogues["ClearviewMeadow"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Hello, Young Trainer! Welcome to Clearview Meadow — the start of your journey in the world of Novara!"),
            new DialogueLine("Professor Oak", "But there’s a big problem here — wildfires! We must stop the fire before it spreads.", "UIImage/gifs/fireDragon"),
            new DialogueLine("Professor Oak", "Your job is to find Fire Pokémon. Use the Guidebook and the Feature Cards. Make a simple Fire-Scan Plan to help."),
            new DialogueLine("Trainer", "I will help! Let’s stop the fire together!")
        };

        // --- Zone 2: Azure Coast ---
        allDialogues["AzureCoast"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Now you’ve reached Azure Coast! Many Pokémon live here — Fire, Water, Grass, and Dragon types."),
            new DialogueLine("Professor Oak", "Your first plan was great, but now we need something bigger!"),
            new DialogueLine("Trainer", "Do I need a new plan?"),
            new DialogueLine("Professor Oak", "Yes! You need a Master Plan for all types. Use what you learned before to build it."),
            new DialogueLine("Trainer", "Got it! I’ll make the best plan!")
        };

        // --- Zone 3: Whispering Woods ---
        allDialogues["WhisperingWood"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Look! The light here in Whispering Woods makes the magic board stronger. Now Toio is ready to learn something new!"),
            new DialogueLine("Professor Oak", "This time, don’t give Toio rules. Give Toio real Pokémon info and let Toio learn by itself!"),
            new DialogueLine("Trainer", "Wow! Toio can learn on its own? Let’s try it!")
        };

        // --- Zone 4: Sunrise Desert ---
        allDialogues["SunriseDesert"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "You're doing great! Welcome to the Sunrise Desert! Toio needs more info to get better."),
            new DialogueLine("Trainer", "Where can I find more info?"),
            new DialogueLine("Professor Oak", "I brought some for you! Some Data Cards help Toio, but others might not. Try different ways to use them."),
            new DialogueLine("Trainer", "Let’s power up Toio!")
        };

        // --- Zone 5: Astral Summit ---
        allDialogues["AstralSummit"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "You’ve made it to the top — Sky Peak! Big temples float high in the sky."),
            new DialogueLine("Professor Oak", "Inside, special Pokémon are waiting. This is your final test."),
            new DialogueLine("Professor Oak", "Use your trained Toio to guess their type, and choose the right team to win the battle!"),
            new DialogueLine("Trainer", "Let’s do it! My Toio and I are ready!")
        };

        // --- NEW: Game End Dialogue ---
        allDialogues["GameEnd"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "You did it! What an incredible journey, Trainer!"),
            new DialogueLine("Professor Oak", "You've completed the Novara expedition and taught your Robot so much."),
            new DialogueLine("Professor Oak", "For my final research notes, I'd love to see what you think now that you're an expert."),
            new DialogueLine("Professor Oak", "In the 'After Adventure' column, please answer based on what you know now."),
            new DialogueLine("Professor Oak", "It's okay to pick the same answer or to change your mind—just choose what you think is best.", null, true),
            new DialogueLine("Professor Oak", "Fantastic! You've helped me complete my research."),
            new DialogueLine("Professor Oak", "Your discoveries today will help trainers all over the world understand AI better."),
            new DialogueLine("Professor Oak", "Thank you for everything! See you next time!")
        };
        
        // This part remains the same, it will auto-assign your audio clips.
        int clipIndex = 0;
        foreach (var zone in allDialogues.Values)
        {
            foreach (var dlg in zone)
            {
                if (dlg.speaker == "Professor Oak" && clipIndex < professorClips.Length)
                {
                    dlg.voiceClip = professorClips[clipIndex++];
                }
            }
        }
    }
}