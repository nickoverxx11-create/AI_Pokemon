using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


public enum Lang { EN, DE }

public enum AsideKind
{
    None,
    Action,
    Info
}

public static class Language
{
    public static Lang Current { get; private set; } = Lang.EN;
    public static event Action OnChanged;
    public static bool IsGerman => Current == Lang.DE;

    public static void Set(Lang lang)
    {
        if (Current == lang) return;
        Current = lang;
        OnChanged?.Invoke();
    }
}

// A simple class to hold one line of dialogue, including who is speaking.
[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string line;
    public string germanLine;

    public AudioClip voiceClip;
    public string spriteSequenceKey;
    public string singleSpriteKey;
    [FormerlySerializedAs("pauseAfter")] public bool requireScanNext;
    public float waitAfterSeconds = 0f;

    [Header("aside")]
    public string asideText;        
    public string asideGermanText;  
    public AsideKind asideKind = AsideKind.Info; 
    private bool isGerman;
    public bool sceneSwitch;

    public DialogueLine(string speaker, string line, string germanLine, string asideText = null,
        string asideGermanText = null, string singleSpriteKey = null, string spriteSequenceKey = null,
        bool requireScanNext = false, bool sceneSwitch = false,  AsideKind asideKind = AsideKind.Info  )

    {
        this.speaker = speaker;
        this.line = line;
        this.germanLine = germanLine;
        this.asideText = asideText;
        this.asideGermanText = asideGermanText;
        this.singleSpriteKey = singleSpriteKey;
        this.spriteSequenceKey = spriteSequenceKey;
        this.requireScanNext = requireScanNext;
        this.sceneSwitch = sceneSwitch;
        this.asideKind = asideKind;
    }

    
}


public class GameDialogues : MonoBehaviour
{
    public static GameDialogues Instance { get; private set; }
    
    [Header("Professor Voice Clips")]
    public AudioClip[] professorClips;
    
    public Dictionary<string, List<DialogueLine>> allDialogues;

    [Header("Button Image")]
    [SerializeField] private Image targetImage;

    [Header("Sprites")]
    [SerializeField] private Sprite englishSprite;
    [SerializeField] private Sprite germanSprite;

    [Header("default language")]
    [SerializeField] private bool startGerman = false;
    
  
        
    private bool isGerman;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isGerman = startGerman;
        ApplyVisual();
        Language.Set(isGerman ? Lang.DE : Lang.EN);
        allDialogues = new Dictionary<string, List<DialogueLine>>();

