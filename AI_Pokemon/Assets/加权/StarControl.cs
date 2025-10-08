using UnityEngine;
using UnityEngine.UI;

public class StarControl : MonoBehaviour
{
    public Transform[] starts;
    public int starNum = 0; // (0-3)
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var item in starts)
        {
            item.GetChild(0).gameObject.SetActive(false);
        }
        starNum = 0;


        if (starts != null)
        {
            for (int i = 0; i < starts.Length; i++)
            {
                // Get or add Button component to the star Image GameObject
                Button starButton = starts[i].GetComponent<Button>();
                if (starButton == null) starButton = starts[i].gameObject.AddComponent<Button>();

                int index = i; // Capture index for lambda expression
                starButton.onClick.RemoveAllListeners(); // Clear previous listeners
                starButton.onClick.AddListener(() => OnStarClick(index));
            }
        }
    }
    void OnStarClick(int index) 
    {
        int clickedStarValue = index + 1; 

        //if (clickedStarValue == starNum)
        //{
        //    starNum = clickedStarValue - 1; 
        //}
        //else
        //{
            starNum = clickedStarValue; 
        //}

        starNum = Mathf.Clamp(starNum, 0, starts.Length);


        if (starts == null) return;
        foreach (var item in starts)
        {
            item.GetChild(0).gameObject.SetActive(false);
        }
        for (int i = 0; i < starNum; i++)
        {
            starts[i].GetChild(0).gameObject.SetActive(true);

        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
