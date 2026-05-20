using UnityEngine;
using UnityEngine.Tilemaps;

public class GroundGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GroundTileRegistry tileRegistry;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Transform player;

    [Header("Screen Boundaries (as % from bottom)")]
    [SerializeField] private float minSurfacePercent = 0.15f;
    [SerializeField] private float maxSurfacePercent = 0.40f;

    [Header("Gap Constraints")]
    [SerializeField] private int minGapWidth = 2;
    [SerializeField] private int maxGapWidth = 5;

    [Header("Ground Constraints")]
    [SerializeField] private int minGroundWidth = 2;
    [SerializeField] private int maxHeightDifference = 3; // tiles

    [Header("Starting Platform")]
    [SerializeField] private int startPlatformWidth = 16;
    [SerializeField] private int startPlatformHeight = 8;

    [Header("Chunk Settings")]
    [SerializeField] private int chunkWidth = 32; // tiles per chunk
    [SerializeField] private int lookaheadDistance = 64; // generate new chunk when player is this close

    [Header("Perlin Noise")]
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private float heightAmplitude = 3f;

    private int currentChunkIndex = 0;
    private int columnsSinceLastEggCheck = 0;
    private float eggSpawnChance = 0.1f;
    private int pendingEggSpawns = 0;

    private float minSurfaceWorldY;
    private float maxSurfaceWorldY;
    private float bottomWorldY;

    public enum ColumnType
    {
        Gap,                // No ground at this X position
        LeftEdge,           // Left edge of a ground segment
        SecondFromLeftEdge, // Second column from left edge (uses SELF, SBSL, BSL)
        RightEdge,          // Right edge of a ground segment
        SecondFromRightEdge,// Second column from right edge (uses SERF, SBSR, BSR)
        Interior            // Interior column between edges
    }

    public struct ColumnPlan
    {
        public ColumnType type;
        public int topRow;          // World Y of the top tile (surface)
        public int bottomRow;       // World Y of the bottom tile
        public int fillerVariant;   // 0 or 1, for ELF/SELF and ERF/SERF matching
        public int topVariant;      // 0-3, for top/secondRow matching
        public int bottomVariant;   // 0 or 1, for FSB/FB matching

        // Constructor for gaps
        public static ColumnPlan CreateGap()
        {
            return new ColumnPlan { type = ColumnType.Gap };
        }

        // Constructor for ground columns
        public static ColumnPlan CreateGround(ColumnType columnType, int top, int bottom, int fillerVar, int topVar, int bottomVar)
        {
            return new ColumnPlan
            {
                type = columnType,
                topRow = top,
                bottomRow = bottom,
                fillerVariant = fillerVar,
                topVariant = topVar,
                bottomVariant = bottomVar
            };
        }
    }

    void Start()
    {
        // Validate registry
        if (!tileRegistry.Validate())
        {
            Debug.LogError("[GroundGenerator] Tile registry validation failed!");
            enabled = false;
            return;
        }

        // Calculate world Y boundaries from screen percentages
        CalculateScreenBoundaries();

        // Generate starting platform
        GenerateStartingPlatform();

        // Generate first chunk to the right of starting platform
        GenerateChunk(0);
    }

    void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("[GroundGenerator] No player reference set!");
            return;
        }

        // Calculate the rightmost generated X position
        int rightmostX = startPlatformWidth + (currentChunkIndex * chunkWidth);

        // Get player's grid X position
        float cellSize = groundTilemap.cellSize.x;
        int playerGridX = Mathf.RoundToInt(player.position.x / cellSize);

        // Check if we need to generate a new chunk
        if (playerGridX > rightmostX - lookaheadDistance)
        {
            GenerateChunk(currentChunkIndex);
        }
    }

    void CalculateScreenBoundaries()
    {
        Camera cam = Camera.main;
        float screenHeight = cam.orthographicSize * 2f;
        float screenBottom = cam.transform.position.y - cam.orthographicSize;

        // Calculate in world space first
        float minSurfaceWorldYFloat = screenBottom + (screenHeight * minSurfacePercent);
        float maxSurfaceWorldYFloat = screenBottom + (screenHeight * maxSurfacePercent);
        float bottomWorldYFloat = screenBottom;

        // Convert to grid coordinates (divide by cell size)
        float cellSize = groundTilemap.cellSize.y;
        minSurfaceWorldY = Mathf.RoundToInt(minSurfaceWorldYFloat / cellSize);
        maxSurfaceWorldY = Mathf.RoundToInt(maxSurfaceWorldYFloat / cellSize);
        bottomWorldY = Mathf.RoundToInt(bottomWorldYFloat / cellSize);

        Debug.Log($"[GroundGenerator] Cell size: {cellSize}");
        Debug.Log($"[GroundGenerator] Surface Y range (grid): {minSurfaceWorldY} to {maxSurfaceWorldY}");
        Debug.Log($"[GroundGenerator] Bottom Y (grid): {bottomWorldY}");
    }

    void GenerateStartingPlatform()
    {
        int surfaceY = Mathf.RoundToInt((minSurfaceWorldY + maxSurfaceWorldY) / 2f);
        int bottomY = Mathf.RoundToInt(bottomWorldY);
        int platformBottom = Mathf.Max(bottomY, surfaceY - startPlatformHeight + 1);
        int startX = 0;

        int topVariant = 0;
        int fillerVariant = 0;
        int bottomVariant = 0;

        Debug.Log($"[GroundGenerator] Starting platform: X {startX} to {startX + startPlatformWidth - 1}, Y {platformBottom} to {surfaceY}");

        for (int x = 0; x < startPlatformWidth; x++)
        {
            ColumnType columnType;

            if (x == 0)
                columnType = ColumnType.LeftEdge;
            else if (x == 1)
                columnType = ColumnType.SecondFromLeftEdge;
            else if (x == startPlatformWidth - 1)
                columnType = ColumnType.RightEdge;
            else if (x == startPlatformWidth - 2)
                columnType = ColumnType.SecondFromRightEdge;
            else
                columnType = ColumnType.Interior;

            ColumnPlan plan = ColumnPlan.CreateGround(
                columnType,
                surfaceY,
                platformBottom,
                fillerVariant,
                topVariant,
                bottomVariant
            );

            PlaceColumn(plan, startX + x);
        }

        Debug.Log("[GroundGenerator] Starting platform complete!");
    }

    void PlaceColumn(ColumnPlan plan, int worldX)
    {
        // If it's a gap, don't place anything
        if (plan.type == ColumnType.Gap)
            return;

        int top = plan.topRow;
        int bottom = plan.bottomRow;
        int height = top - bottom + 1; // +1 because both are inclusive

        // Ensure minimum height of 4 (top, second, second-to-bottom, bottom)
        if (height < 4)
        {
            Debug.LogWarning($"[GroundGenerator] Column at X={worldX} has height {height}, minimum is 4. Skipping.");
            return;
        }

        for (int y = bottom; y <= top; y++)
        {
            TileBase tile = GetTileForPosition(plan, worldX, y, top, bottom);
            if (tile != null)
            {
                Vector3Int tilePos = new Vector3Int(worldX, y, 0);
                groundTilemap.SetTile(tilePos, tile);
            }
        }
    }

    TileBase GetTileForPosition(ColumnPlan plan, int worldX, int y, int top, int bottom)
    {
        bool isTop = (y == top);
        bool isSecond = (y == top - 1);
        bool isSecondBottom = (y == bottom + 1);
        bool isBottom = (y == bottom);
        bool isFiller = !isTop && !isSecond && !isSecondBottom && !isBottom;

        switch (plan.type)
        {
            case ColumnType.LeftEdge:
                if (isTop) return tileRegistry.elt;
                if (isSecond) return tileRegistry.elst;
                if (isSecondBottom) return tileRegistry.elsb;
                if (isBottom) return tileRegistry.elb;
                if (isFiller) return tileRegistry.elfTiles[plan.fillerVariant];
                break;

            case ColumnType.SecondFromLeftEdge:
                if (isTop) return tileRegistry.topTiles[plan.topVariant];
                if (isSecond) return tileRegistry.secondRowTiles[plan.topVariant];
                if (isSecondBottom) return tileRegistry.sbsl;
                if (isBottom) return tileRegistry.bsl;
                if (isFiller) return tileRegistry.selfTiles[plan.fillerVariant]; // Must match ELF variant
                break;

            case ColumnType.RightEdge:
                if (isTop) return tileRegistry.ert;
                if (isSecond) return tileRegistry.erst;
                if (isSecondBottom) return tileRegistry.ersb;
                if (isBottom) return tileRegistry.erb;
                if (isFiller) return tileRegistry.erfTiles[plan.fillerVariant];
                break;

            case ColumnType.SecondFromRightEdge:
                if (isTop) return tileRegistry.topTiles[plan.topVariant];
                if (isSecond) return tileRegistry.secondRowTiles[plan.topVariant];
                if (isSecondBottom) return tileRegistry.sbsr;
                if (isBottom) return tileRegistry.bsr;
                if (isFiller) return tileRegistry.serfTiles[plan.fillerVariant]; // Must match ERF variant
                break;

            case ColumnType.Interior:
                if (isTop) return tileRegistry.topTiles[plan.topVariant];
                if (isSecond) return tileRegistry.secondRowTiles[plan.topVariant];
                if (isSecondBottom) return tileRegistry.fsbTiles[plan.bottomVariant];
                if (isBottom) return tileRegistry.fbTiles[plan.bottomVariant];
                if (isFiller)
                {
                    return tileRegistry.fillerTiles[Random.Range(0, tileRegistry.fillerTiles.Length)];
                }
                break;
        }

        Debug.LogWarning($"[GroundGenerator] No tile found for position ({worldX}, {y}) with plan type {plan.type}");
        return null;
    }

    void GenerateChunk(int chunkIndex)
    {
        Debug.Log($"[GroundGenerator] Generating chunk {chunkIndex}...");

        int chunkStartX = startPlatformWidth + (chunkIndex * chunkWidth);

        ColumnPlan[] chunkPlan = PlanChunk(chunkStartX, chunkWidth, chunkIndex);

        // Place all columns first
        for (int i = 0; i < chunkPlan.Length; i++)
        {
            PlaceColumn(chunkPlan[i], chunkStartX + i);
        }

        // Fix up height transitions
        FixUpHeightTransitions(chunkPlan, chunkStartX);

        // Process any egg spawn checks that fall inside this chunk.
        ProcessEggSpawns(chunkPlan, chunkStartX);

        currentChunkIndex++;
        Debug.Log($"[GroundGenerator] Chunk {chunkIndex} complete!");
    }
    void ProcessEggSpawns(ColumnPlan[] plans, int chunkStartX)
    {
        bool[] eggSpawnUsed = new bool[plans.Length];

        // First satisfy any pending spawns from a previous chunk.
        SpawnPendingEggsInChunk(plans, chunkStartX, eggSpawnUsed);

        for (int i = 0; i < plans.Length; i++)
        {
            columnsSinceLastEggCheck++;

            if (columnsSinceLastEggCheck >= 20)
            {
                columnsSinceLastEggCheck = 0;

                if (Random.value < eggSpawnChance)
                {
                    if (!TrySpawnEggAtIndex(plans, chunkStartX, i, eggSpawnUsed))
                    {
                        pendingEggSpawns++;
                    }

                    eggSpawnChance = 0.1f;
                }
                else
                {
                    eggSpawnChance = Mathf.Min(eggSpawnChance + 0.1f, 1f);
                }
            }
        }
    }

    bool TrySpawnEggAtIndex(ColumnPlan[] plans, int chunkStartX, int startIndex, bool[] eggSpawnUsed)
    {
        for (int i = startIndex; i < plans.Length; i++)
        {
            if (eggSpawnUsed[i])
                continue;

            if (!IsValidSpawnColumn(plans[i]))
                continue;

            if (!HasFlatRunInOneDirection(plans, i))
                continue;

            SpawnEggAtColumn(chunkStartX + i, plans[i].topRow);
            eggSpawnUsed[i] = true;
            return true;
        }

        return false;
    }

    void SpawnPendingEggsInChunk(ColumnPlan[] plans, int chunkStartX, bool[] eggSpawnUsed)
    {
        for (int i = 0; i < plans.Length && pendingEggSpawns > 0; i++)
        {
            if (eggSpawnUsed[i])
                continue;

            if (!IsValidSpawnColumn(plans[i]))
                continue;

            if (!HasFlatRunInOneDirection(plans, i))
                continue;

            SpawnEggAtColumn(chunkStartX + i, plans[i].topRow);
            eggSpawnUsed[i] = true;
            pendingEggSpawns--;
        }
    }

    bool IsValidSpawnColumn(ColumnPlan plan)
    {
        return plan.type == ColumnType.Interior
            || plan.type == ColumnType.SecondFromLeftEdge
            || plan.type == ColumnType.SecondFromRightEdge;
    }

    bool HasFlatRunInOneDirection(ColumnPlan[] plans, int index)
    {
        int requiredFlatCount = 3;
        int topRow = plans[index].topRow;

        bool flatRight = true;
        for (int offset = 1; offset <= requiredFlatCount; offset++)
        {
            int nextIndex = index + offset;
            if (nextIndex >= plans.Length || !IsValidSpawnColumn(plans[nextIndex]) || plans[nextIndex].topRow != topRow)
            {
                flatRight = false;
                break;
            }
        }

        if (flatRight)
            return true;

        bool flatLeft = true;
        for (int offset = 1; offset <= requiredFlatCount; offset++)
        {
            int nextIndex = index - offset;
            if (nextIndex < 0 || !IsValidSpawnColumn(plans[nextIndex]) || plans[nextIndex].topRow != topRow)
            {
                flatLeft = false;
                break;
            }
        }

        return flatLeft;
    }

    void SpawnEggAtColumn(int worldX, int topRow)
    {
        Vector3Int spawnCell = new Vector3Int(worldX, topRow, 0);
        Vector3 spawnWorldPos = groundTilemap.GetCellCenterWorld(spawnCell);
        spawnWorldPos.y += groundTilemap.cellSize.y * 0.25f;
        EggSpawner.SpawnEgg(spawnWorldPos);
    }

    void FixUpHeightTransitions(ColumnPlan[] plans, int startX)
    {
        for (int i = 0; i < plans.Length - 1; i++)
        {
            if (plans[i].type == ColumnType.Gap || plans[i + 1].type == ColumnType.Gap)
                continue;

            FixUpTopTransition(plans, i, startX);
            FixUpBottomTransition(plans, i, startX);
        }
    }

    void FixUpTopTransition(ColumnPlan[] plans, int i, int startX)
    {
        int leftTop = plans[i].topRow;
        int rightTop = plans[i + 1].topRow;

        if (leftTop == rightTop) return;

        if (leftTop > rightTop)
        {
            // Left is higher, right side of left column is exposed
            int exposedBottom = rightTop + 1;
            int exposedTop = leftTop;

            // Edge column (last column of higher section)
            PlaceExposedTopEdge(startX + i, exposedBottom, exposedTop,
                plans[i].fillerVariant, isRight: true);

            // Second-from-edge column
            if (i > 0 && plans[i - 1].type != ColumnType.Gap && plans[i - 1].topRow >= leftTop)
            {
                PlaceExposedTopSecondFromEdge(startX + i - 1, exposedBottom, exposedTop,
                    plans[i - 1].fillerVariant, isRight: true);
            }
        }
        else
        {
            // Right is higher, left side of right column is exposed
            int exposedBottom = leftTop + 1;
            int exposedTop = rightTop;

            // Edge column (first column of higher section)
            PlaceExposedTopEdge(startX + i + 1, exposedBottom, exposedTop,
                plans[i + 1].fillerVariant, isRight: false);

            // Second-from-edge column
            if (i + 2 < plans.Length && plans[i + 2].type != ColumnType.Gap && plans[i + 2].topRow >= rightTop)
            {
                PlaceExposedTopSecondFromEdge(startX + i + 2, exposedBottom, exposedTop,
                    plans[i + 2].fillerVariant, isRight: false);
            }
        }
    }

    void FixUpBottomTransition(ColumnPlan[] plans, int i, int startX)
    {
        int leftBottom = plans[i].bottomRow;
        int rightBottom = plans[i + 1].bottomRow;

        if (leftBottom == rightBottom) return;

        if (leftBottom > rightBottom)
        {
            // Left is shallower, right column is exposed on left below leftBottom
            int exposedTop = leftBottom - 1;
            int exposedBottom = rightBottom;

            // Edge column (the deeper column, at its exposed left side)
            PlaceExposedBottomEdge(startX + i + 1, exposedBottom, exposedTop,
                plans[i + 1].fillerVariant, isRight: false);

            // Second-from-edge column
            if (i + 2 < plans.Length && plans[i + 2].type != ColumnType.Gap && plans[i + 2].bottomRow <= rightBottom)
            {
                PlaceExposedBottomSecondFromEdge(startX + i + 2, exposedBottom, exposedTop,
                    plans[i + 2].fillerVariant, isRight: false);
            }
        }
        else
        {
            // Right is shallower, left column is exposed on right below rightBottom
            int exposedTop = rightBottom - 1;
            int exposedBottom = leftBottom;

            // Edge column (the deeper column, at its exposed right side)
            PlaceExposedBottomEdge(startX + i, exposedBottom, exposedTop,
                plans[i].fillerVariant, isRight: true);

            // Second-from-edge column
            if (i > 0 && plans[i - 1].type != ColumnType.Gap && plans[i - 1].bottomRow <= leftBottom)
            {
                PlaceExposedBottomSecondFromEdge(startX + i - 1, exposedBottom, exposedTop,
                    plans[i - 1].fillerVariant, isRight: true);
            }
        }
    }

    void PlaceExposedTopEdge(int worldX, int fromY, int toY, int fillerVariant, bool isRight)
    {
        for (int y = fromY; y <= toY; y++)
        {
            TileBase tile;

            if (y == toY) // Top of exposed section
            {
                tile = isRight ? tileRegistry.ert : tileRegistry.elt;
            }
            else if (y == toY - 1) // Second from top
            {
                tile = isRight ? tileRegistry.erst : tileRegistry.elst;
            }
            else // Filler
            {
                tile = isRight
                    ? tileRegistry.erfTiles[fillerVariant]
                    : tileRegistry.elfTiles[fillerVariant];
            }

            groundTilemap.SetTile(new Vector3Int(worldX, y, 0), tile);
        }
    }

    void PlaceExposedTopSecondFromEdge(int worldX, int fromY, int toY, int fillerVariant, bool isRight)
    {
        // Top and second-from-top rows are already correct (T and second row tiles)
        // Only overwrite filler rows with SERF/SELF
        for (int y = fromY; y <= toY - 2; y++)
        {
            TileBase tile = isRight
                ? tileRegistry.serfTiles[fillerVariant]
                : tileRegistry.selfTiles[fillerVariant];

            groundTilemap.SetTile(new Vector3Int(worldX, y, 0), tile);
        }
    }

    void PlaceExposedBottomEdge(int worldX, int fromY, int toY, int fillerVariant, bool isRight)
    {
        for (int y = fromY; y <= toY; y++)
        {
            TileBase tile;

            if (y == fromY) // Bottom of exposed section
            {
                tile = isRight ? tileRegistry.erb : tileRegistry.elb;
            }
            else if (y == fromY + 1) // Second from bottom
            {
                tile = isRight ? tileRegistry.ersb : tileRegistry.elsb;
            }
            else // Filler
            {
                tile = isRight
                    ? tileRegistry.erfTiles[fillerVariant]
                    : tileRegistry.elfTiles[fillerVariant];
            }

            groundTilemap.SetTile(new Vector3Int(worldX, y, 0), tile);
        }
    }

    void PlaceExposedBottomSecondFromEdge(int worldX, int fromY, int toY, int fillerVariant, bool isRight)
    {
        // Bottom and second-from-bottom are already correct (FB and FSB tiles)
        // Only overwrite filler rows with SERF/SELF
        for (int y = fromY + 2; y <= toY; y++)
        {
            TileBase tile = isRight
                ? tileRegistry.serfTiles[fillerVariant]
                : tileRegistry.selfTiles[fillerVariant];

            groundTilemap.SetTile(new Vector3Int(worldX, y, 0), tile);
        }
    }

    ColumnPlan[] PlanChunk(int startX, int width, int chunkIndex)
    {
        ColumnPlan[] plans = new ColumnPlan[width];

        bool inGap = true;
        int runLength = 0;
        int currentRunStartX = 0;
        int targetRunLength = 0;

        int segmentTopVariant = 0;
        int segmentFillerVariant = 0;
        int segmentBottomVariant = 0;

        int subSegmentStartIdx = 0;
        bool isFirstSubSegment = true;
        int currentSurfaceY = 0;
        int currentBottomY = 0;

        int flatCellCounter = 0;
        int nextHeightChangeThreshold = 2;

        for (int i = 0; i < width; i++)
        {
            int worldX = startX + i;

            bool shouldTransition = false;

            if (inGap)
            {
                if (runLength >= targetRunLength)
                {
                    shouldTransition = true;
                }
            }
            else
            {
                if (runLength >= targetRunLength)
                {
                    shouldTransition = true;
                }
                else
                {
                    flatCellCounter++;

                    if (flatCellCounter >= nextHeightChangeThreshold + 1)
                    {
                        if (Random.value < 0.5f)
                        {
                            int currentSubSegmentWidth = i - subSegmentStartIdx;
                            if (currentSubSegmentWidth < 2)
                            {
                                // Too narrow for a height change, skip it
                                nextHeightChangeThreshold = Mathf.Min(nextHeightChangeThreshold * 2, 16);
                                flatCellCounter = 0;
                                continue; // Skip this height change
                            }

                            // Also ensure remaining ground run has room for at least 2 more columns
                            int remainingInRun = targetRunLength - runLength;
                            if (remainingInRun < 2)
                            {
                                // Not enough room for the new sub-segment
                                nextHeightChangeThreshold = Mathf.Min(nextHeightChangeThreshold * 2, 16);
                                flatCellCounter = 0;
                                continue;
                            }
                            // Finalize current sub-segment before changing height
                            // Left is gap only if this is the first sub-segment of the ground run
                            AssignColumnTypes(plans, subSegmentStartIdx, i - 1,
                                currentSurfaceY, currentBottomY, segmentFillerVariant,
                                segmentTopVariant, segmentBottomVariant,
                                isFirstSubSegment,
                                false); // right is NOT a gap, adjacent to next sub-segment

                            int previousSurfaceY = currentSurfaceY;

                            // Change surface height
                            int surfaceHeightDelta = Random.Range(-maxHeightDifference, maxHeightDifference + 1);
                            int newSurfaceY = previousSurfaceY + surfaceHeightDelta;
                            newSurfaceY = (int)Mathf.Clamp(newSurfaceY, minSurfaceWorldY, maxSurfaceWorldY);

                            int actualDelta = Mathf.Abs(newSurfaceY - previousSurfaceY);
                            if (actualDelta > maxHeightDifference)
                            {
                                if (newSurfaceY > previousSurfaceY)
                                    newSurfaceY = previousSurfaceY + maxHeightDifference;
                                else
                                    newSurfaceY = previousSurfaceY - maxHeightDifference;
                            }

                            currentSurfaceY = newSurfaceY;

                            // Change bottom height independently
                            int groundHeight = Random.Range(6, 12);
                            currentBottomY = (int)Mathf.Max(bottomWorldY, currentSurfaceY - groundHeight);

                            // Start new sub-segment
                            subSegmentStartIdx = i;
                            isFirstSubSegment = false;

                            flatCellCounter = 0;
                            nextHeightChangeThreshold = 2;
                        }
                        else
                        {
                            nextHeightChangeThreshold = Mathf.Min(nextHeightChangeThreshold * 2, 16);
                            flatCellCounter = 0;
                        }
                    }
                }
            }

            if (shouldTransition)
            {
                if (!inGap)
                {
                    // Finalize last sub-segment of ground run
                    AssignColumnTypes(plans, subSegmentStartIdx, i - 1,
                        currentSurfaceY, currentBottomY, segmentFillerVariant,
                        segmentTopVariant, segmentBottomVariant,
                        isFirstSubSegment,
                        true); // right IS a gap
                }

                inGap = !inGap;
                runLength = 0;
                currentRunStartX = worldX;

                if (inGap)
                {
                    int gapPower = Random.Range(1, 4); // 2^1 to 2^3 = 2 to 8
                    targetRunLength = 1 << gapPower; // Bit shift instead of Mathf.Pow
                }
                else
                {
                    int groundPower = Random.Range(1, 8); // 2^1 to 2^7 = 2 to 128
                    targetRunLength = 1 << groundPower;
                }

                if (!inGap)
                {
                    segmentTopVariant = Random.Range(0, 4);
                    segmentFillerVariant = Random.Range(0, 2);
                    segmentBottomVariant = Random.Range(0, 2);

                    float noiseValue = Mathf.PerlinNoise(worldX * noiseScale, chunkIndex * 0.1f);
                    currentSurfaceY = Mathf.RoundToInt(
                        Mathf.Lerp((float)minSurfaceWorldY, (float)maxSurfaceWorldY, noiseValue)
                    );

                    int groundHeight = Random.Range(6, 12);
                    currentBottomY = (int)Mathf.Max(bottomWorldY, currentSurfaceY - groundHeight);

                    flatCellCounter = 0;
                    nextHeightChangeThreshold = 2;

                    subSegmentStartIdx = i;
                    isFirstSubSegment = true;
                }
            }

            if (inGap)
            {
                plans[i] = ColumnPlan.CreateGap();
            }
            else
            {
                plans[i] = ColumnPlan.CreateGround(
                    ColumnType.Interior, // Placeholder
                    currentSurfaceY,
                    currentBottomY,
                    segmentFillerVariant,
                    segmentTopVariant,
                    segmentBottomVariant
                );
            }

            runLength++;
        }

        // Finalize last run
        if (!inGap && runLength > 0)
        {
            AssignColumnTypes(plans, subSegmentStartIdx, width - 1,
                currentSurfaceY, currentBottomY, segmentFillerVariant,
                segmentTopVariant, segmentBottomVariant,
                isFirstSubSegment,
                true); // right is gap (chunk boundary)
        }

        return plans;
    }

    void AssignColumnTypes(ColumnPlan[] plans, int startIdx, int endIdx,
        int surfaceY, int bottomY, int fillerVar, int topVar, int bottomVar,
        bool leftIsGap, bool rightIsGap)
    {
        int segmentWidth = endIdx - startIdx + 1;

        // Segment too narrow and both sides are gaps - can't form valid ground
        if (segmentWidth < 2 && leftIsGap && rightIsGap)
        {
            Debug.LogWarning($"[GroundGenerator] Ground segment too narrow ({segmentWidth}), converting to gap.");
            for (int i = startIdx; i <= endIdx; i++)
            {
                plans[i] = ColumnPlan.CreateGap();
            }
            return;
        }

        for (int i = startIdx; i <= endIdx; i++)
        {
            ColumnType columnType;
            int localIdx = i - startIdx;
            int distFromEnd = endIdx - i;

            // Left side
            if (localIdx == 0 && leftIsGap)
                columnType = ColumnType.LeftEdge;
            else if (localIdx == 1 && leftIsGap)
                columnType = ColumnType.SecondFromLeftEdge;
            // Right side
            else if (distFromEnd == 0 && rightIsGap)
                columnType = ColumnType.RightEdge;
            else if (distFromEnd == 1 && rightIsGap)
                columnType = ColumnType.SecondFromRightEdge;
            else
                columnType = ColumnType.Interior;

            plans[i] = ColumnPlan.CreateGround(
                columnType,
                surfaceY,
                bottomY,
                fillerVar,
                topVar,
                bottomVar
            );
        }
    }
}