        // --- NEW: Game Start Dialogue ---
        allDialogues["GameStart"] = new List<DialogueLine>
        {
            //Click start to begin
            //new DialogueLine("", "", ""),
            new DialogueLine("Professor Oak", "Hi, young Trainer! I'm Professor Oak.", "Hallo, junger Trainer! Ich bin Professor Oak."),
            new DialogueLine("Professor Oak", "I'm glad you're here to help my Pokémon research.", "Ich bin froh, dass du hier bist, um meine Pokémon-Forschung zu unterstützen."),
            new DialogueLine("Professor Oak", "This is your friend CheckBot — he’ll help you find Pokémon!", "Das ist dein Freund CheckBot – er wird dir helfen, Pokémon zu finden!", singleSpriteKey:"checkBot"),
            new DialogueLine("Professor Oak", "In the land of Novara, there are four kinds of Pokémon.", "Im Land Novara gibt es vier Arten von Pokémon."),
            new DialogueLine("Professor Oak", "Your mission is to catch Pokémon and challenge the Boss Pokémon.", "Deine Mission ist es, Pokémon zu fangen und die Boss-Pokémon herauszufordern."),
            new DialogueLine("Professor Oak", "Please open the guidebook now to learn more about them.", "Bitte öffne jetzt das Handbuch, um mehr über sie zu erfahren."),
            
            //Click Next
            //Aside: Fire Pokémon love the heat! They’re brave and strike fast, living in the hottest places.
            new DialogueLine("Professor Oak", "Great! Now turn to page Fire and scan some real Fire Pokémon to see their power in action!", "Großartig! Blättere nun zu Seite Feuer und scanne einige echte Feuer-Pokémon, um ihre Kraft in Aktion zu sehen!",
                asideText: "Now please open the Pokémon Guidebook", requireScanNext: true, asideKind: AsideKind.Action ),
            //Click Next
            //Aside: Water Pokémon hide in the cold undersea. They stay calm and flowing with steady power.
            new DialogueLine("Professor Oak", "Wonderful! Turn to page Water and scan some real Water Pokémon to explore where they live.", "Wunderbar! Blättere zu Seite Wasser und scanne einige echte Wasser-Pokémon, um zu entdecken, wo sie leben.", 
                asideText:"Fire Pokémon love the heat! They’re brave and strike fast, living in the hottest places.", requireScanNext: true, asideKind: AsideKind.Info ),
            //Click Next
            //Aside: Grass Pokémon live in green forests. They’re great at defending and move a little slower.
            new DialogueLine("Professor Oak", "Excellent! Turn to page Grass and scan some real Grass Pokémon to learn how they protect nature.", "Ausgezeichnet! Blättere zu Seite Grass und scanne einige echte Gras-Pokémon, um zu lernen, wie sie die Natur schützen.", 
                asideText:"Water Pokémon hide in the cold undersea. They stay calm and flowing with steady power.", requireScanNext: true,asideKind: AsideKind.Info),
            //Click Next
            //Aside: Dragon Pokémon have wings and fly in the high mountains. They’re fast and full of mystery.
            new DialogueLine("Professor Oak", "Hmm… it looks like page Dragon is broken! The Dragon Pokémon data seems damaged.", "Hmm… es sieht so aus, als wäre Seite Drachen kaputt! Die Daten der Drachen-Pokémon scheinen beschädigt zu sein.", 
                asideText:"Grass Pokémon live in green forests. They’re great at defending and move a little slower.", requireScanNext: true, asideKind: AsideKind.Info),
            new DialogueLine("Professor Oak", "Let’s explore and fix it later, once we do, we’ll uncover all their secrets!", "Lass uns das später erforschen und reparieren, sobald wir das getan haben, werden wir all ihre Geheimnisse aufdecken!"),

            new DialogueLine("Professor Oak", "To catch them, CheckBot need the right Poké Ball for each kind.", "Um sie zu fangen, benötigt CheckBot für jede Art den richtigen Pokéball."),
            new DialogueLine("Professor Oak", "But how can CheckBot know what kind they are? Look at these Clue Cards!", "Aber woher soll CheckBot wissen, um welche Art es sich handelt? Schau dir diese Clue Cards an!"),
            new DialogueLine("Professor Oak", "How strong, fast, and tough they are, whether they have wings, and how hot or high their home is.", "Wie stark, schnell und zäh sie sind, ob sie Flügel haben und wie heiß oder hoch ihr Zuhause ist.", singleSpriteKey:"clueCard"),
            //Clue Cards appear in order
            new DialogueLine("Professor Oak", "Use these clues to help CheckBot guess what kind each Pokémon is.", "Benutze diese Hinweise, um CheckBot zu helfen, zu erraten, um welche Art von Pokémon es sich handelt."),
            new DialogueLine("Professor Oak", "Catch as many Pokémon as possible and become a Pokémon Master!", "Fange so viele Pokémon wie möglich und werde ein Pokémon-Meister!"),
            new DialogueLine("Professor Oak", "This is my research notebook.", "Das ist mein Forschungsnotizbuch."),
            new DialogueLine("Professor Oak", "I want to know what you think about how robots make choices.", "Ich möchte wissen, was du darüber denkst, wie Roboter Entscheidungen treffen."),
            new DialogueLine("Professor Oak", "Please answer the questions and choose a number to show how sure you are.", "Bitte beantworte die Fragen und wähle eine Zahl, um zu zeigen, wie sicher du dir bist."),
            new DialogueLine("Professor Oak", "There are no right or wrong answers, so just do your best!", "Es gibt keine richtigen oder falschen Antworten, also gib einfach dein Bestes!", sceneSwitch: true),
            //Give them time to answer before entering Zone 1
        };

