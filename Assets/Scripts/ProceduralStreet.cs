using System.Collections.Generic;
using UnityEngine;

public enum BlockType { Playground, Parking, GasStation }

[ExecuteAlways]
public class ProceduralStreet : MonoBehaviour
{
    public Transform driver;
    public GameObject roadTilePrefab;
    public GameObject[] buildingPrefabs;
    public GameObject[] treePrefabs;
    public GameObject[] benchPrefabs;
    public GameObject[] busStopPrefabs;
    public GameObject[] playgroundPrefabs;
    public GameObject[] gasStationPrefabs;
    public GameObject trashCanPrefab;
    public int supermarketEveryNChunks = 5; // ~2 per 5-min drive
    public int catsPerChunk = 2;
    public int electricityPoleEveryNTiles = 5;
    public int parkingZoneEveryNChunks = 3; // ~5 times in 5 min
    public GameObject[] parkingCarPrefabs; // assign car prefabs from Asset Store

    public float tileLength = 10f;
    public int tilesPerChunk = 10;
    public int chunksAhead = 5;
    public int chunksBehind = 1;
    public float lotHalfWidth = 15.5f;
    public float sidewalkZ = 7.0f;
    public float roadY = 0.15f;
    public float lotMargin = 1.5f;
    public int playgroundEveryNChunks = 2;
    public int gasStationEveryNChunks = 10;
    public int parkEveryNChunks = 3;
    public GameObject catPrefab;
    public GameObject[] pedestrianPrefabs;
    public int pedestriansPerChunk = 3;
    public GameObject billboardPrefab;
    [Tooltip("Ten Bis / Clalit posters pasted on some building walls.")]
    public Texture[] billboardAds;
    [Tooltip("1 = one sign every N chunks (100m each).")]
    public int billboardEveryNChunks = 2;
    public int billboardsPerChunk = 1;
    public int seedOffset = 1337;
    public bool showEditorPreview = true;

    [Header("City Backdrop")]
    [Tooltip("Material with a city panorama texture — applied to planes behind the buildings.")]
    public Material cityBackdropMaterial;
    [Tooltip("Distance behind the building line (metres).")]
    public float backdropDistance = 4f;
    [Tooltip("Height of the backdrop wall (metres).")]
    public float backdropHeight = 50f;

    [Header("Block System")]
    [Tooltip("Drive Length — how many blocks total. Street hard-stops after the last block.")]
    public int driveLength = 3;
    [Tooltip("How many chunks (100 m each) make up one block.")]
    public int chunksPerBlock = 5;

    private readonly Dictionary<int, GameObject> _spawned = new Dictionary<int, GameObject>();
    private int _lastChunk = int.MinValue;
    private BlockType[] _blockSequence;
    private int _builtSeqSeed = int.MinValue;
    private int _builtDriveLength;

    void OnEnable()
    {
        if (Application.isPlaying || showEditorPreview) RefreshChunks();
    }

    void OnDisable()
    {
        ClearAll();
    }

    void Update()
    {
        if (!Application.isPlaying && !showEditorPreview) { ClearAll(); return; }
        if (driver == null) return;
        int cur = Mathf.FloorToInt(driver.position.x / (tileLength * tilesPerChunk));
        if (cur == _lastChunk) return;
        _lastChunk = cur;
        RefreshChunks();
    }

    [ContextMenu("Rebuild Preview")]
    public void Rebuild()
    {
        _blockSequence = null;
        ClearAll();
        RefreshChunks();
    }

    [ContextMenu("Randomize Block Order")]
    public void RandomizeBlocks()
    {
        seedOffset = UnityEngine.Random.Range(0, 99999);
        Rebuild();
    }

    [ContextMenu("Print Block Order")]
    public void PrintBlockOrder()
    {
        _blockSequence = null;
        BuildBlockSequence();
        var sb = new System.Text.StringBuilder();
        sb.Append("Block order (Drive Length=" + driveLength + ", seed=" + seedOffset + "):\n");
        for (int i = 0; i < _blockSequence.Length; i++)
            sb.Append("  Block " + (i + 1) + ": " + _blockSequence[i] + "\n");
        Debug.Log(sb.ToString());
    }

    void OnValidate() { _blockSequence = null; }

    void RefreshChunks()
    {
        int cur = driver == null ? 0 : Mathf.FloorToInt(driver.position.x / (tileLength * tilesPerChunk));
        int maxChunk = Mathf.Max(1, driveLength) * Mathf.Max(1, chunksPerBlock) - 1;
        int lo = Mathf.Max(0, cur - chunksBehind);
        int hi = Mathf.Min(cur + chunksAhead, maxChunk);
        for (int i = lo; i <= hi; i++)
        {
            if (!_spawned.ContainsKey(i) || _spawned[i] == null) SpawnChunk(i);
        }
        var toRemove = new List<int>();
        foreach (var kv in _spawned)
        {
            if (kv.Key < lo || kv.Key > hi) toRemove.Add(kv.Key);
        }
        foreach (var k in toRemove) DestroyChunk(k);
    }

    void ClearAll()
    {
        foreach (var kv in _spawned)
        {
            if (kv.Value != null)
            {
                if (Application.isPlaying) Destroy(kv.Value);
                else DestroyImmediate(kv.Value);
            }
        }
        _spawned.Clear();
        _lastChunk = int.MinValue;
    }

    void DestroyChunk(int idx)
    {
        if (_spawned.TryGetValue(idx, out var go) && go != null)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        _spawned.Remove(idx);
    }

