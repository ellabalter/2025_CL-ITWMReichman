using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SimpleDriver : MonoBehaviour
{
    public float forwardSpeed = 12f;
    public bool autoDrive = true;
    public float turnRate = 40f;

    [Tooltip("Auto-drive for this many seconds then loop back. 0 = infinite.")]
    public float driveDurationSeconds = 300f; // 5 minutes

    [Tooltip("X position to teleport back to when the loop resets.")]
    public float loopStartX = 0f;

    private CharacterController _cc;
    private float _elapsed;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float speed = autoDrive ? forwardSpeed : 0f;

        if (!autoDrive)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) speed = forwardSpeed;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) speed = -forwardSpeed * 0.5f;
        }

        float turn = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn += 1f;
        if (turn != 0f) transform.Rotate(0f, turn * turnRate * dt, 0f);

        Vector3 delta = transform.forward * speed * dt;
        if (_cc != null && _cc.enabled)
            _cc.Move(delta);
        else
            transform.position += delta;

        // Loop back after driveDurationSeconds without teleporting — just reset position
        if (autoDrive && driveDurationSeconds > 0f)
        {
            _elapsed += dt;
            if (_elapsed >= driveDurationSeconds)
            {
                _elapsed = 0f;
                var p = transform.position;
                if (_cc != null) _cc.enabled = false;
                transform.position = new Vector3(loopStartX, p.y, p.z);
                if (_cc != null) _cc.enabled = true;
            }
        }
    }
}
