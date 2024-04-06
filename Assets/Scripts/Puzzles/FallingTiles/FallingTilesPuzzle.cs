using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingTilesPuzzle : MonoBehaviour
{
    int currentTileIndex = 0;
    public GameController gameController;
    public int CheckpointIndex;
    public List<GameObject> Tiles1 = new List<GameObject>();
    public List<GameObject> Tiles2 = new List<GameObject>();
    public List<GameObject> Tiles3 = new List<GameObject>();
    public List<GameObject> Tiles4 = new List<GameObject>();
    public List<GameObject> Tiles5 = new List<GameObject>();

    public List<FallingTile> AllTiles = new List<FallingTile>();

    public void SetTileColours()
    {
        // Setup colours for player A
        if (gameController.playerTag == "A")
        {
            foreach (GameObject t in Tiles1) t.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.cyan);
            foreach (GameObject t in Tiles2) t.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.yellow);
            foreach (GameObject t in Tiles3) t.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.magenta);
            foreach (GameObject t in Tiles4) t.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.red);
            foreach (GameObject t in Tiles5) t.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.blue);
        }
    }

    public void OnKeyTileEnter(int i)
    {
        if (i != currentTileIndex + 1)
        {
            foreach (var t in AllTiles) t.DropTile();
        }
        else
        {
            currentTileIndex = i;
        
            if (currentTileIndex == 5)
            {
                gameController.OnCheckpointReached(CheckpointIndex);
            }
        }
    }

    public void ResetTiles()
    {
        currentTileIndex = 0;
        foreach (var t in AllTiles) t.ResetTile();
    }
}
