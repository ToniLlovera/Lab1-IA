using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Referencia al jugador (busca automáticamente por tag 'Player' si se deja vacío).")]
    public Transform player;

    [Tooltip("Objeto padre que contiene a todos los zombies (opcional, ya no es necesario para el broadcast vecinal).")]
    public Transform zombiesParent;

    [Header("Movement Settings")]
    [Tooltip("Velocidad normal de patrulla o deambulación.")]
    public float walkSpeed = 1.5f;

    [Tooltip("Velocidad de persecución cuando detecta al jugador.")]
    public float chaseSpeed = 3.5f;

    [Header("Perception Settings")]
    [Tooltip("Intervalo entre chequeos de frustum (optimización).")]
    public float frustumCheckInterval = 0.3f;

    [Tooltip("Tiempo sin ver al jugador antes de olvidar.")]
    public float forgetTime = 3f;

    [Tooltip("Distancia máxima para detectar al jugador por proximidad (además del frustum).")]
    public float detectionDistance = 20f;

    [Header("Wandering Settings")]
    [Tooltip("Tiempo entre cambios de dirección al deambular.")]
    public float wanderInterval = 5f;
    [Tooltip("Distancia máxima a la que se mueve al deambular.")]
    public float wanderRadius = 10f;

    [Header("Anti-atasco: Separación entre zombies")]
    [Tooltip("Capa que usan los zombies. Crea la Layer 'Zombies' y asígnala a los prefabs.")]
    public LayerMask zombiesMask;
    [Tooltip("Radio para detectar vecinos y aplicar separación.")]
    public float separationRadius = 0.9f;
    [Tooltip("Intensidad de la separación (m/s).")]
    public float separationStrength = 1.2f;

    [Header("Anti-atasco: Desatascador suave")]
    [Tooltip("Velocidad por debajo de la cual se considera que está casi parado.")]
    public float stuckSpeedEps = 0.05f;
    [Tooltip("Tiempo a baja velocidad para considerarse atascado.")]
    public float stuckCheckTime = 0.9f;
    [Tooltip("Intensidad del empujón lateral (m/s).")]
    public float nudgeStrength = 0.6f;
    [Tooltip("Duración del empujón lateral (s).")]
    public float nudgeTime = 0.25f;

    [Header("Walls Detection")]
    [Tooltip("Layer usada por los muros (para raycasts del olor).")]
    public LayerMask wallsMask;

    [Header("Communication (Broadcast + VFX)")]
    [Tooltip("Radio para avisar a otros zombies cercanos.")]
    public float alertRadius = 12f;

    private NavMeshAgent agent;
    private Renderer myRenderer;
    private bool isChasing = false;
    private float unseenTimer = 0f;

   
    private readonly Collider[] sepHits = new Collider[16];
    private Vector3 sepAccum;
    private float stuckTimer;
    private float nudgeTimer;
    private Vector3 nudgeDir;

  
    private readonly Collider[] commHits = new Collider[32];
    private ZombieCommVFX commVFX;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        myRenderer = GetComponentInChildren<Renderer>();
        commVFX = GetComponent<ZombieCommVFX>();

        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        agent.speed = walkSpeed;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80);

    
        if (zombiesMask.value == 0)
        {
            int zLayer = LayerMask.NameToLayer("Zombies");
            if (zLayer >= 0) zombiesMask = 1 << zLayer;
        }

    
        if (wallsMask.value == 0)
        {
            int wLayer = LayerMask.NameToLayer("Walls");
            if (wLayer >= 0) wallsMask = 1 << wLayer;
        }

        StartCoroutine(PerceptionLoop());
        StartCoroutine(WanderLoop());
    }

    IEnumerator PerceptionLoop()
    {
        var wait = new WaitForSeconds(frustumCheckInterval);

        while (true)
        {
            yield return wait;

            if (Camera.main == null || myRenderer == null) continue;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            bool visible = GeometryUtility.TestPlanesAABB(planes, myRenderer.bounds);

            bool close = player != null && Vector3.Distance(transform.position, player.position) < detectionDistance;

            if (visible || close)
            {
                OnSeenByPlayer();
                unseenTimer = 0f;
            }
            else if (isChasing)
            {
                unseenTimer += frustumCheckInterval;
                if (unseenTimer > forgetTime)
                {
                    isChasing = false;
                    agent.speed = walkSpeed;
                    agent.autoBraking = true;
                    agent.ResetPath();
                }
            }
        }
    }

    void OnSeenByPlayer()
    {
        if (player == null || isChasing) return;

        isChasing = true;
        agent.speed = chaseSpeed;
        agent.autoBraking = false;
        agent.SetDestination(player.position);

        var receivers = AlertNearbyWithBroadcast(player);
        if (commVFX != null) commVFX.PlaySendVFX(receivers);
    }

    void OnPlayerSpotted(Vector3 playerPosition)
    {
        if (isChasing) return;

        isChasing = true;
        agent.speed = chaseSpeed;
        agent.autoBraking = false;
        agent.SetDestination(playerPosition);

        if (commVFX != null) commVFX.PlayReceiveVFX();
    }

    void OnHordeAlert(Transform targetPlayer)
    {
        if (!isChasing)
        {
            isChasing = true;
            player = targetPlayer;
            agent.speed = chaseSpeed;
            agent.autoBraking = false;
            agent.SetDestination(player.position);
        }
        if (commVFX != null) commVFX.PlayReceiveVFX();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SmellSource") && agent != null && agent.isOnNavMesh)
        {
            Vector3 smellPos = other.transform.position;
            Vector3 dir = smellPos - transform.position;

 
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dir.normalized, dir.magnitude, wallsMask))
            {
                bool playerClose = player != null && Vector3.Distance(transform.position, player.position) < detectionDistance;

                if (!isChasing && !playerClose)
                {
                    agent.SetDestination(smellPos);
                }
                else if (playerClose && !isChasing)
                {
                    OnSeenByPlayer();
                }
            }
        }
    }

    void Update()
    {
        if (isChasing && player != null)
        {
            if (!agent.pathPending && agent.remainingDistance < 1.0f)
            {
                agent.SetDestination(player.position);
            }
        }

        if (agent != null && agent.isOnNavMesh)
        {
            ApplySeparation();
            HandleStuck();
            ApplyNudge();
        }
    }

    List<Transform> AlertNearbyWithBroadcast(Transform targetPlayer)
    {
        var list = new List<Transform>(16);
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            alertRadius,
            commHits,
            zombiesMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            var c = commHits[i];
            if (!c) continue;
            var go = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;
            if (go == gameObject) continue;

            go.BroadcastMessage("OnHordeAlert", targetPlayer, SendMessageOptions.DontRequireReceiver);

     
            list.Add(go.transform);
        }
        return list;
    }

    void ApplySeparation()
    {
        sepAccum = Vector3.zero;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            sepHits,
            zombiesMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            var c = sepHits[i];
            if (!c) continue;

            var go = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;
            if (go == gameObject) continue;

            Vector3 toMe = transform.position - c.transform.position;
            toMe.y = 0f;
            float dist = toMe.magnitude + 1e-4f;
            sepAccum += toMe / dist;
        }

        if (sepAccum.sqrMagnitude > 0.0001f)
        {
            sepAccum = sepAccum.normalized * separationStrength * Time.deltaTime;
            Vector3 candidate = transform.position + sepAccum;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.3f, NavMesh.AllAreas))
            {
                agent.Move(hit.position - transform.position);
            }
        }
    }

    void HandleStuck()
    {
        bool hasPath = agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.1f;
        bool verySlow = agent.velocity.sqrMagnitude < (stuckSpeedEps * stuckSpeedEps);

        if (hasPath && verySlow)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckCheckTime && nudgeTimer <= 0f)
            {
                Vector3 fwd = transform.forward;
                Vector3 right = new Vector3(fwd.z, 0f, -fwd.x).normalized;
                float side = Random.value < 0.5f ? -1f : 1f;

                nudgeDir = (right * side + Random.insideUnitSphere * 0.25f);
                nudgeDir.y = 0f;
                nudgeDir.Normalize();

                nudgeTimer = nudgeTime;
                stuckTimer = 0f;

                agent.avoidancePriority = Mathf.Clamp(agent.avoidancePriority + Random.Range(-10, -5), 0, 99);
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    void ApplyNudge()
    {
        if (nudgeTimer > 0f)
        {
            nudgeTimer -= Time.deltaTime;
            Vector3 delta = nudgeDir * nudgeStrength * Time.deltaTime;
            Vector3 candidate = transform.position + delta;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.3f, NavMesh.AllAreas))
            {
                agent.Move(hit.position - transform.position);
            }
        }
    }

    IEnumerator WanderLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(wanderInterval);

            if (!isChasing && agent != null && agent.isOnNavMesh)
            {
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += transform.position;
                randomDirection.y = transform.position.y;

                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}
