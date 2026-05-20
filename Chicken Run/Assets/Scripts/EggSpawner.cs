using UnityEngine;

/// <summary>
/// Utility class for spawning Egg prefabs at runtime.
/// Usage: EggSpawner.SpawnEgg(new Vector3(5, 2, 0));
/// </summary>
public class EggSpawner : MonoBehaviour
{
    private static GameObject eggPrefab;

    /// <summary>
    /// Spawns an egg at the specified world position.
    /// Call this after setting the prefab with SetEggPrefab() or make sure it's in Resources/Prefabs/
    /// </summary>
    public static GameObject SpawnEgg(Vector3 position)
    {
        if (eggPrefab == null)
        {
            // Try to load from Resources folder
            eggPrefab = Resources.Load<GameObject>("Prefabs/Egg");

            if (eggPrefab == null)
            {
                Debug.LogError("Egg prefab not found! Set it using EggSpawner.SetEggPrefab(prefab) or place it in Resources/Prefabs/Egg.prefab");
                return null;
            }
        }

        // Ensure egg spawns at z=-1 (in front)
        position.z = -1f;
        GameObject egg = Instantiate(eggPrefab, position, Quaternion.identity);
        return egg;
    }

    /// <summary>
    /// Set the egg prefab to use for spawning. Call this once during initialization.
    /// </summary>
    public static void SetEggPrefab(GameObject prefab)
    {
        eggPrefab = prefab;
    }

    /// <summary>
    /// Spawns multiple eggs in a pattern
    /// </summary>
    public static GameObject[] SpawnEggPattern(Vector3 startPosition, int count, float spacing = 2f)
    {
        GameObject[] eggs = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            eggs[i] = SpawnEgg(startPosition + new Vector3(i * spacing, 0, 0));
        }
        return eggs;
    }
}
