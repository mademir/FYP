using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlappySpikes : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            collision.gameObject.GetComponent<PlayerController>().Die();
        }
    }
}
