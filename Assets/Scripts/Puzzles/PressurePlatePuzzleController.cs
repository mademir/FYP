using System.Collections.Generic;
using UnityEngine;

public class PressurePlatePuzzleController : MonoBehaviour
{
    public int[,] path;
    public List<int[,]> paths;
    public List<GameObject> SolutionTables = new List<GameObject>();
    public List<GameObject> SymbolsTable = new List<GameObject>();

    public List<Material> SolutionMaterials = new List<Material>();

    // Written in the notation where the bottom row is the entrance
    int[,] path1 = {
            { 0, 0, 0, 1, 0, 0},
            { 0, 1, 1, 1, 0, 0},
            { 0, 1, 0, 0, 0, 0},
            { 0, 1, 1, 1, 1, 1},
            { 0, 0, 0, 0, 0, 1},
            { 0, 0, 0, 0, 0, 1}
    };

    int[,] path2 = {
            { 1, 0, 0, 0, 0, 0},
            { 1, 0, 0, 0, 0, 0},
            { 1, 1, 1, 1, 1, 0},
            { 0, 0, 0, 0, 1, 0},
            { 0, 1, 1, 1, 1, 0},
            { 0, 1, 0, 0, 0, 0}
    };

    int[,] path3 = {
            { 0, 0, 0, 0, 0, 1},
            { 1, 1, 1, 1, 1, 1},
            { 1, 0, 0, 0, 0, 0},
            { 1, 1, 1, 1, 0, 0},
            { 0, 0, 0, 1, 0, 0},
            { 0, 0, 0, 1, 0, 0}
    };

    int[,] path4 = {
            { 0, 0, 1, 0, 0, 0},
            { 0, 0, 1, 1, 0, 0},
            { 0, 0, 0, 1, 0, 0},
            { 0, 1, 1, 1, 0, 0},
            { 1, 1, 0, 0, 0, 0},
            { 1, 0, 0, 0, 0, 0}
    };

    int[,] clearPath = {
            { 1, 1, 1, 1, 1, 1},
            { 1, 1, 1, 1, 1, 1},
            { 1, 1, 1, 1, 1, 1},
            { 1, 1, 1, 1, 1, 1},
            { 1, 1, 1, 1, 1, 1},
            { 1, 1, 1, 1, 1, 1}
    };

    private void Start()
    {
        paths = new List<int[,]>() {path1, path2, path3, path4};
        path = clearPath; // Set the default path to clear path for Player B's client side
    }

    public void SetupPuzzle()
{
        // Random correct and incorrect symbol combinations
        List<List<string>> symbols = new List<List<string>>(); //First is correct
        for (int tries = 0; tries < 10; tries++)
        {
            symbols = new List<List<string>>();
            for (int i = 0; i < 4; i++)
            {
                symbols.Add(new List<string>());
                for (int j = 0; j < 4; j++)
                {
                    symbols[i].Add(j.ToString() + Random.Range(0, 4).ToString());
                }
            }

            // If no combination is repeated, can exit loop.
            bool repeated = false;
            for (int i = 0; i < 4; i++) // Loop through combinations
            {
                for (int j = 0; j < 4; j++) // Loop through combinations
                {
                    if (i == j) continue;   // Do not compare the combination against itself
                    bool same = true;
                    for (int k = 0; k < 4; k++) // Go through each symbol
                    {
                        if (symbols[i][k] != symbols[j][k])
                        {
                            same = false;
                            break;
                        }
                    }
                    if (same)
                    {
                        repeated = true;
                        break;
                    }
                }
            }
            if (!repeated) break;
        }

        int correctPathIndex = Random.Range(0, paths.Count);
        path = paths[correctPathIndex];

        List<int[,]> tempPaths = new List<int[,]>(); 
        foreach (var p in paths) tempPaths.Add(p);
        var tempSolutionTables = new List<GameObject>();
        foreach (var t in SolutionTables) tempSolutionTables.Add(t);

        int symbolIncorrectCombinationIndex = 1; // Start from 1 as 0 is correct
        
        for (int i = paths.Count; i > 0; i--)
        {
            int solutionTableIndex = Random.Range(0, tempSolutionTables.Count);
            var table = tempSolutionTables[solutionTableIndex];
            var tableScript = table.GetComponent<PressurePlateSolutionTable>();

            int ind = Random.Range(0, i);
            tableScript.SolutionTable.GetComponent<Renderer>().material = SolutionMaterials[paths.IndexOf(tempPaths[ind])];
            if (tempPaths[ind] == paths[correctPathIndex])
            {
                // Set the correct symbols on this table
                for (int n = 0; n < 4; n++)
                {
                    tableScript.Symbols[n].GetComponent<Renderer>().material = Resources.Load($"Symbols/Materials/{symbols[0][n]}", typeof(Material)) as Material;
                    SymbolsTable[n].GetComponent<Renderer>().material = Resources.Load($"Symbols/Materials/{symbols[0][n]}", typeof(Material)) as Material;
                    /*var mat = Resources.Load($"Symbols/Materials/{symbols[0][n]}", typeof(Material)) as Material;
                    Debug.LogError($"CRT Mat: {mat}\t material str: {symbols[0][n]}\ni:{i}, correctPathIndex: {correctPathIndex}, ind: {ind}, n:{n}, solutionTableIndex:{solutionTableIndex}");
                    tableScript.Symbols[n].GetComponent<Renderer>().material = mat;*/
                }
            }
            else
            {
                // Set random incorrect symbols. 
                for (int n = 0; n < 4; n++)
                {
                    tableScript.Symbols[n].GetComponent<Renderer>().material = Resources.Load($"Symbols/Materials/{symbols[symbolIncorrectCombinationIndex][n]}", typeof(Material)) as Material;
                    /*var mat = Resources.Load($"Symbols/Materials/{symbols[symbolIncorrectCombinationIndex][n]}", typeof(Material)) as Material;
                    Debug.LogError($"INCRT Mat: {mat}\t material str: {symbols[symbolIncorrectCombinationIndex][n]}\ni:{i}, correctPathIndex: {correctPathIndex}, ind: {ind}, n:{n}, solutionTableIndex:{solutionTableIndex}");
                    tableScript.Symbols[n].GetComponent<Renderer>().material = mat;*/
                }
                symbolIncorrectCombinationIndex++;
            }
            tempSolutionTables.Remove(table);
            tempPaths.Remove(tempPaths[ind]);
        }
    }
}
