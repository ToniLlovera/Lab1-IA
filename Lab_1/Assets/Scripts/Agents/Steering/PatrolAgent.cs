using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PatrolAgent : MonoBehaviour
{
    [Header("Waypoints (en orden)")]
    public Transform[] waypoints;

    [Header("Movimiento")]
    public float waypointTolerance = 0.6f;

    [Header("Smoothing / Ghost-like")]
    [Tooltip("Suavizado de orientación hacia un punto adelantado del path.")]
    public float lookaheadTurnSpeed = 6f;

    private NavMeshAgent agent;
    private int currentIndex;
    private int direction; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

      
        agent.autoBraking = false;
        agent.stoppingDistance = waypointTolerance;
        agent.acceleration = Mathf.Max(agent.acceleration, 12f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 600f);

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"{name}: no hay waypoints asignados.");
            enabled = false;
            return;
        }

        currentIndex = Random.Range(0, waypoints.Length);
        direction = Random.value < 0.5f ? 1 : -1;

        SetDestinationToCurrent();
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;

       
        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
            NextWaypoint();

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
        currentIndex += direction;

        if (currentIndex >= waypoints.Length) { currentIndex = waypoints.Length - 2; direction = -1; }
        else if (currentIndex < 0) { currentIndex = 1; direction = +1; }

        SetDestinationToCurrent();
    }

    void SetDestinationToCurrent()
    {
        if (waypoints[currentIndex] != null)
            agent.SetDestination(waypoints[currentIndex].position);
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}