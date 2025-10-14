using UnityEngine;

[CreateAssetMenu(menuName = "Flocking/Boid Settings")]
public class BoidSettings : ScriptableObject
{
    [Header("Movimiento")]
    [Tooltip("Velocidad m�xima de los boids (unidades por segundo).")]
    public float maxSpeed = 7.5f;

    [Tooltip("Fuerza m�xima de steering (aceleraci�n). Limita giros bruscos.")]
    public float maxForce = 10f;


    [Header("Percepci�n")]
    [Tooltip("Radio para detectar vecinos (tambi�n se usa como tama�o de celda del grid).")]
    public float perceptionRadius = 2.5f;

    [Tooltip("Distancia a la que la separaci�n pasa a ser dominante.")]
    public float separationRadius = 1.0f;

    [Header("Pesos de comportamiento")]
    [Tooltip("Peso de la fuerza de separaci�n (evitar colisiones).")]
    public float separationWeight = 1.5f;

    [Tooltip("Peso de la fuerza de alineamiento (seguir la direcci�n promedio).")]
    public float alignmentWeight = 1.0f;

    [Tooltip("Peso de la fuerza de cohesi�n (moverse hacia el grupo).")]
    public float cohesionWeight = 1.0f;

    [Tooltip("Peso del steering hacia el objetivo global (si existe).")]
    public float targetWeight = 0.75f;

    [Tooltip("Peso de la evitaci�n de obst�culos (raycasts).")]
    public float obstacleAvoidWeight = 2.0f;


    [Header("Prioridades")]
    [Tooltip("Si la magnitud de la fuerza de separaci�n supera este umbral, ignora otras fuerzas en ese tick.")]
    public float separationPriorityThreshold = 0.4f;

    [Header("Arrive (target opcional)")]
    [Tooltip("Radio a partir del cual el boid empieza a frenar hacia el objetivo.")]
    public float arriveSlowRadius = 5f;

    [Tooltip("Radio dentro del cual el boid pr�cticamente se detiene (ya lleg�).")]
    public float arriveStopRadius = 1.2f;


    [Header("Rotaci�n")]
    [Tooltip("Factor de respuesta de giro (usado en el Slerp de orientaci�n).")]
    public float turnResponsiveness = 6f;

    [Header("Variaci�n / Ruido")]
    [Tooltip("Peque�o ruido direccional para romper sincron�as y hacerlo m�s org�nico.")]
    public float jitterStrength = 0.15f;


    [Header("Evitaci�n de obst�culos")]
    [Tooltip("Distancia m�xima del raycast de evitaci�n.")]
    public float avoidRayLength = 3.0f;

    [Tooltip("Capa de los objetos que los boids deben evitar.")]
    public LayerMask obstacleMask;


    [Header("�rea de simulaci�n")]
    [Tooltip("Tama�o de la zona en la que los boids pueden moverse.")]
    public Vector3 boundsSize = new Vector3(30, 30, 30);

    [Tooltip("Si est� activo, el espacio es 'toroidal' (al salir por un lado, entra por el opuesto).")]
    public bool wrapBounds = true;

    [Header("Visual")]
    [Tooltip("Escala de los gizmos al mostrar los boids en escena.")]
    public float gizmoBoidScale = 0.15f;


    [Header("Leader Boid ")]
    [Tooltip("Activa o desactiva la creaci�n autom�tica de un l�der.")]
    public bool spawnLeader = true;

    [Tooltip("Multiplicador de velocidad para el l�der (suele ser un poco m�s r�pido).")]
    public float leaderSpeedMultiplier = 1.2f;

    [Tooltip("Multiplicador de fuerza para el l�der (puede girar m�s fuerte).")]
    public float leaderForceMultiplier = 1.2f;

    [Tooltip("Peso con el que los seguidores se sienten atra�dos hacia el l�der.")]
    public float leaderAttractWeight = 1.0f;

    [Tooltip("Distancia preferida al l�der para mantener separaci�n y evitar amontonamiento.")]
    public float leaderPreferredDistance = 2.5f;


    [Header("Leader Wander (si no hay target global)")]
    [Tooltip("Si no existe un objetivo global, el l�der deambula aleatoriamente.")]
    public bool leaderWander = true;

    [Tooltip("Radio del c�rculo de wander delante del l�der.")]
    public float wanderRadius = 2.0f;

    [Tooltip("Distancia del c�rculo de wander desde la posici�n del l�der.")]
    public float wanderDistance = 3.0f;

    [Tooltip("Intensidad de la desviaci�n aleatoria en wander.")]
    public float wanderJitter = 0.8f;
}