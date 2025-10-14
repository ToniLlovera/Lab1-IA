using UnityEngine;

[RequireComponent(typeof(Transform))]
public class Boid : MonoBehaviour
{
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public Vector3 acceleration;

    // Throttling/steering cache
    [HideInInspector] public float nextSteerTime;
    [HideInInspector] public Vector3 cachedSteering;

    public void Initialize(Vector3 initialVelocity, float initialNextSteerTime)
    {
        velocity = initialVelocity;
        acceleration = Vector3.zero;
        nextSteerTime = initialNextSteerTime;
        cachedSteering = Vector3.zero;
    }

    public void AddForce(Vector3 force)
    {
        acceleration += force;
    }

    public void Integrate(float dt, BoidSettings settings)
    {
        // v(t+dt)
        velocity += acceleration * dt;

        // Clamp velocidad
        float maxSpeed = settings.maxSpeed;
        float maxSpeedSq = maxSpeed * maxSpeed;
        if (velocity.sqrMagnitude > maxSpeedSq)
            velocity = velocity.normalized * maxSpeed;

        // Orientación suavizada (Slerp a la dirección de la velocidad)
        Vector3 dir = velocity.sqrMagnitude > 1e-4f ? velocity.normalized : transform.forward;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * settings.turnResponsiveness);

        // x(t+dt)
        transform.position += velocity * dt;

        // limpiar para el siguiente tick
        acceleration = Vector3.zero;
    }
}