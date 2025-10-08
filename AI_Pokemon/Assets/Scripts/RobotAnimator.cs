using UnityEngine;

public class RobotAnimator : MonoBehaviour
{
    public Animator animator;

    public void PlayWalk()
    {
        animator.SetBool("IsWalking", true);
    }

    public void StopWalk()
    {
        animator.SetBool("IsWalking", false);
    }
}