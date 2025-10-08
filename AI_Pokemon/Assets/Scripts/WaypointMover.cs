using UnityEngine;
using System.Collections;

public class WaypointMover : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 2f;
    public float rotateSpeed = 5f;
    public Animator animator;

    private int currentWaypointIndex = 0;
    private bool isMoving = false;


    public void MoveSteps(int steps)
    {
        if (!isMoving)
        {
            Debug.Log("Moving " + steps + " steps...");
            StartCoroutine(MoveToWaypoints(steps));
        }
    }

    private IEnumerator MoveToWaypoints(int steps)
    {
        isMoving = true;
        animator.SetBool("IsWalking", true);

        for (int i = 0; i < steps && currentWaypointIndex < waypoints.Length; i++)
        {
            Debug.Log("Moving to waypoint " + currentWaypointIndex);
            Transform target = waypoints[currentWaypointIndex];

            yield return RotateTowards(target);
            yield return MoveTowards(target);

            currentWaypointIndex++;
        }

        animator.SetBool("IsWalking", false);
        isMoving = false;
    }

    private IEnumerator RotateTowards(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        while (Quaternion.Angle(transform.rotation, lookRotation) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
            yield return null;
        }
    }

    private IEnumerator MoveTowards(Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * moveSpeed);
            yield return null;
        }
    }
}
