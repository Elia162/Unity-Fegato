using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class GestoreDatiYeohRandom : MonoBehaviour
{
    public static GestoreDatiYeohRandom Instance;

    [System.Serializable]
    public struct Variabili
    {
        public string idOriginale;
        public float C1;
        public float C2;
        public float C3;

        public Variabili(string idOrig, float c1, float c2, float c3)
        {
            idOriginale = idOrig;
            C1 = c1;
            C2 = c2;
            C3 = c3;
        }
    }

    [Header("Configurazione File")]
    public TextAsset fileCSV;

    [Header("Le Celle che 'iniziano' lo scambio")]
    private List<string> celleDaMischiare = new List<string> { "B1", "C4", "E2", "F5", "H2", "H5" };

    private Dictionary<string, Variabili> database = new Dictionary<string, Variabili>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Inizializza il generatore casuale basato sul tempo per avere scambi sempre diversi
            Random.InitState((int)System.DateTime.Now.Ticks);

            PopolaEmescolaDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void PopolaEmescolaDatabase()
    {
        if (fileCSV == null)
        {
            Debug.LogError("ERRORE: Non hai assegnato il file CSV!");
            return;
        }

        database.Clear();

        string[] righe = fileCSV.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 1. CARICHIAMO TUTTO NORMALMENTE
        for (int i = 1; i < righe.Length; i++)
        {
            string[] celle = righe[i].Split(';');

            if (celle.Length >= 4)
            {
                string id = celle[0].Trim();
                float c1 = ParseFloat(celle[1]);
                float c2 = ParseFloat(celle[2]);
                float c3 = ParseFloat(celle[3]);

                database.Add(id, new Variabili(id, c1, c2, c3));
            }
        }

        // 2. CREIAMO UNA LISTA DI TUTTE LE CELLE DISPONIBILI (A1, A2... I6)
        List<string> tutteLeChiavi = new List<string>(database.Keys);

        Debug.Log("--- INIZIO SCAMBI ---");

        // 3. FACCIAMO PARTIRE GLI SCAMBI
        foreach (string cellaTarget in celleDaMischiare)
        {
            // Verifichiamo che la cella esista
            if (database.ContainsKey(cellaTarget))
            {
                // Pesca un partner a caso da TUTTE le chiavi (può estrarre anche se stessa!)
                string partnerCasuale = tutteLeChiavi[Random.Range(0, tutteLeChiavi.Count)];

                // Se la cella ha pescato sé stessa...
                if (partnerCasuale == cellaTarget)
                {
                    Debug.Log($"[NESSUN CAMBIO] {cellaTarget} ha pescato casualmente se stessa! Mantiene i parametri originali di {database[cellaTarget].idOriginale}.");
                }
                // Se invece ha pescato un'altra cella, procediamo allo scambio
                else
                {
                    Variabili temp = database[cellaTarget];
                    database[cellaTarget] = database[partnerCasuale];
                    database[partnerCasuale] = temp;

                    // Stampiamo in console cosa è successo
                    Debug.Log($"[SCAMBIO] {cellaTarget} ha preso i parametri di {database[cellaTarget].idOriginale}. Di conseguenza, la cella 'pescata' {partnerCasuale} ha dovuto prendere quelli di {database[partnerCasuale].idOriginale}.");
                }
            }
        }

        Debug.Log("--- SCAMBI TERMINATI. Le altre celle sono rimaste intatte. ---");
    }

    float ParseFloat(string valore)
    {
        valore = valore.Replace(",", ".");
        if (float.TryParse(valore, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        return 0f;
    }

    public Variabili OttieniValori(string id)
    {
        if (database.ContainsKey(id))
        {
            return database[id];
        }

        Debug.LogWarning($"Cella '{id}' non trovata!");
        return new Variabili("Sconosciuto", 0, 0, 0);
    }
}
   