    void SpawnChunk(int idx)
    {
        var chunk = new GameObject("Chunk_" + idx);
        chunk.transform.SetParent(transform, false);
        chunk.hideFlags = HideFlags.DontSave;
        var rng = new System.Random(idx * 7919 + seedOffset);
        float chunkStartX = idx * tileLength * tilesPerChunk;

        BlockType blockType = GetBlockType(idx);
        bool hasLot = blockType == BlockType.Parking;
        bool hasParkingZone = hasLot;
        // Alternate parking side with each successive block index
        int pzSideDet = (Mathf.FloorToInt((float)idx / Mathf.Max(1, chunksPerBlock))) % 2 == 0 ? -1 : 1;
        int lotSide = -pzSideDet;

        // Carriageway is the overlay slab; skip the original 10m tiles (hidden
        // meshes were still thousands of transforms for no visible gain).

        bool placePlayground = blockType == BlockType.Playground && playgroundPrefabs != null && playgroundPrefabs.Length > 0;
        bool placeGasStation = blockType == BlockType.GasStation && gasStationPrefabs != null && gasStationPrefabs.Length > 0;

        int playgroundTile = rng.Next(tilesPerChunk);
        int playgroundSide = rng.NextDouble() < 0.5 ? -1 : 1;
        int gasTile = rng.Next(tilesPerChunk);
        int gasSide = rng.NextDouble() < 0.5 ? -1 : 1;
        if (placePlayground && placeGasStation && playgroundTile == gasTile && playgroundSide == gasSide)
        {
            gasSide = -gasSide;
        }

        var occupied = new HashSet<long>();
        long Slot(int t, int side) => ((long)t << 4) | (side > 0 ? 1L : 0L);
        bool SlotFree(int t, int side, int span)
        {
            for (int dt = -span; dt <= span; dt++)
            {
                if (occupied.Contains(Slot(t + dt, side))) return false;
            }
            return true;
        }
        void Occupy(int t, int side, int span)
        {
            for (int dt = -span; dt <= span; dt++) occupied.Add(Slot(t + dt, side));
        }

        if (placePlayground) Occupy(playgroundTile, playgroundSide, 2);
        if (placeGasStation) Occupy(gasTile, gasSide, 2);

        // Reserve building slots for the parking lot (tiles 2-8 on lot side)
        // pzSide and lotSide derived purely from idx so they stay consistent
        bool hasLot = parkingZoneEveryNChunks > 0 && idx % parkingZoneEveryNChunks == 0;
        bool hasParkingZone = hasLot; // same condition — declared early so tree/bench/pole loops can use it
        int pzSideDet = (idx / (parkingZoneEveryNChunks > 0 ? parkingZoneEveryNChunks : 1) % 2 == 0) ? 1 : -1;
        int lotSide = -pzSideDet; // lot always on opposite side from street parking
        if (hasLot)
        {
            for (int t = 2; t <= 8; t++) Occupy(t, lotSide, 0);
        }

        // Proven low-rise prefab indices: ApartmentBuilding=0, BauhausBld_1_Toto=3, TelAvivBld=5
        int[] lowBldIdx = { 0, 3, 5 };

        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            // First 4 chunks: dense low-rise fill — every 2 tiles, no skipping, varied types
            bool denseStart = (idx >= 0 && idx < 4);
            int step = denseStart ? 2 : 3;
            float skipChance = denseStart ? 0.0f : 0.10f;

            for (int t = 0; t < tilesPerChunk; t += step)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (!SlotFree(t, side, 1)) continue;
                    if (rng.NextDouble() < skipChance) continue;

                    int bldChoiceIdx;
                    if (denseStart)
                        // Cycle through the 3 low-rise types so neighbours differ
                        bldChoiceIdx = lowBldIdx[(t / step * 2 + (side > 0 ? 1 : 0) + idx * 3) % lowBldIdx.Length];
                    else
                        bldChoiceIdx = rng.Next(buildingPrefabs.Length);

                    var pf = buildingPrefabs[bldChoiceIdx];
                    var b = InstantiateChild(pf, chunk.transform);

                    float facingOffset = 0f;
                    var facing = b.GetComponent<BuildingFacing>();
                    if (facing != null) facingOffset = facing.yawOffset;

                    float baseYaw = (side > 0 ? 180f : 0f) + facingOffset + (float)(rng.NextDouble() * 10.0 - 5.0);
                    b.transform.rotation = Quaternion.Euler(0f, baseYaw, 0f) * b.transform.rotation;

                    float lotZ = side * (lotHalfWidth + (denseStart ? (float)(rng.NextDouble() * 1.5) : (float)(rng.NextDouble() * 3.0)));
                    float xJit = (float)(rng.NextDouble() * (denseStart ? 2.0 : 6.0));
                    b.transform.position = new Vector3(chunkStartX + t * tileLength + xJit, 0f, lotZ);

                    GroundAlign(b);
                    KeepOffRoad(b, side);
                    Occupy(t, side, 1);
                }
            }
        }

        StickWallPosters(placedBuildings, chunk.transform, idx, rng);

        // Background city — two staggered rows of buildings behind the building line, on both sides always
        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            for (int row = 0; row < 2; row++)
            {
                float bgZ = lotHalfWidth + 16f + row * 16f;
                int bgStep = row == 0 ? 2 : 3;
                float scaleMin = row == 0 ? 1.0f : 1.2f;
                float scaleRange = row == 0 ? 0.6f : 0.8f;
                for (int t = row; t < tilesPerChunk; t += bgStep)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var pf = buildingPrefabs[rng.Next(buildingPrefabs.Length)];
                        var b = InstantiateChild(pf, chunk.transform);
                        var facing = b.GetComponent<BuildingFacing>();
                        float facingOffset = facing != null ? facing.yawOffset : 0f;
                        float baseYaw = (side > 0 ? 180f : 0f) + facingOffset;
                        b.transform.rotation = Quaternion.Euler(0f, baseYaw, 0f) * b.transform.rotation;
                        float xJit = (float)(rng.NextDouble() * 4.0 - 2.0);
                        float zJit = (float)(rng.NextDouble() * 4.0);
                        b.transform.position = new Vector3(chunkStartX + t * tileLength + xJit, 0f, side * (bgZ + zJit));
                        float sc = scaleMin + (float)(rng.NextDouble() * scaleRange);
                        b.transform.localScale *= sc;
                        CheapBuilding(b);
                    }
                }
            }
        }


        if (placePlayground)
        {
            var pf = playgroundPrefabs[rng.Next(playgroundPrefabs.Length)];
            var g = InstantiateChild(pf, chunk.transform);
            float facingOffset = 0f;
            var facing = g.GetComponent<BuildingFacing>();
            if (facing != null) facingOffset = facing.yawOffset;
            float baseYaw = (playgroundSide > 0 ? 180f : 0f) + facingOffset + (float)(rng.NextDouble() * 10.0 - 5.0);
            g.transform.rotation = Quaternion.Euler(0f, baseYaw, 0f) * g.transform.rotation;
            g.transform.position = new Vector3(chunkStartX + playgroundTile * tileLength, 0f, playgroundSide * (sidewalkZ + 4f));
            GroundAlign(g);
            KeepOffRoad(g, playgroundSide);
        }

        bool placePark = parkEveryNChunks > 0 && idx % parkEveryNChunks == 0 && blockType == BlockType.Playground;
        if (placePark)
        {
            int parkSide = (rng.NextDouble() < 0.5 ? -1 : 1);
            // Avoid same side as playground
            if (placePlayground && parkSide == playgroundSide) parkSide = -parkSide;
            float parkX = chunkStartX;
            float parkW = tileLength * tilesPerChunk * 0.7f; // ~70m wide
            var parkGo = new GameObject("CityPark_" + idx);
            parkGo.transform.SetParent(chunk.transform, false);
            parkGo.hideFlags = HideFlags.DontSave;
            parkGo.transform.position = new Vector3(parkX, 0f, 0f);
            var park = parkGo.AddComponent<CityPark>();
            park.side           = parkSide;
            park.width          = parkW;
            park.depth          = 14f;
            park.sidewalkEdgeZ  = sidewalkZ;
            park.treePrefabs    = treePrefabs;
            park.benchPrefabs   = benchPrefabs;
            park.Build();

            // 2-3 cats strolling on the sidewalk near the park
            if (catPrefab != null)
            {
                int numCats = 2 + (idx % 2);
                for (int ci = 0; ci < numCats; ci++)
                {
                    float catX = parkX + (float)(rng.NextDouble() * parkW);
                    float catZ = parkSide * (sidewalkZ - 0.8f - (float)(rng.NextDouble() * 1.2f));
                    var catGo = new GameObject("StreetCat_" + idx + "_" + ci);
                    catGo.transform.SetParent(chunk.transform, false);
                    catGo.hideFlags = HideFlags.DontSave;
                    catGo.transform.position = new Vector3(catX, 0f, catZ);
                    var sc = catGo.AddComponent<StreetCat>();
                    sc.catPrefab   = catPrefab;
                    sc.patrolRange = 4f + (float)(rng.NextDouble() * 4f);
                    sc.speed       = 0.6f + (float)(rng.NextDouble() * 0.4f);
                    sc.Build();
                }
            }
        }

        if (placeGasStation)
        {
            var pf = gasStationPrefabs[rng.Next(gasStationPrefabs.Length)];
            var g = InstantiateChild(pf, chunk.transform);
            float facingOffset = 0f;
            var facing = g.GetComponent<BuildingFacing>();
            if (facing != null) facingOffset = facing.yawOffset;
            float baseYaw = (gasSide > 0 ? 180f : 0f) + facingOffset + (float)(rng.NextDouble() * 10.0 - 5.0);
            g.transform.rotation = Quaternion.Euler(0f, baseYaw, 0f) * g.transform.rotation;
            g.transform.position = new Vector3(chunkStartX + gasTile * tileLength, 0f, gasSide * (lotHalfWidth + 1f));
            GroundAlign(g);
            KeepOffRoad(g, gasSide);
        }

        if (treePrefabs != null && treePrefabs.Length > 0 && !hasParkingZone)
        {
            var lastTreeTile = new Dictionary<int, int> { { -1, -99 }, { 1, -99 } };
            for (int t = 0; t < tilesPerChunk; t++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    // No trees anywhere on lot chunks (both sides clear for lot asphalt)
                    if (hasLot) continue;
                    if (rng.NextDouble() >= 0.5) continue;
                    if (t - lastTreeTile[side] < 2) continue;
                    lastTreeTile[side] = t;
                    var pf = treePrefabs[rng.Next(treePrefabs.Length)];
                    var g = InstantiateChild(pf, chunk.transform);
                    float xJit = (float)(rng.NextDouble() * 4.0);
                    float zJit = (float)(rng.NextDouble() * 1.5);
                    g.transform.position = new Vector3(chunkStartX + t * tileLength + xJit, 0f, side * (sidewalkZ + zJit));
                    g.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f) * g.transform.rotation;
                }
            }
        }

        if (!hasParkingZone && benchPrefabs != null && benchPrefabs.Length > 0 && rng.NextDouble() < 0.5)
        {
            var pf = benchPrefabs[rng.Next(benchPrefabs.Length)];
            int t = rng.Next(tilesPerChunk);
            int side = rng.NextDouble() < 0.5 ? -1 : 1;
            var g = InstantiateChild(pf, chunk.transform);
            g.transform.position = new Vector3(chunkStartX + t * tileLength, 0f, side * (sidewalkZ - 1f));
            g.transform.rotation = Quaternion.Euler(0f, side > 0 ? 0f : 180f, 0f) * g.transform.rotation;
            GroundAlign(g);
        }

        // Extract materials from road tile prefab
        // [0]=Pavement (sidewalk), [1]=Border, [2]=Road-1-line (asphalt)
        Material roadSurfaceMat = null;   // asphalt — used by ParkingLot, BusBay
        Material pavementMat    = null;   // sidewalk tile — used by ParkingZone bay
        if (roadTilePrefab != null)
        {
            var r = roadTilePrefab.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                var mats = r.sharedMaterials;
                if (mats.Length > 0) pavementMat    = mats[0];
                if (mats.Length > 2) roadSurfaceMat = mats[2];
            }
        }

        int bsBaySide = 0;
        float bsBayGapStart = -1f, bsBayGapEnd = -1f;

        if (busStopPrefabs != null && busStopPrefabs.Length > 0 && rng.NextDouble() < 0.85)
        {
            var pf = busStopPrefabs[rng.Next(busStopPrefabs.Length)];
            int bsT = rng.Next(tilesPerChunk);
            int bsSide = rng.NextDouble() < 0.5 ? -1 : 1;

            // Bus bay: ~12m wide, just enough for one bus + a bit of run-in/run-out
            float bayLen = 12f;
            float bayExtra = 3.0f;
            float bayLocalX = Mathf.Max(0f, bsT * tileLength - bayLen * 0.3f);

            // Record gap so curb stripe on bus-stop side gets cut out
            bsBaySide = bsSide;
            bsBayGapStart = Mathf.Max(0f, bayLocalX - 1f);
            bsBayGapEnd   = bayLocalX + bayLen + 1f;

            var bayGo = new GameObject("BusBay_" + idx);
            bayGo.transform.SetParent(chunk.transform, false);
            bayGo.hideFlags = HideFlags.DontSave;
            bayGo.transform.position = new Vector3(chunkStartX + bayLocalX, 0f, 0f); // Y=0, not roadY
            var bay = bayGo.AddComponent<BusBay>();
            bay.side = bsSide;
            bay.bayLength = bayLen;
            bay.extraWidth = bayExtra;
            bay.roadEdgeZ = 3.5f;
            bay.roadSurfaceY = roadY;
            bay.roadSurfaceMaterial = pavementMat;
            bay.Build();

            // Strip Tree* from all road tile children on bus stop side, and free-standing trees
            var bsTreeKill = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in chunk.transform)
            {
                if (c.name.StartsWith("Road_1_line"))
                {
                    foreach (Transform gc in c)
                        if (gc.name.StartsWith("Tree")) bsTreeKill.Add(gc.gameObject);
                }
                else if (c.name.StartsWith("Tree"))
                {
                    // Free-standing tree — check it is on the bus stop side
                    if ((bsSide > 0 && c.position.z > 0) || (bsSide < 0 && c.position.z < 0))
                        bsTreeKill.Add(c.gameObject);
                }
            }
            foreach (var kill in bsTreeKill)
                { if (Application.isPlaying) Destroy(kill); else DestroyImmediate(kill); }

            // Shelter closer to road — just inside the bay
            var g = InstantiateChild(pf, chunk.transform);
            float shelterZ = bsSide * (1.1f + 1.6f);
            g.transform.position = new Vector3(chunkStartX + bsT * tileLength, 0f, shelterZ);
            g.transform.rotation = Quaternion.Euler(0f, bsSide > 0 ? 0f : 180f, 0f) * g.transform.rotation;
            GroundAlign(g);
        }

        // GasStation blocks always spawn billboards (more of them); other blocks use the normal cadence.
        bool spawnBillboard = billboardPrefab != null && billboardsPerChunk > 0 && (
            blockType == BlockType.GasStation ||
            (billboardEveryNChunks > 0 && ((idx % billboardEveryNChunks) + billboardEveryNChunks) % billboardEveryNChunks == 0));
        if (idx == 0) spawnBillboard = billboardPrefab != null && billboardsPerChunk > 0;
        if (spawnBillboard)
        {
            int n = Mathf.Max(1, blockType == BlockType.GasStation ? billboardsPerChunk + 2 : billboardsPerChunk);
            float bbChunkLen = tileLength * tilesPerChunk;
            for (int i = 0; i < n; i++)
            {
                int bbSide = rng.NextDouble() < 0.5 ? -1 : 1;
                if (hasParkingZone && bbSide == pzSideDet) bbSide = -bbSide;
                float x;
                if (idx == 0)
                    x = chunkStartX + 8f + (float)rng.NextDouble() * 10f;
                else
                    x = chunkStartX + 20f + (float)rng.NextDouble() * Mathf.Max(20f, bbChunkLen - 40f);
                float z = bbSide * (sidewalkZ + 0.8f);

                var g = InstantiateChild(billboardPrefab, chunk.transform);
                g.name = "Billboard_" + idx + "_" + i;
                g.transform.localScale = Vector3.one * 1.35f;
                g.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                g.transform.position = new Vector3(x, 0f, z);
                GroundAlign(g);
                foreach (var col in g.GetComponentsInChildren<Collider>())
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
            }
        }

        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            for (int t = 0; t < tilesPerChunk; t += 3)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (!occupied.Contains(((long)t << 4) | (side > 0 ? 1L : 0L))) continue; // only where building placed
                    if (rng.NextDouble() < 0.5f) continue;
                    var binGo = new GameObject("TrashBin");
                    binGo.transform.SetParent(chunk.transform, false);
                    binGo.hideFlags = HideFlags.DontSave;
                    // Place near the building entrance — close to sidewalk edge
                    float binX = chunkStartX + t * tileLength + (float)(rng.NextDouble() * 2.0);
                    float binZ = side * (sidewalkZ + 0.6f);
                    binGo.transform.position = new Vector3(binX, 0f, binZ);
                    binGo.transform.rotation = Quaternion.Euler(0f, side > 0 ? 160f : 20f, 0f);
                    binGo.AddComponent<IsraeliTrashBin>();
                }
            }
        }

        // ── Curb stripe rules ────────────────────────────────────────────────────
        // Parallel parking:  BOTH sides gap the 36m zone.
        //   pzSide — ParkingZone draws blue/white there instead.
        //   opposite side — no curb at all during the parking (bare road widens).
        // Bus bay side:      gap where bay sits — BusBay draws red/yellow at road edge.
        //   opposite side — full red/white (no change).
        // Parking lot side:  gap tiles 2-8 only (driveway entrance).
        // Default:           full red/white both sides.
        float chunkLen    = tileLength * tilesPerChunk;
        float pzZoneLen   = 36f;  // matches ParkingZone.zoneLength
        float lotGapStart = 4f * tileLength;   // lot placed at tile 4
        float lotGapEnd   = 6f * tileLength;   // driveway ~7m wide, covers tiles 4-6

        for (int side = -1; side <= 1; side += 2)
        {
            var curbGo = new GameObject("Curb_" + (side > 0 ? "R" : "L"));
            curbGo.transform.SetParent(chunk.transform, false);
            curbGo.hideFlags = HideFlags.DontSave;
            curbGo.transform.position = new Vector3(chunkStartX, roadY, 0f);
            var stripe = curbGo.AddComponent<CurbStripe>();
            stripe.length = chunkLen;
            stripe.side = side;
            stripe.zOffset = 3.5f;
            stripe.stripeHeight = 0.08f;
            stripe.stripeWidth = 0.14f;

            if (hasParkingZone && side == pzSideDet)
            {
                stripe.gapStart = 0f;
                stripe.gapEnd   = pzZoneLen;
            }
            else if (hasLot && side == lotSide)
            {
                stripe.gapStart = lotGapStart;
                stripe.gapEnd   = lotGapEnd;
            }

            // Bus bay gap always applied as gap2 so it stacks with any gap1 above
            if (bsBaySide != 0 && side == bsBaySide)
            {
                stripe.gap2Start = bsBayGapStart;
                stripe.gap2End   = bsBayGapEnd;
            }

            stripe.Build();
        }

        // Supermarket every N chunks — skip if this chunk has a parking lot
        if (supermarketEveryNChunks > 0 && idx % supermarketEveryNChunks == 0 && !hasLot)
        {
            int side = rng.NextDouble() < 0.5 ? -1 : 1;
            int t = tilesPerChunk / 2;
            var smGo = new GameObject("Supermarket_" + idx);
            smGo.transform.SetParent(chunk.transform, false);
            smGo.hideFlags = HideFlags.DontSave;
            smGo.AddComponent<IsraeliSupermarket>();
            float lotZ = side * (lotHalfWidth + 2f);
            smGo.transform.position = new Vector3(chunkStartX + t * tileLength, 0f, lotZ);
            smGo.transform.rotation = Quaternion.Euler(0f, side > 0 ? 180f : 0f, 0f);
        }

        // City garden — small park every 7 chunks, alternating sides
        if (idx % 7 == 0 && !hasLot)
        {
            int gSide = (idx / 7 % 2 == 0) ? 1 : -1;
            var gardenGo = new GameObject("CityGarden_" + idx);
            gardenGo.transform.SetParent(chunk.transform, false);
            gardenGo.hideFlags = HideFlags.DontSave;
            gardenGo.transform.position = new Vector3(chunkStartX + tileLength * 3f, 0f, 0f);
            var garden = gardenGo.AddComponent<CityGarden>();
            garden.side = gSide;
            garden.width = 18f;
            garden.depth = 14f;
            garden.Build();
        }

        // Cats near trash bins and buildings — auto-load Kitty_001 if not manually assigned
