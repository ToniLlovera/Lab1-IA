using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Boid : MonoBehaviour
{
    public FlockManager manager;
    private NavMeshAgent agent;
    private List<Boid> neighbors = new List<Boid>();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = Random.Range(manager.minSpeed, manager.maxSpeed);
    }

    void Update()
    {
        if (manager == null || !agent.isOnNavMesh) return;

        manager.GetNeighbors(this, neighbors);

        Vector3 separation = ComputeSeparation() * manager.separationWeight;
        Vector3 alignment = ComputeAlignment() * manager.alignmentWeight;
        Vector3 cohesion = ComputeCohesion() * manager.cohesionWeight;
        Vector3 leaderForce = ComputeLeaderForce() * manager.leaderWeight;

        Vector3 moveTarget = transform.position + separation + alignment + cohesion + leaderForce;

        agent.SetDestination(moveTarget);
    }


    Vector3 ComputeSeparation()
    {
        Vector3 force = Vector3.zero;
        int count = 0;
        foreach (var n in neighbors)
        {
            Vector3 diff = transform.position - n.transform.position;
            float d = diff.magnitude;
            if (d > 0f)
            {
                force += diff.normalized / d;
                count++;
            }
        }
        if (count > 0) force /= count;
        return force;
    }

    Vector3 ComputeAlignment()
    {
        if (neighbors.Count == 0) return Vector3.zero;
        Vector3 avg = Vector3.zero;
        foreach (var n in neighbors) avg += n.agent.velocity;
        avg /= neighbors.Count;
        return avg.normalized;
    }

    Vector3 ComputeCohesion()
    {
        if (neighbors.Count == 0) return Vector3.zero;
        Vector3 avgPos = Vector3.zero;
        foreach (var n in neighbors) avgPos += n.transform.position;
        avgPos /= neighbors.Count;
        return avgPos - transform.position;
    }

    Vector3 ComputeLeaderForce()
    {
        if (manager.leader == null) return Vector3.zero;
        Vector3 toLeader = manager.leader.position - transform.position;
        float dist = toLeader.magnitude;
        if (dist > manager.leaderInfluenceRadius) return Vector3.zero;
        float strength = Mathf.Clamp01((manager.leaderInfluenceRadius - dist) / manager.leaderInfluenceRadius);
        return toLeader.normalized * strength;
    }
}
