using UnityEngine;

using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int currentStep = 0;
    public bool isMoving = false;
    public int totalSteps = 30; 

    public SceneScroller scroller;
    public RobotAnimator robot;

    private void Awake()
    {
        Instance = this;
    }

    public void MoveSteps(int steps)
    {
        if (isMoving) return;

        StartCoroutine(MoveCoroutine(steps));
    }

    private System.Collections.IEnumerator MoveCoroutine(int steps)
    {
        isMoving = true;
        robot.PlayWalk();

        for (int i = 0; i < steps; i++)
        {
            if (currentStep >= totalSteps - 1) break;

            yield return scroller.ScrollOneStep();
            currentStep++;
        }

        robot.StopWalk();
        isMoving = false;

        EventManager.Instance.TriggerEventAt(currentStep);
    }
}
