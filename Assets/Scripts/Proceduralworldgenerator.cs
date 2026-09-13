using UnityEngine;
using System.Collections.Generic;

// ============================================================================
// PROCEDURAL WORLD GENERATOR
// Attach this to a GameObject that has a Terrain component.
// Assign the fields in the Inspector, then right-click the component header
// (or the (...) menu) and choose "Generate World" - or call GenerateWorld()
// from a script/button at runtime.
//
// Requires: terrainData.heightmapResolution set (default 513 is fine).
// ============================================================================

[RequireComponent(typeof(Terrain))]
public class ProceduralWorldGenerator : MonoBehaviour
{
    [Header("Seed")]
    public int seed = 42;
    public bool randomizeSeed = true;

    [Header("Height Noise")]
    [Tooltip("Bigger = smoother, more zoomed-out terrain features")]
    public float noiseScale = 350f;
    public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2.2f;
    public float mountainHeight = 1f;
    [Tooltip("Lower = fewer, larger mountain ranges")]
    public float mountainFrequency = 0.0025f;

    [Header("Biome Noise")]
    [Tooltip("Bigger = larger, more sprawling biome regions")]
    public float biomeScale = 500f;

    [Header("Valleys / Basins (terrain depth)")]
    [Tooltip("Normal land sits around this height; basins/rivers dig down from here, mountains rise above it")]
    [Range(0f, 1f)] public float landBaseline = 0.45f;
    [Tooltip("Lower = larger, more gradual basins for lakes/rivers to sit in")]
    public float basinFrequency = 0.004f;
    [Range(0f, 1f)] public float basinDepth = 0.35f;

    [Header("Rivers")]
    [Range(0.001f, 0.05f)] public float riverWidth = 0.015f;
    public float riverDepth = 0.05f;
    public int riverCount = 3;
    [Tooltip("Wide, shallow shoulder around each river so it reads as a valley, not a scratch")]
    [Range(0f, 0.2f)] public float riverValleyWidth = 0.05f;

    [Header("Lakes")]
    [Range(0f, 1f)] public float lakeHeightLevel = 0.28f;
    [Tooltip("How far the shoreline blends into surrounding land (fraction of heightmap resolution)")]
    [Range(0f, 0.1f)] public float lakeShoreSmoothing = 0.03f;

    [Header("Water Rendering (quick fake)")]
    public bool generateWater = true;
    [Tooltip("Transparent material with a tiling water texture - assign your own for the real look")]
    public Material waterMaterial;
    [Tooltip("World-space width of river ribbons")]
    public float riverMeshWidth = 4f;
    public float waterFlowSpeed = 0.5f;

    [Header("Settlement Zones (reserved flat, tree-free plots)")]
    public int cityCount = 2;
    public int townCount = 4;
    public int villageCount = 6;
    [Tooltip("Normalized fraction of terrain size (0-1)")]
    public float cityRadius = 0.06f;
    public float townRadius = 0.035f;
    public float villageRadius = 0.02f;
    [Range(0f, 1f)] public float settlementFlattenStrength = 0.85f;
    [Tooltip("Max slope (degrees) a plot may start on before flattening")]
    public float settlementMaxSteepness = 15f;

    [Header("Terrain Layers (order: Grass, Rock, Sand, ForestFloor)")]
    public TerrainLayer grassLayer;
    public TerrainLayer rockLayer;
    public TerrainLayer sandLayer;
    public TerrainLayer forestFloorLayer;

    [Header("Vegetation Prefabs")]
    public GameObject[] grassTrees;
    public GameObject[] forestTrees;
    public GameObject[] desertPlants;
    public DetailPrototype grassDetail;

    [Header("Vegetation Density")]
    public int treeAttempts = 8000;
    [Range(0f, 1f)] public float grassTreeDensity = 0.35f;
    [Range(0f, 1f)] public float desertPlantDensity = 0.15f;
    [Range(0f, 1f)] public float forestTreeDensity = 0.55f;

