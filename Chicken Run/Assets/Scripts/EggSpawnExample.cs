using UnityEngine;

/// <summary>
/// Example of how to spawn eggs using the EggSpawner.
/// Add this to a GameObject in your scene to test egg spawning.
/// </summary>
public class EggSpawnExample : MonoBehaviour
{
    [SerializeField] private GameObject chickPrefab;
    [SerializeField] private GameObject eggPrefab;

    void Start()
    {
        // Set the Chick prefab that eggs will spawn when hatching
        if (chickPrefab != null)
        {
            // You can also set this up through the Egg prefab inspector
        }
    }

    void Update()
    {
        // Press E to spawn an egg in front of the chicken
        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3 spawnPos = transform.position + Vector3.right * 3;
            GameObject egg = EggSpawner.SpawnEgg(spawnPos);
            Debug.Log($"Egg spawned at {spawnPos}");
        }

        // Press R to spawn a line of 5 eggs
        if (Input.GetKeyDown(KeyCode.R))
        {
            Vector3 spawnPos = transform.position + Vector3.right * 2;
            GameObject[] eggs = EggSpawner.SpawnEggPattern(spawnPos, 5, 1.5f);
            Debug.Log($"Spawned {eggs.Length} eggs");
        }
    }
}
