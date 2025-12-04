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

    // ESTA VARIABLE YA NO SE USA PARA FRENAR
    public float distanciaMinimaPersecucion = 0.5f;

    [Tooltip("El tamaño del área de detección usada por el BoxCast (X para ancho, Y para alto).")]
    public Vector2 tamanoBoxCast = new Vector2(0.5f, 0.5f);

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
    private float direccionMovimiento = 1f;
    private bool estaEnSuelo;

    // ---------------------------------------------------------------------------------------------------------------------

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null || origenRaycast == null)
        {
            Debug.LogError("Error de configuración: Rigidbody2D o origenRaycast son nulos.");
            enabled = false;
            return;
        }

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

    // ---------------------------------------------------------------------------------------------------------------------

    void FixedUpdate()
    {
        if (chequeoSuelo != null)
        {
            ChequearSuelo();
        }

        if (objetivoJugador != null)
        {
            ChequearJugador();
        }

        if (estadoActual == EstadoEnemigo.Patrullando)
        {
            MovimientoPatrulla();
        }
        else if (estadoActual == EstadoEnemigo.Persiguiendo)
        {
            MovimientoPersecucion();
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    void ChequearSuelo()
    {
        estaEnSuelo = Physics2D.OverlapCircle(chequeoSuelo.position, radioChequeoSuelo, capaSuelo);
    }

    // ---------------------------------------------------------------------------------------------------------------------

    void ChequearJugador()
    {
        Vector2 puntoInicio = origenRaycast.position;
        bool jugadorDetectado = false;
        int layerPlayer = LayerMask.NameToLayer("Player");

        // Determina las direcciones a chequear (Doble BoxCast en Persiguiendo)
        Vector2[] direccionesAChequear;

        if (estadoActual == EstadoEnemigo.Patrullando)
        {
            direccionesAChequear = new Vector2[] { (direccionMovimiento < 0) ? Vector2.left : Vector2.right };
        }
        else
        {
            direccionesAChequear = new Vector2[] { Vector2.left, Vector2.right };
        }

        foreach (Vector2 direccionRaycast in direccionesAChequear)
        {
            RaycastHit2D[] golpes = Physics2D.BoxCastAll(
                puntoInicio, tamanoBoxCast, 0f, direccionRaycast, longitudDeteccionRaycast, capaObjetivo);

            Debug.DrawRay(puntoInicio, direccionRaycast * longitudDeteccionRaycast, Color.red);
            // ❌ Log de parámetros de BoxCast eliminado aquí

            if (golpes.Length > 0)
            {
                foreach (RaycastHit2D golpe in golpes)
                {
                    if (golpe.collider == null) continue;

                    bool tieneTagPlayer = golpe.collider.CompareTag("Player");
                    bool estaEnLayerPlayer = golpe.collider.gameObject.layer == layerPlayer;

                    if (tieneTagPlayer || estaEnLayerPlayer)
                    {
                        Debug.Log("<color=green>¡JUGADOR DETECTADO! (BoxCastAll). Colisionador: " + golpe.collider.name + "</color>");
                        jugadorDetectado = true;

                        if (estadoActual != EstadoEnemigo.Persiguiendo)
                        {
                            estadoActual = EstadoEnemigo.Persiguiendo;
                            Debug.Log("<color=magenta>⭐ ESTADO CAMBIADO: A Persiguiendo (A Muerte) ⭐</color>");
                        }

                        goto DeteccionCompleta;
                    }
                }
            }
        }

    DeteccionCompleta:;

        // Lógica para volver a Patrullar si se pierde el contacto
        if (!jugadorDetectado && estadoActual == EstadoEnemigo.Persiguiendo && !jugadorEnRangoAtaque)
        {
            Debug.Log("<color=yellow>Jugador perdido. Volviendo a Patrullar.</color>");
            estadoActual = EstadoEnemigo.Patrullando;
            direccionMovimiento = (Random.value < 0.5f) ? 1f : -1f;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------

    void MovimientoPatrulla()
    {
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidadPatrulla, rb.linearVelocity.y);
    }

    // ---------------------------------------------------------------------------------------------------------------------

    void MovimientoPersecucion()
    {
        float targetX = objetivoJugador.position.x;
        float currentX = transform.position.x;

        // Calcula la dirección para ir hacia el jugador
        direccionMovimiento = (targetX > currentX) ? 1f : -1f;

        // Aplica la velocidad aumentada
        float velocidadAumentada = velocidadPatrulla * multiplicadorVelocidadPersecucion;

        rb.linearVelocity = new Vector2(direccionMovimiento * velocidadAumentada, rb.linearVelocity.y);
    }

    // ---------------------------------------------------------------------------------------------------------------------

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRangoAtaque = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorEnRangoAtaque = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("<color=red>¡TE HE ATACADO!</color>");

            if (estadoActual == EstadoEnemigo.Patrullando)
            {
                if (Random.value < probabilidadCambioDireccionColision)
                {
                    CambiarDireccion();
                }
            }
            else if (estadoActual == EstadoEnemigo.Persiguiendo)
            {
                // HUIR TRAS EL CONTACTO (El enemigo se pira)
                Debug.Log("<color=orange>🎯 ¡TOCADO! Huyendo...</color>");
                CambiarDireccion(); // Se gira inmediatamente para huir
                estadoActual = EstadoEnemigo.Patrullando; // Vuelve a Patrullar (alejándose)
            }
        }
        else
        {
            // Colisión con otros objetos (sólo cambia si está Patrullando)
            if (estadoActual == EstadoEnemigo.Patrullando)
            {
                if (Random.value < probabilidadCambioDireccionColision)
                {
                    CambiarDireccion();
                }
            }
        }
    }

    void CambiarDireccion()
    {
        direccionMovimiento *= -1;
    }
}