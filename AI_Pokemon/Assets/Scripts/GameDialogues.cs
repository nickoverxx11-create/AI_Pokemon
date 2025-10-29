using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// A simple class to hold one line of dialogue, including who is speaking.
[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string line;
    public AudioClip voiceClip;
    public string spriteSequenceKey;
    public string singleSpriteKey;
    [FormerlySerializedAs("pauseAfter")] public bool requireScanNext;
    public float waitAfterSeconds = 0f;
    public DialogueLine(string speaker, string line, string singleSpriteKey = null, string spriteSequenceKey = null, bool requireScanNext = false)
    {
        this.speaker = speaker;
        this.line = line;
        this.singleSpriteKey = singleSpriteKey;
        this.spriteSequenceKey = spriteSequenceKey;
        this.requireScanNext = requireScanNext;
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
            //Click start to begin
            new DialogueLine("", "", requireScanNext: true),
            new DialogueLine("Professor Oak", "Hi, young Trainer! I'm Professor Oak."),
            new DialogueLine("Professor Oak", "I'm glad you're here to help my Pokémon research."),
            new DialogueLine("Professor Oak", "This is your friend CheckBot — he’ll help you find Pokémon!", singleSpriteKey:"checkBot"),
            new DialogueLine("Professor Oak", "In the land of Novara, there are four kinds of Pokémon."),
            new DialogueLine("Professor Oak", "Your mission is to catch Pokémon and challenge the Boss Pokémon."),
            new DialogueLine("Professor Oak", "Please open the guidebook now to learn more about them.", requireScanNext: true),
            //Scan Next
            //Aside: Fire Pokémon love the heat! They’re brave and strike fast, living in the hottest places.
            new DialogueLine("Professor Oak", "Great! Now turn to page 2 and scan some real Fire Pokémon to see their power in action!",requireScanNext: true),
            //Scan Next
            //Aside: Water Pokémon hide in the cold undersea. They stay calm and flowing with steady power.
            new DialogueLine("Professor Oak", "Wonderful! Turn to page 3 and scan some real Water Pokémon to explore where they live.",requireScanNext: true),
            //Scan Next
            //Aside: Grass Pokémon live in green forests. They’re great at defending and move a little slower.
            new DialogueLine("Professor Oak", "Excellent! Turn to page 4 and scan some real Grass Pokémon to learn how they protect nature.",requireScanNext: true),
            //Scan Next
            //Aside: Dragon Pokémon have wings and fly in the high mountains. They’re fast and full of mystery.
            new DialogueLine("Professor Oak", "Hmm… it looks like page 5 is broken! The Dragon Pokémon data seems damaged."),
            new DialogueLine("Professor Oak", "Let’s explore and fix it later, once we do, we’ll uncover all their secrets!"),

            new DialogueLine("Professor Oak", "To catch them, CheckBot need the right Poké Ball for each kind."),
            new DialogueLine("Professor Oak", "But how can CheckBot know what kind they are? Look at these Clue Cards!"),
            new DialogueLine("Professor Oak", "How strong, fast, and tough they are, whether they have wings, and how hot or high their home is.",singleSpriteKey:"clueCard"),
            //Clue Cards appear in order
            new DialogueLine("Professor Oak", "Use these clues to help CheckBot guess what kind each Pokémon is."),
            new DialogueLine("Professor Oak", "Catch as many Pokémon as possible and become a Pokémon Master!"),
            new DialogueLine("Professor Oak", "Now please turn to page 6. This is my research notebook."),
            new DialogueLine("Professor Oak", "I want to know what you think about how robots make choices."),
            new DialogueLine("Professor Oak", "Please answer the questions and choose a number to show how sure you are."),
            new DialogueLine("Professor Oak", "There are no right or wrong answers, so just do your best!"),
            //Give them time to answer before entering Zone 1
        };

        // --- Zone 1: Clearview Meadow ---
        allDialogues["ClearviewMeadow"] = new List<DialogueLine>
        {
            //Click start to begin
            new DialogueLine("Professor Oak", "Hi, young Trainer! Welcome to Clearview Meadow—your journey begins here!"),
            new DialogueLine("Professor Oak", "But there's trouble… wildfires are spreading fast, and we need to stop them!"),
            new DialogueLine("Professor Oak", "Your mission is to find Fire Pokémon. Use Clue Cards to create a Fire Plan."),
            new DialogueLine("Trainer", "I'm ready, Professor Oak! Let's stop the fire with Checkbot together!")
        };

        // --- Zone 2: Azure Coast ---
        allDialogues["AzureCoast"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to Azure Coast, the next step of your journey!"),
            new DialogueLine("Professor Oak", "A sea storm is coming and different Pokémon are gathering for safety."),
            new DialogueLine("Professor Oak", "Make a Master Plan for all types to tell them apart and keep the peace."),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll look through the Guidebook and make the best plan!")
        };

        // --- Zone 3: Whispering Woods ---
        allDialogues["WhisperingWood"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to Whispering Woods!"),
            new DialogueLine("Professor Oak", "The last mission was tricky, wasn’t it? I’ve build something new for you!"),
            new DialogueLine("Professor Oak", "I upgraded CheckBot into ChompBot — stronger, smarter, and hungry to learn!",singleSpriteKey:"chompBot"),
            new DialogueLine("Professor Oak", "This time, no more Clue Cards. ChompBot learns by “eating” Package Cards!"),
            new DialogueLine("Professor Oak", "Each Package Card shows a Pokémon package — Fire, Water, Grass, or Dragon."),
            new DialogueLine("Professor Oak", "Now, here is your challenge. For each type, you will see two packages."),
            new DialogueLine("Professor Oak", "One comes from my pure collection."),
            new DialogueLine("Professor Oak", "The other was found in the wild — it might be broken or mixed up.",singleSpriteKey:"dataSet"),
            new DialogueLine("Professor Oak", "Be careful! If ChompBot eats the wrong one, it might get confused."),
            new DialogueLine("Professor Oak", "Your task is to find all my pure collection and test ChompBot’s new power!"),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll help ChompBot learn the right way!")
        };

        // --- Zone 4: Sunrise Desert ---
        allDialogues["SunriseDesert"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to the Sunrise Desert! You’re near the end of your journey!"),
            new DialogueLine("Professor Oak", "The desert is hot and wide, and your backpack has little space left."),
            new DialogueLine("Professor Oak", "Choose four Package Cards wisely and feed them to ChompBot!"),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll pick the best combo and make ChompBot unstoppable!")
        };

        // --- Zone 5: Astral Summit ---
        allDialogues["AstralSummit"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to Sky Peak, the final step of your journey!"),
            new DialogueLine("Professor Oak", "Floating temples fill the skies, and powerful Pokémon await within."),
            new DialogueLine("Professor Oak", "This time, you must decide, trust CheckBot’s clear rules or ChompBot’s wild learning?"),
            new DialogueLine("Professor Oak", "Use your Robot to sense their types and form the right team to win!"),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll show what we’ve learned together and crush them all!")
        };

        // --- NEW: Game End Dialogue ---
        allDialogues["GameEnd"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "You did it! What an incredible journey, Trainer!"),
            new DialogueLine("Professor Oak", "You've completed the Novara expedition and taught your Robot so much."),
            new DialogueLine("Professor Oak", "For my final research notes, I'd love to see what you think now that you're an expert."),
            new DialogueLine("Professor Oak", "In the 'After Adventure' column, please answer based on what you know now."),
            new DialogueLine("Professor Oak", "It's okay to pick the same answer or to change your mind—just choose what you think is best."),
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