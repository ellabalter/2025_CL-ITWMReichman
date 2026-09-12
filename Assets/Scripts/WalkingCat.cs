using UnityEngine;

public class WalkingCat : MonoBehaviour
{
    public Color furColor = new Color(0.55f, 0.55f, 0.55f);
    public float speed = 0.8f;
    public float turnSpeed = 70f;
    public float walkTime = 3.5f;
    public float pauseTime = 2.5f;

    private float _timer;
    private bool _walking = true;
    private float _targetYaw;
    private Transform[] _legs = new Transform[4];
    private float _legPhase;

    void Start()
    {
        _timer = walkTime;
        _targetYaw = transform.eulerAngles.y;
        BuildBody();
    }

    void BuildBody()
    {
        var furMat = new Material(Shader.Find("Standard"));
        furMat.color = furColor;

        var darkMat = new Material(Shader.Find("Standard"));
        darkMat.color = furColor * 0.6f;

        var eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = new Color(0.15f, 0.55f, 0.15f); // green eyes

        var noseMat = new Material(Shader.Find("Standard"));
        noseMat.color = new Color(0.9f, 0.5f, 0.5f);

        // === BODY ===
        var body = Part(PrimitiveType.Capsule, furMat);
        body.localPosition = new Vector3(0f, 0.17f, 0f);
        body.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.localScale = new Vector3(0.14f, 0.22f, 0.14f);

        // belly slightly lighter
        var belly = Part(PrimitiveType.Sphere, new Material(Shader.Find("Standard")));
        belly.GetComponent<Renderer>().material.color = Color.Lerp(furColor, Color.white, 0.3f);
        belly.localPosition = new Vector3(0f, 0.12f, 0f);
        belly.localScale = new Vector3(0.11f, 0.08f, 0.18f);

        // === NECK ===
        var neck = Part(PrimitiveType.Capsule, furMat);
        neck.localPosition = new Vector3(0f, 0.27f, 0.12f);
        neck.localRotation = Quaternion.Euler(30f, 0f, 0f);
        neck.localScale = new Vector3(0.08f, 0.09f, 0.08f);

        // === HEAD ===
        var head = Part(PrimitiveType.Sphere, furMat);
        head.localPosition = new Vector3(0f, 0.33f, 0.22f);
        head.localScale = new Vector3(0.14f, 0.13f, 0.14f);

        // Muzzle
        var muzzle = Part(PrimitiveType.Sphere, furMat);
        muzzle.localPosition = new Vector3(0f, 0.30f, 0.29f);
        muzzle.localScale = new Vector3(0.08f, 0.06f, 0.06f);

        // Nose
        var nose = Part(PrimitiveType.Sphere, noseMat);
        nose.localPosition = new Vector3(0f, 0.315f, 0.32f);
        nose.localScale = new Vector3(0.025f, 0.018f, 0.025f);

        // Eyes
        var eyeL = Part(PrimitiveType.Sphere, eyeMat);
        eyeL.localPosition = new Vector3(-0.045f, 0.345f, 0.285f);
        eyeL.localScale = Vector3.one * 0.028f;
        var eyeR = Part(PrimitiveType.Sphere, eyeMat);
        eyeR.localPosition = new Vector3(0.045f, 0.345f, 0.285f);
        eyeR.localScale = Vector3.one * 0.028f;

        // Pupils
        var pupilMat = new Material(Shader.Find("Standard"));
        pupilMat.color = Color.black;
        var pupL = Part(PrimitiveType.Sphere, pupilMat);
        pupL.localPosition = new Vector3(-0.045f, 0.345f, 0.295f);
        pupL.localScale = Vector3.one * 0.015f;
        var pupR = Part(PrimitiveType.Sphere, pupilMat);
        pupR.localPosition = new Vector3(0.045f, 0.345f, 0.295f);
        pupR.localScale = Vector3.one * 0.015f;

        // Ears (triangular-ish with cube)
        var earL = Part(PrimitiveType.Cube, furMat);
        earL.localPosition = new Vector3(-0.055f, 0.40f, 0.21f);
        earL.localRotation = Quaternion.Euler(0f, 0f, -15f);
        earL.localScale = new Vector3(0.04f, 0.06f, 0.02f);
        var earR = Part(PrimitiveType.Cube, furMat);
        earR.localPosition = new Vector3(0.055f, 0.40f, 0.21f);
        earR.localRotation = Quaternion.Euler(0f, 0f, 15f);
        earR.localScale = new Vector3(0.04f, 0.06f, 0.02f);

        // Inner ear
        var innerMat = new Material(Shader.Find("Standard"));
        innerMat.color = new Color(0.9f, 0.6f, 0.65f);
        var iearL = Part(PrimitiveType.Cube, innerMat);
        iearL.localPosition = new Vector3(-0.052f, 0.40f, 0.22f);
        iearL.localRotation = Quaternion.Euler(0f, 0f, -15f);
        iearL.localScale = new Vector3(0.025f, 0.04f, 0.018f);
        var iearR = Part(PrimitiveType.Cube, innerMat);
        iearR.localPosition = new Vector3(0.052f, 0.40f, 0.22f);
        iearR.localRotation = Quaternion.Euler(0f, 0f, 15f);
        iearR.localScale = new Vector3(0.025f, 0.04f, 0.018f);

        // === TAIL ===
        var tail1 = Part(PrimitiveType.Capsule, furMat);
        tail1.localPosition = new Vector3(0f, 0.22f, -0.22f);
        tail1.localRotation = Quaternion.Euler(40f, 0f, 0f);
        tail1.localScale = new Vector3(0.04f, 0.14f, 0.04f);

        var tail2 = Part(PrimitiveType.Capsule, furMat);
        tail2.localPosition = new Vector3(0f, 0.33f, -0.30f);
        tail2.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        tail2.localScale = new Vector3(0.035f, 0.10f, 0.035f);

        // === LEGS (4) ===
        // Front-left, front-right, back-left, back-right
        float[] lx = { -0.065f,  0.065f, -0.065f,  0.065f };
        float[] lz = {  0.13f,   0.13f,  -0.10f,  -0.10f };
        for (int i = 0; i < 4; i++)
        {
            var legRoot = new GameObject("LegRoot_" + i);
            legRoot.transform.SetParent(transform, false);
            legRoot.transform.localPosition = new Vector3(lx[i], 0.17f, lz[i]);

            // Upper leg
            var upper = Part(PrimitiveType.Capsule, furMat);
            upper.SetParent(legRoot.transform, false);
            upper.localPosition = new Vector3(0f, -0.06f, 0f);
            upper.localScale = new Vector3(0.045f, 0.07f, 0.045f);

            // Lower leg (paw)
            var lower = Part(PrimitiveType.Capsule, furMat);
            lower.SetParent(legRoot.transform, false);
            lower.localPosition = new Vector3(0f, -0.16f, 0.02f);
            lower.localScale = new Vector3(0.035f, 0.055f, 0.035f);

            // Paw
            var paw = Part(PrimitiveType.Sphere, furMat);
            paw.SetParent(legRoot.transform, false);
            paw.localPosition = new Vector3(0f, -0.22f, 0.03f);
            paw.localScale = new Vector3(0.05f, 0.03f, 0.06f);

            _legs[i] = legRoot.transform;
        }
    }

