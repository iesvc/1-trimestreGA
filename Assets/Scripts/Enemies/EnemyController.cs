
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EstadoEnemigo { Patrullando, Persiguiendo }

    [Header("Estado Actual")]
    public EstadoEnemigo estadoActual = EstadoEnemigo.Patrullando;

    // ----------------------------------------------------
    [Header("Movimiento")]
    public float velocidadPatrulla = 3f;
    public float multiplicadorVelocidadPersecucion = 1.5f;

    [Tooltip("Distancia mínima para detenerse y permitir colisión")]
    public float distanciaMinimaPersecucion = 0.4f;

    // ----------------------------------------------------
    [Header("Detección y Persecución (BoxCast)")]
    public Transform origenRaycast;
    public float longitudDeteccionRaycast = 35f;
    public LayerMask capaObjetivo;
    public Vector2 tamanoBoxCast = new Vector2(0.5f, 0.5f);

    // ----------------------------------------------------
    [Header("Comportamiento de Patrulla")]
    [Range(0f, 1f)] public float probabilidadCambioDireccionColision = 1f;

    // ----------------------------------------------------
    [Header("Detección de Suelo")]
    public Transform chequeoSuelo;
    public float radioChequeoSuelo = 0.2f;
    public LayerMask capaSuelo;

    // ----------------------------------------------------
    [Header("Debug")]
    public bool mostrarBoxCast = true;

    // ----------------------------------------------------
    // Componentes / Estado interno
    Rigidbody2D rb;
    Transform objetivoJugador;
    bool jugadorEnRangoAtaque = false;

    float direccionMovimiento = 1f;
    bool estaEnSuelo;

    // ====================================================

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
            objetivoJugador = objJugador.transform;

        direccionMovimiento = Random.value < 0.5f ? -1f : 1f;
    }

    // ====================================================

    void FixedUpdate()
    {
        ChequearSuelo();
        ChequearJugador();

        if (estadoActual == EstadoEnemigo.Patrullando)
            MovimientoPatrulla();
        else
            MovimientoPersecucion();
    }

    // ====================================================

    void ChequearSuelo()
    {
        if (chequeoSuelo == null) return;
        estaEnSuelo = Physics2D.OverlapCircle(chequeoSuelo.position, radioChequeoSuelo, capaSuelo);
    }

    // ====================================================

    void ChequearJugador()
    {
        Vector2 puntoInicio = origenRaycast.position;
        bool jugadorDetectado = false;
        int layerPlayer = LayerMask.NameToLayer("Player");

        Vector2[] direccionesAChequear =
            estadoActual == EstadoEnemigo.Patrullando
            ? new Vector2[] { direccionMovimiento < 0 ? Vector2.left : Vector2.right }
            : new Vector2[] { Vector2.left, Vector2.right };

        foreach (Vector2 direccion in direccionesAChequear)
        {
            if (mostrarBoxCast)
            {
                DibujarBoxCast(
                    puntoInicio,
                    tamanoBoxCast,
                    direccion,
                    longitudDeteccionRaycast,
                    Color.red
                );
            }

            RaycastHit2D[] golpes = Physics2D.BoxCastAll(
                puntoInicio,
                tamanoBoxCast,
                0f,
                direccion,
                longitudDeteccionRaycast,
                capaObjetivo
            );

            foreach (RaycastHit2D golpe in golpes)
            {
                if (golpe.collider == null) continue;

                if (golpe.collider.gameObject.layer == layerPlayer ||
                    golpe.collider.CompareTag("Player"))
                {
                    jugadorDetectado = true;
                    estadoActual = EstadoEnemigo.Persiguiendo;
                    return;
                }
            }
        }

        if (!jugadorDetectado && estadoActual == EstadoEnemigo.Persiguiendo && !jugadorEnRangoAtaque)
        {
            estadoActual = EstadoEnemigo.Patrullando;
            direccionMovimiento = Random.value < 0.5f ? -1f : 1f;
        }
    }

    // ====================================================

    void MovimientoPatrulla()
    {
        rb.linearVelocity = new Vector2(
            direccionMovimiento * velocidadPatrulla,
            rb.linearVelocity.y
        );
    }

    // ====================================================

    void MovimientoPersecucion()
    {
        if (objetivoJugador == null) return;

        float distancia = Mathf.Abs(objetivoJugador.position.x - transform.position.x);

        // 🔒 TOPE REAL → ahora sí colisiona
        if (distancia <= distanciaMinimaPersecucion)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        direccionMovimiento =
            objetivoJugador.position.x > transform.position.x ? 1f : -1f;

        float velocidad = velocidadPatrulla * multiplicadorVelocidadPersecucion;

        rb.linearVelocity = new Vector2(
            direccionMovimiento * velocidad,
            rb.linearVelocity.y
        );
    }

    // ====================================================
    // DEBUG VISUAL
    // ====================================================

    void DibujarBoxCast(Vector2 origen, Vector2 size, Vector2 direccion, float distancia, Color color)
    {
        Vector2 final = origen + direccion * distancia;
        DibujarRect(origen, size, color);
        DibujarRect(final, size, color);
        Debug.DrawLine(origen, final, color);
    }

    void DibujarRect(Vector2 centro, Vector2 size, Color color)
    {
        Vector2 h = size / 2f;
        Vector2 a = centro + new Vector2(-h.x, -h.y);
        Vector2 b = centro + new Vector2(h.x, -h.y);
        Vector2 c = centro + new Vector2(h.x, h.y);
        Vector2 d = centro + new Vector2(-h.x, h.y);

        Debug.DrawLine(a, b, color);
        Debug.DrawLine(b, c, color);
        Debug.DrawLine(c, d, color);
        Debug.DrawLine(d, a, color);
    }

    // ====================================================

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            jugadorEnRangoAtaque = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            jugadorEnRangoAtaque = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (estadoActual == EstadoEnemigo.Patrullando)
        {
            if (Random.value < probabilidadCambioDireccionColision)
                CambiarDireccion();
        }
        else if (estadoActual == EstadoEnemigo.Persiguiendo)
        {
            CambiarDireccion();
            estadoActual = EstadoEnemigo.Patrullando;
        }
    }

    void CambiarDireccion()
    {
        direccionMovimiento *= -1f;
    }
}
