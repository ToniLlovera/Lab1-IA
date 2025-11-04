using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BloodMarker : MonoBehaviour
{
    [Tooltip("Duración antes de autodestruir el marcador.")]
    public float lifeTime = 4f;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Destroy(gameObject, lifeTime);
    }
}
