using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ProceduralStreet : MonoBehaviour
{
    public const float RoadEdgeZ = 3.6f; // 3.6m per lane — wide enough for a car

    public Transform driver;
    public GameObject roadTilePrefab;
    public GameObject[] buildingPrefabs;
    public GameObject[] treePrefabs;
    public GameObject[] benchPrefabs;
    public GameObject[] busStopPrefabs;
    public GameObject[] playgroundPrefabs;
    public GameObject[] gasStationPrefabs;
    public GameObject trashCanPrefab;
    public int supermarketEveryNChunks = 5;
    public int catsPerChunk = 1;
    public int electricityPoleEveryNTiles = 5;
    public int parkingZoneEveryNChunks = 3;
    public GameObject[] parkingCarPrefabs;

    public float tileLength = 10f;
    public int tilesPerChunk = 10;
    public int chunksAhead = 3;
    public int chunksBehind = 1;
    public float lotHalfWidth = 16.5f;
    public float sidewalkZ = 6.2f;
    public float roadY = 0.15f;
    public float lotMargin = 1.5f;
    public int playgroundEveryNChunks = 2;
    public int gasStationEveryNChunks = 10;
    public int parkEveryNChunks = 3;
    public GameObject catPrefab;
    public GameObject billboardPrefab;
    [Tooltip("Ten Bis / Clalit posters pasted on some building walls.")]
    public Texture[] billboardAds;
    [Tooltip("1 = one sign every N chunks (100m each).")]
    public int billboardEveryNChunks = 2;
    public int billboardsPerChunk = 1;
    public int seedOffset = 1337;
    public bool showEditorPreview = true;

    private readonly Dictionary<int, GameObject> _spawned = new Dictionary<int, GameObject>();
    private int _lastChunk = int.MinValue;

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            // Meshy buildings are 0.3–1.2M verts each. Keep shadow maps short
            // or the GPU spends the whole frame redrawing the city for shadows.
            QualitySettings.shadowDistance = 35f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.pixelLightCount = 1;
        }
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
        ClearAll();
        RefreshChunks();
    }

    void RefreshChunks()
    {
        int cur = driver == null ? 0 : Mathf.FloorToInt(driver.position.x / (tileLength * tilesPerChunk));
        for (int i = cur - chunksBehind; i <= cur + chunksAhead; i++)
        {
            if (!_spawned.ContainsKey(i) || _spawned[i] == null) SpawnChunk(i);
        }
        var toRemove = new List<int>();
        foreach (var kv in _spawned)
        {
            if (kv.Key < cur - chunksBehind || kv.Key > cur + chunksAhead) toRemove.Add(kv.Key);
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

        bool hasLot = parkingZoneEveryNChunks > 0 && idx % parkingZoneEveryNChunks == 0;
        bool hasParkingZone = hasLot;
        // -1 is the driver's right when facing +X (right-hand traffic).
        int pzSideDet = (idx / (parkingZoneEveryNChunks > 0 ? parkingZoneEveryNChunks : 1) % 2 == 0) ? -1 : 1;
        int lotSide = -pzSideDet;

        // Carriageway is the overlay slab; skip the original 10m tiles (hidden
        // meshes were still thousands of transforms for no visible gain).

        bool placePlayground = playgroundPrefabs != null && playgroundPrefabs.Length > 0 && playgroundEveryNChunks > 0 && (idx % playgroundEveryNChunks == 0);
        bool placeGasStation = gasStationPrefabs != null && gasStationPrefabs.Length > 0 && gasStationEveryNChunks > 0 && (idx % gasStationEveryNChunks == 0);

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
        if (hasLot)
        {
            for (int t = 2; t <= 8; t++) Occupy(t, lotSide, 0);
        }

        var placedBuildings = new List<KeyValuePair<GameObject, int>>();
        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            // Every 20m, no skips, little jitter — a continuous street wall, not scattered lots.
            const int step = 2;
            for (int t = 0; t < tilesPerChunk; t += step)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (!SlotFree(t, side, 0)) continue;

                    var pf = buildingPrefabs[rng.Next(buildingPrefabs.Length)];
                    var b = InstantiateChild(pf, chunk.transform);

                    float facingOffset = 0f;
                    var facing = b.GetComponent<BuildingFacing>();
                    if (facing != null) facingOffset = facing.yawOffset;

                    float baseYaw = (side > 0 ? 180f : 0f) + facingOffset + (float)(rng.NextDouble() * 3.0 - 1.5);
                    b.transform.rotation = Quaternion.Euler(0f, baseYaw, 0f) * b.transform.rotation;

                    float lotZ = side * lotHalfWidth;
                    float xJit = (float)(rng.NextDouble() * 1.2);
                    b.transform.position = new Vector3(chunkStartX + t * tileLength + xJit, 0f, lotZ);

                    GroundAlign(b);
                    KeepOffRoad(b, side);
                    CheapBuilding(b);
                    Occupy(t, side, 0);
                    placedBuildings.Add(new KeyValuePair<GameObject, int>(b, side));
                }
            }
        }

        StickWallPosters(placedBuildings, chunk.transform, idx, rng);

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
            CheapBuilding(g);
        }

        bool placePark = parkEveryNChunks > 0 && idx % parkEveryNChunks == 0 && !hasLot;
        if (placePark)
        {
            int parkSide = (rng.NextDouble() < 0.5 ? -1 : 1);
            if (placePlayground && parkSide == playgroundSide) parkSide = -parkSide;
            float parkX = chunkStartX;
            float parkW = tileLength * tilesPerChunk * 0.7f;
            var parkGo = new GameObject("CityPark_" + idx);
            parkGo.transform.SetParent(chunk.transform, false);
            parkGo.hideFlags = HideFlags.DontSave;
            parkGo.transform.position = new Vector3(parkX, 0f, 0f);
            var park = parkGo.AddComponent<CityPark>();
            park.side = parkSide;
            park.width = parkW;
            park.depth = 14f;
            park.sidewalkEdgeZ = sidewalkZ;
            park.treePrefabs = treePrefabs;
            park.benchPrefabs = benchPrefabs;
            park.Build();

            int numCats = catsPerChunk > 0 ? 1 : 0;
            for (int ci = 0; ci < numCats; ci++)
            {
                float catX = parkX + (float)(rng.NextDouble() * parkW);
                float catZ = parkSide * (sidewalkZ - 0.8f - (float)(rng.NextDouble() * 1.2f));
                SpawnStreetCat(chunk.transform, new Vector3(catX, 0f, catZ), parkSide > 0 ? 90f : -90f, rng);
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
            CheapBuilding(g);
        }

        if (treePrefabs != null && treePrefabs.Length > 0 && !hasParkingZone)
        {
            var lastTreeTile = new Dictionary<int, int> { { -1, -99 }, { 1, -99 } };
            for (int t = 0; t < tilesPerChunk; t++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
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
                    CheapProp(g);
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

        Material pavementMat = null;
        if (roadTilePrefab != null)
        {
            var r = roadTilePrefab.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                var mats = r.sharedMaterials;
                if (mats.Length > 0) pavementMat = mats[0];
            }
        }
        Material asphaltMat = MakeAsphalt();

        SpawnWideCarriageway(
            chunk.transform, chunkStartX, tileLength * tilesPerChunk, asphaltMat, pavementMat,
            hasParkingZone ? pzSideDet : 0, 0f, hasParkingZone ? 36f : -1f);

        int bsBaySide = 0;
        float bsBayGapStart = -1f, bsBayGapEnd = -1f;

        if (busStopPrefabs != null && busStopPrefabs.Length > 0 && rng.NextDouble() < 0.85)
        {
            var pf = busStopPrefabs[rng.Next(busStopPrefabs.Length)];
            int bsT = rng.Next(tilesPerChunk);
            int bsSide = rng.NextDouble() < 0.5 ? -1 : 1;

            float bayLen = 12f;
            float bayExtra = 3.0f;
            float bayLocalX = Mathf.Max(0f, bsT * tileLength - bayLen * 0.3f);

            bsBaySide = bsSide;
            bsBayGapStart = bayLocalX;
            bsBayGapEnd = bayLocalX + bayLen;

            var bayGo = new GameObject("BusBay_" + idx);
            bayGo.transform.SetParent(chunk.transform, false);
            bayGo.hideFlags = HideFlags.DontSave;
            bayGo.transform.position = new Vector3(chunkStartX + bayLocalX, 0f, 0f);
            var bay = bayGo.AddComponent<BusBay>();
            bay.side = bsSide;
            bay.bayLength = bayLen;
            bay.extraWidth = bayExtra;
            bay.roadEdgeZ = RoadEdgeZ;
            bay.roadSurfaceY = roadY;
            bay.roadSurfaceMaterial = pavementMat;
            bay.Build();

            var bsTreeKill = new List<GameObject>();
            foreach (Transform c in chunk.transform)
            {
                if (c.name.StartsWith("Road_1_line"))
                {
                    foreach (Transform gc in c)
                        if (gc.name.StartsWith("Tree")) bsTreeKill.Add(gc.gameObject);
                }
                else if (c.name.StartsWith("Tree"))
                {
                    if ((bsSide > 0 && c.position.z > 0) || (bsSide < 0 && c.position.z < 0))
                        bsTreeKill.Add(c.gameObject);
                }
            }
            foreach (var kill in bsTreeKill)
            {
                if (Application.isPlaying) Destroy(kill); else DestroyImmediate(kill);
            }

            var g = InstantiateChild(pf, chunk.transform);
            float shelterZ = bsSide * (RoadEdgeZ + 1.6f);
            g.transform.position = new Vector3(chunkStartX + bsT * tileLength, 0f, shelterZ);
            g.transform.rotation = Quaternion.Euler(0f, bsSide > 0 ? 0f : 180f, 0f) * g.transform.rotation;
            GroundAlign(g);
        }

        bool spawnBillboard = billboardPrefab != null && billboardsPerChunk > 0 && billboardEveryNChunks > 0
            && ((idx % billboardEveryNChunks) + billboardEveryNChunks) % billboardEveryNChunks == 0;
        if (idx == 0) spawnBillboard = billboardPrefab != null && billboardsPerChunk > 0;
        if (spawnBillboard)
        {
            int n = Mathf.Max(1, billboardsPerChunk);
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
                    if (!occupied.Contains(((long)t << 4) | (side > 0 ? 1L : 0L))) continue;
                    if (rng.NextDouble() < 0.5f) continue;
                    if (trashCanPrefab == null) continue;
                    var binGo = InstantiateChild(trashCanPrefab, chunk.transform);
                    binGo.name = "TrashBin";
                    float binX = chunkStartX + t * tileLength + (float)(rng.NextDouble() * 2.0);
                    float binZ = side * (sidewalkZ + 0.6f);
                    binGo.transform.position = new Vector3(binX, 0f, binZ);
                    binGo.transform.rotation = Quaternion.Euler(0f, side > 0 ? 160f : 20f, 0f) * binGo.transform.rotation;
                    GroundAlign(binGo);
                }
            }
        }

        float chunkLen = tileLength * tilesPerChunk;
        float pzZoneLen = 36f;
        float lotGapStart = 4f * tileLength;
        float lotGapEnd = 6f * tileLength;

        for (int side = -1; side <= 1; side += 2)
        {
            var curbGo = new GameObject("Curb_" + (side > 0 ? "R" : "L"));
            curbGo.transform.SetParent(chunk.transform, false);
            curbGo.hideFlags = HideFlags.DontSave;
            curbGo.transform.position = new Vector3(chunkStartX, roadY, 0f);
            var stripe = curbGo.AddComponent<CurbStripe>();
            stripe.length = chunkLen;
            stripe.side = side;
            stripe.zOffset = RoadEdgeZ;
            stripe.stripeHeight = 0.16f;
            stripe.stripeWidth = 0.22f;
            stripe.segmentLength = 0.65f;

            if (hasParkingZone && side == pzSideDet)
            {
                stripe.gapStart = 0f;
                stripe.gapEnd = pzZoneLen;
            }
            else if (hasLot && side == lotSide)
            {
                stripe.gapStart = lotGapStart;
                stripe.gapEnd = lotGapEnd;
            }

            if (bsBaySide != 0 && side == bsBaySide)
            {
                float seg = stripe.segmentLength;
                stripe.altStart = Mathf.Floor(bsBayGapStart / seg) * seg;
                stripe.altEnd = Mathf.Ceil(bsBayGapEnd / seg) * seg;
            }

            stripe.Build();
        }

        var trashPositions = new List<Vector3>();
        foreach (Transform c in chunk.transform)
            if (c.name.StartsWith("TrashBin")) trashPositions.Add(c.position);

        int catsThisChunk = Mathf.Max(0, catsPerChunk);
        for (int ci = 0; ci < catsThisChunk; ci++)
        {
            float catX, catZ;
            int catSide = rng.NextDouble() < 0.5 ? -1 : 1;

            if (trashPositions.Count > 0 && rng.NextDouble() < 0.6f)
            {
                var bin = trashPositions[rng.Next(trashPositions.Count)];
                catX = bin.x + (float)(rng.NextDouble() * 1.5 - 0.75);
                catZ = bin.z + (float)(rng.NextDouble() * 0.6 - 0.3);
                catSide = bin.z >= 0 ? 1 : -1;
            }
            else
            {
                int catT = rng.Next(tilesPerChunk);
                catX = chunkStartX + catT * tileLength + (float)(rng.NextDouble() * tileLength * 0.8f);
                catZ = catSide * (sidewalkZ + 0.5f + (float)(rng.NextDouble() * 2.5f));
            }

            SpawnStreetCat(
                chunk.transform,
                new Vector3(catX, roadY, catZ),
                catSide > 0 ? 90f : -90f,
                rng);
        }

        if (electricityPoleEveryNTiles > 0)
        {
            float poleSpacingM = electricityPoleEveryNTiles * tileLength;
            for (int t = 0; t < tilesPerChunk; t += electricityPoleEveryNTiles)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var poleGo = new GameObject("ElecPole");
                    poleGo.transform.SetParent(chunk.transform, false);
                    poleGo.hideFlags = HideFlags.DontSave;
                    poleGo.transform.position = new Vector3(chunkStartX + t * tileLength, 0f, side * (sidewalkZ + 2.5f));
                    var pole = poleGo.AddComponent<ElectricityPole>();
                    pole.poleSpacing = poleSpacingM;
                    pole.Build();
                }
            }
        }

        if (hasParkingZone)
        {
            int pzSide = pzSideDet;

            var pzGo = new GameObject("ParkingZone_" + idx);
            pzGo.transform.SetParent(chunk.transform, false);
            pzGo.hideFlags = HideFlags.DontSave;
            pzGo.transform.position = new Vector3(chunkStartX, 0f, 0f);
            var pz = pzGo.AddComponent<ParkingZone>();
            pz.zoneLength = 36f;
            pz.side = pzSide;
            pz.curbZ = RoadEdgeZ;
            pz.carPrefabs = parkingCarPrefabs;
            pz.roadSurfaceMaterial = asphaltMat;
            pz.Build();

            var plGo = new GameObject("ParkingLot_" + idx);
            plGo.transform.SetParent(chunk.transform, false);
            plGo.hideFlags = HideFlags.DontSave;
            plGo.transform.position = new Vector3(chunkStartX + tileLength * 4f, 0f, 0f);
            var pl = plGo.AddComponent<ParkingLot>();
            pl.side = -pzSide;
            pl.roadEdgeZ = RoadEdgeZ;
            pl.rows = 2;
            pl.cols = 4;
            pl.carPrefabs = parkingCarPrefabs;
            pl.roadSurfaceMaterial = asphaltMat;
            pl.Build();
        }

        _spawned[idx] = chunk;
    }

    void SpawnStreetCat(Transform parent, Vector3 pos, float yaw, System.Random rng)
    {
        if (catPrefab == null) return;

        var catGo = new GameObject("StreetCat");
        catGo.transform.SetParent(parent, worldPositionStays: false);
        catGo.transform.position = pos;
        catGo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        try
        {
            var sc = catGo.AddComponent<StreetCat>();
            sc.catPrefab = catPrefab;
            sc.patrolRange = 3f + (float)(rng.NextDouble() * 4f);
            sc.speed = 0.5f + (float)(rng.NextDouble() * 0.4f);
            sc.sidewalkMinZ = sidewalkZ - 0.5f;
            sc.sidewalkMaxZ = sidewalkZ + 3.5f;
            sc.Build();
            CheapProp(catGo);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("StreetCat spawn failed: " + ex.Message);
        }

        SetHideFlagsRecursive(catGo.transform);
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

    static Material Tiled(Material src, float worldX, float worldZ, float tileX, float tileZ)
    {
        var m = new Material(src);
        m.mainTextureScale = new Vector2(worldX / tileX, worldZ / tileZ);
        return m;
    }

    void Slab(Transform parent, Material mat, Vector3 worldPos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Carriageway";
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.transform.localScale = size;
        var rend = go.GetComponent<Renderer>();
        if (mat != null) rend.sharedMaterial = mat;
        go.hideFlags = HideFlags.DontSave;
        var col = go.GetComponent<Collider>();
        if (col)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    void StickWallPosters(List<KeyValuePair<GameObject, int>> placed, Transform chunk, int idx, System.Random rng)
    {
        if (billboardAds == null || placed == null || placed.Count == 0) return;
        int adCount = 0;
        for (int i = 0; i < billboardAds.Length; i++)
            if (billboardAds[i] != null) adCount++;
        if (adCount == 0) return;

        int slot = ((idx % adCount) + adCount) % adCount;
        Texture tex = null;
        int seen = 0;
        for (int i = 0; i < billboardAds.Length; i++)
        {
            if (billboardAds[i] == null) continue;
            if (seen == slot)
            {
                tex = billboardAds[i];
                break;
            }
            seen++;
        }
        if (tex == null) return;

        float xBias = (float)(rng.NextDouble() * 2.0 - 1.0);
        int start = rng.Next(placed.Count);
        for (int a = 0; a < placed.Count && a < 8; a++)
        {
            var pick = placed[(start + a) % placed.Count];
            if (AttachWallPoster(pick.Key, pick.Value, tex, xBias))
                return;
        }
    }

    bool AttachWallPoster(GameObject building, int streetSide, Texture tex, float xBias)
    {
        if (building == null || tex == null) return false;
        if (streetSide == 0) streetSide = building.transform.position.z >= 0f ? 1 : -1;

        var rends = building.GetComponentsInChildren<MeshRenderer>();
        if (rends.Length == 0) return false;
        Bounds wb = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            if (rends[i] != null && rends[i].enabled)
                wb.Encapsulate(rends[i].bounds);
        }

        if (wb.size.x < 3.5f || wb.size.y < 4.0f) return false;

        // +Z lots face the road in -Z; -Z lots face +Z.
        Vector3 towardStreet = new Vector3(0f, 0f, -streetSide);
        float streetFaceZ = streetSide > 0 ? wb.min.z : wb.max.z;
        float coreZ = wb.center.z;

        // Plants/stairs puff the AABB toward the sidewalk. Pull the poster
        // into the building mass, then sit it just in front of that plane.
        float wallZ = Mathf.Lerp(streetFaceZ, coreZ, 0.62f);
        float minLotZ = streetSide * (sidewalkZ + 3.0f);
        if (streetSide > 0) wallZ = Mathf.Max(wallZ, minLotZ);
        else wallZ = Mathf.Min(wallZ, minLotZ);

        // The placed pivot is on the lot. Don't let a puffed AABB pull the
        // poster out onto the sidewalk in front of the actual wall.
        float pivotZ = building.transform.position.z;
        if (Mathf.Abs(wallZ - pivotZ) > 3.5f)
            wallZ = pivotZ + towardStreet.z * 1.1f;

        float posterH = 2.1f;
        float posterW = posterH * 0.72f;
        posterW = Mathf.Min(posterW, wb.size.x * 0.38f);
        posterH = posterW / 0.72f;

        float x = wb.center.x + xBias * wb.size.x * 0.12f;
        float halfW = posterW * 0.5f + 0.35f;
        x = Mathf.Clamp(x, wb.min.x + halfW, wb.max.x - halfW);

        float y = 2.15f + posterH * 0.5f;
        y = Mathf.Clamp(y, 1.8f, Mathf.Min(4.2f, wb.max.y - posterH * 0.5f - 0.4f));

        Vector3 pos = new Vector3(x, y, wallZ) + towardStreet * 0.06f;

        if (Mathf.Abs(pos.z) < sidewalkZ + 2.4f) return false;
        if (Mathf.Abs(pos.z - coreZ) > wb.extents.z * 0.9f) return false;

        var shader = Shader.Find("IsraeliStreet/BillboardPoster");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        var mat = new Material(shader);
        mat.hideFlags = HideFlags.DontSave;
        mat.mainTexture = tex;
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "WallPoster_" + tex.name;
        quad.transform.SetParent(building.transform, true);
        quad.transform.position = pos;
        quad.transform.rotation = Quaternion.LookRotation(towardStreet, Vector3.up);
        Vector3 lossy = building.transform.lossyScale;
        float sx = Mathf.Abs(lossy.x) > 0.001f ? posterW / Mathf.Abs(lossy.x) : posterW;
        float sy = Mathf.Abs(lossy.y) > 0.001f ? posterH / Mathf.Abs(lossy.y) : posterH;
        quad.transform.localScale = new Vector3(sx, sy, 1f);

        var col = quad.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }

        var r = quad.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return true;
    }

    void GroundAlign(GameObject go)
    {
        var rends = go.GetComponentsInChildren<MeshRenderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        float minY = b.min.y;
        float extra = 0f;
        var facing = go.GetComponent<BuildingFacing>();
        if (facing != null) extra = facing.groundOffset;
        var p = go.transform.position;
        go.transform.position = new Vector3(p.x, p.y - minY + extra, p.z);
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
        SetHideFlagsRecursive(go.transform);
        return go;
    }

    // Million-vert Meshy facades don't need to receive shadows (casting is enough).
    static void CheapBuilding(GameObject go)
    {
        if (go == null) return;
        var rends = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
            rends[i].receiveShadows = false;
    }

    static void CheapProp(GameObject go)
    {
        if (go == null) return;
        var rends = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            rends[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rends[i].receiveShadows = false;
        }
    }

    void SetHideFlagsRecursive(Transform t)
    {
        t.gameObject.hideFlags = HideFlags.DontSave;
        for (int i = 0; i < t.childCount; i++) SetHideFlagsRecursive(t.GetChild(i));
    }
}
