using UnityEngine;
using UnityEngine.AI;

public class WanderingAgent : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Configuración de wandering")]
    public float wanderRadius = 10f;      // radio máximo para elegir destino
    public float wanderTimer = 3f;        // tiempo entre cambios de destino
    public float rotationSpeed = 5f;      // velocidad de giro suave

    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer; // fuerza movimiento inicial
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavMeshLocation(wanderRadius);
            agent.SetDestination(newPos);
            timer = 0;
        }

        // Rotación suave hacia la dirección del movimiento
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // Genera un punto aleatorio válido en la NavMesh
    Vector3 RandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position; // si falla, quedarse
    }
}
