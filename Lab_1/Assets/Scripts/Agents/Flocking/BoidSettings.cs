using UnityEngine;

[CreateAssetMenu(menuName = "Flocking/Boid Settings")]
public class BoidSettings : ScriptableObject
{
    [Header("Movimiento")]
    [Tooltip("Velocidad máxima de los boids (unidades por segundo).")]
    public float maxSpeed = 7.5f;

    [Tooltip("Fuerza máxima de steering (aceleración). Limita giros bruscos.")]
    public float maxForce = 10f;


    [Header("Percepción")]
    [Tooltip("Radio para detectar vecinos (también se usa como tamaño de celda del grid).")]
    public float perceptionRadius = 2.5f;

    [Tooltip("Distancia a la que la separación pasa a ser dominante.")]
    public float separationRadius = 1.0f;

    [Header("Pesos de comportamiento")]
    [Tooltip("Peso de la fuerza de separación (evitar colisiones).")]
    public float separationWeight = 1.5f;

    [Tooltip("Peso de la fuerza de alineamiento (seguir la dirección promedio).")]
    public float alignmentWeight = 1.0f;

    [Tooltip("Peso de la fuerza de cohesión (moverse hacia el grupo).")]
    public float cohesionWeight = 1.0f;

    [Tooltip("Peso del steering hacia el objetivo global (si existe).")]
    public float targetWeight = 0.75f;

    [Tooltip("Peso de la evitación de obstáculos (raycasts).")]
    public float obstacleAvoidWeight = 2.0f;


    [Header("Prioridades")]
    [Tooltip("Si la magnitud de la fuerza de separación supera este umbral, ignora otras fuerzas en ese tick.")]
    public float separationPriorityThreshold = 0.4f;

    [Header("Arrive (target opcional)")]
    [Tooltip("Radio a partir del cual el boid empieza a frenar hacia el objetivo.")]
    public float arriveSlowRadius = 5f;

    [Tooltip("Radio dentro del cual el boid prácticamente se detiene (ya llegó).")]
    public float arriveStopRadius = 1.2f;


    [Header("Rotación")]
    [Tooltip("Factor de respuesta de giro (usado en el Slerp de orientación).")]
    public float turnResponsiveness = 6f;

    [Header("Variación / Ruido")]
    [Tooltip("Pequeño ruido direccional para romper sincronías y hacerlo más orgánico.")]
    public float jitterStrength = 0.15f;


    [Header("Evitación de obstáculos")]
    [Tooltip("Distancia máxima del raycast de evitación.")]
    public float avoidRayLength = 3.0f;

    [Tooltip("Capa de los objetos que los boids deben evitar.")]
    public LayerMask obstacleMask;


    [Header("Área de simulación")]
    [Tooltip("Tamaño de la zona en la que los boids pueden moverse.")]
    public Vector3 boundsSize = new Vector3(30, 30, 30);

    [Tooltip("Si está activo, el espacio es 'toroidal' (al salir por un lado, entra por el opuesto).")]
    public bool wrapBounds = true;

    [Header("Visual")]
    [Tooltip("Escala de los gizmos al mostrar los boids en escena.")]
    public float gizmoBoidScale = 0.15f;


    [Header("Leader Boid ")]
    [Tooltip("Activa o desactiva la creación automática de un líder.")]
    public bool spawnLeader = true;

    [Tooltip("Multiplicador de velocidad para el líder (suele ser un poco más rápido).")]
    public float leaderSpeedMultiplier = 1.2f;

    [Tooltip("Multiplicador de fuerza para el líder (puede girar más fuerte).")]
    public float leaderForceMultiplier = 1.2f;

    [Tooltip("Peso con el que los seguidores se sienten atraídos hacia el líder.")]
    public float leaderAttractWeight = 1.0f;

    [Tooltip("Distancia preferida al líder para mantener separación y evitar amontonamiento.")]
    public float leaderPreferredDistance = 2.5f;


    [Header("Leader Wander (si no hay target global)")]
    [Tooltip("Si no existe un objetivo global, el líder deambula aleatoriamente.")]
    public bool leaderWander = true;

    [Tooltip("Radio del círculo de wander delante del líder.")]
    public float wanderRadius = 2.0f;

    [Tooltip("Distancia del círculo de wander desde la posición del líder.")]
    public float wanderDistance = 3.0f;

    [Tooltip("Intensidad de la desviación aleatoria en wander.")]
    public float wanderJitter = 0.8f;
}