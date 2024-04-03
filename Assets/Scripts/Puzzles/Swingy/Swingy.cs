using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swingy : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            collision.gameObject.GetComponent<PlayerController>().Die();
        }
    }

    public void MakeInvisible()
    {
        foreach(Renderer renderer in gameObject.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
    }
}
