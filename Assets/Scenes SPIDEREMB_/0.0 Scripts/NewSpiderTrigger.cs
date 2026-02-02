using UnityEngine;

public class SpiderAnimationTrigger : MonoBehaviour
{
    public Animator animator;

    void Update()
    {
        // Bewegung abfragen (WASD oder Pfeiltasten)
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        bool isWalking = (Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveZ) > 0.01f);

        animator.SetBool("IsWalking", isWalking);
    }
}
