using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PatrolAgent : MonoBehaviour
{
    [Header("Contenedor de Waypoints")]
    public Transform waypointHolder;

    [Header("Movimiento")]
    public float waypointTolerance = 0.6f;

    [Header("Smoothing / Ghost-like")]
    [Tooltip("Suavizado de orientación hacia un punto adelantado del path.")]
    public float lookaheadTurnSpeed = 6f;

    private NavMeshAgent agent;
    private Transform[] waypoints;
    private int currentIndex;
    private int direction; // 1 = hacia adelante, -1 = hacia atrás

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // ⚙️ Configuración básica del NavMeshAgent
        agent.autoBraking = false;
        agent.stoppingDistance = waypointTolerance;
        agent.acceleration = Mathf.Max(agent.acceleration, 12f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 600f);

        // ✅ Obtener todos los waypoints desde el contenedor
        if (waypointHolder != null)
        {
            waypoints = new Transform[waypointHolder.childCount];
            for (int i = 0; i < waypointHolder.childCount; i++)
                waypoints[i] = waypointHolder.GetChild(i);
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"{name}: no hay waypoints en el WaypointHolder.");
            enabled = false;
            return;
        }

        // 🎲 Punto inicial y dirección aleatorios
        currentIndex = Random.Range(0, waypoints.Length);
        direction = Random.value < 0.5f ? 1 : -1;

        SetDestinationToCurrent();
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;

        // 🧭 Cambiar de waypoint al llegar
        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
            NextWaypoint();

        // 👻 "Ghost-like" smoothing
        var corners = agent.path.corners;
        if (corners != null && corners.Length >= 2)
        {
            Vector3 lookPt = corners[Mathf.Min(2, corners.Length - 1)];
            Vector3 dir = (lookPt - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                var targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookaheadTurnSpeed);
            }
        }
    }

    void NextWaypoint()
    {
        int nextIndex = currentIndex + direction;

        // 🔁 Invertir dirección en los extremos
        if (nextIndex >= waypoints.Length)
        {
            direction = -1;
            nextIndex = waypoints.Length - 2;
        }
        else if (nextIndex < 0)
        {
            direction = 1;
            nextIndex = 1;
        }

        currentIndex = nextIndex;
        SetDestinationToCurrent();
    }

    void SetDestinationToCurrent()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (currentIndex < 0 || currentIndex >= waypoints.Length) return;
        if (!agent.isOnNavMesh) return;

        Transform target = waypoints[currentIndex];
        if (target != null)
            agent.SetDestination(target.position);
    }

    void OnDrawGizmosSelected()
    {
        if (waypointHolder == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypointHolder.childCount; i++)
        {
            Transform wp = waypointHolder.GetChild(i);
            if (wp == null) continue;

            Gizmos.DrawSphere(wp.position, 0.2f);

            if (i + 1 < waypointHolder.childCount)
            {
                Transform next = waypointHolder.GetChild(i + 1);
                if (next != null)
                    Gizmos.DrawLine(wp.position, next.position);
            }
        }
    }
}
