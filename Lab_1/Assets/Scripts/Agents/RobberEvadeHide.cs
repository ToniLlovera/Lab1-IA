using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobberEvadeHide : MonoBehaviour
{
    [Header("Referencias")]
    public Transform police;

    [Tooltip("Posibles obstáculos para ocultarse (con Collider).")]
    public Transform[] hidingSpots;

    [Header("Evade")]
    [Tooltip("Distancia objetivo que intenta ganar respecto a la posición futura de la policía.")]
    public float evadeDistance = 6f;

    [Tooltip("Frecuencia (Hz) para refrescar decisiones.")]
    public float updateHz = 10f;

    [Header("Hide")]
    [Tooltip("Margen que se añade detrás del obstáculo.")]
    public float hideOffset = 2.0f;

    [Tooltip("Raycast máximo para encontrar el punto de ocultación en el collider.")]
    public float hideRayLength = 50f;

    [Header("Rotación")]
    public float turnResponsiveness = 6f;

    [Header("Línea de visión (opcional)")]
    public LayerMask losBlockers; 
    private NavMeshAgent agent;
    private float _accum;
    private Vector3 _policePrevPos;
    private bool _hasPrev;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name}: no está sobre el NavMesh.");
    }

    void Update()
    {
        if (police == null || !agent.isOnNavMesh) return;

        _accum += Time.deltaTime;
        float tick = 1f / Mathf.Max(1f, updateHz);
        if (_accum < tick) { RotateOnly(); return; }
        _accum = 0f;

        bool hasLOS = HasLineOfSight(police.position, transform.position);
        if (hasLOS && TryGetBestHidingSpot(out Vector3 hidePos))
        {
            agent.SetDestination(hidePos);
        }
        else
        {
            Evade();
        }

        RotateOnly();
    }

    void RotateOnly()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            var trot = Quaternion.LookRotation(agent.velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, trot, Time.deltaTime * turnResponsiveness);
        }
    }

    bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return true;

     
        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, dist, losBlockers, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    void Evade()
    {
        Vector3 policeVel = Vector3.zero;
        var rb = police.GetComponent<Rigidbody>();
        float tick = 1f / Mathf.Max(1f, updateHz);

        if (rb)
        {
            policeVel = rb.linearVelocity;
        }
        else
        {
         
            if (_hasPrev)
                policeVel = (police.position - _policePrevPos) / tick;
            _policePrevPos = police.position;
            _hasPrev = true;
        }

        Vector3 toPolice = police.position - transform.position;
        float distance = toPolice.magnitude;
        float denom = agent.speed + Mathf.Max(0.1f, policeVel.magnitude);
        float t = distance / denom;

        Vector3 futurePolice = police.position + policeVel * t;
        Vector3 evadeDir = (transform.position - futurePolice).normalized;
        Vector3 dest = transform.position + evadeDir * evadeDistance;

        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(dest);
    }

    bool TryGetBestHidingSpot(out Vector3 result)
    {
        result = Vector3.zero;
        float best = Mathf.Infinity;
        Collider bestCol = null;

        if (hidingSpots == null || hidingSpots.Length == 0) return false;

        foreach (var t in hidingSpots)
        {
            if (!t) continue;
            var col = t.GetComponent<Collider>();
            if (!col) continue;

        
            Vector3 dir = (t.position - police.position).normalized;

      
            Ray backRay = new Ray(t.position, -dir);
            if (col.Raycast(backRay, out RaycastHit info, hideRayLength))
            {
                Vector3 hidePos = info.point + dir * hideOffset;

            
                float d = Vector3.Distance(transform.position, hidePos);
                if (d < best)
                {
                    best = d;
                    bestCol = col;
                    result = hidePos;
                }
            }
        }

        return bestCol != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (hidingSpots != null)
            foreach (var t in hidingSpots)
                if (t) Gizmos.DrawWireCube(t.position, Vector3.one * 0.6f);
    }
}