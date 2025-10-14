using UnityEngine;
using UnityEngine.AI;

public class LeaderBoid : MonoBehaviour
{
    public Transform[] waypoints;
    private int current = 0;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[current].position);
    }

    void Update()
    {
        if (waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            current = (current + 1) % waypoints.Length;
            agent.SetDestination(waypoints[current].position);
        }
    }
}
