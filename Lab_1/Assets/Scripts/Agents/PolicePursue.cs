using UnityEngine;
using UnityEngine.AI;

public class PolicePursue : MonoBehaviour
{
    public Transform robber; // assigna l’objecte del lladre des de l’Inspector
    private NavMeshAgent agent;

    [Range(0.5f, 5f)]
    public float predictionTime = 1.5f; // quant anticipa el policia

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (robber == null || !agent.isOnNavMesh) return;

        // Agafem el component Rigidbody o velocitat del lladre
        Rigidbody rb = robber.GetComponent<Rigidbody>();
        Vector3 targetVelocity = rb ? rb.linearVelocity : Vector3.zero;

        // Calculem la distància
        Vector3 toTarget = robber.position - transform.position;
        float distance = toTarget.magnitude;

        // Temps de predicció segons la distància i velocitat
        float dynamicPrediction = Mathf.Clamp(distance / (agent.speed + targetVelocity.magnitude), 0f, predictionTime);

        // Posició futura estimada del lladre
        Vector3 futurePosition = robber.position + targetVelocity * dynamicPrediction;

        // Movem el policia cap allà
        agent.SetDestination(futurePosition);

        // Rotació suau cap a la direcció del moviment
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }
    }
}
