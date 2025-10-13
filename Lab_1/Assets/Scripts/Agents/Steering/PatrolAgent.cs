using UnityEngine;
using UnityEngine.AI;

public class PatrolAgent : MonoBehaviour
{
    public Transform waypointHolder;
    private Transform[] waypoints;
    private NavMeshAgent agent;

    private int currentIndex;
    private int direction; // 1 = adelante, -1 = atrás

    public float waypointTolerance = 0.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Cargar waypoints desde el WaypointHolder
        int count = waypointHolder.childCount;
        waypoints = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            waypoints[i] = waypointHolder.GetChild(i);
        }

        if (waypoints.Length == 0) return;

        // --- Punto aleatorio inicial ---
        currentIndex = Random.Range(0, waypoints.Length);

        // Dirección aleatoria
        direction = Random.value < 0.5f ? 1 : -1;

        // Ir al waypoint inicial
        GoToCurrentWaypoint();
    }

    void Update()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= waypointTolerance || agent.destination == transform.position)
            {
                NextWaypoint();
            }
        }
    }

    void GoToCurrentWaypoint()
    {
        if (waypoints.Length == 0) return;
        agent.SetDestination(waypoints[currentIndex].position);
    }

    void NextWaypoint()
    {
        if (waypoints.Length < 2) return;

        // Cambiar índice según la dirección
        currentIndex += direction;

        // Invertir dirección si llega al final o al principio
        if (currentIndex >= waypoints.Length)
        {
            currentIndex = waypoints.Length - 2;
            direction = -1;
        }
        else if (currentIndex < 0)
        {
            currentIndex = 1;
            direction = 1;
        }

        GoToCurrentWaypoint();
    }

    void OnDrawGizmos()
    {
        if (waypointHolder == null) return;

        int count = waypointHolder.childCount;
        if (count < 2) return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < count - 1; i++)
        {
            Transform a = waypointHolder.GetChild(i);
            Transform b = waypointHolder.GetChild(i + 1);

            Gizmos.DrawSphere(a.position, 0.2f);
            Gizmos.DrawLine(a.position, b.position);
        }

        Gizmos.DrawSphere(waypointHolder.GetChild(count - 1).position, 0.2f);
    }
}
