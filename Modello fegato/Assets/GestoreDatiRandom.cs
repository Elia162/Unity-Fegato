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
        public float knee, y0, m1, m2, k;

        // Rinominati i parametri in valKnee, valY0, ecc. per evitare il conflitto
        public Variabili(string id, float valKnee, float valY0, float valM1, float valM2, float valK)
        {
            idOriginale = id;
            knee = valKnee;
            y0 = valY0;
            m1 = valM1;
            m2 = valM2;
            k = valK;
        }
    }

    public TextAsset fileCSV;
    private List<string> celleDaMischiare = new List<string> { "B1", "C4", "F5", "H2" };
    private Dictionary<string, Variabili> database = new Dictionary<string, Variabili>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Random.InitState((int)System.DateTime.Now.Ticks);
            PopolaEmescolaDatabase();
            StampaTabellaInConsole();
        }
        else Destroy(gameObject);
    }

    void PopolaEmescolaDatabase()
    {
        if (fileCSV == null) return;

        database.Clear();
        string[] righe = fileCSV.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, Variabili> datiCSV = new Dictionary<string, Variabili>();

        for (int i = 1; i < righe.Length; i++)
        {
            string[] c = righe[i].Split(',');
            if (c.Length >= 7)
            {
                string id = c[0].Trim();
                if (string.IsNullOrEmpty(id) || datiCSV.ContainsKey(id)) continue;
                datiCSV.Add(id, new Variabili(id, ParseFloat(c[2]), ParseFloat(c[3]), ParseFloat(c[4]), ParseFloat(c[5]), ParseFloat(c[6])));
            }
        }

        // Popolamento griglia
        for (char l = 'A'; l <= 'I'; l++)
        {
            for (int n = 1; n <= 6; n++)
            {
                string nome = l.ToString() + n.ToString();
                if (datiCSV.ContainsKey(nome)) database.Add(nome, datiCSV[nome]);
                else if (datiCSV.ContainsKey("Others")) database.Add(nome, datiCSV["Others"]);
                else database.Add(nome, new Variabili("Sconosciuto", 0, 0, 0, 0, 0));
            }
        }

        // Scambio
        List<string> chiavi = new List<string>(database.Keys);
        foreach (string target in celleDaMischiare)
        {
            if (database.ContainsKey(target))
            {
                string partner = chiavi[Random.Range(0, chiavi.Count)];
                Variabili temp = database[target];
                database[target] = database[partner];
                database[partner] = temp;
            }
        }
    }

    private void StampaTabellaInConsole()
    {
        Debug.Log("--- DATABASE PARAMETRI (Tabella) ---");
        string format = "| {0,-6} | {1,-8} | {2,-8} | {3,-8} | {4,-8} | {5,-8} |";
        Debug.Log(string.Format(format, "Cella", "Knee", "Y0", "M1", "M2", "K"));
        Debug.Log(new string('-', 60));

        foreach (var entry in database)
        {
            var d = entry.Value;
            Debug.Log(string.Format(format, entry.Key, d.knee.ToString("F2"), d.y0.ToString("F2"), d.m1.ToString("F2"), d.m2.ToString("F2"), d.k.ToString("F2")));
        }
    }

    float ParseFloat(string v)
    {
        v = v.Replace(",", ".");
        return float.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out float r) ? r : 0f;
    }

    public Variabili OttieniValori(string id)
    {
        return database.ContainsKey(id) ? database[id] : new Variabili("Sconosciuto", 0, 0, 0, 0, 0);
    }
}