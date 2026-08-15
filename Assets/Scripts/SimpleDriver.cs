using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SimpleDriver : MonoBehaviour
{
    public float forwardSpeed = 12f;
    public bool autoDrive = true;
    public float turnRate = 40f;

    private CharacterController _cc;

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
        {
            _cc.Move(delta);
        }
        else
        {
            transform.position += delta;
        }
    }
}
