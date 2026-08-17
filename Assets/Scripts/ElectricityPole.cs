using UnityEngine;

// Israeli electricity/telecom pole with transformer box and wires.
[ExecuteAlways]
public class ElectricityPole : MonoBehaviour
{
    void Awake() { if (transform.childCount == 0) Build(); }

    void Build()
    {
        var grey   = Mat(new Color(0.62f, 0.62f, 0.60f));
        var dark   = Mat(new Color(0.18f, 0.18f, 0.18f));
        var brown  = Mat(new Color(0.35f, 0.28f, 0.20f));
        var green  = Mat(new Color(0.22f, 0.40f, 0.18f));

        // Main lattice pole — tall thin box
        Cube(grey,  new Vector3(0f, 5f, 0f),    new Vector3(0.22f, 10f, 0.22f));

        // Cross-arm at top
        Cube(grey,  new Vector3(0f, 9.4f, 0f),  new Vector3(2.2f, 0.12f, 0.12f));

        // Diagonal braces on cross-arm
        var diagL = Cube(grey, new Vector3(-0.7f, 9.1f, 0f), new Vector3(0.08f, 0.7f, 0.08f));
        diagL.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        var diagR = Cube(grey, new Vector3(0.7f, 9.1f, 0f), new Vector3(0.08f, 0.7f, 0.08f));
        diagR.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);

        // Second cross-arm lower
        Cube(grey,  new Vector3(0f, 8.0f, 0f),  new Vector3(1.4f, 0.10f, 0.10f));

        // Insulators (dark cylinders) — top arm
        for (int i = -1; i <= 1; i++)
        {
            Cylinder(dark, new Vector3(i * 0.8f, 9.55f, 0f), new Vector3(0.09f, 0.12f, 0.09f));
            Cylinder(dark, new Vector3(i * 0.8f, 9.3f,  0f), new Vector3(0.06f, 0.08f, 0.06f));
        }
        // Insulators — lower arm
        for (int i = -1; i <= 1; i++)
            Cylinder(dark, new Vector3(i * 0.5f, 8.12f, 0f), new Vector3(0.07f, 0.10f, 0.07f));

        // Transformer box on pole mid-section
        Cube(brown, new Vector3(0.3f, 5.8f, 0f), new Vector3(0.5f, 0.7f, 0.4f));
        Cube(dark,  new Vector3(0.3f, 5.45f, 0f), new Vector3(0.45f, 0.06f, 0.36f)); // base plate

        // Wires — run +X only from this pole to the next (50m span).
        // Each pole emits wire in the +X direction; the adjacent pole's wire meets it.
        float poleSpacing = 50f; // electricityPoleEveryNTiles(5) * tileLength(10)
        float wireMid = poleSpacing * 0.5f;
        // Slight sag: centre of wire is 0.5m lower than the ends
        float sagDrop = 0.5f;

        // Top arm — 3 wires
        for (int wi = -1; wi <= 1; wi++)
        {
            float wz = wi * 0.8f;
            var w = Cylinder(dark, new Vector3(wireMid, 9.4f - sagDrop, wz), new Vector3(0.025f, poleSpacing, 0.025f));
            w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        // Lower arm — 2 wires
        for (int wi = -1; wi <= 1; wi += 2)
        {
            float wz = wi * 0.5f;
            var w = Cylinder(dark, new Vector3(wireMid, 8.0f - sagDrop * 0.7f, wz), new Vector3(0.02f, poleSpacing, 0.02f));
            w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // Green base paint (Israeli style — often painted green at base)
        Cube(green, new Vector3(0f, 0.5f, 0f), new Vector3(0.24f, 1.0f, 0.24f));
    }

    GameObject Cube(Material mat, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Setup(go, mat, pos, size);
        return go;
    }

    GameObject Cylinder(Material mat, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(go, mat, pos, size);
        return go;
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
