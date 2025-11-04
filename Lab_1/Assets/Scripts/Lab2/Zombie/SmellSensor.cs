using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SmellSensor : MonoBehaviour
{
    [Tooltip("Radio de detección del olor (ajustar en el SphereCollider).")]
    public float detectionRadius = 8f;

    void Start()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = detectionRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SmellSource"))
        {
            // Notificar al padre
            transform.root.SendMessage(
                "OnTriggerEnter",
                other,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }
}
