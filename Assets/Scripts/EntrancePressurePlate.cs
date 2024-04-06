using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntrancePressurePlate : MonoBehaviour
{
    public Animator animator;
    public Animator Door;
    public GameController gameController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && !gameController.PlayerTagsAssigned)
        {
            animator.GetComponentInParent<NetworkNode>().SetAnimationTrigger("TrDown", gameController.client, true);
            Door.GetComponentInParent<NetworkNode>().SetAnimationTrigger("TrOpen", gameController.client, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player" && !gameController.PlayerTagsAssigned)
        {
            animator.GetComponentInParent<NetworkNode>().SetAnimationTrigger("TrUp", gameController.client, true);
            Door.GetComponentInParent<NetworkNode>().SetAnimationTrigger("TrClose", gameController.client, true);
        }
    }
}
