using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SteeringAgent : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float mass = 1f;
    [HideInInspector] public Vector3 velocity;

    protected virtual void Start()
    {
        velocity = Vector3.zero;
    }

    protected virtual void Update()
    {

    }

    public void ApplySteering(Vector3 steeringForce, float deltaTime)
    {
        // Clamp steering force
        if (steeringForce.magnitude > maxForce)
            steeringForce = steeringForce.normalized * maxForce;

        Vector3 acceleration = steeringForce / mass;
        velocity += acceleration * deltaTime;

        // Clamp speed
        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;


        transform.position += velocity * deltaTime;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = velocity.normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * deltaTime);
        }
    }

    // Steering helpers
    public Vector3 Seek(Vector3 target)
    {
        Vector3 desired = (target - transform.position);
        if (desired.sqrMagnitude < 0.000001f) return Vector3.zero;
        desired = desired.normalized * maxSpeed;
        return desired - velocity;
    }

    public Vector3 Flee(Vector3 threat)
    {
        Vector3 desired = (transform.position - threat);
        if (desired.sqrMagnitude < 0.000001f) return Vector3.zero;
        desired = desired.normalized * maxSpeed;
        return desired - velocity;
    }

    public Vector3 Arrive(Vector3 target, float slowingRadius)
    {
        Vector3 toTarget = target - transform.position;
        float dist = toTarget.magnitude;
        if (dist < 0.01f) return Vector3.zero;
        float r = Mathf.Clamp(dist / slowingRadius, 0.01f, 1f);
        Vector3 desired = toTarget.normalized * maxSpeed * r;
        return desired - velocity;
    }
}

