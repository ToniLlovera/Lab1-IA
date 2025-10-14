using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Flock : MonoBehaviour
{
    [Header("Manager")]
    public FlockManager miManager;
    private NavMeshAgent agent;

    [Header("Pesos de reglas (tuneables)")]
    public float wSeparacion = 1.2f;
    public float wAlineacion = 1.0f;
    public float wCohesion = 1.0f;

    [Header("Lider")]
    public float wLiderAtraccion = 1.2f;
    public float wLiderAlineacion = 0.6f;
    public float radioLider = 6f;

    [Header("Parametros")]
    public float distanciaSeparacion = 1.0f;
    public float velocidadMinDeseada = 2.0f;
    public float velocidadMaxDeseada = 5.0f;

    [Header("Wander")]
    public float wanderCooldown = 1.0f;
    private float wanderTimer = 0f;
    private Vector3 wanderTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Velocidad inicial tomada del manager
        float v = Random.Range(miManager.velocidadMin, miManager.velocidadMax);
        agent.speed = Mathf.Clamp(v, velocidadMinDeseada, velocidadMaxDeseada);

        // Garantiza estar sobre NavMesh
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                Debug.LogWarning($"{name} no está sobre el NavMesh.");
        }

        wanderTarget = transform.position;
    }

    void Update()
    {
        AplicarReglas();
        if (wanderTimer > 0f) wanderTimer -= Time.deltaTime;
    }

    void AplicarReglas()
    {
        GameObject[] todos = miManager.todosLosBoids;

        Vector3 separacion = Vector3.zero;
        Vector3 alineacion = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        int vecinos = 0;

        foreach (GameObject obj in todos)
        {
            if (obj == this.gameObject) continue;
            if (miManager.lider != null && obj.transform == miManager.lider) continue;

            float d = Vector3.Distance(obj.transform.position, transform.position);
            if (d <= miManager.distanciaVecino)
            {
                vecinos++;
                cohesion += obj.transform.position;

                if (d < distanciaSeparacion)
                    separacion += (transform.position - obj.transform.position) / Mathf.Max(d, 0.001f);

                if (obj.TryGetComponent<NavMeshAgent>(out var otherAgent))
                {
                    Vector3 v = otherAgent.velocity;
                    if (v.sqrMagnitude > 0.0001f) alineacion += v.normalized;
                }
                else
                {
                    alineacion += obj.transform.forward;
                }
            }
        }

        Vector3 direccionDeseada = Vector3.zero;

        if (vecinos > 0)
        {
            cohesion /= vecinos;
            Vector3 dirCohesion = (cohesion - transform.position).normalized;

            if (separacion.sqrMagnitude > 0.0001f) separacion.Normalize();
            if (alineacion.sqrMagnitude > 0.0001f) alineacion.Normalize();

            direccionDeseada += dirCohesion * wCohesion;
            direccionDeseada += separacion * wSeparacion;
            direccionDeseada += alineacion * wAlineacion;
        }
        else
        {
            // Wander fluido
            if (wanderTimer <= 0f || agent.remainingDistance < 0.3f || !agent.hasPath)
            {
                Vector3 candidato = transform.position + Random.insideUnitSphere * 5f;
                if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    if ((hit.position - transform.position).magnitude < 1f)
                        candidato = transform.position + transform.forward * 3f;

                    wanderTarget = hit.position;
                }
                wanderTimer = wanderCooldown;
            }
            direccionDeseada += (wanderTarget - transform.position).normalized;
        }

        // Influencia del líder
        if (miManager.lider != null)
        {
            Vector3 haciaLider = miManager.lider.position - transform.position;
            float distLider = haciaLider.magnitude;

            if (distLider > 0.001f)
            {
                Vector3 dirLider = haciaLider / distLider;
                float factor = (distLider > radioLider) ? Mathf.InverseLerp(radioLider, radioLider * 3f, distLider) : 0.2f;
                direccionDeseada += dirLider * wLiderAtraccion * Mathf.Clamp01(factor);

                if (miManager.lider.TryGetComponent<NavMeshAgent>(out var leaderAgent))
                {
                    Vector3 vLider = leaderAgent.velocity;
                    if (vLider.sqrMagnitude > 0.0001f)
                        direccionDeseada += vLider.normalized * wLiderAlineacion;
                }
                else
                {
                    direccionDeseada += miManager.lider.forward * (wLiderAlineacion * 0.5f);
                }
            }
        }

        // Normalizar y calcular destino dinámico
        if (direccionDeseada.sqrMagnitude > 0.0001f)
        {
            direccionDeseada.Normalize();

            // Paso dinámico fluido basado en velocidad y distancia al líder
            float pasoBase = Mathf.Clamp(agent.speed * Random.Range(1.2f, 2.5f), 3f, 12f);
            float factorDistLider = 1f;
            if (miManager.lider != null)
            {
                factorDistLider = Mathf.Clamp(Vector3.Distance(transform.position, miManager.lider.position) / 5f, 1f, 5f);
            }

            Vector3 destino = transform.position + direccionDeseada * pasoBase * factorDistLider;

            // Ajuste con NavMesh
            if (agent.isOnNavMesh && NavMesh.SamplePosition(destino, out NavMeshHit hit2, 12f, NavMesh.AllAreas))
            {
                bool debeActualizar =
                    !agent.hasPath ||
                    agent.remainingDistance < 0.2f ||
                    (agent.destination - hit2.position).sqrMagnitude > 0.25f;

                if (debeActualizar)
                {
                    agent.speed = Mathf.Clamp(agent.speed, velocidadMinDeseada, velocidadMaxDeseada);
                    agent.SetDestination(hit2.position);
                }
            }
        }
    }
}
