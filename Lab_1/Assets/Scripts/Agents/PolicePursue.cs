using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PolicePursue : MonoBehaviour
{
    [Header("References")]
    public Transform robber;

    [Header("Pursuit Settings")]
    [Tooltip("Veces por segundo que actualizamos el destino (evita SetDestination cada frame).")]
    public float updateHz = 12f;
    [Tooltip("Multiplicador del tiempo de anticipación.")]
    public float leadTimeMultiplier = 1.1f;
    [Tooltip("Máximo horizonte de predicción (segundos).")]
    public float maxPrediction = 1.2f;
    [Tooltip("Suavizado de la posición predicha (0 = sin suavizado, 1 = muy suave).")]
    [Range(0f, 1f)] public float predictionSmoothing = 0.25f;
    [Tooltip("Radio a partir del cual consideramos 'capturado' para evitar jitter.")]
    public float captureRadius = 1.0f;

    [Header("Memory & Search")]
    public float memoryDuration = 5f;
    public float searchRadius = 5f;
    public float searchInterval = 1.5f;

    [Header("Line of Sight")]
    public LayerMask losBlockers;

    [Header("Agent Tuning")]
    public float desiredSpeed = 220f;
    public float desiredAcceleration = 80f;
    public float desiredAngularSpeed = 1080f;

    [Header("Rotation")]
    public float turnResponsiveness = 8f;

    [Header("Debug")]
    public bool debugDraw = true;

    enum State { Pursuing, Searching, Patrolling }
    State currentState = State.Patrolling;

    NavMeshAgent agent;
    float tickAccum;
    Vector3 lastKnownPosition;
    float memoryTimer;
    float searchTimer;
    bool hasLOS;

   
    Vector3 lastRobberPos;
    bool hasLastRobberPos;

 
    Vector3 smoothedPredicted;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name}: not on NavMesh.");

   
        agent.autoBraking = false;     
        agent.stoppingDistance = 0f;     
        agent.speed = desiredSpeed;        
        agent.acceleration = desiredAcceleration;
        agent.angularSpeed = desiredAngularSpeed;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoRepath = true;

        memoryTimer = 0f;
        searchTimer = 0f;
        hasLastRobberPos = false;
    }

    void Update()
    {
        if (robber == null || !agent.isOnNavMesh) return;

       
        Vector3 robberVel = EstimateRobberVelocity(Time.deltaTime);

       
        tickAccum += Time.deltaTime;
        float tick = 1f / Mathf.Max(1f, updateHz);
        bool doUpdate = false;
        if (tickAccum >= tick)
        {
            tickAccum = 0f;
            doUpdate = true;
        }

        hasLOS = HasLineOfSight(transform.position, robber.position);

        switch (currentState)
        {
            case State.Pursuing:
                HandlePursuing(doUpdate, robberVel);
                break;
            case State.Searching:
                HandleSearching(doUpdate);
                break;
            case State.Patrolling:
                HandlePatrolling(doUpdate);
                break;
        }

        RotateOnly();
    }

    void HandlePursuing(bool doUpdate, Vector3 robberVel)
    {
        if (hasLOS)
        {
            lastKnownPosition = robber.position;
            memoryTimer = memoryDuration;

            if (doUpdate)
            {
                Vector3 predicted = PredictFuturePosition(robber.position, robberVel);
              
                smoothedPredicted = Vector3.Lerp(predicted, smoothedPredicted, predictionSmoothing);

             
                if ((smoothedPredicted - agent.destination).sqrMagnitude > 0.04f)
                    agent.SetDestination(smoothedPredicted);
            }

           
            float dist = Vector3.Distance(transform.position, robber.position);
            if (dist < captureRadius && doUpdate)
            {
                Vector3 orbitOffset = (transform.right * 0.5f) + (transform.forward * 0.25f);
                agent.SetDestination(robber.position + orbitOffset);
            }
        }
        else
        {
            memoryTimer -= Time.deltaTime;
            if (memoryTimer > 0f) currentState = State.Searching;
            else currentState = State.Patrolling;
        }
    }

    void HandleSearching(bool doUpdate)
    {
        if (doUpdate) agent.SetDestination(lastKnownPosition);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                searchTimer = searchInterval;
                float angle = Time.time * 0.5f;
                Vector3 searchOffset = new Vector3(Mathf.Cos(angle) * searchRadius, 0, Mathf.Sin(angle) * searchRadius);
                Vector3 searchTarget = lastKnownPosition + searchOffset;

                if (NavMesh.SamplePosition(searchTarget, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        if (hasLOS) currentState = State.Pursuing;
    }

    void HandlePatrolling(bool doUpdate)
    {
        if (doUpdate && !agent.hasPath)
        {
            Vector3 randomPatrolPoint = transform.position + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
            if (NavMesh.SamplePosition(randomPatrolPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        if (hasLOS) currentState = State.Pursuing;
    }

    Vector3 PredictFuturePosition(Vector3 targetPos, Vector3 targetVel)
    {
        float dist = Vector3.Distance(transform.position, targetPos);
        float denom = Mathf.Max(0.1f, agent.speed + 0.1f);
        float t = Mathf.Min(maxPrediction, (dist / denom) * leadTimeMultiplier);
        Vector3 predicted = targetPos + targetVel * t;
        predicted.y = targetPos.y; 
        return predicted;
    }

    Vector3 EstimateRobberVelocity(float dt)
    {
        if (robber == null) return Vector3.zero;

      
        var rb = robber.GetComponent<Rigidbody>();
        if (rb != null)
            return rb.linearVelocity;

        
        if (!hasLastRobberPos)
        {
            lastRobberPos = robber.position;
            hasLastRobberPos = true;
            return Vector3.zero;
        }

        Vector3 vel = (robber.position - lastRobberPos) / Mathf.Max(0.0001f, dt);
        lastRobberPos = robber.position;
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
        if (robber != null)
        {
            Gizmos.color = hasLOS ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, robber.position + Vector3.up);
        }
    }
}