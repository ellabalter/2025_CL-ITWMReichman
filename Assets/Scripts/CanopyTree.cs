using UnityEngine;

// Broad Israeli street canopy tree (ficus/plane tree style).
[ExecuteAlways]
public class CanopyTree : MonoBehaviour
{
    public Color trunkColor  = new Color(0.78f, 0.72f, 0.62f);
    public Color canopyColor = new Color(0.22f, 0.52f, 0.15f);
    public float trunkHeight = 3.5f;
    public float canopyRadius = 3.0f;

    void Awake() { if (transform.childCount == 0) Build(); }

    void Build()
    {
        var trunkMat  = Mat(trunkColor);
        var canopyMat = Mat(canopyColor);
        var darkMat   = Mat(new Color(trunkColor.r * 0.7f, trunkColor.g * 0.7f, trunkColor.b * 0.7f));

        // Thick trunk base
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(trunk, trunkMat, new Vector3(0f, trunkHeight * 0.5f, 0f),
              new Vector3(0.55f, trunkHeight * 0.5f, 0.55f));

        // Trunk texture bumps — small irregular cubes for bark look
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f * Mathf.Deg2Rad;
            float h = trunkHeight * 0.2f + i * (trunkHeight * 0.12f);
            var bump = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Setup(bump, darkMat,
                  new Vector3(Mathf.Cos(angle) * 0.28f, h, Mathf.Sin(angle) * 0.28f),
                  new Vector3(0.12f, 0.3f, 0.12f));
        }

        // Main canopy sphere
        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Setup(canopy, canopyMat,
              new Vector3(0f, trunkHeight + canopyRadius * 0.7f, 0f),
              Vector3.one * canopyRadius * 2f);

        // Secondary canopy lobes for irregular shape
        var offsets = new Vector3[] {
            new Vector3( 1.2f, 0.3f,  0.4f),
            new Vector3(-1.1f, 0.2f,  0.5f),
            new Vector3( 0.3f, 0.5f, -1.0f),
            new Vector3( 0.5f, 0.8f,  1.1f),
        };
        foreach (var off in offsets)
        {
            var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Setup(lobe, canopyMat,
                  new Vector3(off.x, trunkHeight + canopyRadius * 0.7f + off.y, off.z),
                  Vector3.one * (canopyRadius * 1.1f));
        }
    }

    void Setup(GameObject go, Material mat, Vector3 pos, Vector3 size)
    {
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }
}
