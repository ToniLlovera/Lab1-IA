using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    [Header("Referencias")]
    public BoidSettings settings;
    public Boid boidPrefab;

    [Header("Spawn")]
    public int boidCount = 200;
    public Vector3 spawnCenter;
    public Vector3 spawnSize = new Vector3(10, 10, 10);

    [Header("Objetivo (opcional)")]
    public Transform target;

    [Header("Optimización (throttling/steering)")]
    [Tooltip("Cada cuánto recalcula steerings por boid (segundos aprox.).")]
    public float steeringInterval = 0.1f; // ~10 Hz
    [Tooltip("Desincroniza ligeramente para evitar picos y patrones.")]
    public float steeringJitter = 0.02f;


    private readonly List<Boid> boids = new List<Boid>();
    private Dictionary<Vector3Int, List<int>> grid = new Dictionary<Vector3Int, List<int>>();
    private float cellSize;

    void Start()
    {
        if (settings == null || boidPrefab == null)
        {
            Debug.LogError("Asigna BoidSettings y Boid prefab en el inspector.");
            enabled = false;
            return;
        }

        cellSize = Mathf.Max(0.1f, settings.perceptionRadius);

 
        var centerWS = transform.TransformPoint(spawnCenter);
        for (int i = 0; i < boidCount; i++)
        {
            Vector3 pos = centerWS + new Vector3(
                Random.Range(-spawnSize.x * 0.5f, spawnSize.x * 0.5f),
                Random.Range(-spawnSize.y * 0.5f, spawnSize.y * 0.5f),
                Random.Range(-spawnSize.z * 0.5f, spawnSize.z * 0.5f)
            );

            var b = Instantiate(boidPrefab, pos, Quaternion.identity, transform);
            Vector3 vel = Random.onUnitSphere * (settings.maxSpeed * 0.5f);
            float initialNextSteer = Time.time + Random.Range(0f, steeringInterval);
            b.Initialize(vel, initialNextSteer);
            boids.Add(b);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        RebuildGrid();
        Simulate(dt);
    }

    void Simulate(float dt)
    {
        var half = settings.boundsSize * 0.5f;

        for (int i = 0; i < boids.Count; i++)
        {
            Boid b = boids[i];
            Vector3 pos = b.transform.position;

        
            if (Time.time >= b.nextSteerTime)
            {
                b.nextSteerTime = Time.time + steeringInterval + Random.Range(0f, steeringJitter);
                b.cachedSteering = ComputeSteering(i, b);
            }

    
            b.AddForce(b.cachedSteering);

     
            Vector3 localPos = b.transform.position - transform.position;
            if (settings.wrapBounds)
            {
 
                if (localPos.x > half.x) localPos.x = -half.x;
                else if (localPos.x < -half.x) localPos.x = half.x;
                if (localPos.y > half.y) localPos.y = -half.y;
                else if (localPos.y < -half.y) localPos.y = half.y;
                if (localPos.z > half.z) localPos.z = -half.z;
                else if (localPos.z < -half.z) localPos.z = half.z;

                b.transform.position = transform.position + localPos;
            }
            else
            {

                Vector3 desired = Vector3.zero;
                if (localPos.x > half.x) desired.x = -settings.maxSpeed;
                else if (localPos.x < -half.x) desired.x = settings.maxSpeed;
                if (localPos.y > half.y) desired.y = -settings.maxSpeed;
                else if (localPos.y < -half.y) desired.y = settings.maxSpeed;
                if (localPos.z > half.z) desired.z = -settings.maxSpeed;
                else if (localPos.z < -half.z) desired.z = settings.maxSpeed;

                if (desired != Vector3.zero)
                {
                    Vector3 steer = SteerTowards(desired, b.velocity);
                    b.AddForce(steer);
                }
            }

            b.Integrate(dt, settings);
        }
    }

  
    Vector3 ComputeSteering(int index, Boid b)
    {
        Vector3 pos = b.transform.position;


        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        float perceptionR2 = settings.perceptionRadius * settings.perceptionRadius;
        float separationR2 = settings.separationRadius * settings.separationRadius;

        foreach (int j in GetNeighbors(index, pos))
        {
            if (j == index) continue;
            Boid other = boids[j];
            Vector3 to = other.transform.position - pos;
            float d2 = to.sqrMagnitude;
            if (d2 > perceptionR2 || d2 <= 0f) continue;

            neighborCount++;
            alignment += other.velocity;
            cohesion += other.transform.position;

            if (d2 < separationR2)
            {

                separation -= to / Mathf.Max(0.0001f, d2);
            }
        }

        Vector3 steering = Vector3.zero;

        if (neighborCount > 0)
        {
            alignment /= neighborCount;
            cohesion = (cohesion / neighborCount) - pos;


            Vector3 steerAlign = alignment.sqrMagnitude > 1e-6f
                ? SteerTowards(alignment.normalized * settings.maxSpeed, b.velocity) * settings.alignmentWeight
                : Vector3.zero;

            Vector3 steerCoh = cohesion.sqrMagnitude > 1e-6f
                ? SteerTowards(cohesion.normalized * settings.maxSpeed, b.velocity) * settings.cohesionWeight
                : Vector3.zero;

            Vector3 steerSep = separation.sqrMagnitude > 1e-6f
                ? SteerTowards(separation.normalized * settings.maxSpeed, b.velocity) * settings.separationWeight
                : Vector3.zero;


            float sepMag = steerSep.magnitude;
            if (sepMag > settings.separationPriorityThreshold)
            {
                steering += steerSep;

                steering += ComputeObstacleAvoidance(b);
                steering += ComputeTargetArrive(b);

                if (settings.jitterStrength > 0f)
                    steering += Random.insideUnitSphere * settings.jitterStrength;
                return steering;
            }

 
            steering += steerSep + steerAlign + steerCoh;
        }

        steering += ComputeTargetArrive(b);

        steering += ComputeObstacleAvoidance(b);

        if (settings.jitterStrength > 0f)
        {
            steering += Random.insideUnitSphere * settings.jitterStrength;
        }

        return steering;
    }

    Vector3 ComputeTargetArrive(Boid b)
    {
        if (target == null) return Vector3.zero;

        Vector3 toTarget = target.position - b.transform.position;
        float d = toTarget.magnitude;

        if (d <= settings.arriveStopRadius) return Vector3.zero;

        float desiredSpeed = settings.maxSpeed * Mathf.Clamp01(d / settings.arriveSlowRadius);
        Vector3 desiredVel = (d > 1e-4f ? toTarget / d : Vector3.zero) * desiredSpeed;

        Vector3 steer = SteerTowards(desiredVel, b.velocity);
        return steer * settings.targetWeight;
    }

    Vector3 ComputeObstacleAvoidance(Boid b)
    {
        float L = settings.avoidRayLength;
        if (L <= 0.01f) return Vector3.zero;

        Vector3 pos = b.transform.position;
        Vector3 dir = (b.velocity.sqrMagnitude > 1e-4f ? b.velocity.normalized : b.transform.forward);
        Vector3 right = Vector3.Cross(Vector3.up, dir);

        bool hitCenter = Physics.SphereCast(pos, 0.1f, dir, out RaycastHit hc, L, settings.obstacleMask);
        bool hitRight = Physics.SphereCast(pos, 0.1f, (dir + right * 0.6f).normalized, out RaycastHit hr, L * 0.8f, settings.obstacleMask);
        bool hitLeft = Physics.SphereCast(pos, 0.1f, (dir - right * 0.6f).normalized, out RaycastHit hl, L * 0.8f, settings.obstacleMask);

        if (!(hitCenter || hitLeft || hitRight))
            return Vector3.zero;

        Vector3 avoidDir;
        if (!hitRight) avoidDir = (dir + right).normalized;
        else if (!hitLeft) avoidDir = (dir - right).normalized;
        else avoidDir = Vector3.Reflect(dir, hc.normal);

        Vector3 steerAvoid = SteerTowards(avoidDir * settings.maxSpeed, b.velocity);
        return steerAvoid * settings.obstacleAvoidWeight;
    }

    Vector3 SteerTowards(Vector3 desiredVelocity, Vector3 currentVelocity)
    {
        Vector3 steer = desiredVelocity - currentVelocity;
        float maxForce = settings.maxForce;
        float maxForceSq = maxForce * maxForce;
        if (steer.sqrMagnitude > maxForceSq)
            steer = steer.normalized * maxForce;
        return steer;
    }

    void RebuildGrid()
    {
        grid.Clear();
        for (int i = 0; i < boids.Count; i++)
        {
            Vector3Int cell = WorldToCell(boids[i].transform.position);
            if (!grid.TryGetValue(cell, out var list))
            {
                list = new List<int>(8);
                grid[cell] = list;
            }
            list.Add(i);
        }
    }

    IEnumerable<int> GetNeighbors(int index, Vector3 position)
    {
        Vector3Int c = WorldToCell(position);
       
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int n = new Vector3Int(c.x + x, c.y + y, c.z + z);
                    if (grid.TryGetValue(n, out var list))
                    {
                        for (int k = 0; k < list.Count; k++)
                            yield return list[k];
                    }
                }
    }

    Vector3Int WorldToCell(Vector3 worldPos)
    {
        Vector3 p = worldPos / cellSize;
        return new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
    }


    void OnDrawGizmosSelected()
    {
        if (settings == null) return;

        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, settings.boundsSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            foreach (var b in boids)
            {
                Gizmos.DrawWireSphere(b.transform.position, settings.perceptionRadius * 0.25f);

                Vector3 dir = (b.velocity.sqrMagnitude > 1e-4f ? b.velocity.normalized : b.transform.forward);
                Vector3 f = dir * settings.gizmoBoidScale * 2f;
                Gizmos.DrawLine(b.transform.position, b.transform.position + f);
            }
        }
    }
}