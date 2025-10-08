using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeGameMode : MonoBehaviour
{
    public static ChangeGameMode Instance;
    private void Awake()
    {
        Instance = this;
    }
    public GameObject camera1;
    public Button adventureBtn;
    public Button trainAiBtn;
    public GameObject mode1;
    public GameObject mode2;
    void Start()
    {
        camera1.SetActive(true);
        mode1.SetActive(true);
        mode2.SetActive(false);

        adventureBtn.onClick.AddListener(ChangeMode1);
        trainAiBtn.onClick.AddListener(ChangeMode2);
    }

    public void CloseBtn()
    {
        adventureBtn.enabled = false;
        trainAiBtn.enabled = false;
        trainAiBtn.transform.GetChild(1).gameObject.SetActive(true);
    }
    public void OpenBtn()
    {
        trainAiBtn.transform.GetChild(1).gameObject.SetActive(false);
        adventureBtn.enabled = true;
        trainAiBtn.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeMode1()
    {
        camera1.SetActive(true);
        mode1.SetActive(true);
        mode2.SetActive(false);
        adventureBtn.GetComponent<Image>().color = Color.black;
        adventureBtn.transform.GetChild(0).GetComponent<Text>().color = Color.white;
        trainAiBtn.GetComponent<Image>().color = Color.white;
        trainAiBtn.transform.GetChild(0).GetComponent<Text>().color = Color.black;
    }
    public void ChangeMode2()
    {
        camera1.SetActive(false);
        mode1.SetActive(false);
        mode2.SetActive(true);
        adventureBtn.GetComponent<Image>().color = Color.white;
        adventureBtn.transform.GetChild(0).GetComponent<Text>().color = Color.black;
        trainAiBtn.GetComponent<Image>().color = Color.black;
        trainAiBtn.transform.GetChild(0).GetComponent<Text>().color = Color.white;
    }


}
