using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PressurePlate : MonoBehaviour
{
    public Animator animator;
    public PressurePlatePuzzleController puzzleController;

    private void OnEnable()
    {
        puzzleController = transform.GetComponentInParent<PressurePlatePuzzleController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            animator.SetTrigger("TrDown");
            GetComponent<AudioSource>().Play();
            if (!checkPlateIsOnPath()) other.GetComponent<PlayerController>().Die();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            animator.SetTrigger("TrUp");
        }
    }

    private bool checkPlateIsOnPath()
    {
        int row = transform.GetSiblingIndex();
        int col = transform.parent.GetSiblingIndex();
        var res = puzzleController.path[col, row] == 1;
        //Debug.Log($"{row}, {col}: {res}");
        return res;
    }
}
