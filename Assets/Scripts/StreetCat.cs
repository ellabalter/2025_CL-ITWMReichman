using UnityEngine;

// A cat that paces back and forth along the sidewalk.
// Attach a cat prefab as the visual child; this script drives movement.
[ExecuteAlways]
public class StreetCat : MonoBehaviour
{
    public float patrolRange = 6f;   // metres each way from start
    public float speed       = 0.8f; // m/s
    public GameObject catPrefab;

    private Vector3 _origin;
    private float   _dir = 1f;
    private bool    _built;

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
        go.transform.localScale    = Vector3.one * 0.35f; // cats are small
        go.hideFlags = HideFlags.DontSave;
        _origin = transform.position;
        _built  = true;
    }

    void Start()
    {
        _origin = transform.position;
        _built  = true;
    }

    void Update()
    {
        if (!Application.isPlaying || !_built) return;
        transform.position += Vector3.right * (_dir * speed * Time.deltaTime);
        float dist = transform.position.x - _origin.x;
        if (dist > patrolRange || dist < -patrolRange)
        {
            _dir = -_dir;
            transform.Rotate(0f, 180f, 0f);
        }
    }
}
