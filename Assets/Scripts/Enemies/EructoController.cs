using UnityEngine;

public class EructoController : MonoBehaviour
{
    [Header("Daño")]
    [Tooltip("Cada cuántos segundos hace daño mientras el jugador está dentro.")]
    public float intervaloDaño = 1f;

    [Header("Vida del eructo")]
    [Tooltip("Cuánto dura la nube de gas antes de desaparecer.")]
    public float duracion = 2f;

    private float tiempoSiguienteDaño = 0f;
    private bool jugadorDentro = false;
    private PlayerController playerController;

    private void Start()
    {
        // Por si acaso, nos aseguramos de que la nube se destruye sola
        Destroy(gameObject, duracion);
    }

    private void Update()
    {
        if (!jugadorDentro || playerController == null) return;

        // Contamos tiempo para el siguiente tick de daño
        tiempoSiguienteDaño -= Time.deltaTime;

        if (tiempoSiguienteDaño <= 0f)
        {
            // Hacemos daño aprovechando tu lógica de vida / invulnerabilidad
            playerController.QuitarVida();
            tiempoSiguienteDaño = intervaloDaño;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDentro = true;
        playerController = other.GetComponent<PlayerController>();

        // Primer golpe instantáneo al entrar
        if (playerController != null)
        {
            playerController.QuitarVida();
            tiempoSiguienteDaño = intervaloDaño;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugadorDentro = false;
        playerController = null;
    }
}
