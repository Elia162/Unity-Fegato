using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Image fillImage; // Riferimento all'immagine della barra che si riempie

    [Header("Script References")]
    // RICORDA: Sostituisci 'PlayerStats' con il VERO nome del tuo script
    public MeshDeformerInput playerScript;

    void Start()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 25f;
        }
    }

    void Update()
    {
        // Controlliamo che tutti i riferimenti siano assegnati
        if (playerScript != null && healthSlider != null && fillImage != null)
        {
            // RICORDA: Sostituisci 'currentHealth' con la VERA variabile del tuo script
            float rawValue = playerScript.indentazione;

            // Limitiamo il valore tra 0 e 25
            float clampedValue = Mathf.Clamp(rawValue, 0f, 25f);

            // Aggiorniamo la lunghezza della barra
            healthSlider.value = clampedValue;

            // Calcoliamo la percentuale da 0 a 1 per il colore (0/25 = 0, 25/25 = 1)
            float percentage = clampedValue / 25f;

            // Cambiamo il colore gradualmente: a 0 è Verde, a 1 (cioè 25) è Rosso
            fillImage.color = Color.Lerp(Color.green, Color.red, percentage);

            fillImage.enabled = clampedValue > 0f;
        }
    }
}
