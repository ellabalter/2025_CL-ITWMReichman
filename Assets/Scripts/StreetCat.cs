using UnityEngine;

[ExecuteAlways]
public class StreetCat : MonoBehaviour
{
    public float patrolRange = 5f;
    public float speed       = 0.7f;
    public GameObject catPrefab;

    // Clamp patrol to stay on sidewalk (set by ProceduralStreet)
    public float sidewalkMinZ = 1.5f;
    public float sidewalkMaxZ = 5.0f;

    private Vector3  _origin;
    private float    _dir = 1f;
    private Animator _anim;
    private bool     _built;

    public void Build()
    {
        foreach (Transform c in transform)
        {
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }
        if (catPrefab == null) return;

        GameObject go;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(catPrefab, transform);
        else
#endif
            go = Instantiate(catPrefab, transform);

        if (go == null) return;
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one * 0.6f;
        go.hideFlags = HideFlags.DontSave;

        // Force solid black — the Kitty prefab material has alpha=0 by default
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.materials)
            {
                if (m == null) continue;
                m.color = new Color(0.05f, 0.05f, 0.05f, 1f);
                m.SetFloat("_Mode", 0f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                m.SetInt("_ZWrite", 1);
                m.DisableKeyword("_ALPHATEST_ON");
                m.DisableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = -1;
            }
        }

        _anim = go.GetComponentInChildren<Animator>();

        _origin = transform.position;
        _built  = true;
    }

    void Start()
    {
        _origin = transform.position;
        _anim   = GetComponentInChildren<Animator>();
        _built  = true;
    }

    void Update()
    {
        if (!Application.isPlaying || !_built || _anim == null) return;

        // Move along X (street direction)
        transform.position += transform.forward * speed * Time.deltaTime;

        float dist = transform.position.x - _origin.x;
        if (dist > patrolRange || dist < -patrolRange)
        {
            _dir = -_dir;
            transform.Rotate(0f, 180f, 0f);
        }

        // Keep on sidewalk — clamp Z away from road
        var p = transform.position;
        int side = p.z >= 0 ? 1 : -1;
        float absZ = Mathf.Abs(p.z);
        absZ = Mathf.Clamp(absZ, sidewalkMinZ, sidewalkMaxZ);
        transform.position = new Vector3(p.x, p.y, side * absZ);

        // Drive walk animation via Vert parameter
        _anim.SetFloat("Vert", 1f);
    }
}