        // --- Zone 1: Clearview Meadow ---
        allDialogues["ClearviewMeadow"] = new List<DialogueLine>
        {
            //Click start to begin
            new DialogueLine("Professor Oak", "Hi, young Trainer! Welcome to Clearview Meadow—your journey begins here!", "Hallo, junger Trainer! Willkommen in Clearview Meadow – deine Reise beginnt hier!"),
            new DialogueLine("Professor Oak", "But there's trouble… wildfires are spreading fast, and we need to stop them!", "Aber es gibt Ärger… Waldbrände breiten sich schnell aus, und wir müssen sie aufhalten!"),
            new DialogueLine("Professor Oak", "Your mission is to find Fire Pokémon. Use Clue Cards to create a Fire Plan.", "Deine Mission ist es, Feuer-Pokémon zu finden. Benutze die Clue Cards, um einen Feuer-Plan zu erstellen."),
            new DialogueLine("Trainer", "I'm ready, Professor Oak! Let's stop the fire with Checkbot together!", "Ich bin bereit, Professor Oak! Lass uns das Feuer gemeinsam mit Checkbot aufhalten!")
        };

        // --- Zone 2: Azure Coast ---
        allDialogues["AzureCoast"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to Azure Coast, the next step of your journey!", "Willkommen an der Azure Coast, dem nächsten Schritt deiner Reise!"),
            new DialogueLine("Professor Oak", "A sea storm is coming and different Pokémon are gathering for safety.", "Ein Seesturm zieht auf und verschiedene Pokémon versammeln sich in Sicherheit."),
            new DialogueLine("Professor Oak", "Make a Master Plan for all types to tell them apart and keep the peace.", "Erstelle einen Masterplan für alle Typen, um sie auseinanderzuhalten und den Frieden zu wahren."),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll look through the Guidebook and make the best plan!", "Ich bin bereit, Professor Oak! Ich werde das Handbuch durchsehen und den besten Plan machen!")
        };

        // --- Zone 3: Whispering Woods ---
        allDialogues["WhisperingWood"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to Whispering Woods!", "Willkommen im Whispering Woods!"),
            new DialogueLine("Professor Oak", "The last mission was tricky, wasn’t it? I’ve build something new for you!", "Die letzte Mission war knifflig, nicht wahr? Ich habe etwas Neues für dich gebaut!"),
            new DialogueLine("Professor Oak", "I upgraded CheckBot into ChompBot — stronger, smarter, and hungry to learn!", "Ich habe CheckBot zu ChompBot aufgerüstet – stärker, klüger und hungrig zu lernen!", singleSpriteKey:"chompBot"),
            new DialogueLine("Professor Oak", "This time, no more Clue Cards. ChompBot learns by “eating” Package Cards!", "Dieses Mal keine Clue Cards mehr. ChompBot lernt durch das „Essen“ von Paketkarten!"),
            new DialogueLine("Professor Oak", "Each Package Card shows a Pokémon package — Fire, Water, Grass, or Dragon.", "Jede Paketkarte zeigt ein Pokémon-Paket – Feuer, Wasser, Gras oder Drache."),
            new DialogueLine("Professor Oak", "Now, here is your challenge. For each type, you will see two packages.", "Hier ist deine Herausforderung. Für jeden Typ siehst du zwei Pakete."),
            new DialogueLine("Professor Oak", "One comes from my pure collection.", "Eines stammt aus meiner reinen Sammlung."),
            new DialogueLine("Professor Oak", 
                "The other package was tampered with by Meowth — some of the data may be wrong or mixed up.", 
                "Das andere Paket wurde von Mauzi durcheinandergebracht – einige Daten könnten falsch oder vermischt sein.", 
                singleSpriteKey:"dataSet"),

            new DialogueLine("Professor Oak", 
                "Be careful! If ChompBot eats Meowth’s messy data, it might learn the wrong things.", 
                "Sei vorsichtig! Wenn ChompBot Mauzis chaotische Daten frisst, könnte es falsche Dinge lernen."),
                
            new DialogueLine("Professor Oak", "Your task is to find all my pure collection and test ChompBot’s new power!", "Deine Aufgabe ist es, meine gesamte reine Sammlung zu finden und die neue Kraft von ChompBot zu testen!"),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll help ChompBot learn the right way!", "Ich bin bereit, Professor Oak! Ich werde ChompBot helfen, auf die richtige Weise zu lernen!")
        };

