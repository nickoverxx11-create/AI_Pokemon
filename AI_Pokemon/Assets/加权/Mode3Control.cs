using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Mode3Control : MonoBehaviour
{
    public static Mode3Control Instance;
    private void Awake()
    {
        Instance = this;
    }

    public Transform[] roots;

    public Image[] iconSprs;

    void Start()
    {
        //clearBtn.onClick.AddListener(delegate {
        //    foreach (var item in roots)
        //    {
        //        if (item.childCount > 0)
        //        {
        //            Destroy(item.GetChild(0).gameObject);
        //        }
        //    }
        //});
        CheckSpr();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddCard(Transform trs, GameObject g)
    {
        if (trs.childCount > 0)
        {
            return;
        }

        if (g.transform.parent.name != "dragRoot")
        {
            return;
        }

        var item = Instantiate(g, trs);
        item.transform.localPosition = Vector3.zero;
        item.AddComponent<CloseOpenChild>();
        CheckSpr();

    }

    public void CheckSpr()
    {
        StartCoroutine(WaitTime());
    }
    IEnumerator WaitTime()
    {
        yield return new WaitForSeconds(0.1f);
        foreach (var item in iconSprs)
        {
            item.color = Color.white;
            item.GetComponent<DraggableItemMode3>().enabled = true;
        }
        foreach (var item in roots)
        {
            if (item.childCount > 0)
            {
                foreach (var item2 in iconSprs)
                {
                    if (item2.sprite == item.GetChild(0).GetComponent<Image>().sprite)
                    {
                        item2.color = Color.gray;
                        item2.GetComponent<DraggableItemMode3>().enabled = false;
                    }
                }
            }
        }
    }


}
