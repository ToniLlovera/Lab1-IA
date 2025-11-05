using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Referencia al jugador (busca automáticamente por tag 'Player' si se deja vacío).")]
    public Transform player;

    [Tooltip("Objeto padre que contiene a todos los zombies.")]
    public Transform zombiesParent;

    [Header("Movement Settings")]
    [Tooltip("Velocidad normal de patrulla o deambulación.")]
    public float walkSpeed = 1.5f;

    [Tooltip("Velocidad de persecución cuando detecta al jugador.")]
    public float chaseSpeed = 3.5f;

    [Header("Perception Settings")]
    [Tooltip("Intervalo entre chequeos de frustum (optimización).")]
    public float frustumCheckInterval = 0.3f;

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

    private NavMeshAgent agent;
    private Renderer myRenderer;
    private bool isChasing = false;

    private readonly Collider[] sepHits = new Collider[16];
    private Vector3 sepAccum;
    private float stuckTimer;
    private float nudgeTimer;
    private Vector3 nudgeDir;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        myRenderer = GetComponentInChildren<Renderer>();

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

        StartCoroutine(PerceptionLoop());
    }

    IEnumerator PerceptionLoop()
    {
        var wait = new WaitForSeconds(frustumCheckInterval);

        while (true)
        {
            yield return wait;

            if (Camera.main == null || myRenderer == null) continue;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (GeometryUtility.TestPlanesAABB(planes, myRenderer.bounds))
            {
                OnSeenByPlayer();
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

        if (zombiesParent != null)
        {
            foreach (Transform child in zombiesParent)
            {
                if (child != transform)
                {
                    child.SendMessage("OnPlayerSpotted", player.position, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    void OnPlayerSpotted(Vector3 playerPosition)
    {
        if (isChasing) return;

        isChasing = true;
        agent.speed = chaseSpeed;
        agent.autoBraking = false;
        agent.SetDestination(playerPosition);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SmellSource"))
        {
            Vector3 smellPos = other.transform.position;
            agent.SetDestination(smellPos);
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
            agent.Move(sepAccum); 
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
            agent.Move(delta);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
     
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
#endif
}
