using System;
using System.Collections;
using System.Collections.Generic;
using toio.Samples.Sample_Sensor;
using toio.Simulator;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    [Header("Scene GameObjects")]
    public GameObject meadowScene;
    public GameObject azureCoastScene;
    public GameObject whisperingWoodScene;
    public GameObject sunriseDesertScene;
    public GameObject astralSummitScene;

    [Header("InGame UI")]
    public Text sceneNameText;     //for in game
    public CanvasGroup inGameUIGroup; 
    
    [Header("Intro UI")]
    public CanvasGroup introGroup;  
    public Text sceneNameBigText;       //for intro

    [Header("Dialogue Bubbles")] 
    public Image bubbleAnimImage;
    public GameObject  professorAvatar;
    public CanvasGroup professorBubble;
    public Text      professorText;
    public GameObject  studentAvatar;
    public CanvasGroup studentBubble;
    public Text      studentText;
    public AudioSource audioSource;
    
    [Header("Optional Directors & Zooms")]
    public PlayableDirector meadowDirector;
    public AutoZoomIn azureCoastZoom;
    public PlayableDirector whisperingWoodDirector;
    public PlayableDirector sunriseDesertDirector;
    public AutoZoomIn astralSummitZoom;
    
    [Header("Pause Control")]
    public KeyCode resumeKey = KeyCode.Space;    // 空格继续
    public Button resumeButton; 
    private bool _resumeRequested = false;
    
    [Header("Other")]
    public float typingSpeed = 0.04f;
    
    private bool isIntroPlaying = false;

    private bool isStart = true;
    private Dictionary<string, Sprite[]> _spriteSequences = new Dictionary<string, Sprite[]>();
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
        var frames = Resources.LoadAll<Sprite>("UIImage/gifs/fireDragon");
        Array.Sort(frames, (a,b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
        _spriteSequences["UIImage/gifs/fireDragon"] = frames;
    }


    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(RequestResume);
        
    }
    
    public void StopAllSceneActivities()
    {
        // Stop all coroutines running on this script (like PlaySceneIntro, PlayDialogue, etc.)
        StopAllCoroutines();
        
        // Immediately hide all the intro and dialogue UI elements
        introGroup.gameObject.SetActive(false);
        professorAvatar.gameObject.SetActive(false);
        studentAvatar.gameObject.SetActive(false);
        
        // Reset the gatekeeper flag
        isIntroPlaying = false;
        
        Debug.Log("--- All SceneController activities stopped. ---");
    }

    public void UpdateScene(int newScene, Action onIntroComplete = null)
    {
        if (isIntroPlaying) return;
        isIntroPlaying = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusicForZone((GameZone)newScene);
        }
        
        meadowScene.SetActive(false);
        azureCoastScene.SetActive(false);
        whisperingWoodScene.SetActive(false);
        sunriseDesertScene.SetActive(false);
        astralSummitScene.SetActive(false);

        inGameUIGroup.gameObject.SetActive(false);
        if (meadowDirector) meadowDirector.Stop();
        if (azureCoastZoom) azureCoastZoom.enabled = false;
        if (whisperingWoodDirector) whisperingWoodDirector.Stop();
        if (sunriseDesertDirector) sunriseDesertDirector.Stop();
        if (astralSummitZoom) astralSummitZoom.enabled = false;

        string sceneName = "";
        string zoneKey = "";
        IEnumerator deferredAction = null;

        switch (newScene)
        {
            case 0:
                meadowScene.SetActive(true);
                sceneName = "Clearview Meadow";
                zoneKey = "ClearviewMeadow";
                if (meadowDirector) deferredAction = PlayTimeline(meadowDirector);
                break;
            case 1:
                azureCoastScene.SetActive(true);
                sceneName = "Azure Coast";
                zoneKey = "AzureCoast";
                if (azureCoastZoom) deferredAction = EnableZoom(azureCoastZoom);
                break;
            case 2:
                whisperingWoodScene.SetActive(true);
                sceneName = "Whispering Wood";
                zoneKey = "WhisperingWood";
                if (whisperingWoodDirector) deferredAction = PlayTimeline(whisperingWoodDirector);
                break;
            case 3:
                sunriseDesertScene.SetActive(true);
                sceneName = "Sunrise Desert";
                zoneKey = "SunriseDesert";
                if (sunriseDesertDirector) deferredAction = PlayTimeline(sunriseDesertDirector);
                break;
            case 4:
                astralSummitScene.SetActive(true);
                sceneName = "Astral Summit";
                zoneKey = "AstralSummit";
                if (astralSummitZoom) deferredAction = EnableZoom(astralSummitZoom);
                break;
            default:
                sceneName = "???";
                break;

        }

        sceneNameText.text = sceneName;
        StartCoroutine(PlaySceneIntro(sceneName, zoneKey, deferredAction, onIntroComplete));
    }

    private IEnumerator PlaySceneIntro(string title, string zoneKey, IEnumerator afterIntro, System.Action onIntroComplete)
    {
        
        sceneNameBigText.gameObject.SetActive(true);
        introGroup.gameObject.SetActive(true);
        introGroup.alpha = 0f;

        sceneNameBigText.text = title;
        sceneNameBigText.transform.localScale = Vector3.zero;
        SetTextAlpha(sceneNameBigText, 1f);

        yield return FadeCanvasGroup(introGroup, 0f, 1f, 1f);

        float scaleTime = 1f;
        float timer = 0f;
        while (timer < scaleTime)
        {
            float t = timer / scaleTime;
            sceneNameBigText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            timer += Time.deltaTime;
            yield return null;
        }
        sceneNameBigText.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(2f);

        yield return FadeTextAlpha(sceneNameBigText, 1f, 0f, 0.5f);
        sceneNameBigText.gameObject.SetActive(false);

        yield return PlayDialogue(zoneKey);

        yield return FadeCanvasGroup(introGroup, 1f, 0f, 1f);
        introGroup.gameObject.SetActive(false);

        if (title == "Clearview Meadow")
        {
            yield return Level1LabZone.Instance.StartLabZoneSequence(() =>
            {
                Debug.Log("Lab sequence completed!");
            });
        }
        else if (title == "Azure Coast")
        {
            yield return Level2LabZone.Instance.StartLabZoneSequence(() =>
            {
                Debug.Log("Lab sequence completed!");
            });
        }
        else if (title == "Whispering Wood")
        {
            yield return Level3LabZone.Instance.StartLabZoneSequence(() =>
            {
                Debug.Log("Lab sequence completed!");
            });
        }
        else if (title == "Sunrise Desert")
        {
            yield return Level4LabZone.Instance.StartLabZoneSequence(() =>
            {
                Debug.Log("Lab sequence completed!");
            });
        }
        else if (title == "Astral Summit")
        {
            yield return Level5LabZone.Instance.StartLabZoneSequence(() =>
            {
                Debug.Log("Lab sequence completed!");
            });
            AudioManager.Instance?.SetMusicPaused(true); 
            yield return PlayGameEndSequence();
        }

        if (afterIntro != null)
            yield return afterIntro;
        onIntroComplete?.Invoke();
        
        isIntroPlaying = false;

    }

    public IEnumerator PlayGameStartSequence()
    {
        sceneNameBigText.gameObject.SetActive(false);
        introGroup.gameObject.SetActive(true);
        introGroup.alpha = 0f;
        yield return FadeCanvasGroup(introGroup, 0f, 1f, 0.4f);

        // 1. Store the original typing speed.
        float originalSpeed = typingSpeed;
        // 2. Set a new, faster speed just for this dialogue. (0.01f is very fast)
        typingSpeed = 0.05f; 
        
        yield return PlayDialogue("GameStart");

        typingSpeed = originalSpeed;

        yield return FadeCanvasGroup(introGroup, 1f, 0f, 0.4f);
        introGroup.gameObject.SetActive(false);
    }
    
    public IEnumerator PlayGameEndSequence()
    {
        sceneNameBigText.gameObject.SetActive(false);
        introGroup.gameObject.SetActive(true);
        introGroup.alpha = 0f;
        yield return FadeCanvasGroup(introGroup, 0f, 1f, 0.4f);

        // 1. Store the original typing speed.
        float originalSpeed = typingSpeed;
        // 2. Set a new, faster speed just for this dialogue. (0.01f is very fast)
        typingSpeed = 0.05f; 
        
        yield return PlayDialogue("GameEnd");

        typingSpeed = originalSpeed;

        yield return FadeCanvasGroup(introGroup, 1f, 0f, 0.4f);
        introGroup.gameObject.SetActive(false);
    }

    
    private IEnumerator PlayTimeline(PlayableDirector director)
    {
        yield return null;
        director.Play();
    }

    private IEnumerator EnableZoom(AutoZoomIn zoomScript)
    {
        yield return null;
        zoomScript.enabled = true;
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float timer = 0f;
        group.alpha = from;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        group.alpha = to;
    }
    

    public void FadeInGameUI(float duration = 1f)
    {
        inGameUIGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(inGameUIGroup, 0f, 1f, duration));
    }
    

    public void FadeOutGameUI(float duration = 0.5f, System.Action onComplete = null)
    {
        StartCoroutine(_FadeOutGameUI(duration, onComplete));
    }
    
    private IEnumerator _FadeOutGameUI(float duration, System.Action onComplete)
    {
        yield return StartCoroutine(FadeCanvasGroup(inGameUIGroup, 1f, 0f, duration));
        inGameUIGroup.gameObject.SetActive(false);
        onComplete?.Invoke();
    }
    private IEnumerator FadeTextAlpha(Text text, float from, float to, float duration)
    {
        float timer = 0f;
        SetTextAlpha(text, from);
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, timer / duration);
            SetTextAlpha(text, alpha);
            yield return null;
        }
        SetTextAlpha(text, to);
    }

    private void SetTextAlpha(Text text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }

    private IEnumerator TypeSentence(string sentence, Text uiText, float speed)
    {
        uiText.text = "";
        foreach (char c in sentence)
        {
            uiText.text += c;
            yield return new WaitForSeconds(speed);
        }
    }

    private IEnumerator PlayDialogue(string zoneKey, Action onComplete = null)
    {
        professorAvatar.gameObject.SetActive(true); 
        studentAvatar.gameObject.SetActive(true);
        professorBubble.alpha = 0;
        studentBubble.alpha   = 0;
        professorText.text    = "";
        studentText.text      = "";
        var lines = GameDialogues.Instance.allDialogues[zoneKey];
        foreach (var dlg in lines)
        {
            HideAllBubblesAndText();

            if (dlg.speaker == "Professor Oak")
            {
                professorAvatar.SetActive(true);
                yield return ShowBubbleWithTyping(
                    professorBubble, professorText, dlg);
            }
            else
            {
                studentAvatar.SetActive(true);
                yield return ShowBubbleWithTyping(
                    studentBubble, studentText, dlg);
            }
            
        }
        
        HideAllBubblesAndText();
        professorAvatar.gameObject.SetActive(false); 
        studentAvatar.gameObject.SetActive(false);
        onComplete?.Invoke();
    }
    
    private void HideAllBubblesAndText()
    {
        professorBubble.alpha = 0;
        studentBubble.alpha   = 0;
        professorBubble.gameObject.SetActive(false);
        studentBubble.gameObject.SetActive(false);
        professorText.text = "";
        studentText.text   = "";
    }
    
    private IEnumerator ShowBubbleWithTyping(CanvasGroup bubble, Text uiText, DialogueLine dlg)
    {
        bubble.gameObject.SetActive(true);

        //uiText.text = "";
        uiText.text = dlg.line;
        yield return FadeCanvasGroup(bubble, 0f, 1f, 0.2f);
        
        if (dlg.voiceClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(dlg.voiceClip);
        }
        
        bool animationDone = false;
        if (!string.IsNullOrEmpty(dlg.spriteSequenceKey)
            && _spriteSequences.TryGetValue(dlg.spriteSequenceKey, out var seq))
        {
            bubbleAnimImage.gameObject.SetActive(true);
            StartCoroutine(PlaySpriteSequence(seq, 12f, () => {
                animationDone = true;
            }));
        }
        else
        {
            animationDone = true;
        }
        
        /*foreach (char c in dlg.line)
        {
            uiText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }*/
        
        
        if (dlg.voiceClip != null && audioSource != null)
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);  
        }
        
        yield return new WaitUntil(() => animationDone);

        if (dlg.pauseAfter)
        {
            resumeButton.gameObject.SetActive(true);
            AudioManager.Instance?.SetMusicPaused(true); 
            yield return WaitForResume();
            resumeButton.gameObject.SetActive(false);
            AudioManager.Instance?.SetMusicPaused(false); 
        }else if (dlg.waitAfterSeconds > 0f)
        {
            AudioManager.Instance?.SetMusicPaused(true); 
            yield return new WaitForSeconds(dlg.waitAfterSeconds);
            AudioManager.Instance?.SetMusicPaused(false); 
        }
        

        bubbleAnimImage.gameObject.SetActive(false);
        yield return FadeCanvasGroup(bubble, 1f, 0f, 0.2f);
        uiText.text = "";
        bubble.gameObject.SetActive(false);
    }
    
    private IEnumerator PlaySpriteSequence(Sprite[] frames, float fps, Action onComplete)
    {
        float delay = 1f / fps;
        for (int i = 0; i < frames.Length; i++)
        {
            bubbleAnimImage.sprite = frames[i];
            yield return new WaitForSeconds(delay);
        }
        onComplete?.Invoke();
    }

    private IEnumerator WaitForResume()
    {
        _resumeRequested = false;
        
        while (true)
        {
            if (_resumeRequested) break;
            if (Input.GetKeyDown(resumeKey)) break;
            if (Input.GetMouseButtonDown(0)) break;
            yield return null;
        }
        
    }
    private void RequestResume()
    {
        _resumeRequested = true;
    }
    

}
