using System.Collections;
using UnityEngine;

public class ChabbyController : MonoBehaviour
{
    private enum Estado { Patrulla, Alerta, Ataque }
    [SerializeField] private Estado estadoActual = Estado.Patrulla;

    public Transform origenVista;
    public Transform puntoDisparo;
    public GameObject prefabNubeGas;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Transform player;
    private Animator animator;

    [Header("Patrulla")]
    public float velocidadPatrulla = 2f;
    public float minTiempoGiroPatrulla = 2f;
    public float maxTiempoGiroPatrulla = 5f;

    private float dirMovimiento = -1f;      // -1 izq, 1 der
    private float temporizadorGiroPatrulla;

    [Header("Alerta")]
    public float velocidadAlerta = 3f;
    public float duracionAlerta = 3f;
    private Vector2 ultimaPosicionVista;
    private float tiempoEnAlerta = 0f;

    [Header("Visión")]
    public float distanciaVista = 10f;
    public LayerMask mascaraVision;

    [Header("Ataque")]
    public float velocidadAtaque = 4f;
    public float distanciaMinimaAtaque = 1.5f;
    public float tiempoEntreAtaques = 2f;

    private bool jugadorEnRangoAtaque = false;
    private bool puedeAtacar = true;

    // -------- OTROS --------
    private bool mirandoDerecha = false;
    private float objetivoVelocidadX = 0f;
    private bool veJugador = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        GameObject objPlayer = GameObject.FindGameObjectWithTag("Player");
        if (objPlayer != null) player = objPlayer.transform;

        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        ReiniciarTemporizadorPatrulla();
        ultimaPosicionVista = transform.position;
    }

    private void Update()
    {
        veJugador = VeAlJugador();

        switch (estadoActual)
        {
            case Estado.Patrulla:
                LogicaPatrulla();
                break;
            case Estado.Alerta:
                LogicaAlerta();
                break;
            case Estado.Ataque:
                LogicaAtaque();
                break;
        }

        ActualizarFlip();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(objetivoVelocidadX, rb.linearVelocity.y);
    }

    private void LogicaPatrulla()
    {
        objetivoVelocidadX = dirMovimiento * velocidadPatrulla;

        temporizadorGiroPatrulla -= Time.deltaTime;
        if (temporizadorGiroPatrulla <= 0f)
        {
            dirMovimiento *= -1f;
            ReiniciarTemporizadorPatrulla();
        }
    }

    private void ReiniciarTemporizadorPatrulla()
    {
        temporizadorGiroPatrulla = Random.Range(minTiempoGiroPatrulla, maxTiempoGiroPatrulla);
    }

    private void LogicaAlerta()
    {
        float dist = Vector2.Distance(transform.position, ultimaPosicionVista);

        if (dist > 0.2f)
        {
            float dirX = Mathf.Sign(ultimaPosicionVista.x - transform.position.x);
            objetivoVelocidadX = dirX * velocidadAlerta;
            tiempoEnAlerta = 0f;
        }
        else
        {
            objetivoVelocidadX = 0f;
            tiempoEnAlerta += Time.deltaTime;

            if (tiempoEnAlerta >= duracionAlerta)
            {
                estadoActual = Estado.Patrulla;
                ReiniciarTemporizadorPatrulla();
            }
        }

    }

    private void LogicaAtaque()
    {
        if (player == null)
        {
            estadoActual = Estado.Patrulla;
            return;
        }

        float dx = player.position.x - transform.position.x;
        float distAbs = Mathf.Abs(dx);
        float dirX = Mathf.Sign(dx);

        if (distAbs > distanciaMinimaAtaque)
            objetivoVelocidadX = dirX * velocidadAtaque;
        else
            objetivoVelocidadX = 0f;

        if (jugadorEnRangoAtaque && puedeAtacar)
            StartCoroutine(CorrutinaAtaque());

        if (!jugadorEnRangoAtaque && !veJugador)
        {
            estadoActual = Estado.Alerta;
            tiempoEnAlerta = 0f;
        }
    }
    private IEnumerator CorrutinaAtaque()
    {
        puedeAtacar = false;

        if (prefabNubeGas != null && puntoDisparo != null)
            
            Instantiate(prefabNubeGas, puntoDisparo.position, Quaternion.identity);
        else
            Debug.LogWarning("Chabby: falta prefabNubeGas o puntoDisparo asignado.");

        yield return new WaitForSeconds(tiempoEntreAtaques);
        puedeAtacar = true;
    }
    private bool VeAlJugador()
    {
        if (player == null || origenVista == null) return false;

        Vector2 origen = origenVista.position;
        Vector2 destino = player.position;
        Vector2 dir = (destino - origen).normalized;

        float distanciaReal = Vector2.Distance(origen, destino);
        if (distanciaReal > distanciaVista) return false;

        RaycastHit2D hit = Physics2D.Raycast(origen, dir, distanciaReal, mascaraVision);

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            ultimaPosicionVista = player.position;

            if (estadoActual == Estado.Patrulla || estadoActual == Estado.Alerta)
                estadoActual = Estado.Ataque;

            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorEnRangoAtaque = true;

        if (estadoActual != Estado.Ataque)
            estadoActual = Estado.Ataque;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorEnRangoAtaque = false;
    }

    // ------------------ FLIP ------------------
    private void ActualizarFlip()
    {
        if (objetivoVelocidadX > 0.05f) mirandoDerecha = true;
        else if (objetivoVelocidadX < -0.05f) mirandoDerecha = false;

        if (sprite != null)
            sprite.flipX = !mirandoDerecha;
    }
}