    Transform Part(PrimitiveType type, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(transform, false);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material = mat;
        return go.transform;
    }

    Transform Part(PrimitiveType type, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material = mat;
        return go.transform;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _walking = !_walking;
            _timer = _walking ? walkTime : pauseTime;
            if (_walking)
                _targetYaw = transform.eulerAngles.y + Random.Range(-70f, 70f);
        }

        if (_walking)
        {
            float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, _targetYaw, turnSpeed * Time.deltaTime);
            transform.eulerAngles = new Vector3(0f, yaw, 0f);
            transform.position += transform.forward * speed * Time.deltaTime;

            // Animate legs — alternating diagonal pairs
            _legPhase += Time.deltaTime * 8f;
            if (_legs[0] != null)
            {
                float a = Mathf.Sin(_legPhase) * 18f;
                _legs[0].localRotation = Quaternion.Euler( a, 0f, 0f);
                _legs[3].localRotation = Quaternion.Euler( a, 0f, 0f);
                _legs[1].localRotation = Quaternion.Euler(-a, 0f, 0f);
                _legs[2].localRotation = Quaternion.Euler(-a, 0f, 0f);
            }
        }
        else
        {
            // Sitting still — legs neutral
            if (_legs[0] != null)
                for (int i = 0; i < 4; i++)
                    _legs[i].localRotation = Quaternion.identity;
        }
    }
}
