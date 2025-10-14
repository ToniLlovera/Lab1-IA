using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class RobberEvadeHide : MonoBehaviour
{
    [Header("Police Reference")]
    public Transform police;  // Referencia al policía

    [Header("Hide Objects")]
    [Tooltip("Array de NavMeshObstacles o padres que contienen obstáculos para esconderse.")]
    public NavMeshObstacle[] hideObstacles;  // Objetos para esconderse

    [Header("Evade Settings")]
    public float evadeDistance = 6f;  // Distancia base para huir
    public float updateHz = 10f;  // Frecuencia de actualización
    public float zigZagRadius = 2f;  // Radio de zig-zag para evadir

    [Header("Hide Settings")]
    public float hideOffset = 1.5f;  // Offset para posicionarse detrás del obstáculo
    public float minHideDuration = 2f;  // Duración mínima de escondite
    public float maxHideDuration = 5f;  // Duración máxima de escondite
    public float safeDistanceToExit = 10f;  // Distancia mínima para salir del escondite

    [Header("Rotation")]
    public float turnResponsiveness = 6f;  // Responsividad de rotación

    [Header("Line of Sight")]
    public LayerMask losBlockers;  // Capas que bloquean la línea de visión

    [Header("Debug")]
    public bool debugDraw = true;

    private NavMeshAgent agent;
    private float _accum;  // Acumulador para la frecuencia de actualización
    private Vector3 _currentHideTarget;  // Posición actual del escondite
    private bool _isHiding;  // Estado de escondite
    private enum State { Fleeing, Hiding, Exiting }  // Estados del ladrón
    private State currentState = State.Fleeing;  // Estado inicial

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Debug.Log("Robber Start: Agent is on NavMesh? " + agent.isOnNavMesh);
        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name}: not on NavMesh.");
        agent.speed = 3f;  // Asegura una velocidad inicial
    }

    void Update()
    {
        if (police == null)
        {
            Debug.LogWarning("Police reference is null! Assign it in the Inspector.");
            return;
        }
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Agent is not on NavMesh!");
            return;
        }

        _accum += Time.deltaTime;
        float tick = 1f / Mathf.Max(1f, updateHz);
        if (_accum < tick)
        {
            RotateOnly();
            return;
        }
        _accum = 0f;

        bool policeSeesMe = HasLineOfSight(police.position, transform.position);
        Debug.Log("Police sees me? " + policeSeesMe + " | Current State: " + currentState);

        switch (currentState)
        {
            case State.Fleeing:
                HandleFleeing(policeSeesMe);
                break;
            case State.Hiding:
                HandleHiding(policeSeesMe);
                break;
            case State.Exiting:
                HandleExiting(policeSeesMe);
                break;
        }

        RotateOnly();
    }

    private void HandleFleeing(bool policeSeesMe)
    {
        Debug.Log("HandleFleeing: Police sees me? " + policeSeesMe);
        if (policeSeesMe)
        {
            if (TryGetHideBehindObstacle(out Vector3 hidePos))
            {
                _currentHideTarget = hidePos;
                agent.SetDestination(hidePos);
                currentState = State.Hiding;
                Debug.Log("Setting destination to hide position: " + hidePos);
            }
            else
            {
                EvadeZigZag();
                Debug.Log("Evading with zig-zag");
            }
        }
        else
        {
            Vector3 fleeDirection = (transform.position - police.position).normalized * evadeDistance;
            Vector3 randomOffset = new Vector3(Random.Range(-zigZagRadius, zigZagRadius), 0, Random.Range(-zigZagRadius, zigZagRadius));
            Vector3 dest = transform.position + fleeDirection + randomOffset;

            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                Debug.Log("Setting flee destination: " + hit.position);
            }
            else
            {
                Debug.LogWarning("No valid NavMesh position for flee destination! Trying fallback.");
                // Fallback: Intentar un destino sin offset
                Vector3 fallbackDest = transform.position + fleeDirection;
                if (NavMesh.SamplePosition(fallbackDest, out hit, 1f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    Debug.Log("Fallback destination set: " + hit.position);
                }
                else
                {
                    Debug.LogError("Even fallback destination is invalid!");
                }
            }

            if (debugDraw) Debug.DrawLine(transform.position, dest, Color.yellow, 0.2f);
        }
    }

    private void HandleHiding(bool policeSeesMe)
    {
        Debug.Log("HandleHiding: Checking if at hide target");
        if (_currentHideTarget != Vector3.zero &&
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            StartCoroutine(HideCoroutine());
            Debug.Log("Starting HideCoroutine at position: " + transform.position);
        }
    }

    private IEnumerator HideCoroutine()
    {
        float hideTime = Random.Range(minHideDuration, maxHideDuration);
        float timer = 0f;
        Debug.Log("Hiding for up to " + hideTime + " seconds");

        while (timer < hideTime)
        {
            if (!HasLineOfSight(police.position, transform.position) && Vector3.Distance(transform.position, police.position) > safeDistanceToExit)
            {
                currentState = State.Exiting;
                Debug.Log("Exiting hide early: Safe conditions met");
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;
        currentState = State.Exiting;
        Debug.Log("Hide duration ended, exiting to flee");
    }

    private void HandleExiting(bool policeSeesMe)
    {
        Debug.Log("HandleExiting: Police sees me? " + policeSeesMe);
        Vector3 fleeDirection = (transform.position - police.position).normalized * evadeDistance * 1.5f;
        Vector3 dest = transform.position + fleeDirection;

        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("Setting exit destination: " + hit.position);
        }
        else
        {
            Debug.LogWarning("No valid NavMesh position for exit destination!");
        }

        if (!policeSeesMe)
            currentState = State.Fleeing;

        if (debugDraw) Debug.DrawLine(transform.position, dest, Color.cyan, 0.2f);
    }

    void EvadeZigZag()
    {
        Vector3 dir = (transform.position - police.position).normalized;
        Vector3 randomOffset = new Vector3(Random.Range(-zigZagRadius, zigZagRadius), 0, Random.Range(-zigZagRadius, zigZagRadius));
        Vector3 dest = transform.position + dir * evadeDistance + randomOffset;
        Debug.Log("Attempting zig-zag to: " + dest);  // Log adicional para ver la posición

        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log("ZigZag destination set: " + hit.position);
        }
        else
        {
            Debug.LogWarning("No valid NavMesh position for zig-zag at: " + dest);
        }

        if (debugDraw) Debug.DrawLine(transform.position, dest, Color.magenta, 0.2f);
    }

    bool TryGetHideBehindObstacle(out Vector3 result)
    {
        result = Vector3.zero;
        float bestScore = Mathf.Infinity;

        foreach (var obs in hideObstacles)
        {
            if (obs == null) continue;

            Vector3 dirFromPolice = (obs.transform.position - police.position).normalized;
            Vector3 hidePosCandidate = obs.transform.position + dirFromPolice * (obs.radius + hideOffset);

            if (NavMesh.SamplePosition(hidePosCandidate, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                if (!HasLineOfSight(police.position, hit.position))
                {
                    float distanceToHide = Vector3.Distance(transform.position, hit.position);
                    float distanceFromPolice = Vector3.Distance(hit.position, police.position);
                    float score = distanceToHide + (distanceFromPolice * 0.5f);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        result = hit.position;
                    }
                }
            }
        }

        if (result != Vector3.zero)
            Debug.Log("Found hide position: " + result);

        return result != Vector3.zero;
    }

    bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from);
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;

        return !Physics.Raycast(from + Vector3.up, dir.normalized, dist, losBlockers);
    }

    void RotateOnly()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(agent.velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnResponsiveness);
        }
    }
}