#if UNITY_EDITOR
        if (catPrefab == null)
            catPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ithappy/Animals_FREE/Prefabs/Kitty_001.prefab");
#endif
        if (catPrefab != null)
        {
            // Collect trash bin positions to place cats nearby
            var trashPositions = new System.Collections.Generic.List<Vector3>();
            foreach (Transform c in chunk.transform)
                if (c.name.StartsWith("TrashBin")) trashPositions.Add(c.position);

            int catsThisChunk = Mathf.Max(0, catsPerChunk);
            for (int ci = 0; ci < catsThisChunk; ci++)
            {
                if (trashPositions.Count > 0 && rng.NextDouble() < 0.6f)
                {
                    // Place next to a trash bin
                    var bin = trashPositions[rng.Next(trashPositions.Count)];
                    catX = bin.x + (float)(rng.NextDouble() * 1.5 - 0.75);
                    catZ = bin.z + (float)(rng.NextDouble() * 0.6 - 0.3);
                    catSide = bin.z >= 0 ? 1 : -1;
                }
                else
                {
                    // Place along building edge / inner sidewalk
                    int catT = rng.Next(tilesPerChunk);
                    catX = chunkStartX + catT * tileLength + (float)(rng.NextDouble() * tileLength * 0.8f);
                    // Keep between road edge (sidewalkZ=5.5) and buildings — never on road
                    catZ = catSide * (sidewalkZ + 0.5f + (float)(rng.NextDouble() * 2.5f));
                }

                var catGo = new GameObject("StreetCat_" + idx + "_" + ci);
                catGo.transform.SetParent(chunk.transform, false);
                catGo.hideFlags = HideFlags.DontSave;
                // Face along the street
                catGo.transform.rotation = Quaternion.Euler(0f, catSide > 0 ? 90f : -90f, 0f);
                catGo.transform.position = new Vector3(catX, roadY, catZ);
                var sc = catGo.AddComponent<StreetCat>();
                sc.catPrefab     = catPrefab;
                sc.patrolRange   = 3f + (float)(rng.NextDouble() * 4f);
                sc.speed         = 0.5f + (float)(rng.NextDouble() * 0.4f);
                sc.sidewalkMinZ  = sidewalkZ - 0.5f; // just inside road edge
                sc.sidewalkMaxZ  = sidewalkZ + 3.5f; // up to building line
                sc.Build();
            }
        }

        if (pedestrianPrefabs != null && pedestrianPrefabs.Length > 0 && pedestriansPerChunk > 0)
        {
            for (int pi = 0; pi < pedestriansPerChunk; pi++)
            {
                int side = rng.NextDouble() < 0.5 ? -1 : 1;
                float px = chunkStartX + (float)(rng.NextDouble() * tilesPerChunk * tileLength);
                float pz = side * (RoadEdgeZ + 1.0f + (float)(rng.NextDouble() * (sidewalkZ - RoadEdgeZ - 1.4f)));
                float yaw = (rng.NextDouble() < 0.5 ? 90f : 270f) + (float)(rng.NextDouble() * 10.0 - 5.0);
                SpawnPedestrian(chunk.transform, new Vector3(px, roadY, pz), yaw, rng);
            }
        }

        // Electricity poles along both sidewalks — skip on parking zone chunks
        if (electricityPoleEveryNTiles > 0 && !hasParkingZone)
        {
            float poleSpacingM = electricityPoleEveryNTiles * tileLength;
            for (int t = 0; t < tilesPerChunk; t += electricityPoleEveryNTiles)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    // No skip — every pole must exist so wires connect to the next
                    var poleGo = new GameObject("ElecPole");
                    poleGo.transform.SetParent(chunk.transform, false);
                    poleGo.hideFlags = HideFlags.DontSave;
                    // No X jitter — exact position so wires line up
                    poleGo.transform.position = new Vector3(chunkStartX + t * tileLength, 0f, side * (sidewalkZ + 2.5f));
                    var pole = poleGo.AddComponent<ElectricityPole>();
                    pole.poleSpacing = poleSpacingM;
                }
            }
        }

        // Street parking zone + off-street parking lot, 3 times in a 5-min drive
        if (parkingZoneEveryNChunks > 0 && idx % parkingZoneEveryNChunks == 0)
        {
            int pzSide = pzSideDet;

            var pzGo = new GameObject("ParkingZone_" + idx);
            pzGo.transform.SetParent(chunk.transform, false);
            pzGo.hideFlags = HideFlags.DontSave;
            pzGo.transform.position = new Vector3(chunkStartX, 0f, 0f);
            var pz = pzGo.AddComponent<ParkingZone>();
            pz.zoneLength = 36f;
            pz.side = pzSide;
            pz.curbZ = 3.5f;
            pz.carPrefabs = parkingCarPrefabs;
            pz.roadSurfaceMaterial = pavementMat;
            pz.Build();

            var plGo = new GameObject("ParkingLot_" + idx);
            plGo.transform.SetParent(chunk.transform, false);
            plGo.hideFlags = HideFlags.DontSave;
            plGo.transform.position = new Vector3(chunkStartX + tileLength * 4f, 0f, 0f);
            var pl = plGo.AddComponent<ParkingLot>();
            pl.side = -pzSide;
            pl.roadEdgeZ = 3.5f;
            pl.rows = 2;
            pl.cols = 4;
            pl.carPrefabs = parkingCarPrefabs;
            pl.roadSurfaceMaterial = roadSurfaceMat;
            pl.Build();
        }

        {
            float bdLen = tileLength * tilesPerChunk;
            float cx = chunkStartX + bdLen * 0.5f;
            float backdropZ = lotHalfWidth + backdropDistance;
            Material bdMat = cityBackdropMaterial != null ? cityBackdropMaterial : MakeCityBackdrop();
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 2; row++)
                {
                    float rowZ = backdropZ + row * 20f;
                    float rowH = backdropHeight - row * 8f;
                    float halfH = rowH * 0.5f;
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "CityBackdrop";
                    quad.transform.SetParent(chunk.transform, false);
                    quad.hideFlags = HideFlags.DontSave;
                    quad.transform.position = new Vector3(cx, halfH, side * rowZ);
                    quad.transform.rotation = Quaternion.Euler(0f, side > 0 ? 180f : 0f, 0f);
                    quad.transform.localScale = new Vector3(bdLen, rowH, 1f);
                    var r = quad.GetComponent<Renderer>();
                    r.sharedMaterial = bdMat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    var col = quad.GetComponent<Collider>();
                    if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
                }
            }
        }

        _spawned[idx] = chunk;
    }

    void SpawnStreetCat(Transform parent, Vector3 pos, float yaw, System.Random rng)
    {
        if (catPrefab == null) return;

        var catGo = new GameObject("StreetCat");
        catGo.transform.SetParent(parent, worldPositionStays: false);
        // Place at Y=0 so Build()'s ground-align lands feet at Y=0, then we lift to road surface
        catGo.transform.position = new Vector3(pos.x, 0f, pos.z);
        catGo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        try
        {
            var sc = catGo.AddComponent<StreetCat>();
            sc.catPrefab = catPrefab;
            sc.patrolRange = 3f + (float)(rng.NextDouble() * 4f);
            sc.speed = 0.5f + (float)(rng.NextDouble() * 0.4f);
            sc.sidewalkMinZ = RoadEdgeZ + 0.3f;
            sc.sidewalkMaxZ = sidewalkZ;
            sc.Build();
            // Build() aligns the model feet to Y=0; lift the whole cat up to the road surface
            var cp = catGo.transform.position;
            catGo.transform.position = new Vector3(cp.x, roadY, cp.z);
            CheapProp(catGo);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("StreetCat spawn failed: " + ex.Message);
        }

        SetHideFlagsRecursive(catGo.transform);
    }

    void SpawnPedestrian(Transform parent, Vector3 pos, float yaw, System.Random rng)
    {
        if (pedestrianPrefabs == null || pedestrianPrefabs.Length == 0) return;
        var pf = pedestrianPrefabs[rng.Next(pedestrianPrefabs.Length)];
        if (pf == null) return;

        var go = new GameObject("Pedestrian");
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.position = new Vector3(pos.x, 0f, pos.z);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.hideFlags = HideFlags.DontSave;

        try
        {
            GameObject model;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                model = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pf, go.transform);
            else
#endif
                model = Instantiate(pf, go.transform);

            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Ground-align: shift up so feet sit at Y=0
            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                var p = model.transform.position;
                model.transform.position = new Vector3(p.x, p.y - b.min.y, p.z);
            }

            model.hideFlags = HideFlags.DontSave;

            // Play legacy animation (Mixamo FBX imported with Legacy rig)
            var legacyAnim = model.GetComponentInChildren<Animation>();
            if (legacyAnim != null) { legacyAnim.wrapMode = WrapMode.Loop; legacyAnim.Play(); }

            // Random shirt colour — tints the body material per pedestrian
            Color[] shirtColors = {
                new Color(0.85f, 0.20f, 0.20f), // red
                new Color(0.20f, 0.40f, 0.85f), // blue
                new Color(0.20f, 0.65f, 0.25f), // green
                new Color(0.90f, 0.55f, 0.10f), // orange
                new Color(0.55f, 0.20f, 0.70f), // purple
                new Color(0.15f, 0.15f, 0.15f), // black
                new Color(0.95f, 0.95f, 0.95f), // white
                new Color(0.30f, 0.55f, 0.50f), // teal
            };
            Color shirt = shirtColors[rng.Next(shirtColors.Length)];
            var block = new MaterialPropertyBlock();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name.ToLower().Contains("body"))
                {
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", shirt);
                    block.SetColor("_Color", shirt);
                    r.SetPropertyBlock(block);
                }
            }

            var sp = go.AddComponent<StreetPedestrian>();
            sp.speed = 1.2f + (float)(rng.NextDouble() * 0.4);
            sp.patrolRange = 12f + (float)(rng.NextDouble() * 10f);
            sp.sidewalkMinZ = RoadEdgeZ + 0.5f;
            sp.sidewalkMaxZ = sidewalkZ;
            sp.Build();

            var cp = go.transform.position;
            go.transform.position = new Vector3(cp.x, roadY, cp.z);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Pedestrian spawn failed: " + ex.Message);
        }

        SetHideFlagsRecursive(go.transform);
    }

    void StripBakedStreetFurniture(GameObject road)
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in road.transform)
        {
            string n = child.name;
            if (n.StartsWith("Bench_") || n.StartsWith("Trash_can") || n.StartsWith("Tree"))
            {
                toDestroy.Add(child.gameObject);
            }
            else if (n.StartsWith("Road_1_line"))
            {
                var poles = new List<GameObject>();
                foreach (Transform gc in child)
                    if (gc.name.StartsWith("Pole"))
                        poles.Add(gc.gameObject);
                foreach (var p in poles)
                {
                    if (Application.isPlaying) Destroy(p); else DestroyImmediate(p);
                }
            }
        }
        foreach (var go in toDestroy)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }

    static Material _cityBackdrop;
    static Material MakeCityBackdrop()
    {
        if (_cityBackdrop == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            _cityBackdrop = new Material(sh);
            _cityBackdrop.color = new Color(0.72f, 0.70f, 0.66f);
            if (_cityBackdrop.HasProperty("_Smoothness")) _cityBackdrop.SetFloat("_Smoothness", 0f);
            if (_cityBackdrop.HasProperty("_Glossiness")) _cityBackdrop.SetFloat("_Glossiness", 0f);
            _cityBackdrop.hideFlags = HideFlags.HideAndDontSave;
        }
        return _cityBackdrop;
    }

    static Material _asphalt;
    static Material _yellow;
    static Material _whitePaint;

    static Material MakeAsphalt()
    {
        if (_asphalt == null)
        {
            _asphalt = new Material(Shader.Find("Standard"));
            _asphalt.color = new Color(0.25f, 0.25f, 0.25f);
            _asphalt.SetFloat("_Glossiness", 0.12f);
            _asphalt.SetFloat("_Metallic", 0f);
            _asphalt.hideFlags = HideFlags.HideAndDontSave;
        }
        return _asphalt;
    }

    void SpawnWideCarriageway(Transform parent, float startX, float length, Material asphalt, Material pavement,
        int skipWalkSide = 0, float skipWalkStart = -1f, float skipWalkEnd = -1f)
    {
        if (asphalt == null) asphalt = MakeAsphalt();
        if (_yellow == null)
        {
            _yellow = new Material(Shader.Find("Standard"));
            _yellow.color = new Color(0.92f, 0.78f, 0.12f);
            _yellow.hideFlags = HideFlags.HideAndDontSave;
        }
        if (_whitePaint == null)
        {
            _whitePaint = new Material(Shader.Find("Standard"));
            _whitePaint.color = new Color(0.92f, 0.92f, 0.90f);
            _whitePaint.hideFlags = HideFlags.HideAndDontSave;
        }

        float y = roadY + 0.02f;
        float half = RoadEdgeZ;
        float cx = startX + length * 0.5f;
        float roadW = half * 2f;

        Slab(parent, asphalt, new Vector3(cx, y, 0f), new Vector3(length, 0.04f, roadW));

        float walk = Mathf.Max(1.6f, sidewalkZ - RoadEdgeZ);
        float walkZ = half + walk * 0.5f;
        var walkMat = pavement != null ? Tiled(pavement, length, walk, 4f, 4f) : asphalt;
        SpawnSidewalk(parent, walkMat, startX, length, y, walkZ, walk, 1, skipWalkSide, skipWalkStart, skipWalkEnd);
        SpawnSidewalk(parent, walkMat, startX, length, y, -walkZ, walk, -1, skipWalkSide, skipWalkStart, skipWalkEnd);

        float lineY = y + 0.022f;
        Slab(parent, _yellow, new Vector3(cx, lineY, half - 0.14f), new Vector3(length, 0.01f, 0.14f));
        Slab(parent, _yellow, new Vector3(cx, lineY, -(half - 0.14f)), new Vector3(length, 0.01f, 0.14f));
        for (float x = 0.4f; x < length; x += 4.5f)
        {
            float dash = Mathf.Min(2.4f, length - x);
            Slab(parent, _whitePaint, new Vector3(startX + x + dash * 0.5f, lineY, 0f), new Vector3(dash, 0.01f, 0.14f));
        }
    }

    void SpawnSidewalk(Transform parent, Material walkMat, float startX, float length, float y,
        float walkZ, float walk, int side, int skipWalkSide, float skipStart, float skipEnd)
    {
        if (skipWalkSide == side && skipEnd > skipStart)
        {
            float a0 = startX;
            float a1 = startX + skipStart;
            float b0 = startX + skipEnd;
            float b1 = startX + length;
            if (a1 - a0 > 0.5f)
                Slab(parent, walkMat, new Vector3((a0 + a1) * 0.5f, y - 0.005f, walkZ), new Vector3(a1 - a0, 0.03f, walk));
            if (b1 - b0 > 0.5f)
                Slab(parent, walkMat, new Vector3((b0 + b1) * 0.5f, y - 0.005f, walkZ), new Vector3(b1 - b0, 0.03f, walk));
            return;
        }
        float cx = startX + length * 0.5f;
        Slab(parent, walkMat, new Vector3(cx, y - 0.005f, walkZ), new Vector3(length, 0.03f, walk));
    }

    void GroundAlign(GameObject go)
    {
        var rends = go.GetComponentsInChildren<MeshRenderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        var p = go.transform.position;
        go.transform.position = new Vector3(p.x, p.y - b.min.y, p.z);
    }

    void KeepOffRoad(GameObject go, int side)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        float roadEdge = sidewalkZ + lotMargin;
        float shift = 0f;
        if (side > 0 && b.min.z < roadEdge) shift = roadEdge - b.min.z;
        else if (side < 0 && b.max.z > -roadEdge) shift = -roadEdge - b.max.z;
        if (Mathf.Abs(shift) > 0.001f)
        {
            var p = go.transform.position;
            go.transform.position = new Vector3(p.x, p.y, p.z + shift);
        }
    }

    GameObject InstantiateChild(GameObject prefab, Transform parent)
    {
        GameObject go;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            if (go == null) go = Instantiate(prefab, parent);
        }
        else
