using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    public TMP_Text textMeshPro;
    public string fullText;
    public float delay = 0.1f;


    private void OnEnable()
    {
        fullText = textMeshPro.text;
        StartCoroutine(ShowTextWithTypewriterEffect());
    }
    private void Start()
    {
    }
    

    IEnumerator ShowTextWithTypewriterEffect()
    {
        textMeshPro.text = "";
        foreach (char c in fullText)
        {
            textMeshPro.text += c;
            yield return new WaitForSeconds(delay);
        }
    }
}