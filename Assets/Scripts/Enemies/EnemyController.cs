using UnityEngine;

public class EnemyController : MonoBehaviour
{
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

    [Header("Detección y Persecución (BoxCast)")]
    [Tooltip("Objeto vacío desde donde se lanza el rayo (ej: Raycast Pakko).")]
    public Transform origenRaycast;
    [Tooltip("Distancia a la que el BoxCast buscará al jugador.")]
    public float longitudDeteccionRaycast = 35f;
    [Tooltip("Capas que el BoxCast debe considerar (debe incluir el jugador).")]
    public LayerMask capaObjetivo;
    [Tooltip("Distancia mínima para detenerse si el jugador está demasiado cerca (para evitar temblores).")]
    public float distanciaMinimaPersecucion = 0.5f;
    [Tooltip("El tamaño del área de detección usada por el BoxCast (X para ancho, Y para alto).")]
    public Vector2 tamanoBoxCast = new Vector2(0.5f, 0.5f); // <-- NUEVA VARIABLE

    [Header("Comportamiento de Patrulla")]
    [Tooltip("Probabilidad de que el enemigo cambie de dirección al chocar con un obstáculo o el jugador (mientras patrulla).")]
    [Range(0f, 1f)] public float probabilidadCambioDireccionColision = 1f;

    [Header("Detección de Suelo")]
    [Tooltip("Punto (Transform hijo) para chequear si el enemigo toca el suelo.")]
    public Transform chequeoSuelo;
    [Tooltip("Radio del círculo de detección de suelo.")]
    public float radioChequeoSuelo = 0.2f;
    [Tooltip("Capas consideradas como 'suelo'.")]
    public LayerMask capaSuelo;

    // --- Componentes ---
    private Rigidbody2D rb;
    private Transform objetivoJugador;
    private bool jugadorEnRangoAtaque = false;

    // --- Estado Interno ---
    private float direccionMovimiento = 1f; // 1f: Derecha, -1f: Izquierda
    private bool estaEnSuelo;

    void Start()
    {
        // Inicializa componentes y encuentra al jugador.
        rb = GetComponent<Rigidbody2D>();

        GameObject objetoJugador = GameObject.FindGameObjectWithTag("Player");
        if (objetoJugador != null)
        {
            objetivoJugador = objetoJugador.transform;
        }

        if (Random.value < 0.5f)
        {
            direccionMovimiento = -1f;
        }
    }

    void FixedUpdate()
    {
        // Lógica de movimiento y detección basada en el estado actual.
        ChequearSuelo();
        ChequearJugador();

        if (estadoActual == EstadoEnemigo.Patrullando)
        {
            MovimientoPatrulla();
        }
        else if (estadoActual == EstadoEnemigo.Persiguiendo)
        {
            MovimientoPersecucion();
        }
    }

    void ChequearSuelo()
    {
        // Determina si el enemigo está tocando el suelo.
        estaEnSuelo = Physics2D.OverlapCircle(chequeoSuelo.position, radioChequeoSuelo, capaSuelo);
    }

