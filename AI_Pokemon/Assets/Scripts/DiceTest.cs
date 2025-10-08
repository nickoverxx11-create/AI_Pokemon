using UnityEngine;

public class DiceTest : MonoBehaviour
{
    public WaypointMover mover;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            int roll = Random.Range(1, 7);
            Debug.Log("Dice rolled: " + roll);
            mover.MoveSteps(roll);
        }
    }
}