using System.Collections.Generic;
using UnityEngine;

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

    public float tileLength = 10f;
    public int tilesPerChunk = 10;
    public int chunksAhead = 5;
    public int chunksBehind = 1;
    public float lotHalfWidth = 14f;
    public float sidewalkZ = 5.5f;
    public float roadY = 0.15f;
    public float lotMargin = 1.5f;
    public int playgroundEveryNChunks = 4;
    public int gasStationEveryNChunks = 6;
    public int seedOffset = 1337;
    public bool showEditorPreview = true;

    private readonly Dictionary<int, GameObject> _spawned = new Dictionary<int, GameObject>();
    private int _lastChunk = int.MinValue;

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

        if (roadTilePrefab != null)
        {
            for (int t = 0; t < tilesPerChunk; t++)
            {
                var road = InstantiateChild(roadTilePrefab, chunk.transform);
                road.transform.position = new Vector3(chunkStartX + t * tileLength, roadY, 0f);
                road.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

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

        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            for (int t = 0; t < tilesPerChunk; t += 3)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (!SlotFree(t, side, 1)) continue;
                    if (rng.NextDouble() < 0.15) continue;

                    var pf = buildingPrefabs[rng.Next(buildingPrefabs.Length)];
                    var b = InstantiateChild(pf, chunk.transform);

                    float facingOffset = 0f;
                    var facing = b.GetComponent<BuildingFacing>();
                    if (facing != null) facingOffset = facing.yawOffset;

                    float baseYaw = (side > 0 ? 180f : 0f) + facingOffset + (float)(rng.NextDouble() * 10.0 - 5.0);
                    b.transform.rotation = Quaternion.Euler(0f, baseYaw, 0f) * b.transform.rotation;

                    float lotZ = side * (lotHalfWidth + (float)(rng.NextDouble() * 3.0));
                    float xJit = (float)(rng.NextDouble() * 6.0);
                    b.transform.position = new Vector3(chunkStartX + t * tileLength + xJit, 0f, lotZ);

                    GroundAlign(b);
                    KeepOffRoad(b, side);
                    Occupy(t, side, 1);
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
            g.transform.position = new Vector3(chunkStartX + playgroundTile * tileLength, 0f, playgroundSide * (lotHalfWidth + 1f));
            GroundAlign(g);
            KeepOffRoad(g, playgroundSide);
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

        if (treePrefabs != null && treePrefabs.Length > 0)
        {
            var lastTreeTile = new Dictionary<int, int> { { -1, -99 }, { 1, -99 } };
            for (int t = 0; t < tilesPerChunk; t++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
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

        if (benchPrefabs != null && benchPrefabs.Length > 0 && rng.NextDouble() < 0.5)
        {
            var pf = benchPrefabs[rng.Next(benchPrefabs.Length)];
            int t = rng.Next(tilesPerChunk);
            int side = rng.NextDouble() < 0.5 ? -1 : 1;
            var g = InstantiateChild(pf, chunk.transform);
            g.transform.position = new Vector3(chunkStartX + t * tileLength, 0f, side * (sidewalkZ - 1f));
            g.transform.rotation = Quaternion.Euler(0f, side > 0 ? 0f : 180f, 0f) * g.transform.rotation;
        }

        if (busStopPrefabs != null && busStopPrefabs.Length > 0 && rng.NextDouble() < 0.3)
        {
            var pf = busStopPrefabs[rng.Next(busStopPrefabs.Length)];
            int t = rng.Next(tilesPerChunk);
            int side = rng.NextDouble() < 0.5 ? -1 : 1;
            var g = InstantiateChild(pf, chunk.transform);
            g.transform.position = new Vector3(chunkStartX + t * tileLength, 0f, side * sidewalkZ);
            g.transform.rotation = Quaternion.Euler(0f, side > 0 ? 0f : 180f, 0f) * g.transform.rotation;
        }

        _spawned[idx] = chunk;
    }

    void GroundAlign(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        float minY = b.min.y;
        var p = go.transform.position;
        go.transform.position = new Vector3(p.x, p.y - minY, p.z);
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

    void SetHideFlagsRecursive(Transform t)
    {
        t.gameObject.hideFlags = HideFlags.DontSave;
        for (int i = 0; i < t.childCount; i++) SetHideFlagsRecursive(t.GetChild(i));
    }
}