#endif
        {
            go = Instantiate(prefab, parent);
        }
        // Zero out any baked Y offset the prefab may have been saved with
        var p = go.transform.position;
        go.transform.position = new Vector3(p.x, 0f, p.z);
        SetHideFlagsRecursive(go.transform);
        return go;
    }

    void SetHideFlagsRecursive(Transform t)
    {
        t.gameObject.hideFlags = HideFlags.DontSave;
        for (int i = 0; i < t.childCount; i++) SetHideFlagsRecursive(t.GetChild(i));
    }

    BlockType GetBlockType(int chunkIdx)
    {
        BuildBlockSequence();
        int n = Mathf.Max(1, chunksPerBlock);
        int blockFloor = Mathf.FloorToInt((float)chunkIdx / n);
        int blockIdx = Mathf.Clamp(blockFloor, 0, Mathf.Max(1, driveLength) - 1);
        return _blockSequence[blockIdx];
    }

    void BuildBlockSequence()
    {
        int safeCount = Mathf.Max(1, driveLength);
        if (_blockSequence != null && _builtSeqSeed == seedOffset && _builtDriveLength == safeCount) return;
        _builtSeqSeed    = seedOffset;
        _builtDriveLength = safeCount;
        _blockSequence   = new BlockType[safeCount];
        var types = new BlockType[] { BlockType.Playground, BlockType.Parking, BlockType.GasStation };
        var rng = new System.Random(seedOffset ^ 0xBEEF);
        int lastPick = -1;
        for (int i = 0; i < safeCount; i++)
        {
            int pick;
            int tries = 0;
            do { pick = rng.Next(types.Length); tries++; } while (pick == lastPick && tries < 10);
            _blockSequence[i] = types[pick];
            lastPick = pick;
        }
    }
}
