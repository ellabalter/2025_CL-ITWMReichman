using UnityEngine;

[ExecuteAlways]
public class ElectricityPole : MonoBehaviour
{
    public float poleSpacing = 50f; // set by ProceduralStreet to match actual pole interval

    void OnEnable() { if (transform.childCount == 0) Build(); }

    public void Build()
    {
        var grey  = Mat(new Color(0.62f, 0.62f, 0.60f));
        var dark  = Mat(new Color(0.18f, 0.18f, 0.18f));
        var brown = Mat(new Color(0.35f, 0.28f, 0.20f));
        var green = Mat(new Color(0.22f, 0.40f, 0.18f));

        // Main pole
        Cube(grey, new Vector3(0f, 5f, 0f), new Vector3(0.22f, 10f, 0.22f));

        // Top cross-arm
        Cube(grey, new Vector3(0f, 9.4f, 0f), new Vector3(2.2f, 0.12f, 0.12f));

        // Diagonal braces
        var diagL = Cube(grey, new Vector3(-0.7f, 9.1f, 0f), new Vector3(0.08f, 0.7f, 0.08f));
        diagL.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        var diagR = Cube(grey, new Vector3(0.7f, 9.1f, 0f), new Vector3(0.08f, 0.7f, 0.08f));
        diagR.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);

        // Lower cross-arm
        Cube(grey, new Vector3(0f, 8.0f, 0f), new Vector3(1.4f, 0.10f, 0.10f));

        // Insulators — top arm
        for (int i = -1; i <= 1; i++)
        {
            Cylinder(dark, new Vector3(i * 0.8f, 9.55f, 0f), new Vector3(0.09f, 0.12f, 0.09f));
            Cylinder(dark, new Vector3(i * 0.8f, 9.3f, 0f),  new Vector3(0.06f, 0.08f, 0.06f));
        }
        // Insulators — lower arm
        for (int i = -1; i <= 1; i++)
            Cylinder(dark, new Vector3(i * 0.5f, 8.12f, 0f), new Vector3(0.07f, 0.10f, 0.07f));

        // Transformer box
        Cube(brown, new Vector3(0.3f, 5.8f, 0f),  new Vector3(0.5f, 0.7f, 0.4f));
        Cube(dark,  new Vector3(0.3f, 5.45f, 0f), new Vector3(0.45f, 0.06f, 0.36f));

        // Green base paint
        Cube(green, new Vector3(0f, 0.5f, 0f), new Vector3(0.24f, 1.0f, 0.24f));

        // Wires — run in +X from this pole to the next (poleSpacing away).
        // Built as a catenary approximation: tilted cylinder centred between the two poles,
        // stretched so it spans the gap with a slight sag in the middle.
        float span     = poleSpacing;
        float wireMidX = span * 0.5f;
        float sagDrop  = 0.6f; // how much the wire droops at centre

        // Top arm — 3 wires at Z = -0.8, 0, +0.8
        for (int wi = -1; wi <= 1; wi++)
        {
            float wz = wi * 0.8f;
            SpawnWire(dark, new Vector3(0f, 9.55f, wz), new Vector3(span, 9.55f - sagDrop, wz));
        }
        // Lower arm — 2 wires at Z = -0.5, +0.5
        for (int wi = -1; wi <= 1; wi += 2)
        {
            float wz = wi * 0.5f;
            SpawnWire(dark, new Vector3(0f, 8.12f, wz), new Vector3(span, 8.12f - sagDrop * 0.7f, wz));
        }
    }

    // Draws a wire as a thin cylinder between two LOCAL positions, with slight sag via midpoint
    void SpawnWire(Material mat, Vector3 from, Vector3 to)
    {
        Vector3 mid = (from + to) * 0.5f;
        float length = Vector3.Distance(from, to);
        Vector3 dir = (to - from).normalized;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = mid;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
        go.transform.localScale = new Vector3(0.025f, length * 0.5f, 0.025f);
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
        go.hideFlags = HideFlags.DontSave;
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
        go.hideFlags = HideFlags.DontSave;
        var col = go.GetComponent<Collider>();
        if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
    }

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }
}
