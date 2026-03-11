using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class GestoreDati : MonoBehaviour
{
    public static GestoreDati Instance;

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

    // Il nostro dizionario principale
    private Dictionary<string, Variabili> database = new Dictionary<string, Variabili>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PopolaDatabaseFisso(); // Carica i dati mettendoli ognuno al proprio posto
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void PopolaDatabaseFisso()
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

        // Partiamo da 1 per saltare la riga di intestazione (Cella;Profondità;knee;y0;m1;m2;k)
        for (int i = 1; i < righe.Length; i++)
        {
            string[] celle = righe[i].Split(';');

            // Ci assicuriamo di avere almeno 7 colonne (da 0 a 6)
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

                // Mappatura delle colonne basata sul tuo file:
                // 0=Cella | 1=Profondità(ignorata) | 2=knee | 3=y0 | 4=m1 | 5=m2 | 6=k
                float valKnee = ParseFloat(celle[2]);
                float valY0 = ParseFloat(celle[3]);
                float valM1 = ParseFloat(celle[4]);
                float valM2 = ParseFloat(celle[5]);
                float valK = ParseFloat(celle[6]);

                datiDalCSV.Add(idCella, new Variabili(idCella, valKnee, valY0, valM1, valM2, valK));
            }
        }

        // --- 2. GENERAZIONE DELLA GRIGLIA COMPLETA (Da A1 a I6) ---
        // Genera le lettere da A (char 65) a I (char 73)
        for (char lettera = 'A'; lettera <= 'I'; lettera++)
        {
            // Genera i numeri da 1 a 6
            for (int numero = 1; numero <= 6; numero++)
            {
                string nomeCella = lettera.ToString() + numero.ToString(); // Esempio: "A1", "B2", ecc.

                // Caso A: La cella ha parametri specifici scritti nel CSV (es. B1, C4)
                if (datiDalCSV.ContainsKey(nomeCella))
                {
                    var d = datiDalCSV[nomeCella];
                    database.Add(nomeCella, new Variabili(nomeCella, d.knee, d.y0, d.m1, d.m2, d.k));
                }
                // Caso B: La cella NON è nel CSV, quindi prende i parametri "Others"
                else if (datiDalCSV.ContainsKey("Others"))
                {
                    var d = datiDalCSV["Others"];
                    // Impostiamo l'id originale su "Others" così sappiamo che ha preso i parametri di default
                    database.Add(nomeCella, new Variabili("Others", d.knee, d.y0, d.m1, d.m2, d.k));
                }
                else
                {
                    Debug.LogWarning($"Manca la riga 'Others' nel CSV! Assegno 0 a {nomeCella}.");
                    database.Add(nomeCella, new Variabili("Sconosciuto", 0, 0, 0, 0, 0));
                }
            }
        }

        Debug.Log($"--- DATABASE CARICATO --- Totale celle generate: {database.Count}. Le celle mantengono i loro parametri originari.");
    }

    // Convertitore sicuro per le stringhe (gestisce sia la virgola italiana "0,5" che il punto "0.5")
    float ParseFloat(string valore)
    {
        valore = valore.Replace(",", ".");
        if (float.TryParse(valore, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        return 0f;
    }

    // Funzione che le altre classi useranno per chiedere i dati
    public Variabili OttieniValori(string id)
    {
        if (database.ContainsKey(id))
        {
            return database[id];
        }

        Debug.LogWarning($"Cella '{id}' non trovata nel dizionario!");
        return new Variabili("Sconosciuto", 0, 0, 0, 0, 0);
    }
}
