using UnityEngine;

// Bus bay: widens sidewalk on one side with pavement material, red/yellow kerb behind it.
// Parent must be at Y=0. ProceduralStreet sets properties then calls Build() manually.
[ExecuteAlways]
public class BusBay : MonoBehaviour
{
    public int side = 1;
    public float bayLength = 12f;
    public float extraWidth = 3.0f;
    public float roadEdgeZ = 3.5f;
    public float roadSurfaceY = 0.15f;
    public Material roadSurfaceMaterial; // should be the pavement/sidewalk material

    public void Build()
    {
        float slabTop = roadSurfaceY + 0.003f;
        float slabH   = slabTop;
        float slabCentreZ = roadEdgeZ + extraWidth * 0.5f;

        var pavMat = roadSurfaceMaterial != null ? roadSurfaceMaterial : Mat(new Color(0.72f, 0.70f, 0.65f));

        // Widened sidewalk slab — only the extra strip beyond existing roadEdgeZ
        Block(pavMat,
            new Vector3(bayLength * 0.5f, slabTop - slabH * 0.5f, side * slabCentreZ),
            new Vector3(bayLength, slabH, extraWidth));

        // Red/yellow kerb at outer edge, inset 1m from each end
        float kerbInset = 1.0f;
        float kerbLen   = bayLength - kerbInset * 2f;
        float outerZ    = roadEdgeZ + extraWidth;
        float kerbY     = slabTop + 0.045f;
        int segs = Mathf.CeilToInt(kerbLen / 0.65f);
        var redMat    = Mat(new Color(0.92f, 0.06f, 0.06f));
        var yellowMat = Mat(new Color(0.97f, 0.85f, 0.06f));
        for (int i = 0; i < segs; i++)
        {
            var mat = (i % 2 == 0) ? redMat : yellowMat;
            Block(mat,
                new Vector3(kerbInset + i * 0.65f + 0.325f, kerbY, side * outerZ),
                new Vector3(0.63f, 0.09f, 0.18f));
        }
    }

    void Block(Material mat, Vector3 lp, Vector3 sz)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = lp;
        go.transform.localScale = sz;
        go.GetComponent<Renderer>().material = mat;
        go.hideFlags = HideFlags.DontSave;
        var c = go.GetComponent<Collider>();
        if (c) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
    }

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }
}
