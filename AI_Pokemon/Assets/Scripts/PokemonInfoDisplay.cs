using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PokemonInfo
{
    public string Name;
    public string TypeEmoji;
    public Sprite  Sprite;   
    public string EmojiNameLine;
    public string[] DescriptionLines;

    public AudioClip SoundEffect;
    
    

    public PokemonInfo(string name, Sprite sprite, AudioClip sound, string typeEmoji, string emojiNameLine, params string[] descriptionLines)
    {
        Name = name;
        Sprite = sprite;
        TypeEmoji = typeEmoji;
        SoundEffect = sound;
        EmojiNameLine = emojiNameLine;
        DescriptionLines = descriptionLines;
    }
}

public class PokemonInfoDisplay : MonoBehaviour
{
    public static PokemonInfoDisplay Instance;

    [Header("Audio")]
    public AudioSource soundSource; // This will be our speaker

    [Header("UI")]
    public Image  guideImage;      
    public Text   titleText;        
    public Text   descriptionText;  
    
    public List<Sprite> sprites;    

    [Header("Sound Effects (MP3s)")]
    public AudioClip charizardSound;
    public AudioClip growlitheSound;
    public AudioClip gyaradosSound;
    public AudioClip wailmerSound;
    public AudioClip bayleefSound;
    public AudioClip leafeonSound;

    private Dictionary<string, PokemonInfo> symbolToPokemon;

    private bool _isOnCooldown = false;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        symbolToPokemon = new Dictionary<string, PokemonInfo>
        {
            { "×", new PokemonInfo("Charizard",sprites[0], charizardSound ,"🔥", "🔥 Charizard",
                "Charizard lives near hot volcanoes 🌋.",
                "It flies high and breathes big fire 🔥!",
                "Charizard hits hard 💥.") },

            { "÷", new PokemonInfo("Growlithe", sprites[1], growlitheSound, "🔥", "🔥 Growlithe 🐶",
                "Growlithe likes to stay on the ground 🏜️.",
                "It doesn't run fast 🦶, but it is brave ⚔️!",
                "It gets hurt easily if hit hard 🛡️❌.") },

            { "%", new PokemonInfo("Gyarados", sprites[2], gyaradosSound, "💧", "💧 Gyarados",
                "Gyarados swims in the deep sea 🌊.",
                "It jumps high and has wings 🕊️.",
                "It is very strong ⚔️ and hard to stop 🛡️.") },

            { "+", new PokemonInfo("Wailmer", sprites[3], wailmerSound,"💧", "💧 Wailmer 🐳",
                "Wailmer floats in the ocean 🌊.",
                "No one can hurt him 🛡️ — but he is harmless too ⚔️❌.",
                "He is big 😄, stays cool ❄️, and lives deep below the water 🌊.") },

            { "-", new PokemonInfo("Bayleef", sprites[4], bayleefSound, "🌿", "🌿 Bayleef 🦕",
                "Bayleef walks in sunny gardens 🌸.",
                "It moves slowly 🐌 but doesn’t fall down easily 🛡️.",
                "Bayleef loves the sun ☀️ and lives on high mountains 🏔️.") },

            { "=", new PokemonInfo("Leafeon", sprites[5], leafeonSound, "🌿", "🌿 Leafeon",
                "Leafeon lives in quiet forests 🌳.",
                "It is calm 😌 but can stand strong when needed 🛡️.",
                "Leafeon loves warm sun ☀️ but hides from fire 🔥.") }
        };
        guideImage.enabled = false;
    }
    
    public void Show(string symbol)
    {
        if (_isOnCooldown)
    {
        return;
    }
        if (!symbolToPokemon.TryGetValue(symbol, out var info)) return;
        
         if (info.SoundEffect != null && soundSource != null)
    {
        soundSource.PlayOneShot(info.SoundEffect);
    }

        guideImage.sprite    = info.Sprite;
        guideImage.enabled   = true;
        titleText.text       = $"{info.TypeEmoji} {info.Name}";
        descriptionText.text = string.Join("\n", info.DescriptionLines);
        
        StopAllCoroutines();
        StartCoroutine(HideAndResetCooldown(8f));
    }

    private IEnumerator HideAndResetCooldown(float duration)
{
    // 1. Immediately start the cooldown.
    _isOnCooldown = true;

    // 2. Wait for the specified duration.
    yield return new WaitForSeconds(duration);

    // 3. After the time is up, hide the UI.
    guideImage.enabled = false;
    titleText.text = "";
    descriptionText.text = "";
    
    // 4. IMPORTANT: Reset the cooldown flag so a new sound can be played.
    _isOnCooldown = false;
}
}

