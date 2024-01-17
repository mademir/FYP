using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlatePuzzleController : MonoBehaviour
{
    // Written in the notation where the bottom row is the entrance
    public int[,] path = {
            { 0, 0, 0, 1, 0, 0},
            { 0, 1, 1, 1, 0, 0},
            { 0, 1, 0, 0, 0, 0},
            { 0, 1, 1, 1, 1, 1},
            { 0, 0, 0, 0, 0, 1},
            { 0, 0, 0, 0, 0, 1}
        };

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
