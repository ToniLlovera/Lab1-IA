using UnityEngine;


public class SteeringAgent : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxSpeed = 8f;
    public float maxForce = 12f;

    [Header("Rotación")]
    [Tooltip("Factor de respuesta del Slerp hacia la dirección de la velocidad.")]
    public float turnResponsiveness = 8f;

    [Header("Debug")]
    public bool drawVelocity = true;

    [HideInInspector] public Vector3 velocity;
    private Vector3 _acc;

    public void AddForce(Vector3 force)
    {
        _acc += force;
    }

 
    public void ApplySteering(Vector3 steeringForce, float dt)
    {
        _acc += steeringForce;
        Integrate(dt);
    }

    public void Integrate(float dt)
    {
      
        velocity += _acc * dt;

        
        float maxSpeedSq = maxSpeed * maxSpeed;
        if (velocity.sqrMagnitude > maxSpeedSq)
            velocity = velocity.normalized * maxSpeed;

      
        if (velocity.sqrMagnitude > 1e-4f)
        {
            var dir = velocity.normalized;
            var trot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, trot, turnResponsiveness * dt);
        }

      
        transform.position += velocity * dt;

     
        _acc = Vector3.zero;
    }

    public Vector3 SteerTowards(Vector3 desiredVelocity)
    {
        Vector3 steer = desiredVelocity - velocity;
        float maxF = maxForce;
        float maxFSq = maxF * maxF;
        if (steer.sqrMagnitude > maxFSq)
            steer = steer.normalized * maxF;
        return steer;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawVelocity) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + velocity);
    }
}