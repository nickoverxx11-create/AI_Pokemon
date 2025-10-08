using UnityEngine;
using UnityEngine.UI;

public class Mode2Control : MonoBehaviour
{
    public static Mode2Control Instance;
    private void Awake()
    {
        Instance = this;
    }

    public Transform[] roots;
    public Button clearBtn;

    public GameObject jiaPfb;
    public GameObject jianPfb;

    void Start()
    {
        clearBtn.onClick.AddListener(delegate {
            foreach (var item in roots)
            {
                if (item.childCount > 0)
                {
                    Destroy(item.GetChild(0).gameObject);
                }
            }
        });
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddCard(Transform trs, int index)
    {
        if (trs.childCount > 0)
        {
            return;
        }
        if (index == 1)
        {
            var item = Instantiate(jiaPfb, trs);
            item.transform.localPosition = Vector3.zero;
        }
        else if (index == 2)
        {
            var item = Instantiate(jianPfb, trs);
            item.transform.localPosition = Vector3.zero;
        }
    }

}
