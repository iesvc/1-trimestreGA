using UnityEngine;

public class PakkoController : MonoBehaviour
{
    public enum EstadoEnemigo { Patrullando, Persiguiendo, Retirada, Vigilando }

    [Header("Configuración de Estado")]
    public EstadoEnemigo estadoActual = EstadoEnemigo.Patrullando;

    [Header("Animaciones")]
    private Animator anim;
    // Nueva variable para controlar la distancia real de ataque
    public float distanciaParaAtacar = 2.5f;

    [Header("Movimiento de Patrulla")]
    public float velocidadPatrulla = 3f;
    [Range(0, 100)] public float probabilidadGiroEspontaneo = 0.2f;

    [Header("Vigilancia")]
    [Range(0, 100)] public float probabilidadDeMirarAtras = 0.3f;
    public float tiempoMirandoAtras = 1.2f;

    [Header("Persecución")]
    public float multiplicadorPersecucion = 1.8f;
    public float distanciaParaFrenar = 1.2f; // Un poco más para que no se pegue tanto
    public float tiempoPersistencia = 2.0f;

    [Header("Combate")]
    public float fuerzaRebote = 6f;
    public float tiempoRetirada = 0.4f;

    [Header("Detección")]
    public Transform origenRaycast;
    public float distanciaDeteccion = 35f;
    public LayerMask capaObjetivo;
    public Vector2 tamanoBoxCast = new Vector2(0.5f, 0.5f);

    private Rigidbody2D rb;
    private Transform objetivoJugador;
    private float direccionHorizontal = 1f;

    private float tiempoDesdeUltimaVista;
    private float tiempoEnRetirada;
    private float timerEspera;
    private Vector2 ultimaPosicionConocida;
    private bool jugadorEnLaMira = false;
    private bool estaEnRangoDeAtaque = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) objetivoJugador = player.transform;

        direccionHorizontal = (Random.value < 0.5f) ? 1f : -1f;
        if (origenRaycast == null) enabled = false;
    }

    void FixedUpdate()
    {
        SistemaDeDeteccion();

        if (estadoActual == EstadoEnemigo.Retirada)
            GestionarRetirada();
        else
            GestionarIA();

        AplicarMovimiento();
        GestionarGiroVisual();
        ActualizarAnimaciones();
    }

    void SistemaDeDeteccion()
    {
        if (objetivoJugador == null) return;

        Vector2 direccionRaycast = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.BoxCast(origenRaycast.position, tamanoBoxCast, 0f, direccionRaycast, distanciaDeteccion, capaObjetivo);

        Debug.DrawRay(origenRaycast.position, direccionRaycast * distanciaDeteccion, jugadorEnLaMira ? Color.green : Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            jugadorEnLaMira = true;
            tiempoDesdeUltimaVista = 0f;
            ultimaPosicionConocida = objetivoJugador.position;

            // Calculamos si además de verlo, está lo bastante cerca para atacar
            float distanciaReal = Vector2.Distance(transform.position, objetivoJugador.position);
            estaEnRangoDeAtaque = (distanciaReal <= distanciaParaAtacar);
        }
        else
        {
            jugadorEnLaMira = false;
            estaEnRangoDeAtaque = false;
            tiempoDesdeUltimaVista += Time.fixedDeltaTime;
        }
    }

    void GestionarIA()
    {
        if (jugadorEnLaMira || tiempoDesdeUltimaVista < tiempoPersistencia)
            estadoActual = EstadoEnemigo.Persiguiendo;
        else if (estadoActual == EstadoEnemigo.Vigilando)
        {
            timerEspera += Time.fixedDeltaTime;
            if (timerEspera >= tiempoMirandoAtras)
            {
                estadoActual = EstadoEnemigo.Patrullando;
                timerEspera = 0;
            }
        }
        else
            estadoActual = EstadoEnemigo.Patrullando;
    }

    void ActualizarAnimaciones()
    {
        if (anim == null) return;

        // SOLO ataca si está en rango de ataque (cerca), no solo por verlo
        estaEnRangoDeAtaque = true;
        anim.SetBool("IsAttacking", estaEnRangoDeAtaque);

        // Para que sepa si está caminando o quieto (si tienes un float de velocidad)
        anim.SetFloat("VelocidadX", Mathf.Abs(rb.linearVelocity.x));
    }

    void AplicarMovimiento()
    {
        // Solo se frena si está REALMENTE atacando
        float factorAtaque = estaEnRangoDeAtaque ? 0f : 1f;

        switch (estadoActual)
        {
            case EstadoEnemigo.Patrullando:
                rb.linearVelocity = new Vector2(direccionHorizontal * velocidadPatrulla, rb.linearVelocity.y);
                if (Random.value < (probabilidadGiroEspontaneo / 100f)) direccionHorizontal *= -1;
                break;

            case EstadoEnemigo.Vigilando:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                break;

            case EstadoEnemigo.Persiguiendo:
                float diferenciaX = ultimaPosicionConocida.x - transform.position.x;

                if (Mathf.Abs(diferenciaX) > distanciaParaFrenar)
                {
                    direccionHorizontal = Mathf.Sign(diferenciaX);
                    // Si no está en rango de ataque, corre normal
                    rb.linearVelocity = new Vector2(direccionHorizontal * (velocidadPatrulla * multiplicadorPersecucion) * factorAtaque, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }
                break;

            case EstadoEnemigo.Retirada:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
                break;
        }
    }

    // ... (El resto del código de giro visual y colisión se mantiene igual)
    void GestionarGiroVisual()
    {
        float escalaX = Mathf.Abs(transform.localScale.x);
        if (estadoActual == EstadoEnemigo.Vigilando)
            transform.localScale = new Vector3(escalaX * -direccionHorizontal, transform.localScale.y, transform.localScale.z);
        else if (rb.linearVelocity.x != 0)
            transform.localScale = new Vector3(escalaX * Mathf.Sign(rb.linearVelocity.x), transform.localScale.y, transform.localScale.z);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            estadoActual = EstadoEnemigo.Retirada;
            tiempoEnRetirada = 0f;
            Vector2 direccionRebote = (transform.position - collision.transform.position).normalized;
            direccionRebote += Vector2.up * 0.4f;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direccionRebote * fuerzaRebote, ForceMode2D.Impulse);
        }
        else if (estadoActual == EstadoEnemigo.Patrullando)
        {
            direccionHorizontal *= -1;
        }
    }

    void GestionarRetirada()
    {
        tiempoEnRetirada += Time.fixedDeltaTime;
        if (tiempoEnRetirada >= tiempoRetirada)
        {
            estadoActual = EstadoEnemigo.Patrullando;
            tiempoEnRetirada = 0f;
        }
    }
}