    private Terrain terrain;
    private TerrainData terrainData;
    private int heightRes;
    private float[,] heights;
    private float[,] biomeMap;   // 0 = desert, ~0.5 = grassland, 1 = forest
    private List<Vector2Int> lakeCells = new List<Vector2Int>();
    private List<List<Vector2Int>> lakeBasins = new List<List<Vector2Int>>();
    private List<HashSet<Vector2Int>> riverCellSets = new List<HashSet<Vector2Int>>();

    private struct SettlementZone
    {
        public Vector2 center;   // normalized 0-1 across the terrain
        public float radius;     // normalized
        public float centerHeight;
        public string label;
    }
    private List<SettlementZone> settlementZones = new List<SettlementZone>();

    void Awake()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
    }

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        if (terrainData == null) terrainData = terrain.terrainData;

        if (randomizeSeed) seed = Random.Range(0, 999999);
        Random.InitState(seed);
        Vector2 offset = new Vector2(Random.Range(-100000f, 100000f), Random.Range(-100000f, 100000f));

        heightRes = terrainData.heightmapResolution;
        heights = new float[heightRes, heightRes];
        biomeMap = new float[heightRes, heightRes];

        terrainData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer, sandLayer, forestFloorLayer };

        GenerateBiomeMap(offset);
        GenerateHeightMap(offset);
        CarveRivers(offset);
        CarveLakes();
        SmoothLakeShores();

        // First pass: commit heights now so GetSteepness reflects the real terrain
        // when we go looking for flat spots for settlements.
        terrainData.SetHeights(0, 0, heights);

        settlementZones.Clear();
        PlaceZonesOfType(cityCount, cityRadius, "City");
        PlaceZonesOfType(townCount, townRadius, "Town");
        PlaceZonesOfType(villageCount, villageRadius, "Village");
        FlattenSettlementZones();

        // Second pass: apply the flattened settlement plots.
        terrainData.SetHeights(0, 0, heights);

        PaintTextures();
        ScatterVegetation();
        BuildWaterMeshes();

        Debug.Log($"World generated with seed {seed} - {settlementZones.Count} settlement zones placed.");
    }

    // ------------------------------------------------------------------
    // BIOME MAP - low frequency noise decides desert / grassland / forest
    // ------------------------------------------------------------------
    void GenerateBiomeMap(Vector2 offset)
    {
        for (int y = 0; y < heightRes; y++)
        {
            for (int x = 0; x < heightRes; x++)
            {
                float nx = (x + offset.x) / biomeScale;
                float ny = (y + offset.y) / biomeScale;

                float moisture = Mathf.PerlinNoise(nx, ny);
                moisture += 0.3f * Mathf.PerlinNoise(nx * 3f + 50f, ny * 3f + 50f);
                moisture = Mathf.Clamp01(moisture / 1.3f);

                biomeMap[y, x] = moisture; // <0.35 desert, 0.35-0.65 grass, >0.65 forest
            }
        }
    }

    // ------------------------------------------------------------------
    // HEIGHT MAP - fBm variation on a solid land baseline + ridged-noise
    // mountains + basins, so ordinary land stays well above lake level.
    // ------------------------------------------------------------------
    float FBM(float x, float y, int oct, float pers, float lac)
    {
        float total = 0f, amplitude = 1f, frequency = 1f, maxValue = 0f;
        for (int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= pers;
            frequency *= lac;
        }
        return total / maxValue;
    }

    float RidgedNoise(float x, float y)
    {
        float n = Mathf.PerlinNoise(x, y);
        return 1f - Mathf.Abs(n * 2f - 1f); // sharp ridges instead of smooth bumps
    }

    void GenerateHeightMap(Vector2 offset)
    {
        for (int y = 0; y < heightRes; y++)
        {
            for (int x = 0; x < heightRes; x++)
            {
                float nx = (x + offset.x) / noiseScale;
                float ny = (y + offset.y) / noiseScale;

                float baseShape = FBM(nx, ny, octaves, persistence, lacunarity);

                float mountainMaskRaw = Mathf.PerlinNoise(
                    (x + offset.x) * mountainFrequency,
                    (y + offset.y) * mountainFrequency);
                float mountainMask = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((mountainMaskRaw - 0.45f) * 3f));

                float ridged = RidgedNoise(nx * 2.5f, ny * 2.5f);
                float mountains = ridged * mountainMask * mountainHeight;

                // Broad, independent basin layer: pushes low-lying ground further down
                // so there's real depth for lakes and rivers to sit in, mostly away from
                // mountains so ranges don't get flattened out by it.
                float basinRaw = Mathf.PerlinNoise(
                    (x + offset.x) * basinFrequency,
                    (y + offset.y) * basinFrequency);
                float basinPull = Mathf.Clamp01((0.5f - basinRaw) * 2f);

                // Center baseShape (0..1) around 0 so it becomes a +/- variation
                // applied on top of a solid land baseline, instead of starting near
                // zero and only ever adding height. Without this, flat biomes average
                // out well below lakeHeightLevel and silently get swallowed by lakes.
                float variation = baseShape - 0.5f;

                float biome = biomeMap[y, x];
                float finalHeight;

                if (biome < 0.35f)
                {
                    // Desert: mostly flat, dune ripples, occasional rocky outcrop
                    float dunes = (Mathf.Sin((nx * 40f) + FBM(nx, ny, 2, 0.5f, 2f) * 6f) * 0.5f + 0.5f) * 0.06f;
                    float rockyPatch = Mathf.Clamp01((FBM(nx * 4f, ny * 4f, 3, 0.5f, 2f) - 0.6f) * 3f) * 0.15f;
                    finalHeight = landBaseline + variation * 0.12f + dunes + rockyPatch + mountains * 0.22f;
                }
                else if (biome < 0.65f)
                {
                    // Grassland: gentle rolling hills, kept fairly flat so it's walkable/buildable
                    finalHeight = landBaseline + variation * 0.18f + mountains * 0.35f;
                }
                else
                {
                    // Forest: hillier terrain under the canopy
                    finalHeight = landBaseline + variation * 0.28f + mountains * 0.8f;
                }

                finalHeight -= basinPull * basinDepth * (1f - mountainMask);

                heights[y, x] = Mathf.Clamp01(finalHeight);
            }
        }
    }

    // ------------------------------------------------------------------
    // RIVERS - carve a winding trench where noise sits near 0.5, plus a wide,
    // shallow shoulder either side so it reads as a valley, not a scratch.
    // The narrow center-line cells are also recorded per river so we can
    // build a water mesh that follows the exact same groove later.
    // ------------------------------------------------------------------
    void CarveRivers(Vector2 offset)
    {
        riverCellSets.Clear();

        for (int r = 0; r < riverCount; r++)
        {
            float ox = r * 1013.3f;
            float oy = r * 777.7f;
            HashSet<Vector2Int> cellsForThisRiver = new HashSet<Vector2Int>();

            for (int y = 0; y < heightRes; y++)
            {
                for (int x = 0; x < heightRes; x++)
                {
                    float nx = (x + offset.x + ox) / (noiseScale * 1.4f);
                    float ny = (y + offset.y + oy) / (noiseScale * 1.4f);

                    float riverNoise = Mathf.PerlinNoise(nx, ny);
                    float dist = Mathf.Abs(riverNoise - 0.5f);

                    if (dist < riverWidth)
                    {
                        float t = 1f - (dist / riverWidth);
                        heights[y, x] = Mathf.Max(0f, heights[y, x] - riverDepth * t);
                        cellsForThisRiver.Add(new Vector2Int(x, y));
                    }
                    else if (dist < riverWidth + riverValleyWidth)
                    {
                        float shoulderT = 1f - ((dist - riverWidth) / riverValleyWidth);
                        heights[y, x] = Mathf.Max(0f, heights[y, x] - riverDepth * 0.25f * shoulderT);
                    }
                }
            }

            riverCellSets.Add(cellsForThisRiver);
        }
    }

    // ------------------------------------------------------------------
    // LAKES - flood-fill low basins, flatten to a shared water level, then
    // blend the surrounding shoreline in gradually rather than a hard cut.
    // Each basin's cells are kept separately so we can size a water mesh
    // to fit it later.
    // ------------------------------------------------------------------
    void CarveLakes()
    {
        lakeCells.Clear();
        lakeBasins.Clear();
        bool[,] visited = new bool[heightRes, heightRes];
        List<Vector2Int> basin = new List<Vector2Int>();

        for (int y = 0; y < heightRes; y++)
        {
            for (int x = 0; x < heightRes; x++)
            {
                if (visited[y, x] || heights[y, x] > lakeHeightLevel) continue;

                basin.Clear();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(x, y));
                visited[y, x] = true;

                while (queue.Count > 0)
                {
                    Vector2Int p = queue.Dequeue();
                    basin.Add(p);

                    Vector2Int[] neighbors = {
                        new Vector2Int(p.x + 1, p.y), new Vector2Int(p.x - 1, p.y),
                        new Vector2Int(p.x, p.y + 1), new Vector2Int(p.x, p.y - 1)
                    };

                    foreach (var n in neighbors)
                    {
                        if (n.x < 0 || n.x >= heightRes || n.y < 0 || n.y >= heightRes) continue;
                        if (visited[n.y, n.x]) continue;
                        if (heights[n.y, n.x] > lakeHeightLevel) continue;

                        visited[n.y, n.x] = true;
                        queue.Enqueue(n);
                    }
                }

                // Skip tiny noise-dip "puddles" - only real basins become lakes
                if (basin.Count > 40)
                {
                    lakeBasins.Add(new List<Vector2Int>(basin));
                    foreach (var p in basin)
                    {
                        heights[p.y, p.x] = lakeHeightLevel;
                        lakeCells.Add(p);
                    }
                }
            }
        }
    }

    void SmoothLakeShores()
    {
        if (lakeCells.Count == 0) return;

        int steps = Mathf.Max(1, Mathf.RoundToInt(lakeShoreSmoothing * heightRes));
        HashSet<Vector2Int> current = new HashSet<Vector2Int>(lakeCells);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>(lakeCells);

        for (int step = 1; step <= steps; step++)
        {
            HashSet<Vector2Int> next = new HashSet<Vector2Int>();
            float falloff = 1f - (float)step / (steps + 1);

            foreach (var p in current)
            {
                Vector2Int[] neighbors = {
                    new Vector2Int(p.x + 1, p.y), new Vector2Int(p.x - 1, p.y),
                    new Vector2Int(p.x, p.y + 1), new Vector2Int(p.x, p.y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.x < 0 || n.x >= heightRes || n.y < 0 || n.y >= heightRes) continue;
                    if (visited.Contains(n)) continue;
                    if (heights[n.y, n.x] <= lakeHeightLevel) continue; // already water

                    heights[n.y, n.x] = Mathf.Lerp(heights[n.y, n.x], lakeHeightLevel, falloff * 0.6f);
                    next.Add(n);
                    visited.Add(n);
                }
            }
            current = next;
        }
    }

    // ------------------------------------------------------------------
    // WATER MESHES - a properly-sized flat quad per lake basin, and a
    // ribbon mesh per river that follows the exact carved groove (each
    // vertex samples the real terrain height at that point along the path).
    // ------------------------------------------------------------------
    void BuildWaterMeshes()
    {
        // Remove any water objects left over from a previous generation.
        for (int i = terrain.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = terrain.transform.GetChild(i);
            if (child.name.StartsWith("Water_"))
                DestroyImmediate(child.gameObject);
        }

        if (!generateWater) return;

        BuildLakeMeshes();
        BuildRiverMeshes();
    }

    void BuildLakeMeshes()
    {
        for (int i = 0; i < lakeBasins.Count; i++)
        {
            var basin = lakeBasins[i];
            if (basin.Count == 0) continue;

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var c in basin)
            {
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
            }

            float worldMinX = (float)minX / (heightRes - 1) * terrainData.size.x;
            float worldMaxX = (float)maxX / (heightRes - 1) * terrainData.size.x;
            float worldMinZ = (float)minY / (heightRes - 1) * terrainData.size.z;
            float worldMaxZ = (float)maxY / (heightRes - 1) * terrainData.size.z;

            GameObject go = new GameObject($"Water_Lake_{i}");
            go.transform.SetParent(terrain.transform, false);
            go.transform.localPosition = new Vector3(
                (worldMinX + worldMaxX) * 0.5f,
                lakeHeightLevel * terrainData.size.y + 0.05f,
                (worldMinZ + worldMaxZ) * 0.5f);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = BuildFlatQuadMesh(worldMaxX - worldMinX, worldMaxZ - worldMinZ);
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (waterMaterial != null) mr.material = waterMaterial;

            WaterFlowAnimator anim = go.AddComponent<WaterFlowAnimator>();
            anim.flowSpeed = waterFlowSpeed;
            anim.flowDirection = new Vector2(0.3f, 0.7f); // gentle diagonal shimmer for still water
        }
    }

    Mesh BuildFlatQuadMesh(float width, float length)
    {
        Mesh mesh = new Mesh();
        float hw = Mathf.Max(width, 0.5f) * 0.5f;
        float hl = Mathf.Max(length, 0.5f) * 0.5f;

        Vector3[] verts = {
            new Vector3(-hw, 0, -hl),
            new Vector3(hw, 0, -hl),
            new Vector3(-hw, 0, hl),
            new Vector3(hw, 0, hl)
        };
        Vector2[] uvs = {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
        };
        int[] tris = { 0, 2, 1, 1, 2, 3 };

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void BuildRiverMeshes()
    {
        for (int r = 0; r < riverCellSets.Count; r++)
        {
            List<Vector2Int> orderedPath = TraceRiverPath(riverCellSets[r]);
            if (orderedPath.Count < 2) continue;

            List<Vector3> worldPoints = new List<Vector3>();
            Vector3 lastAdded = new Vector3(float.PositiveInfinity, 0f, 0f);

            foreach (var cell in orderedPath)
            {
                float wx = (float)cell.x / (heightRes - 1) * terrainData.size.x;
                float wz = (float)cell.y / (heightRes - 1) * terrainData.size.z;
                float wy = heights[cell.y, cell.x] * terrainData.size.y + 0.05f;
                Vector3 p = new Vector3(wx, wy, wz);

                // Thin out points that are closer together than half the river's
                // width - keeps the mesh light and avoids near-duplicate verts.
                if (worldPoints.Count == 0 || Vector3.Distance(p, lastAdded) > riverMeshWidth * 0.5f)
                {
                    worldPoints.Add(p);
                    lastAdded = p;
                }
            }

            if (worldPoints.Count < 2) continue;

            GameObject go = BuildRiverRibbon(worldPoints, riverMeshWidth, r);
            if (go != null)
            {
                WaterFlowAnimator anim = go.AddComponent<WaterFlowAnimator>();
                anim.flowSpeed = waterFlowSpeed;
                anim.flowDirection = new Vector2(0f, 1f); // scroll along the river's length
            }
        }
    }

    // Approximates a centerline through a thick band of "river" cells by
    // greedily walking to the nearest not-yet-visited cell each step. Good
    // enough for a quick fake; a very noisy/wide band can produce a slightly
    // wobbly path, which riverMeshWidth and riverWidth help control.
    List<Vector2Int> TraceRiverPath(HashSet<Vector2Int> cells)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        if (cells.Count == 0) return path;

        List<Vector2Int> cellList = new List<Vector2Int>(cells);
        if (cellList.Count > 4000) cellList = cellList.GetRange(0, 4000); // safety cap

        HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(cellList);

        Vector2Int current = default;
        int bestScore = int.MaxValue;
        foreach (var c in remaining)
        {
            int score = c.x + c.y;
            if (score < bestScore) { bestScore = score; current = c; }
        }
        remaining.Remove(current);
        path.Add(current);

        while (remaining.Count > 0)
        {
            Vector2Int nearest = default;
            int bestDistSq = int.MaxValue;
            bool found = false;

            foreach (var c in remaining)
            {
                int dx = c.x - current.x;
                int dy = c.y - current.y;
                int distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = c;
                    found = true;
                }
            }

            // A big jump means we've reached the end of a connected strand -
            // stop rather than draw a straight line across the gap.
            if (!found || bestDistSq > 400) break;

            current = nearest;
            remaining.Remove(current);
            path.Add(current);
        }

        return path;
    }

    GameObject BuildRiverRibbon(List<Vector3> pathPoints, float width, int index)
    {
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        float distanceAccum = 0f;

        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 forward;
            if (i == 0) forward = (pathPoints[1] - pathPoints[0]).normalized;
            else if (i == pathPoints.Count - 1) forward = (pathPoints[i] - pathPoints[i - 1]).normalized;
            else forward = (pathPoints[i + 1] - pathPoints[i - 1]).normalized;

            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
            right = right.normalized * (width * 0.5f);

            verts.Add(pathPoints[i] - right);
            verts.Add(pathPoints[i] + right);

            if (i > 0) distanceAccum += Vector3.Distance(pathPoints[i], pathPoints[i - 1]);
            float v = distanceAccum / Mathf.Max(width, 0.01f);
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));

            if (i > 0)
            {
                int baseIndex = (i - 1) * 2;
                tris.Add(baseIndex); tris.Add(baseIndex + 2); tris.Add(baseIndex + 1);
                tris.Add(baseIndex + 1); tris.Add(baseIndex + 2); tris.Add(baseIndex + 3);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject($"Water_River_{index}");
        go.transform.SetParent(terrain.transform, false);
        go.transform.localPosition = Vector3.zero;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        if (waterMaterial != null) mr.material = waterMaterial;

        return go;
    }

    // ------------------------------------------------------------------
    // SETTLEMENT ZONES - reserve flat, tree-free circular plots for cities,
    // towns and villages so there's room to build later.
    // ------------------------------------------------------------------
    void PlaceZonesOfType(int count, float radius, string label)
    {
        for (int i = 0; i < count; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < 300 && !placed; attempt++)
            {
                float nx = Random.value;
                float ny = Random.value;

                float steepness = terrainData.GetSteepness(nx, ny);
                int hx = Mathf.RoundToInt(nx * (heightRes - 1));
                int hy = Mathf.RoundToInt(ny * (heightRes - 1));
                float height = heights[hy, hx];

                if (steepness > settlementMaxSteepness) continue;
                if (height <= lakeHeightLevel + 0.03f || height > 0.75f) continue;

                bool tooClose = false;
                foreach (var z in settlementZones)
                {
                    float d = Vector2.Distance(new Vector2(nx, ny), z.center);
                    if (d < (radius + z.radius) * 1.4f) { tooClose = true; break; }
                }
                if (tooClose) continue;

                settlementZones.Add(new SettlementZone
                {
                    center = new Vector2(nx, ny),
                    radius = radius,
                    centerHeight = height,
                    label = label
                });
                placed = true;
            }

            if (!placed)
                Debug.LogWarning($"Could not find flat space for a {label} zone - terrain may be too mountainous, or too small for the settlement counts/radii given.");
        }
    }

    void FlattenSettlementZones()
    {
        foreach (var zone in settlementZones)
        {
            int hx = Mathf.RoundToInt(zone.center.x * (heightRes - 1));
            int hy = Mathf.RoundToInt(zone.center.y * (heightRes - 1));
            int radiusCells = Mathf.Max(1, Mathf.RoundToInt(zone.radius * heightRes));

            for (int y = Mathf.Max(0, hy - radiusCells); y <= Mathf.Min(heightRes - 1, hy + radiusCells); y++)
            {
                for (int x = Mathf.Max(0, hx - radiusCells); x <= Mathf.Min(heightRes - 1, hx + radiusCells); x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(hx, hy)) / radiusCells;
                    if (dist > 1f) continue;

                    // Fully flat within 70% of the radius, feathering out to natural
                    // terrain by the edge so it doesn't look like a cookie-cutter disc.
                    float falloff = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - 0.7f) / 0.3f));
                    heights[y, x] = Mathf.Lerp(heights[y, x], zone.centerHeight, settlementFlattenStrength * falloff);
                }
            }
        }
    }

    float GetSettlementInfluence(float nx, float ny)
    {
        float maxInfluence = 0f;
        foreach (var zone in settlementZones)
        {
            float dist = Vector2.Distance(new Vector2(nx, ny), zone.center) / zone.radius;
            if (dist < 1f)
            {
                float influence = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - 0.7f) / 0.3f));
                maxInfluence = Mathf.Max(maxInfluence, influence);
            }
        }
        return maxInfluence;
    }

    // ------------------------------------------------------------------
    // TEXTURES - blend grass/rock/sand/forest floor by biome + slope + altitude,
    // then clear settlement plots to plain grass so they read as buildable land.
    // ------------------------------------------------------------------
    void PaintTextures()
    {
        int alphaRes = terrainData.alphamapResolution;
        int layerCount = terrainData.terrainLayers.Length;
        float[,,] splatmap = new float[alphaRes, alphaRes, layerCount];

        for (int y = 0; y < alphaRes; y++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                float normX = (float)x / (alphaRes - 1);
                float normY = (float)y / (alphaRes - 1);

                float steepness = terrainData.GetSteepness(normX, normY) / 90f;
                int hx = Mathf.RoundToInt(normX * (heightRes - 1));
                int hy = Mathf.RoundToInt(normY * (heightRes - 1));
                float biome = biomeMap[hy, hx];
                float height = heights[hy, hx];

                float grassWeight = 0f, rockWeight = 0f, sandWeight = 0f, forestFloorWeight = 0f;

                if (biome < 0.35f) sandWeight = 1f;
                else if (biome < 0.65f) grassWeight = 1f;
                else forestFloorWeight = 1f;

                float rockBlend = Mathf.Clamp01((steepness - 0.35f) * 2.5f);
                grassWeight *= (1f - rockBlend);
                sandWeight *= (1f - rockBlend);
                forestFloorWeight *= (1f - rockBlend);
                rockWeight = rockBlend;

                float altitudeRock = Mathf.Clamp01((height - 0.6f) * 2.5f);
                rockWeight = Mathf.Max(rockWeight, altitudeRock);

                float settlementInfluence = GetSettlementInfluence(normX, normY);
                if (settlementInfluence > 0f)
                {
                    grassWeight = Mathf.Lerp(grassWeight, 1f, settlementInfluence);
                    rockWeight *= (1f - settlementInfluence);
                    sandWeight *= (1f - settlementInfluence);
                    forestFloorWeight *= (1f - settlementInfluence);
                }

                float total = grassWeight + rockWeight + sandWeight + forestFloorWeight;
                if (total <= 0f) total = 1f;

                splatmap[y, x, 0] = grassWeight / total;
                splatmap[y, x, 1] = rockWeight / total;
                splatmap[y, x, 2] = sandWeight / total;
                splatmap[y, x, 3] = forestFloorWeight / total;
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmap);
    }

    // ------------------------------------------------------------------
    // VEGETATION - trees via rejection sampling per biome (density tunable),
    // skipped entirely inside settlement zones. Grass detail unchanged.
    // ------------------------------------------------------------------
    void ScatterVegetation()
    {
        List<TreePrototype> prototypes = new List<TreePrototype>();
        foreach (var prefab in grassTrees) prototypes.Add(new TreePrototype { prefab = prefab });
        int forestStart = prototypes.Count;
        foreach (var prefab in forestTrees) prototypes.Add(new TreePrototype { prefab = prefab });
        int desertStart = prototypes.Count;
        foreach (var prefab in desertPlants) prototypes.Add(new TreePrototype { prefab = prefab });

        terrainData.treePrototypes = prototypes.ToArray();

        List<TreeInstance> trees = new List<TreeInstance>();

        for (int i = 0; i < treeAttempts; i++)
        {
            float nx = Random.value;
            float ny = Random.value;

            int hx = Mathf.RoundToInt(nx * (heightRes - 1));
            int hy = Mathf.RoundToInt(ny * (heightRes - 1));
            float biome = biomeMap[hy, hx];
            float height = heights[hy, hx];
            float steepness = terrainData.GetSteepness(nx, ny);

            if (steepness > 30f || height <= lakeHeightLevel + 0.01f) continue;
            if (GetSettlementInfluence(nx, ny) > 0.15f) continue; // keep plots clear for building

            int protoIndex;
            float heightScale, widthScale;

            if (biome < 0.35f)
            {
                if (desertPlants.Length == 0 || Random.value > desertPlantDensity) continue;
                protoIndex = desertStart + Random.Range(0, desertPlants.Length);
                heightScale = widthScale = 0.8f + Random.value * 0.4f;
            }
            else if (biome < 0.65f)
            {
                if (grassTrees.Length == 0 || Random.value > grassTreeDensity) continue;
                protoIndex = Random.Range(0, grassTrees.Length);
                heightScale = widthScale = 0.9f + Random.value * 0.5f;
            }
            else
            {
                if (forestTrees.Length == 0 || Random.value > forestTreeDensity) continue;
                protoIndex = forestStart + Random.Range(0, forestTrees.Length);
                // Tall forest trees: push height noticeably above width
                heightScale = 1.4f + Random.value * 1.2f;
                widthScale = 0.9f + Random.value * 0.4f;
            }

            trees.Add(new TreeInstance
            {
                position = new Vector3(nx, height, ny),
                prototypeIndex = protoIndex,
                widthScale = widthScale,
                heightScale = heightScale,
                color = Color.white,
                lightmapColor = Color.white,
                rotation = Random.value * Mathf.PI * 2f
            });
        }

        terrainData.SetTreeInstances(trees.ToArray(), true);

        if (grassDetail != null)
        {
            terrainData.detailPrototypes = new DetailPrototype[] { grassDetail };
            int detailRes = terrainData.detailResolution;
            int[,] detailLayer = new int[detailRes, detailRes];

            for (int y = 0; y < detailRes; y++)
            {
                for (int x = 0; x < detailRes; x++)
                {
                    float nx = (float)x / detailRes;
                    float ny = (float)y / detailRes;
                    int hx = Mathf.RoundToInt(nx * (heightRes - 1));
                    int hy = Mathf.RoundToInt(ny * (heightRes - 1));
                    float biome = biomeMap[hy, hx];
                    float height = heights[hy, hx];

                    if (biome >= 0.3f && biome < 0.7f && height > lakeHeightLevel + 0.01f
                        && GetSettlementInfluence(nx, ny) < 0.15f)
                        detailLayer[y, x] = Random.Range(0, 6);
                }
            }

            terrainData.SetDetailLayer(0, 0, 0, detailLayer);
        }
    }

    // ------------------------------------------------------------------
    // EDITOR VISUALIZATION - see reserved settlement plots in the Scene view
    // ------------------------------------------------------------------
    void OnDrawGizmosSelected()
    {
        if (settlementZones == null || settlementZones.Count == 0) return;
        if (terrain == null) terrain = GetComponent<Terrain>();
        if (terrain == null) return;
        if (terrainData == null) terrainData = terrain.terrainData;
        if (terrainData == null) return;

        foreach (var zone in settlementZones)
        {
            Color c = zone.label == "City" ? Color.red : (zone.label == "Town" ? Color.yellow : Color.green);
            Gizmos.color = c;

            Vector3 worldPos = terrain.transform.position + new Vector3(
                zone.center.x * terrainData.size.x,
                zone.centerHeight * terrainData.size.y + 5f,
                zone.center.y * terrainData.size.z);

            Gizmos.DrawWireSphere(worldPos, zone.radius * terrainData.size.x);
        }
    }
}

// ============================================================================
// WATER FLOW ANIMATOR
// Auto-attached to each generated water mesh. Scrolls the material's UV
// offset each frame to fake a flowing/shimmering surface. Uses the
// Renderer's instance material, so each mesh can scroll independently.
// ============================================================================
public class WaterFlowAnimator : MonoBehaviour
{
    public float flowSpeed = 0.5f;
    public Vector2 flowDirection = new Vector2(0f, 1f);

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        if (rend == null) return;
        rend.material.mainTextureOffset += flowDirection * flowSpeed * Time.deltaTime;
    }
}