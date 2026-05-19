# Egg & Chick Prefab System Setup Guide

## What I've Changed

I've updated the scripts to make the Egg & Chick system prefab-ready:

### 1. **EggController.cs**

- Added a new serialized field: `chickPrefab` - reference to the Chick prefab
- Modified `Hatch()` to **spawn the Chick prefab** instead of relying on an existing scene instance
- Automatically detects collision with spawned chick and triggers escape

### 2. **ChickController.cs**

- Auto-finds the Main Camera if none is assigned (using `Camera.main`)
- More portable - works in any scene now

### 3. **New: EggSpawner.cs**

- Static utility class for spawning eggs anywhere
- Usage: `EggSpawner.SpawnEgg(position)`
- Also supports patterns: `EggSpawner.SpawnEggPattern(position, count, spacing)`

### 4. **New: EggSpawnExample.cs**

- Example script showing how to use the spawner
- Press E to spawn single egg, R to spawn 5 eggs in a line

---

## Step-by-Step Setup

### Step 1: Create the Chick Prefab

1. In the Hierarchy, select the **Chick** GameObject
2. Drag it into `Assets/` (or create a Prefabs folder: `Assets/Prefabs/`)
3. Name it `Chick.prefab`
4. You can delete the Chick instance from the scene now (or keep it if you want it at startup)

### Step 2: Create the Egg Prefab

1. In the Hierarchy, select the **Egg** GameObject
2. Drag it into `Assets/Prefabs/` folder
3. Name it `Egg.prefab`
4. Now select the **Egg prefab** in the Project folder (not the scene instance)

### Step 3: Link the Chick Prefab to Egg Prefab

1. In the Inspector of the **Egg.prefab**, find the **EggController** script
2. In the **Egg Controller** component, drag the **Chick.prefab** into the `Chick Prefab` field
3. **Save** the prefab changes

### Step 4: (Optional) Set up Resources Folder for Auto-Loading

If you want `EggSpawner` to auto-load prefabs without manual setup:

1. Create: `Assets/Resources/Prefabs/`
2. Move your `Egg.prefab` to `Assets/Resources/Prefabs/Egg.prefab`
3. Now `EggSpawner.SpawnEgg()` will work without calling `SetEggPrefab()`

---

## Usage Examples

### Spawn a single egg at a position:

```csharp
Vector3 spawnPosition = new Vector3(5, 2, 0);
GameObject egg = EggSpawner.SpawnEgg(spawnPosition);
```

### Spawn multiple eggs in a line:

```csharp
Vector3 startPos = new Vector3(0, 1, 0);
GameObject[] eggs = EggSpawner.SpawnEggPattern(startPos, 5, 2f); // 5 eggs, 2 units apart
```

### Manual prefab setup (if not using Resources folder):

```csharp
public class MyGameManager : MonoBehaviour
{
    [SerializeField] private GameObject eggPrefab;

    void Start()
    {
        EggSpawner.SetEggPrefab(eggPrefab);

        // Now spawn eggs
        EggSpawner.SpawnEgg(new Vector3(3, 1, 0));
    }
}
```

### Add to a button or trigger:

```csharp
public void OnButtonPressed()
{
    EggSpawner.SpawnEgg(chickTransform.position + Vector3.right * 3);
}
```

---

## What Happens When You Spawn an Egg

1. ✅ Egg appears at the specified position
2. ✅ Chicken can move around and peck it
3. ✅ Each peck shows crack animation (5 frames)
4. ✅ After 5 pecks, egg hatches
5. ✅ **Chick prefab is spawned** at egg location
6. ✅ Chick waits ~1.5 seconds, then flies away
7. ✅ Chick auto-destroys when off-screen

---

## Notes

- The **Egg** and **Chick** can be deleted from the Main scene after creating prefabs (they were just for setup)
- You can have **multiple eggs spawned** at once - each works independently
- Each egg spawns its own chick when hatched
- The system is fully scriptable - spawn eggs from code anywhere in your game
