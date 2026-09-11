using UnityEngine;

[ExecuteAlways]
public class StreetPedestrian : MonoBehaviour
{
    public float speed      = 1.3f;
    public float patrolRange = 18f;
    public float sidewalkMinZ = 4.0f;
    public float sidewalkMaxZ = 6.0f;

    private Vector3 _origin;
    private bool    _built;

    public void Build()
    {
        _origin = transform.position;
        _built  = true;
        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.speed = 1f;
    }

    void Start()
    {
        _origin = transform.position;
        _built  = true;
    }

    void Update()
    {
        if (!Application.isPlaying || !_built) return;

        transform.position += transform.forward * speed * Time.deltaTime;

        float dist = transform.position.x - _origin.x;
        if (dist > patrolRange || dist < -patrolRange)
            transform.Rotate(0f, 180f, 0f);

        var p    = transform.position;
        int side = p.z >= 0 ? 1 : -1;
        float absZ = Mathf.Clamp(Mathf.Abs(p.z), sidewalkMinZ, sidewalkMaxZ);
        transform.position = new Vector3(p.x, p.y, side * absZ);
    }
}
