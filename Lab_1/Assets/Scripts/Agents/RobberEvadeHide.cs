using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class RobberEvadeHide : MonoBehaviour
{
    public Transform police;
    public List<Transform> hidingSpots; // Assigna objectes com murs, arbres, cotxes, etc.
    private NavMeshAgent agent;

    public float evadeDistance = 10f;
    public float hideCheckDistance = 15f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (police == null || !agent.isOnNavMesh) return;

        // Si el policia �s massa a prop, intentem amagar-nos
        if (Vector3.Distance(transform.position, police.position) < hideCheckDistance)
        {
            Transform bestSpot = FindBestHidingSpot();
            if (bestSpot != null)
            {
                // Direcci� des del policia fins a l�amagatall
                Vector3 dirToHide = (bestSpot.position - police.position).normalized;
                Vector3 hidePos = bestSpot.position + dirToHide * 2.0f; // un petit marge darrere

                agent.SetDestination(hidePos);
                return;
            }
        }

        // Si no hi ha lloc on amagar-se, fem EVASION
        Evade();
    }

    void Evade()
    {
        Rigidbody rb = police.GetComponent<Rigidbody>();
        Vector3 policeVelocity = rb ? rb.linearVelocity : Vector3.zero;

        Vector3 toPolice = police.position - transform.position;
        float distance = toPolice.magnitude;
        float predictionTime = distance / (agent.speed + policeVelocity.magnitude);

        Vector3 futurePosition = police.position + policeVelocity * predictionTime;
        Vector3 evadeDir = (transform.position - futurePosition).normalized;

        agent.SetDestination(transform.position + evadeDir * evadeDistance);
    }

    Transform FindBestHidingSpot()
    {
        Transform bestSpot = null;
        float bestDistance = Mathf.Infinity;

        foreach (Transform spot in hidingSpots)
        {
            Vector3 dirToSpot = spot.position - police.position;
            float distance = dirToSpot.magnitude;

            // Comprovem si hi ha l�nia de visi�
            if (Physics.Raycast(police.position, dirToSpot.normalized, out RaycastHit hit))
            {
                if (hit.transform == spot && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSpot = spot;
                }
            }
        }
        return bestSpot;
    }
}
