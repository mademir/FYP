using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlappyLever : MonoBehaviour
{
    public Client client;
    public GameObject PressEText;
    public List<GameObject> Flaps = new List<GameObject>();
    bool onDefaultPosition = true;
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player" && Input.GetKeyDown(KeyCode.E))
        {
            // Animate lever and alternating flaps 
            GetComponentInChildren<NetworkNode>().SetAnimationTrigger(onDefaultPosition ? "TrToRight" : "TrToLeft", client, true);
            onDefaultPosition = !onDefaultPosition;

            for (int i = 0; i < Flaps.Count; i++)
            {
                Flaps[i].GetComponent<NetworkNode>().SetAnimationTrigger(i%2 == (onDefaultPosition ? 0 : 1) ? "TrUp" : "TrDown", client, true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            PressEText.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            PressEText.SetActive(false);
        }
    }
}
