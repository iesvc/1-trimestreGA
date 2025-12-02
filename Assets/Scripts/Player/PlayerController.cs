using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración Movimiento")]
    public float velocidadCaminar = 2f;
    public float velocidadCorrer = 4f;
    public float velocidadAgachado = 1f;

    [Header("Configuración Salto")]
    public float fuerzaSaltoBase = 20f;
    public float multiplicadorSaltoCarrera = 0.5f;
    public float multiplicadorCorteSalto = 0.5f;
    public Transform checkSuelo;
    public float radioCheckSuelo = 0.2f;
    public LayerMask capaSuelo;

    [Header("Configuración Combate")]
    public GameObject prefabProyectil;
    public Transform puntoDisparo;
    public float fuerzaDisparoBase = 5f;
    public float cadenciaDisparo = 0.2f;
    public int municionMaxima = 15;
    public float tiempoRecarga = 2f;

    private Rigidbody2D rb;
    private float inputHorizontal;
    private bool estaEnSuelo;
    private bool agachado;
    private bool corriendo;
    private bool atacando;

    private bool bocaAbierta = false;

    // Controles de animacion para:
    //          - cuerpo entero (Cu)
    //          - ojos (Ojo)
    //          - boca (Boca)
    private Animator animCu;
    private Animator animOjo;
    private Animator animBoca;

    private SpriteRenderer sprCu;
    private SpriteRenderer sprOjo;
    private SpriteRenderer sprBoca;

    private bool mirandoDerecha = true;

    private float siguienteDisparoTime = 0f;
    private int municionActual;
    private bool recargando = false;

    public float distanciaBorde = 0.6f;
    

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        municionActual = municionMaxima;
        Transform hijoOjos = transform.GetChild(0);
        Transform hijoBoca = transform.GetChild(1);
        Transform hijoCuerpo = transform.GetChild(2);

        animOjo = hijoOjos.GetComponent<Animator>();
        animBoca = hijoBoca.GetComponent<Animator>();
        animCu = hijoCuerpo.GetComponent<Animator>();

        sprOjo = hijoOjos.GetComponent<SpriteRenderer>();
        sprBoca = hijoBoca.GetComponent<SpriteRenderer>();
        sprCu = hijoCuerpo.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        ProcesarInputs();
        ProcesarDireccion();
        ProcesarAnimaciones();
        ProcesarDisparo();
    }

    private void FixedUpdate()
    {
        MoverJugador();
        ChequearSuelo();
    }

    private void ProcesarInputs()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal"); // GetAxisRaw para respuesta más inmediata

        corriendo = Input.GetKey(KeyCode.LeftShift);
        agachado = Input.GetKey(KeyCode.LeftControl);

        if (Input.GetButtonDown("Jump") && estaEnSuelo)
        {
            Saltar();
            animCu.SetBool("isJumping",true);
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * multiplicadorCorteSalto);
        }
        if (Input.GetButtonUp("Fire1"))
        {
            atacando = false;
            bocaAbierta = false;
        }
    }

    private void MoverJugador()
    {
        float velocidadActual = velocidadCaminar;

        if (agachado) velocidadActual = velocidadAgachado;
        else if (corriendo) velocidadActual = velocidadCorrer;

        if (agachado && estaEnSuelo && inputHorizontal != 0)
        {
            float direccionIntento = Mathf.Sign(inputHorizontal);

            Vector2 origenRayo = (Vector2)transform.position + (Vector2.right * direccionIntento * distanciaBorde);

            bool haySueloFuturo = Physics2D.Raycast(origenRayo + Vector2.up * 0.2f, Vector2.down, 2.5f, capaSuelo);

            Debug.DrawRay(origenRayo + Vector2.up * 0.2f, Vector2.down * 2.5f, checkSuelo ? Color.green : Color.red);

            if (!haySueloFuturo)
            {
                inputHorizontal = 0;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }

        rb.linearVelocity = new Vector2(inputHorizontal * velocidadActual, rb.linearVelocity.y);
    }

    private void ProcesarDireccion()
    {
        if (inputHorizontal > 0.01f && !mirandoDerecha)
        {
            Flip();
        }
        else if (inputHorizontal < -0.01f && mirandoDerecha)
        {
            Flip();
        }
    }
    private void Flip()
    {
        mirandoDerecha = !mirandoDerecha;

        // Giramos todos los SpriteRenderers
        if (sprCu != null) sprCu.flipX = !sprCu.flipX;
        if (sprOjo != null) sprOjo.flipX = !sprOjo.flipX;
        if (sprBoca != null) sprBoca.flipX = !sprBoca.flipX;

    }
    private void ProcesarAnimaciones()
    {
        float velocidadHorizontalTotal = Mathf.Abs(rb.linearVelocity.x);

        animCu.SetFloat("speedX", velocidadHorizontalTotal);
        animCu.SetBool("isMoving", velocidadHorizontalTotal > 0.1f);
        animCu.SetBool("isRunning", corriendo && velocidadHorizontalTotal > 0.1f);
        animCu.SetBool("isGrounded", estaEnSuelo);
        animCu.SetBool("isJumping", !estaEnSuelo);
        if (!estaEnSuelo)
        {
            animOjo.Play("ojosCerrados");
        }
        else
        {
            animOjo.Play("ojosAbiertos");
        }

        if (bocaAbierta)
        {
            animBoca.Play("bocaAbierta");
        }
        else
        {
            animBoca.Play("bocaCerrada");
        }
    }

    private void Saltar()
    {
        float fuerzaTotal = fuerzaSaltoBase;

        // Transformación de energía cinética a potencial (Salto con inercia)
        if (Mathf.Abs(rb.linearVelocity.x) > velocidadCaminar)
        {
            // A mayor velocidad X, mayor salto. Usamos una fórmula simple pero efectiva.
            float bonusInercia = (Mathf.Abs(rb.linearVelocity.x) - velocidadCaminar) * multiplicadorSaltoCarrera;
            fuerzaTotal += bonusInercia;
        }

        if (agachado) fuerzaTotal *= 0.8f;

        rb.AddForce(Vector2.up * fuerzaTotal, ForceMode2D.Impulse);
    }

    private void ProcesarDisparo()
    {
        bool quiereDisparar = Input.GetButton("Fire1");

        // Control de boca según si está intentando disparar y no está recargando
        if (quiereDisparar && !recargando)
        {
            bocaAbierta = true;
        }
        else
        {
            bocaAbierta = false;
        }

        // Control de disparo real (cadencia / munición)
        if (quiereDisparar && Time.time >= siguienteDisparoTime && !recargando)
        {
            if (municionActual > 0)
            {
                Disparar();
                atacando = true;
                siguienteDisparoTime = Time.time + cadenciaDisparo;
            }
            else
            {
                atacando = false;
                bocaAbierta = false;
                StartCoroutine(Recargar());
            }
        }
    }

    private void Disparar()
    {
        municionActual--;

        GameObject bala = Instantiate(prefabProyectil, puntoDisparo.position, Quaternion.identity);
        Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();

        float direccion = transform.localScale.x;
        if (inputHorizontal != 0) direccion = Mathf.Sign(inputHorizontal);

        Vector2 velocidadDisparo = new Vector2(direccion * fuerzaDisparoBase, fuerzaDisparoBase * 0.5f);

        rbBala.linearVelocity = velocidadDisparo + (Vector2)rb.linearVelocity;
    }

    IEnumerator Recargar()
    {
        recargando = true;
        atacando = false;
        bocaAbierta = false;
        Debug.Log("Recargando...");
        // ToDo Sonido para recargar estrellitas

        yield return new WaitForSeconds(tiempoRecarga);

        municionActual = municionMaxima;
        recargando = false;
        Debug.Log("¡Recarga completa!");
    }

    private void ChequearSuelo()
    {
        estaEnSuelo = Physics2D.OverlapCircle(checkSuelo.position, radioCheckSuelo, capaSuelo);
    }

    void OnDrawGizmos()
    {
        if (checkSuelo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(checkSuelo.position, radioCheckSuelo);
        }

        Gizmos.color = Color.yellow;

        Vector2 centro = transform.position;
        Vector2 origenDerecha = centro + (Vector2.right * distanciaBorde) + (Vector2.up * 0.2f);
        Vector2 origenIzquierda = centro + (Vector2.left * distanciaBorde) + (Vector2.up * 0.2f);

        Gizmos.DrawLine(origenDerecha, origenDerecha + Vector2.down * 0.7f);
        Gizmos.DrawLine(origenIzquierda, origenIzquierda + Vector2.down * 0.7f);
    }

}