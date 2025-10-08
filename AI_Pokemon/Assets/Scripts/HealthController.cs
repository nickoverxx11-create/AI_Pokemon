using UnityEngine;

public class HealthController : MonoBehaviour
{
    public static HealthController Instance;
    private void Awake()
    {
        Instance = this;
    }
    public GameObject [] helaths;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HelathsInit();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void HelathsInit()
    {
        foreach (var item in helaths)
        {
            item.SetActive(true);
        }
    }

    public bool GetHurt()
    {
        foreach (var item in helaths)
        {
            if (item.activeInHierarchy)
            {
                item.SetActive(false);
                break;
            }
        }

        foreach (var item in helaths)
        {
            if (item.activeInHierarchy)
            {
                return true;
            }
        }
        return false;
    }
}
