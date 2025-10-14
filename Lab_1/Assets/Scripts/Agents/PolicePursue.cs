using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class PolicePursue : MonoBehaviour
{
    [Header("References")]
    public Transform robber;  // Referencia al ladrón

    [Header("Pursuit Settings")]
    public float updateHz = 15f;  // Frecuencia de actualización aumentada para mayor reactividad
    public float leadTimeMultiplier = 1.2f;  // Multiplicador para predecir mejor la trayectoria
    public float memoryDuration = 5f;  // Duración de memoria aumentada
    public float searchRadius = 5f;  // Radio de búsqueda aumentado
    public float searchInterval = 1.5f;  // Intervalo de búsqueda más frecuente
    public float patrolSpeed = 2f;  // Velocidad para patrullar

    [Header("Line of Sight")]
    public LayerMask losBlockers;  // Capas que bloquean la línea de visión

    [Header("Rotation")]
    public float turnResponsiveness = 8f;  // Mayor responsividad de rotación

    [Header("Debug")]
    public bool debugDraw = true;

    private NavMeshAgent agent;
    private float _accum;
    private Vector3 _lastKnownPosition;
    private float _memoryTimer;
    private bool _hasLOS;
    private float _searchTimer;
    private enum State { Pursuing, Searching, Patrolling }  // Estados para el policía
    private State currentState = State.Patrolling;  // Estado inicial: patrullar

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name}: not on NavMesh.");

        _memoryTimer = 0f;
        _searchTimer = 0f;
        agent.speed = 220;
        agent.acceleration = 120;// Iniciar con velocidad de patrulla
    }

    void Update()
    {
        if (robber == null || !agent.isOnNavMesh) return;

        _accum += Time.deltaTime;
        float tick = 1f / Mathf.Max(1f, updateHz);
        if (_accum < tick)
        {
            RotateOnly();
            return;
        }
        _accum = 0f;

        _hasLOS = HasLineOfSight(transform.position, robber.position);

        switch (currentState)
        {
            case State.Pursuing:
                HandlePursuing();
                break;
            case State.Searching:
                HandleSearching();
                break;
            case State.Patrolling:
                HandlePatrolling();
                break;
        }

        RotateOnly();  // Siempre rotar hacia la dirección de movimiento
    }

    private void HandlePursuing()
    {
        if (_hasLOS)
        {
            _lastKnownPosition = robber.position;
            _memoryTimer = memoryDuration;
            Vector3 robberVel = EstimateRobberVelocity(Time.deltaTime * updateHz);
            Pursue(robber.position, robberVel);
        }
        else
        {
            _memoryTimer -= Time.deltaTime;
            if (_memoryTimer > 0f)
            {
                currentState = State.Searching;  // Cambiar a búsqueda si se pierde la vista
            }
            else
            {
                currentState = State.Patrolling;  // Volver a patrullar
            }
        }
    }

    private void HandleSearching()
    {
        agent.SetDestination(_lastKnownPosition);  // Ir a la última posición conocida

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            _searchTimer -= Time.deltaTime;
            if (_searchTimer <= 0f)
            {
                _searchTimer = searchInterval;
                // Búsqueda en patrón: círculos alrededor de la posición
                float angle = Time.time * 0.5f;  // Ángulo para movimiento circular
                Vector3 searchOffset = new Vector3(
                    Mathf.Cos(angle) * searchRadius,
                    0,
                    Mathf.Sin(angle) * searchRadius
                );
                Vector3 searchTarget = _lastKnownPosition + searchOffset;

                if (NavMesh.SamplePosition(searchTarget, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        if (_hasLOS) currentState = State.Pursuing;  // Volver a perseguir si se ve al ladrón
    }

    private void HandlePatrolling()
    {
        // Patrullar aleatoriamente por el mapa
        if (!agent.hasPath)
        {
            Vector3 randomPatrolPoint = transform.position + new Vector3(
                Random.Range(-10f, 10f),
                0,
                Random.Range(-10f, 10f)
            );

            if (NavMesh.SamplePosition(randomPatrolPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        if (_hasLOS) currentState = State.Pursuing;  // Cambiar a persecución si se ve al ladrón
    }

    void Pursue(Vector3 targetPos, Vector3 targetVel)
    {
        float dist = Vector3.Distance(transform.position, targetPos);
        float denom = agent.speed + Mathf.Max(0.1f, targetVel.magnitude);
        float t = (dist / denom) * leadTimeMultiplier;
        Vector3 predicted = targetPos + targetVel * t;

        if (NavMesh.SamplePosition(predicted, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(predicted);
    }

    Vector3 EstimateRobberVelocity(float deltaT)
    {
        Vector3 vel = Vector3.zero;
        var rb = robber.GetComponent<Rigidbody>();

        if (rb != null)
        {
            vel = rb.linearVelocity;
        }
        else if (robber != null)
        {
            vel = (robber.position - transform.position) / deltaT;  // Estimación básica
        }

        return vel;
    }

    bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;

        return !Physics.Raycast(from + Vector3.up, dir.normalized, dist, losBlockers, QueryTriggerInteraction.Ignore);
    }

    void RotateOnly()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(agent.velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnResponsiveness);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _hasLOS ? Color.green : Color.red;
        if (robber != null)
            Gizmos.DrawLine(transform.position + Vector3.up, robber.position + Vector3.up);
    }
}