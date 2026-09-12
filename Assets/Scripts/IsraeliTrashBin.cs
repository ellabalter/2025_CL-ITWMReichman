using UnityEngine;

// Builds an Israeli-style 240L wheeled green bin (פח אשפה).
[ExecuteAlways]
public class IsraeliTrashBin : MonoBehaviour
{
    void Awake() { if (transform.childCount == 0) Build(); }

    void Build()
    {
        var green = Mat(new Color(0.22f, 0.45f, 0.18f));
        var dark  = Mat(new Color(0.12f, 0.12f, 0.12f));
        var grey  = Mat(new Color(0.35f, 0.35f, 0.35f));

        // Body — tall rectangular bin, slightly tapered look via scale
        var body = Cube(green, new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.9f, 0.38f));

        // Lid — slightly wider, hinged at back
        Cube(green, new Vector3(0f, 0.92f, -0.02f), new Vector3(0.48f, 0.06f, 0.42f));

        // Lid handle ridge
        Cube(dark, new Vector3(0f, 0.96f, 0.14f), new Vector3(0.18f, 0.04f, 0.06f));

        // Front panel indents (decorative lines)
        Cube(dark, new Vector3(0f, 0.55f, 0.192f), new Vector3(0.36f, 0.55f, 0.01f));

        // Axle bar at bottom back
        Cube(grey, new Vector3(0f, 0.1f, -0.16f), new Vector3(0.5f, 0.04f, 0.04f));

        // Left wheel
        var wL = Cylinder(dark, new Vector3(-0.26f, 0.1f, -0.16f), new Vector3(0.09f, 0.04f, 0.09f));
        wL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // Right wheel
        var wR = Cylinder(dark, new Vector3(0.26f, 0.1f, -0.16f), new Vector3(0.09f, 0.04f, 0.09f));
        wR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // Wheel hubcaps
        Cylinder(grey, new Vector3(-0.28f, 0.1f, -0.16f), new Vector3(0.05f, 0.01f, 0.05f));
        Cylinder(grey, new Vector3(0.28f, 0.1f, -0.16f), new Vector3(0.05f, 0.01f, 0.05f));

        // Small front foot/stopper
        Cube(dark, new Vector3(0f, 0.03f, 0.17f), new Vector3(0.12f, 0.06f, 0.06f));
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
        if (Application.isPlaying) Destroy(go.GetComponent<Collider>());
        else DestroyImmediate(go.GetComponent<Collider>());
    }

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }
}
