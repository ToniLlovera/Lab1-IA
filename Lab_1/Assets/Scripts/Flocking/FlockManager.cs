using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FlockManager : MonoBehaviour
{
    [Header("Prefabs & Setup")]
    public GameObject boidPrefab;
    public Transform leader; 
    public int initialBoidCount = 50;

    [Header("Flocking parameters")]
    public float neighborRadius = 3f;
    [Range(0f, 10f)] public float separationWeight = 1.5f;
    [Range(0f, 10f)] public float alignmentWeight = 1f;
    [Range(0f, 10f)] public float cohesionWeight = 1f;
    [Range(0f, 10f)] public float leaderWeight = 2f;
    public float leaderInfluenceRadius = 20f; 

    [Header("Movement limits")]
    public float minSpeed = 2f;
    public float maxSpeed = 6f;
    public float maxForce = 5f;

    [HideInInspector] public List<Boid> boids = new List<Boid>();

    [Header("Spawn area")]
    public Vector3 spawnCenter = Vector3.zero;
    public Vector3 spawnSize = new Vector3(20f, 0f, 20f);

    void Start()
    {
        for (int i = 0; i < initialBoidCount; i++)
        {
            // spawn aleatorio dentro de spawnSize
            Vector3 randomPos = spawnCenter + new Vector3(
                (Random.value - 0.5f) * spawnSize.x,
                0f, // para 2D/XZ; ajusta Y si tu NavMesh es 3D
                (Random.value - 0.5f) * spawnSize.z
            );

            // buscar posición válida en NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas))
            {
                GameObject go = Instantiate(boidPrefab, hit.position, Quaternion.identity);
                Boid b = go.GetComponent<Boid>();
                if (b == null) b = go.AddComponent<Boid>();
                b.manager = this;
                boids.Add(b);
            }
            else
            {
                Debug.LogWarning("No se pudo colocar boid en NavMesh cerca de " + randomPos);
            }
        }
    }
    public void GetNeighbors(Boid boid, List<Boid> outNeighbors)
    {
        outNeighbors.Clear();
        Vector3 pos = boid.transform.position;
        float r2 = neighborRadius * neighborRadius;
        for (int i = 0; i < boids.Count; i++)
        {
            Boid other = boids[i];
            if (other == boid) continue;
            if ((other.transform.position - pos).sqrMagnitude <= r2)
                outNeighbors.Add(other);
        }
    }

    public Vector3 Limit(Vector3 v, float max)
    {
        if (v.sqrMagnitude > max * max)
            return v.normalized * max;
        return v;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(spawnCenter, spawnSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
    }
}
