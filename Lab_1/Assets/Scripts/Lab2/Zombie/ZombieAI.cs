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

    private NavMeshAgent agent;
    private Renderer myRenderer;
    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        myRenderer = GetComponentInChildren<Renderer>();

        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
        }

        agent.speed = walkSpeed;
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
        // Si persigue, actualiza el destino hacia el jugador cada cierto tiempo
        if (isChasing && player != null)
        {
            if (!agent.pathPending && agent.remainingDistance < 1.0f)
            {
                agent.SetDestination(player.position);
            }
        }
    }
}
