using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Enumeración para definir los estados del enemigo
    public enum EstadoEnemigo { Patrullando, Persiguiendo }
    [Header("Estado Actual")]
    [Tooltip("El estado de comportamiento actual del enemigo.")]
    public EstadoEnemigo estadoActual = EstadoEnemigo.Patrullando;

    // --- Variables de Ajuste ---
    [Header("Movimiento")]
    [Tooltip("La velocidad horizontal a la que el enemigo patrulla.")]
    public float velocidadPatrulla = 3f;
    [Tooltip("El multiplicador de velocidad extra cuando el enemigo persigue al jugador.")]
    public float multiplicadorVelocidadPersecucion = 1.5f;

    [Header("Detección y Persecución (Raycast)")]
    [Tooltip("Objeto vacío desde donde se lanza el rayo (ej: Raycast Pakko).")]
    public Transform origenRaycast;
    [Tooltip("Distancia a la que el Raycast buscará al jugador.")]
    public float longitudDeteccionRaycast = 5f;
    [Tooltip("Capas que el Raycast debe considerar (debe incluir el jugador).")]
    public LayerMask capaObjetivo; // targetLayer
    [Tooltip("Distancia mínima para detenerse si el jugador está demasiado cerca (para evitar temblores).")]
    public float distanciaMinimaPersecucion = 0.5f;

    [Header("Comportamiento de Patrulla")]
    [Tooltip("Probabilidad de que el enemigo cambie de dirección al chocar con un obstáculo o el jugador (mientras patrulla).")]
    [Range(0f, 1f)] public float probabilidadCambioDireccionColision = 1f;

    [Header("Detección de Suelo")]
    [Tooltip("Punto (Transform hijo) para chequear si el enemigo toca el suelo.")]
    public Transform chequeoSuelo; // groundCheck
    [Tooltip("Radio del círculo de detección de suelo.")]
    public float radioChequeoSuelo = 0.2f;
    [Tooltip("Capas consideradas como 'suelo'.")]
    public LayerMask capaSuelo; // groundLayer

    // --- Componentes ---
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform objetivoJugador; // playerTarget
    private bool jugadorEnRangoAtaque = false; // playerInAttackRange (Jugador en el Circle Collider Trigger)

    // --- Estado Interno ---
    private float direccionMovimiento = 1f; // 1f para derecha, -1f para izquierda
    private bool estaEnSuelo; // isGrounded

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb == null)
        {
            Debug.LogError("Se requiere un Rigidbody2D en este enemigo.");
            enabled = false;
            return;
        }

        if (origenRaycast == null) // Comprobación del origen del rayo
        {
            Debug.LogError("Se requiere asignar el Transform del origen del raycast (ej: Raycast Pakko).");
            enabled = false;
            return;
        }

        if (chequeoSuelo == null) // Comprobación del Ground Check
        {
            Debug.LogWarning("Se requiere un Chequeo de Suelo (Transform) para la detección, aunque no se salte.");
        }

        // 1. Encontrar al Jugador por Etiqueta (Player)
        GameObject objetoJugador = GameObject.FindGameObjectWithTag("Player");
        if (objetoJugador != null)
        {
            objetivoJugador = objetoJugador.transform;
        }

        // Inicializar dirección aleatoria
        if (Random.value < 0.5f)
        {
            direccionMovimiento = -1f;
        }
    }

    void FixedUpdate()
    {
        // 0. CHEQUEO DE SUELO
        if (chequeoSuelo != null)
        {
            ChequearSuelo();
        }

        // 1. CHEQUEO DE DETECCIÓN
        if (objetivoJugador != null)
        {
            ChequearJugador();
        }

        // 2. LÓGICA BASADA EN ESTADO
        if (estadoActual == EstadoEnemigo.Patrullando)
        {
            MovimientoPatrulla();
        }
        else if (estadoActual == EstadoEnemigo.Persiguiendo)
        {
            MovimientoPersecucion();
        }

        // 3. ACTUALIZAR VISUALES
        // Voltear el sprite según la dirección actual
        if (sr != null)
        {
            if (Mathf.Abs(direccionMovimiento) > 0.1f)
            {
                sr.flipX = direccionMovimiento < 0;
            }
        }
    }

    // -------------------------------------------------------------------
    // --- LÓGICA DE ESTADOS Y MOVIMIENTO ---
    // -------------------------------------------------------------------

    void ChequearSuelo()
    {
        estaEnSuelo = Physics2D.OverlapCircle(chequeoSuelo.position, radioChequeoSuelo, capaSuelo);
    }

    void ChequearJugador()
    {
        // La dirección del rayo se basa en la orientación actual del sprite
        Vector2 direccionRaycast = (sr.flipX) ? Vector2.left : Vector2.right;

        // Lanzar Raycast desde el origen específico
        RaycastHit2D golpe = Physics2D.Raycast(origenRaycast.position, direccionRaycast, longitudDeteccionRaycast, capaObjetivo);

        bool raycastGolpeaJugador = golpe.collider != null && golpe.collider.CompareTag("Player");

        // Transición de estado: Patrullando -> Persiguiendo
        if (raycastGolpeaJugador || jugadorEnRangoAtaque)
        {
            estadoActual = EstadoEnemigo.Persiguiendo;
        }
        // Transición de estado: Persiguiendo -> Patrullando
        else if (estadoActual == EstadoEnemigo.Persiguiendo && !raycastGolpeaJugador && !jugadorEnRangoAtaque)
        {
            estadoActual = EstadoEnemigo.Patrullando;
            direccionMovimiento = (Random.value < 0.5f) ? 1f : -1f;
        }
    }

    void MovimientoPatrulla()
    {
        // Mantiene la velocidad de patrulla
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidadPatrulla, rb.linearVelocity.y);
    }

    void MovimientoPersecucion()
    {
        // Determinar la dirección hacia el jugador
        float targetX = objetivoJugador.position.x;
        float currentX = transform.position.x;

        // 1. Lógica de MOVIMIENTO HORIZONTAL
        if (Mathf.Abs(targetX - currentX) < distanciaMinimaPersecucion)
        {
            direccionMovimiento = 0f; // Detener movimiento horizontal
        }
        else
        {
            // Establecer la dirección a 1 o -1 según la posición del jugador
            direccionMovimiento = (targetX > currentX) ? 1f : -1f;
        }

        // Aplicar la velocidad de persecución
        float velocidadActual = velocidadPatrulla * multiplicadorVelocidadPersecucion;
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidadActual, rb.linearVelocity.y);
    }

    // -------------------------------------------------------------------
    // --- LÓGICA DE TRIGGERS (Circle Collider 2D) ---
    // -------------------------------------------------------------------

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRangoAtaque = true;
            if (estadoActual == EstadoEnemigo.Patrullando)
            {
                estadoActual = EstadoEnemigo.Persiguiendo;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRangoAtaque = false;
        }
    }

    // -------------------------------------------------------------------
    // --- LÓGICA DE COLISIÓN (Solo Patrulla) ---
    // -------------------------------------------------------------------

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Solo aplica la lógica si está patrullando.
        if (estadoActual == EstadoEnemigo.Patrullando)
        {
            // 1. Verificar si la colisión es con el Jugador
            if (collision.gameObject.CompareTag("Player"))
            {
                // MOSTRAR MENSAJE: Si choca con el jugador
                Debug.Log("Enemigo colisionó con: " + collision.gameObject.name);

                // Cambia de dirección (si se cumple la probabilidad)
                if (Random.value < probabilidadCambioDireccionColision)
                {
                    CambiarDireccion();
                }
            }
            // 2. Si la colisión es con Cualquier otra cosa (muro, suelo, etc.)
            else
            {
                // Cambia de dirección (si se cumple la probabilidad)
                if (Random.value < probabilidadCambioDireccionColision)
                {
                    CambiarDireccion();
                }
            }
        }
    }

    // -------------------------------------------------------------------
    // --- Funciones Auxiliares ---
    // -------------------------------------------------------------------

    void CambiarDireccion()
    {
        direccionMovimiento *= -1;
    }

    // Para ver los rangos de detección en el editor
    private void OnDrawGizmosSelected()
    {
        // Dibujar el Raycast de Detección (desde el OrigenRaycast)
        if (origenRaycast != null && sr != null)
        {
            Gizmos.color = Color.red;
            Vector2 direccionRaycast = (sr.flipX) ? Vector2.left : Vector2.right;
            Gizmos.DrawRay(origenRaycast.position, direccionRaycast * longitudDeteccionRaycast);

        }

        // Dibujar el Chequeo de Suelo
        if (chequeoSuelo != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(chequeoSuelo.position, radioChequeoSuelo);
        }
    }
}