        // --- Zone 4: Sunrise Desert ---
        allDialogues["SunriseDesert"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to the Sunrise Desert! You’re near the end of your journey!", "Willkommen in der Sunrise Desert! Du bist kurz vor dem Ende deiner Reise!"),
            new DialogueLine("Professor Oak", "The desert is hot and wide, and your backpack has little space left.", "Die Wüste ist heiß und weit, und dein Rucksack hat nur noch wenig Platz."),
            new DialogueLine("Professor Oak", "Choose four Package Cards wisely and feed them to ChompBot!", "Wähle vier Paketkarten weise aus und füttere sie an ChompBot!"),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll pick the best combo and make ChompBot unstoppable!", "Ich bin bereit, Professor Oak! Ich werde die beste Kombination auswählen und ChompBot unaufhaltsam machen!")
        };

        // --- Zone 5: Astral Summit ---
        allDialogues["AstralSummit"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "Welcome to Sky Peak, the final step of your journey!", "Willkommen auf dem Sky Peak, dem letzten Schritt deiner Reise!"),
            new DialogueLine("Professor Oak", "Floating temples fill the skies, and powerful Pokémon await within.", "Schwebende Tempel füllen den Himmel, und mächtige Pokémon warten darin."),
            new DialogueLine("Professor Oak", "This time, you must decide, trust CheckBot’s clear rules or ChompBot’s wild learning?", "Dieses Mal musst du dich entscheiden, vertraust du den klaren Regeln von CheckBot oder dem wilden Lernen von ChompBot?"),
            new DialogueLine("Professor Oak", "Use your Robot to sense their types and form the right team to win!", "Benutze deinen Roboter, um ihre Typen zu erkennen und das richtige Team zum Gewinnen zu bilden!"),
            new DialogueLine("Trainer", "I’m ready, Professor Oak! I’ll show what we’ve learned together and crush them all!", "Ich bin bereit, Professor Oak! Ich werde zeigen, was wir zusammen gelernt haben, und sie alle vernichten!")
        };

        // --- NEW: Game End Dialogue ---
        allDialogues["GameEnd"] = new List<DialogueLine>
        {
            new DialogueLine("Professor Oak", "You did it! What an incredible journey, young Trainer!", "Du hast es geschafft! Was für eine unglaubliche Reise, junger Trainer!"),
            new DialogueLine("Professor Oak", "This is my research notebook again.", "Das ist wieder mein Forschungsnotizbuch."),
            new DialogueLine("Professor Oak", "Now what you think about how robots make choices after playing this game.", "Was denkst du jetzt darüber, wie Roboter Entscheidungen treffen, nachdem du dieses Spiel gespielt hast?"),
            new DialogueLine("Professor Oak", "Please answer the questions and choose a number to show how sure you are.", "Bitte beantworte die Fragen und wähle eine Zahl, um zu zeigen, wie sicher du dir bist."),
            new DialogueLine("Professor Oak", "There are no right or wrong answers, so just do your best!", "Es gibt keine richtigen oder falschen Antworten, also gib einfach dein Bestes!"),
            new DialogueLine("Professor Oak", "Thanks for being here to help my Pokémon research. See you next time!", "Danke, dass du hier warst, um meine Pokémon-Forschung zu unterstützen. Bis zum nächsten Mal!"),
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
    public void OnClickToggle()
    {
        isGerman = !isGerman;
        ApplyVisual();
        Language.Set(isGerman ? Lang.DE : Lang.EN);
        if (SceneController.Instance) SceneController.Instance.RefreshCurrentBubbleText();
    }

    private void ApplyVisual()
    {
        if (targetImage) targetImage.sprite = isGerman ? germanSprite : englishSprite;
    }
}