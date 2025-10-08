using UnityEngine;
using UnityEngine.UI;

public class CloseOpenChild : MonoBehaviour
{
    Button btn;
    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(delegate {
            var item = transform.GetChild(0).gameObject;
            item.SetActive(!item.activeInHierarchy);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
