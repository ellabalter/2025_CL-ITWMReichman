using UnityEngine;

// Red-and-white painted raised kerb at the road/sidewalk boundary.
public class CurbStripe : MonoBehaviour
{
    public float length = 100f;
    public float stripeWidth = 0.18f;   // kerb thickness (road-facing dimension)
    public float stripeHeight = 0.14f;  // raised kerb height
    public float segmentLength = 0.5f;
    public int side = 1;
    public float zOffset = 3.5f;        // road tile edge (road maxZ=3.5 after 7m width)
    public float gapStart  = -1f;        // local X where gap begins (-1 = no gap)
    public float gapEnd    = -1f;        // local X where gap ends
    public float gap2Start = -1f;        // optional second gap
    public float gap2End   = -1f;

    // Build() is called manually by ProceduralStreet after properties are set

    public void Build()
    {
        var redMat   = Mat(new Color(0.92f, 0.06f, 0.06f));
        var whiteMat = Mat(new Color(0.95f, 0.95f, 0.95f));

        int count = Mathf.CeilToInt(length / segmentLength);
        for (int i = 0; i < count; i++)
        {
            float segX = i * segmentLength + segmentLength * 0.5f;
            if (gapStart  >= 0 && segX >= gapStart  && segX <= gapEnd)  continue;
            if (gap2Start >= 0 && segX >= gap2Start && segX <= gap2End) continue;

            var mat = (i % 2 == 0) ? redMat : whiteMat;
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.transform.SetParent(transform, false);
            var col = seg.GetComponent<Collider>();
            if (col) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
            seg.GetComponent<Renderer>().material = mat;
            seg.transform.localPosition = new Vector3(
                segX,
                stripeHeight * 0.5f - 0.02f,
                side * zOffset);
            seg.transform.localScale = new Vector3(segmentLength - 0.02f, stripeHeight, stripeWidth);
        }
    }

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }
}
