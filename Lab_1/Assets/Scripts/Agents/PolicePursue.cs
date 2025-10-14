using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PolicePursue : MonoBehaviour
{
    [Header("Objetivos")]
    public Transform robber;

    [Header("Predicción")]
    [Tooltip("Máximo tiempo de predicción.")]
    public float predictionTime = 2.0f;

    [Tooltip("Frecuencia de actualización de destino (Hz).")]
    public float updateHz = 10f;

    [Header("Rotación Suave")]
    public float turnResponsiveness = 5f;

    private NavMeshAgent agent;
    private float _accum;
    private Vector3 _robberPrevPos;
    private bool _hasPrev;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name}: no está sobre el NavMesh.");
    }

    void Update()
    {
        if (robber == null || !agent.isOnNavMesh) return;

        _accum += Time.deltaTime;
        float tick = 1f / Mathf.Max(1f, updateHz);
        if (_accum < tick) goto RotateOnly; 
        _accum = 0f;

        
        Vector3 targetVelocity = Vector3.zero;
        var rb = robber.GetComponent<Rigidbody>();
        if (rb)
        {
            targetVelocity = rb.linearVelocity;
        }
        else
        {
            if (_hasPrev)
                targetVelocity = (robber.position - _robberPrevPos) / tick;
            _robberPrevPos = robber.position;
            _hasPrev = true;
        }

        
        Vector3 toTarget = robber.position - transform.position;
        float distance = toTarget.magnitude;
        float denom = agent.speed + Mathf.Max(0.1f, targetVelocity.magnitude);
        float t = Mathf.Clamp(distance / denom, 0f, predictionTime);

        Vector3 futurePos = robber.position + targetVelocity * t;
        agent.SetDestination(futurePos);

    RotateOnly:
        
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            var trot = Quaternion.LookRotation(agent.velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, trot, Time.deltaTime * turnResponsiveness);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!robber) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, robber.position);
    }
}