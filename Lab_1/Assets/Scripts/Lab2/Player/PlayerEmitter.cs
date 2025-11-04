using System.Collections;
using UnityEngine;

public class PlayerEmitter : MonoBehaviour
{
    [Header("Smell Emission Settings")]
    [Tooltip("Prefab del marcador de olor que deja el jugador (debe tener tag 'SmellSource').")]
    public GameObject bloodPrefab;

    [Tooltip("Intervalo mínimo entre marcadores de olor.")]
    public float minInterval = 0.5f;

    [Tooltip("Intervalo máximo entre marcadores de olor.")]
    public float maxInterval = 1f;

    [Tooltip("Distancia mínima para considerar que el jugador se ha movido.")]
    public float minMoveThreshold = 0.1f;

    private Vector3 lastPosition;
    private Coroutine emitRoutine;

    void Start()
    {
        lastPosition = transform.position;

        if (bloodPrefab == null)
            Debug.LogWarning("PlayerEmitter: bloodPrefab no asignado.");

        emitRoutine = StartCoroutine(EmitWhileMoving());
    }

    IEnumerator EmitWhileMoving()
    {
        while (true)
        {
            yield return null;

            Vector3 currentPos = transform.position;

            // Si el jugador se ha movido una distancia significativa, generar marcador
            if (Vector3.Distance(currentPos, lastPosition) > minMoveThreshold)
            {
                float waitTime = Random.Range(minInterval, maxInterval);
                SpawnBloodMarker();
                yield return new WaitForSeconds(waitTime);
            }

            lastPosition = currentPos;
        }
    }

    void SpawnBloodMarker()
    {
        if (bloodPrefab == null) return;

        GameObject marker = Instantiate(bloodPrefab, transform.position, Quaternion.identity);
        marker.tag = "SmellSource"; // asegurarse de que tiene el tag correcto
    }

    void OnDisable()
    {
        if (emitRoutine != null)
            StopCoroutine(emitRoutine);
    }
}
