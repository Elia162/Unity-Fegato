using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class GestoreDatiRandom : MonoBehaviour
{
    public static GestoreDatiRandom Instance;

    [System.Serializable]
    public struct Variabili
    {
        public string idOriginale;

        // Tutti i 5 parametri utili
        public float knee;
        public float y0;
        public float m1;
        public float m2;
        public float k;

        public Variabili(string idOrig, float valKnee, float valY0, float valM1, float valM2, float valK)
        {
            idOriginale = idOrig;
            knee = valKnee;
            y0 = valY0;
            m1 = valM1;
            m2 = valM2;
            k = valK;
        }
    }

    [Header("Configurazione File")]
    [Tooltip("Trascina qui il file .csv esportato da Excel")]
    public TextAsset fileCSV;

    [Header("Le Celle che 'iniziano' lo scambio")]
    [Tooltip("Scrivi qui i nomi delle celle che devono scambiarsi i parametri")]
    private List<string> celleDaMischiare = new List<string> { "B1", "C4", "F5", "H2"};

    // Il nostro dizionario principale
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
            Debug.LogError("ERRORE: Non hai assegnato il file CSV nello script!");
            return;
        }

        database.Clear();

        string[] righe = fileCSV.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // --- 1. LETTURA DEL CSV ---
        Dictionary<string, Variabili> datiDalCSV = new Dictionary<string, Variabili>();

        for (int i = 1; i < righe.Length; i++) // Salta l'intestazione
        {
            string[] celle = righe[i].Split(';');

            if (celle.Length >= 7)
            {
                string idCella = celle[0].Trim();

                // --- FIX: Ignoriamo le righe vuote generate da Excel ---
                if (string.IsNullOrEmpty(idCella))
                    continue;

                // --- FIX: Evitiamo crash se hai per sbaglio dei duplicati veri nel CSV ---
                if (datiDalCSV.ContainsKey(idCella))
                {
                    Debug.LogWarning($"Attenzione: ID '{idCella}' duplicato nel CSV. Lo ignoro.");
                    continue;
                }

                // Mappatura delle colonne: 2=knee | 3=y0 | 4=m1 | 5=m2 | 6=k
                // (La colonna 1 'Profondità' viene ignorata in automatico)
                float valKnee = ParseFloat(celle[2]);
                float valY0 = ParseFloat(celle[3]);
                float valM1 = ParseFloat(celle[4]);
                float valM2 = ParseFloat(celle[5]);
                float valK = ParseFloat(celle[6]);

                datiDalCSV.Add(idCella, new Variabili(idCella, valKnee, valY0, valM1, valM2, valK));
            }
        }

        // --- 2. GENERAZIONE DELLA GRIGLIA COMPLETA (Da A1 a I6) ---
        for (char lettera = 'A'; lettera <= 'I'; lettera++)
        {
            for (int numero = 1; numero <= 6; numero++)
            {
                string nomeCella = lettera.ToString() + numero.ToString();

                if (datiDalCSV.ContainsKey(nomeCella))
                {
                    var d = datiDalCSV[nomeCella];
                    database.Add(nomeCella, new Variabili(nomeCella, d.knee, d.y0, d.m1, d.m2, d.k));
                }
                else if (datiDalCSV.ContainsKey("Others"))
                {
                    var d = datiDalCSV["Others"];
                    database.Add(nomeCella, new Variabili("Others", d.knee, d.y0, d.m1, d.m2, d.k));
                }
                else
                {
                    database.Add(nomeCella, new Variabili("Sconosciuto", 0, 0, 0, 0, 0));
                }
            }
        }

        // --- 3. LOGICA DI SCAMBIO CASUALE ---
        List<string> tutteLeChiavi = new List<string>(database.Keys);

        Debug.Log("--- INIZIO SCAMBI ---");

        foreach (string cellaTarget in celleDaMischiare)
        {
            if (database.ContainsKey(cellaTarget))
            {
                // Pesca un partner a caso da TUTTA la griglia creata
                string partnerCasuale = tutteLeChiavi[Random.Range(0, tutteLeChiavi.Count)];

                if (partnerCasuale == cellaTarget)
                {
                    Debug.Log($"[NESSUN CAMBIO] {cellaTarget} ha pescato casualmente se stessa! Mantiene i parametri originali di {database[cellaTarget].idOriginale}.");
                }
                else
                {
                    // Esegue lo scambio di TUTTI E 5 i valori
                    Variabili temp = database[cellaTarget];
                    database[cellaTarget] = database[partnerCasuale];
                    database[partnerCasuale] = temp;

                    Debug.Log($"[SCAMBIO] {cellaTarget} ha preso i parametri di {partnerCasuale}. La cella {partnerCasuale} ha preso quelli di {database[partnerCasuale].idOriginale}.");
                }
            }
        }

        Debug.Log("--- SCAMBI TERMINATI ---");
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
        return new Variabili("Sconosciuto", 0, 0, 0, 0, 0);
    }
}
