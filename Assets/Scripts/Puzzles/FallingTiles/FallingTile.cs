using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingTile : MonoBehaviour
{
    public int KeyTileIndex = -1;
    public FallingTilesPuzzle Puzzle;
    public GameObject Fallable;

    private void OnTriggerEnter(Collider other)
    {
        Fallable.GetComponent<NetworkNode>().PlayAudio("Audio/tile-step-in", Puzzle.gameController.client, true);
        if (KeyTileIndex > -1)
        {
            Puzzle.OnKeyTileEnter(KeyTileIndex);

            //Light up tile for player B
            if (Puzzle.gameController.playerTag == "B") Fallable.transform.Find("Colour").GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Fallable.GetComponent<NetworkNode>().PlayAudio("Audio/tile-step-out", Puzzle.gameController.client, true);
        if (KeyTileIndex == -1)
        {
            DropTile();
        }
    }

    public void DropTile()
    {
        Fallable.GetComponent<NetworkNode>().SetIsKinematic("false", Puzzle.gameController.client, true);
        Fallable.GetComponent<NetworkNode>().PlayAudio("Audio/tile-fall", Puzzle.gameController.client, true);
    }

    public void ResetTile()
    {
        Fallable.GetComponent<NetworkNode>().SetIsKinematic("true", Puzzle.gameController.client, true);
        Fallable.GetComponent<NetworkNode>().ResetLocalTransform(Puzzle.gameController.client, true);
        if (Puzzle.gameController.playerTag == "B") Fallable.transform.Find("Colour").GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
    }
}
