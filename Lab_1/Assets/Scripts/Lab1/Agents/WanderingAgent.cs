using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WanderingAgent : MonoBehaviour
{
    [Header("Wander")]
    [Tooltip("Radio del área para elegir destinos aleatorios (centrado hacia delante).")]
    public float wanderRadius = 8f;

    [Tooltip("Tiempo base entre elecciones de destino.")]
    public float wanderInterval = 2.5f;

    [Tooltip("Jitter del intervalo para evitar sincronías.")]
    public float wanderIntervalJitter = 0.3f;

    [Tooltip("Factor de giro hacia la velocidad actual.")]
    public float turnResponsiveness = 6f;

    [Tooltip("Multiplicador de margen en SamplePosition para evitar paredes/bordes.")]
    public float sampleMarginMultiplier = 1.2f;

    private NavMeshAgent agent;
    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderInterval * Random.Range(0.3f, 1.0f);

        
        agent.autoBraking = false;
        agent.stoppingDistance = 0f;
        agent.acceleration = Mathf.Max(agent.acceleration, 10f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 600f);
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;

        timer += Time.deltaTime;
        float targetInterval = wanderInterval + Random.Range(-wanderIntervalJitter, wanderIntervalJitter);

        if (timer >= targetInterval)
        {
            Vector3 forwardBias = transform.position + transform.forward * (wanderRadius * 0.5f);
            Vector3 randomDirection = forwardBias + Random.insideUnitSphere * wanderRadius;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius * sampleMarginMultiplier, NavMesh.AllAreas))
                agent.SetDestination(hit.position);

            timer = 0f;
        }

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(agent.velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnResponsiveness);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}