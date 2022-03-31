using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the game state
/// </summary>
public class GameManager : MonoBehaviour
{
    // TODO: CHECK IF ALL CURRENTLY PRIVATE FIELDS HAVE STAYED PRIVATE, CHECK NAMING CONVENTION.
    [SerializeField] private int gridLength;
    [SerializeField] private int gridWidth;
    [SerializeField] private GameObject asteroidPrefab;

    private void Start()
    {
        for (int i = 0; i < gridLength; i++)
        {
            for(int j = 0; j < gridWidth; j++)
            {
                Instantiate(asteroidPrefab, new Vector3(i - gridLength/2, j - gridWidth/2, 0), Quaternion.identity);
            }
        }
    }
}