    void ChequearJugador()
    {
        Vector2 direccionRaycast = (direccionMovimiento < 0) ? Vector2.left : Vector2.right;
        Vector2 puntoInicio = origenRaycast.position;

        // USAMOS BOXCASTALL para obtener TODOS los colisionadores golpeados.
        RaycastHit2D[] golpes = Physics2D.BoxCastAll(
            puntoInicio,
            tamanoBoxCast,
            0f, // Ángulo de rotación
            direccionRaycast,
            longitudDeteccionRaycast,
            capaObjetivo);

        Debug.DrawRay(puntoInicio, direccionRaycast * longitudDeteccionRaycast, Color.red);

        bool jugadorDetectado = false;
        int layerPlayer = LayerMask.NameToLayer("Player");
        Debug.Log(golpes);

        // 1. Iterar sobre todos los golpes para ver si alguno es el jugador
        if (golpes.Length > 0)
        {
            foreach (RaycastHit2D golpe in golpes)
            {
                if (golpe.collider == null) continue; // Saltar colisionadores nulos

                // Comprobación A: ¿Tiene el Tag "Player"?
                bool tieneTagPlayer = golpe.collider.CompareTag("Player");

                // Comprobación B: ¿Está en la capa "Player"?
                bool estaEnLayerPlayer = golpe.collider.gameObject.layer == layerPlayer;

                // Si golpeamos algo que cumple cualquiera de las condiciones, es el objetivo.
                if (tieneTagPlayer || estaEnLayerPlayer)
                {
                    Debug.Log("<color=green>¡JUGADOR DETECTADO! (BoxCastAll). Colisionador: " + golpe.collider.name + "</color>");
                    jugadorDetectado = true;
                    break; // Detenemos la búsqueda, encontramos al jugador
                }
                else
                {
                    // Si golpea algo, pero no es el jugador, imprimimos qué es.
                    Debug.Log("Bloqueado por: " + golpe.collider.name + " con Tag: " + golpe.collider.tag);
                }
            }
        }

        // 2. Transición de estados basada en el resultado de la iteración
        if (jugadorDetectado)
        {
            estadoActual = EstadoEnemigo.Persiguiendo;
        }
        else if (estadoActual == EstadoEnemigo.Persiguiendo && !jugadorDetectado && !jugadorEnRangoAtaque)
        {
            // 3. Volver a patrullar si se pierde el contacto
            Debug.Log("<color=yellow>Jugador perdido. Volviendo a Patrullar.</color>");
            estadoActual = EstadoEnemigo.Patrullando;
            direccionMovimiento = (Random.value < 0.5f) ? 1f : -1f;
        }
    }

    void MovimientoPatrulla()
    {
        // Aplica velocidad horizontal constante al Rigidbody (en el padre).
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidadPatrulla, rb.linearVelocity.y);
    }

    void MovimientoPersecucion()
    {
        // Mueve al enemigo hacia la posición del jugador.
        float targetX = objetivoJugador.position.x;
        float currentX = transform.position.x;

        if (Mathf.Abs(targetX - currentX) < distanciaMinimaPersecucion)
        {
            direccionMovimiento = 0f;
        }
        else
        {
            direccionMovimiento = (targetX > currentX) ? 1f : -1f;
        }

        // Aumenta la velocidad usando el multiplicador
        float velocidadActual = velocidadPatrulla * multiplicadorVelocidadPersecucion;
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidadActual, rb.linearVelocity.y);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Detecta si el jugador entra en el rango del Collider Trigger (rango de ataque).
        if (other.CompareTag("Player"))
        {
            // Solo marca que está en rango. NO cambia el estado a Persiguiendo.
            jugadorEnRangoAtaque = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Marca que el jugador ha salido del rango del Collider Trigger.
        if (other.CompareTag("Player"))
        {
            jugadorEnRangoAtaque = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Maneja la colisión con el jugador u obstáculos cuando está patrullando.
        if (collision.gameObject.CompareTag("Player"))
        {
            // MENSAJE DE ATAQUE DIRECTO (Colisión de la cápsula)
            Debug.Log("<color=red>¡TE HE ATACADO!</color>");

            // Lógica de cambio de dirección, solo si está patrullando.
            if (estadoActual == EstadoEnemigo.Patrullando)
            {
                if (Random.value < probabilidadCambioDireccionColision)
                {
                    CambiarDireccion();
                }
            }
        }
        else if (estadoActual == EstadoEnemigo.Patrullando)
        {
            // Colisión con otros objetos (muros, etc.)
            if (Random.value < probabilidadCambioDireccionColision)
            {
                CambiarDireccion();
            }
        }
    }

    void CambiarDireccion()
    {
        // Invierte la dirección de movimiento.
        direccionMovimiento *= -1;
    }
}