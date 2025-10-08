using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JiaQuanController : MonoBehaviour
{
    public static JiaQuanController Instance;

    private void Awake()
    {
        Instance = this;
    }
    public GameObject cardPfb;//Weight card prefab template
    public Transform cardRoot;//Generated node parent
    int cardNum=0;

    public Transform[] leftCards;//Cards on the left
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCard(Transform trs)
    {
        if (cardNum>=4)
        {
            return;
        }
        cardNum++;
        var item = Instantiate(cardPfb, cardRoot);
        item.GetComponent<Image>().color = trs.GetComponent<Image>().color;
        item.transform.GetChild(0).GetComponent<Image>().sprite = trs.GetChild(1).GetComponent<Image>().sprite;
        item.transform.GetChild(1).GetComponent<Text>().text = trs.GetChild(2).GetComponent<Text>().text;
    }


    public void RemoveCard(Transform trs)
    {
        foreach (var item in leftCards)
        {
            if (trs.GetChild(1).GetComponent<Text>().text == item.GetChild(2).GetComponent<Text>().text)
            {
                cardNum--;
                item.GetComponent<DraggableItem>().enabled = true;
                return;
            }
        }
    }
}