using UnityEngine;

// Small Israeli city garden: grass patch, benches, a fountain, path, low fence.
[ExecuteAlways]
public class CityGarden : MonoBehaviour
{
    public float width  = 18f;  // along street (X)
    public float depth  = 14f;  // away from street (Z)
    public int side = 1;

    void Awake() { if (transform.childCount == 0) Build(); }

    public void Build()
    {
        var grassMat    = Mat(new Color(0.22f, 0.48f, 0.18f));
        var pathMat     = Mat(new Color(0.68f, 0.62f, 0.52f));
        var fenceMat    = Mat(new Color(0.50f, 0.50f, 0.50f));
        var benchMat    = Mat(new Color(0.45f, 0.32f, 0.18f));
        var waterMat    = Mat(new Color(0.30f, 0.55f, 0.80f), 0.1f, 0.9f);
        var stoneMat    = Mat(new Color(0.62f, 0.60f, 0.58f));
        var flowerRed   = Mat(new Color(0.85f, 0.15f, 0.15f));
        var flowerYellow= Mat(new Color(0.90f, 0.80f, 0.10f));

        float z0 = side * 3.5f; // offset from road edge into lot area

        // Grass base
        Cube(grassMat, new Vector3(width * 0.5f, 0.04f, z0 + side * depth * 0.5f),
                       new Vector3(width, 0.08f, depth));

        // Central path (diagonal cross)
        Cube(pathMat, new Vector3(width * 0.5f, 0.07f, z0 + side * depth * 0.5f),
                      new Vector3(width, 0.04f, 1.2f));
        Cube(pathMat, new Vector3(width * 0.5f, 0.07f, z0 + side * depth * 0.5f),
                      new Vector3(1.2f, 0.04f, depth));

        // Central fountain
        Cube(stoneMat, new Vector3(width * 0.5f, 0.22f, z0 + side * depth * 0.5f),
                       new Vector3(2.2f, 0.44f, 2.2f));
        Cube(waterMat, new Vector3(width * 0.5f, 0.45f, z0 + side * depth * 0.5f),
                       new Vector3(1.6f, 0.08f, 1.6f));
        // Fountain spout
        Cube(stoneMat, new Vector3(width * 0.5f, 0.70f, z0 + side * depth * 0.5f),
                       new Vector3(0.18f, 0.50f, 0.18f));

        // Low fence perimeter (4 sides)
        float fw = 0.10f, fh = 0.55f;
        Cube(fenceMat, new Vector3(width * 0.5f,  fh * 0.5f, z0),                      new Vector3(width, fh, fw));
        Cube(fenceMat, new Vector3(width * 0.5f,  fh * 0.5f, z0 + side * depth),       new Vector3(width, fh, fw));
        Cube(fenceMat, new Vector3(0f,             fh * 0.5f, z0 + side * depth * 0.5f), new Vector3(fw, fh, depth));
        Cube(fenceMat, new Vector3(width,          fh * 0.5f, z0 + side * depth * 0.5f), new Vector3(fw, fh, depth));

        // 2 benches
        SpawnBench(benchMat, stoneMat, new Vector3(width * 0.25f, 0f, z0 + side * depth * 0.5f));
        SpawnBench(benchMat, stoneMat, new Vector3(width * 0.75f, 0f, z0 + side * depth * 0.5f));

        // Flower beds (4 corners)
        float fx = 3f, fz = side * 3.5f;
        SpawnFlowerBed(flowerRed,    new Vector3(fx,         0f, z0 + fz));
        SpawnFlowerBed(flowerYellow, new Vector3(width - fx, 0f, z0 + fz));
        SpawnFlowerBed(flowerRed,    new Vector3(fx,         0f, z0 + side * (depth - 3.5f)));
        SpawnFlowerBed(flowerYellow, new Vector3(width - fx, 0f, z0 + side * (depth - 3.5f)));

        // Small trees (4 positions)
        SpawnTree(new Vector3(fx,         0f, z0 + side * depth * 0.5f));
        SpawnTree(new Vector3(width - fx, 0f, z0 + side * depth * 0.5f));
    }

    void SpawnBench(Material seatMat, Material legMat, Vector3 pos)
    {
        // Seat
        Cube(seatMat, pos + new Vector3(0f, 0.48f, 0f), new Vector3(1.4f, 0.08f, 0.44f));
        // Back rest
        Cube(seatMat, pos + new Vector3(0f, 0.72f, -0.18f), new Vector3(1.4f, 0.44f, 0.07f));
        // Legs
        Cube(legMat, pos + new Vector3(-0.55f, 0.24f, 0f), new Vector3(0.10f, 0.48f, 0.40f));
        Cube(legMat, pos + new Vector3( 0.55f, 0.24f, 0f), new Vector3(0.10f, 0.48f, 0.40f));
    }

    void SpawnFlowerBed(Material flowerMat, Vector3 pos)
    {
        var soilMat = Mat(new Color(0.35f, 0.25f, 0.15f));
        Cube(soilMat,   pos + new Vector3(0f, 0.05f, 0f), new Vector3(1.8f, 0.10f, 1.8f));
        Cube(flowerMat, pos + new Vector3(0f, 0.22f, 0f), new Vector3(1.4f, 0.22f, 1.4f));
    }

    void SpawnTree(Vector3 pos)
    {
        var trunkMat  = Mat(new Color(0.38f, 0.26f, 0.16f));
        var canopyMat = Mat(new Color(0.18f, 0.48f, 0.16f));
        Cube(trunkMat,  pos + new Vector3(0f, 0.8f, 0f),  new Vector3(0.26f, 1.6f, 0.26f));
        Cube(canopyMat, pos + new Vector3(0f, 2.4f, 0f),  new Vector3(2.0f, 1.6f, 2.0f));
        Cube(canopyMat, pos + new Vector3(0f, 3.4f, 0f),  new Vector3(1.4f, 1.0f, 1.4f));
    }

    GameObject Cube(Material mat, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material = mat;
        var c = go.GetComponent<Collider>();
        if (c) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
        return go;
    }

    Material Mat(Color c, float metallic = 0f, float smoothness = 0.3f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smoothness);
        return m;
    }
}
