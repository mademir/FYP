using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingTilesSpikes : MonoBehaviour
{
    public FallingTilesPuzzle Puzzle;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            collision.gameObject.GetComponent<PlayerController>().Die();
            Puzzle.ResetTiles();
        }
    }
}
