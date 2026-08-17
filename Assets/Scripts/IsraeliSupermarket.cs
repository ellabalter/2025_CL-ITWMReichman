using UnityEngine;

// Builds a simple Israeli-style supermarket / makolet facade at runtime.
[ExecuteAlways]
public class IsraeliSupermarket : MonoBehaviour
{
    void Awake() { if (transform.childCount == 0) Build(); }

    void Build()
    {
        var whiteMat = Mat(new Color(0.92f, 0.92f, 0.88f));
        var blueMat  = Mat(new Color(0.1f,  0.35f, 0.75f));
        var redMat   = Mat(new Color(0.82f, 0.1f,  0.1f));
        var yellowMat= Mat(new Color(0.95f, 0.82f, 0.1f));
        var glassMat = Mat(new Color(0.55f, 0.75f, 0.9f, 0.6f), true);
        var awningMat= Mat(new Color(0.85f, 0.15f, 0.15f));

        // Main building block
        Block(whiteMat, new Vector3(0, 2.5f, 0), new Vector3(8f, 5f, 4f));

        // Blue stripe band at top
        Block(blueMat, new Vector3(0, 4.8f, 0), new Vector3(8f, 0.4f, 4.05f));

        // Red & yellow "שופרסל" style sign board
        Block(redMat,  new Vector3(0, 3.9f, 2.05f), new Vector3(7.6f, 0.8f, 0.1f));
        Block(yellowMat, new Vector3(-2.5f, 3.9f, 2.1f), new Vector3(1.8f, 0.65f, 0.05f));
        Block(blueMat,   new Vector3(0.5f,  3.9f, 2.1f), new Vector3(2.5f, 0.65f, 0.05f));

        // Awning over entrance
        Block(awningMat, new Vector3(0, 2.9f, 2.4f), new Vector3(4f, 0.15f, 1f));
        Block(redMat,    new Vector3(-1.8f, 2.9f, 2.4f), new Vector3(0.4f, 0.15f, 1f));
        Block(redMat,    new Vector3( 1.8f, 2.9f, 2.4f), new Vector3(0.4f, 0.15f, 1f));

        // Shop window
        Block(glassMat, new Vector3(-2f, 1.3f, 2.05f), new Vector3(2.5f, 1.8f, 0.05f));

        // Glass door
        Block(glassMat, new Vector3(0.5f, 1.1f, 2.05f), new Vector3(1.0f, 2.2f, 0.05f));

        // Produce crates outside
        Block(Mat(new Color(0.6f, 0.4f, 0.2f)), new Vector3(-3.2f, 0.3f, 2.2f), new Vector3(0.6f, 0.6f, 0.6f));
        Block(Mat(new Color(0.5f, 0.85f, 0.2f)), new Vector3(-2.6f, 0.3f, 2.2f), new Vector3(0.6f, 0.6f, 0.6f));
        Block(Mat(new Color(1f, 0.6f, 0.1f)),    new Vector3(-3.2f, 0.9f, 2.2f), new Vector3(0.6f, 0.4f, 0.55f));
    }

    void Block(Material mat, Vector3 localPos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material = mat;
        if (Application.isPlaying) Destroy(go.GetComponent<BoxCollider>());
        else DestroyImmediate(go.GetComponent<BoxCollider>());
    }

    Material Mat(Color c, bool transparent = false)
    {
        var shader = transparent
            ? Shader.Find("Standard")
            : Shader.Find("Standard");
        var m = new Material(shader);
        if (transparent)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            c.a = 0.5f;
        }
        m.color = c;
        return m;
    }
}
