using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HintPanelController : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup hintGroup;
    public Text hintText;

    [Header("Timing")]
    public float showDuration = 1f;   
    public float fadeDuration = 0.3f;  

    private Coroutine _currentRoutine;

    // labId: 1 = Lab1, 2 = Lab2, 4 = Lab4
    private Dictionary<int, string[]> _hintsEN;
    private Dictionary<int, string[]> _hintsDE;

    private int _currentLabId = 0;      
    private int _currentHintIndex = -1; 
    private string _currentENText;    
    private string _currentDEText;  
    private void Awake()
    {
        InitHints();

        if (hintGroup != null)
        {
            hintGroup.alpha = 0f;
            hintGroup.gameObject.SetActive(false);
        }
    }

     private void OnEnable()
    {
        // 订阅语言切换事件
        Language.OnChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        Language.OnChanged -= OnLanguageChanged;
    }

    private void InitHints()
    {
        _hintsEN = new Dictionary<int, string[]>();
        _hintsDE = new Dictionary<int, string[]>();
        
        _hintsEN[1] = new[]
        {
            "Hint 1: Not sure where to start? Look at page 2 — it shows what makes a Pokémon a Fire type. You can test your plan and try again if it doesn’t work!",
            "Hint 2: Look at which Pokémon are not Fire types — if your plan catches them too, there might be a conflict! Try removing or changing one clue to fix it.",
            "Hint 3: You can pick 1 to 4 Clue Cards for your plan. Do you think using more cards always makes it better? Try and see!"
        };
        
        _hintsEN[2] = new[]
        {
            "Hint 1: Look at all 4 pages! Since the Dragon page is broken, use your imagination for dragons. If a clue isn’t clearly high or low for that type, it might not be needed in that plan.",
            "Hint 2: Because order matters now, put the most important clue first — it’s usually the one that makes this Pokémon type special!",
            "Hint 3: If two types share the same clue, make a trade-off. You can put that clue last in both plans, or keep it in one and remove it from the other. Try both ways and see what works best!"
        };
        
        _hintsEN[4] = new[]
        {
            "Hint 1: The safest way is to include one pure package for each type, keep everything balanced.",
            "Hint 2: The Big Clear Mix package covers all types at once — maybe it’s a smart shortcut?",
            "Hint 3: Even with the same combo, you might get different grades — can you guess why?"
        };
        
        _hintsDE[1] = new[]
        {
            "Hint 1: Nicht sicher, wo du anfangen sollst? Schau auf Seite 2 — dort steht, was ein Pokémon zum Fire type macht. Du kannst deinen Plan testen und es erneut versuchen, wenn er nicht funktioniert!",
            "Hint 2: Schau dir an, welche Pokémon keine Fire types sind — wenn dein Plan sie auch erwischt, gibt es vielleicht einen Konflikt! Versuche, einen Hinweis zu entfernen oder zu ändern, um das zu beheben.",
            "Hint 3: Du kannst 1 bis 4 Clue Cards für deinen Plan auswählen. Denkst du, dass mehr Karten den Plan immer besser machen? Probier es aus!"
        };
        
        _hintsDE[2] = new[]
        {
            "Hint 1: Sieh dir alle 4 Seiten an! Da die Dragon-Seite kaputt ist, benutze deine Vorstellung für Dragons. Wenn ein Hinweis nicht eindeutig „high“ oder „low“ für diesen Typ ist, wird er vielleicht nicht in diesem Plan benötigt.",
            "Hint 2: Da die Reihenfolge jetzt wichtig ist, setze den wichtigsten Hinweis nach vorne — meistens ist es der Hinweis, der diesen Pokémon-Typ besonders macht!",
            "Hint 3: Wenn zwei Typen denselben Hinweis teilen, musst du einen Kompromiss machen. Du kannst diesen Hinweis in beiden Plänen nach hinten setzen oder ihn in einem Plan behalten und im anderen entfernen. Probiere beides aus und schau, was besser funktioniert!"
        };
        
        _hintsDE[4] = new[]
        {
            "Hint 1: Am sichersten ist es, ein pure package für jeden Typ einzubauen, um alles ausgeglichen zu halten.",
            "Hint 2: Das Big Clear Mix package deckt alle Typen gleichzeitig ab — vielleicht ist das eine clevere Abkürzung?",
            "Hint 3: Selbst mit derselben Kombination kannst du unterschiedliche Noten bekommen — kannst du erraten, warum?"
        };
    }
    

    public void OnHint1Button()
    {
        ShowHint(1);
    }

    public void OnHint2Button()
    {
        ShowHint(2);
    }

    public void OnHint3Button()
    {
        ShowHint(3);
    }
    
    public void ShowHint(int index)
    {
        int labId = GetCurrentLabId();
        if (labId == 0)
        {
            Debug.LogWarning("HintPanel: current lab not recognized, no hints.");
            return;
        }

        int arrayIndex = index - 1;

        if (!_hintsEN.TryGetValue(labId, out var enArr) ||
            !_hintsDE.TryGetValue(labId, out var deArr))
        {
            Debug.LogWarning($"HintPanel: hints not fully configured for Lab {labId}");
            return;
        }

        if (arrayIndex < 0 || arrayIndex >= enArr.Length || arrayIndex >= deArr.Length)
        {
            Debug.LogWarning($"HintPanel: hint index {index} out of range for Lab {labId}");
            return;
        }
        
        _currentLabId      = labId;
        _currentHintIndex  = index;
        _currentENText     = enArr[arrayIndex];
        _currentDEText     = deArr[arrayIndex];
        
        string textToShow = Language.IsGerman ? _currentDEText : _currentENText;

        if (_currentRoutine != null)
            StopCoroutine(_currentRoutine);

        _currentRoutine = StartCoroutine(ShowHintRoutine(textToShow));
    }

    private int GetCurrentLabId()
    {
        if (SceneController.Instance == null)
            return 0;

        int sceneIdx = SceneController.Instance.CurrentSceneIndex;


        switch (sceneIdx)
        {
            case 0: return 1; // Clearview Meadow → Lab1
            case 1: return 2; // Azure Coast      → Lab2
            case 3: return 4; // Sunrise Desert   → Lab4
            default:
                return 0; 
        }
    }


    private void OnLanguageChanged()
    {
        if (hintGroup == null || hintText == null) return;
        if (!hintGroup.gameObject.activeSelf) return;
        if (_currentHintIndex < 1 || _currentLabId == 0) return;
        
        string newText = Language.IsGerman ? _currentDEText : _currentENText;
        hintText.text = newText;
    }
    

    private IEnumerator ShowHintRoutine(string text)
    {
        if (hintGroup == null || hintText == null)
            yield break;

        hintGroup.gameObject.SetActive(true);
        hintText.text = text;

        // 淡入
        yield return FadeCanvasGroup(hintGroup, 0f, 1f, fadeDuration);

        // 停留 showDuration 秒
        yield return new WaitForSeconds(showDuration);

        // 淡出
        yield return FadeCanvasGroup(hintGroup, 1f, 0f, fadeDuration);

        hintGroup.gameObject.SetActive(false);
        _currentRoutine = null;

        // 结束后可以清一下状态（可选）
        _currentLabId = 0;
        _currentHintIndex = -1;
        _currentENText = null;
        _currentDEText = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float timer = 0f;
        group.alpha = from;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }
}