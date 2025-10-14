using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.autoBraking = false;   
        agent.acceleration = 100f;  
        agent.angularSpeed = 999f;   
    }

    void Update()
    {
        // Detecta clicks del mouse
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.isStopped = false;       
                agent.SetDestination(hit.point);
            }
        }
    }
}
