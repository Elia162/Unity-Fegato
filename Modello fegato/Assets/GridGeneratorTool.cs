using UnityEngine;
using UnityEditor;

public class GridGeneratorEditorTool_Alpha : EditorWindow
{
    // Parametri di configurazione ---
    private GameObject targetObject;
    private int columns = 9; // Default 9 (A-I)
    private int rows = 6;    // Default 6 (1-6)

    private const string UndoGroupName = "Generazione Griglia Collider Alpha";

    [MenuItem("Tools/Generatore Griglia Collider")]
    public static void ShowWindow()
    {
        GetWindow<GridGeneratorEditorTool_Alpha>("Griglia Alpha Naming");
    }

    private void OnGUI()
    {
        GUILayout.Label("Configurazione e Generazione Griglia", EditorStyles.boldLabel);

        // Campo per selezionare l'oggetto target
        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Oggetto Target (Silicone)",
            targetObject,
            typeof(GameObject),
            true
        );

        // Campi per le dimensioni della griglia
        columns = EditorGUILayout.IntSlider("Colonne (Lettere A-Z)", columns, 1, 26);
        rows = EditorGUILayout.IntField("Righe (Numeri)", rows);

        // Assicuriamoci che i valori siano validi e limitiamo le colonne a 26 (A-Z)
        columns = Mathf.Clamp(columns, 1, 26);
        rows = Mathf.Max(1, rows);

        EditorGUILayout.Space();

        // Bottone per generare
        if (GUILayout.Button("Genera Collider Griglia"))
        {
            GenerateColliders();
        }

        // Bottone per pulire
        if (GUILayout.Button("Pulisci Collider Figli Generati"))
        {
            CleanupPreviousColliders();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("La griglia utilizza la nomenclatura A1 (basso-sinistra) fino a I6 (alto-destra) (in base alle impostazioni correnti).", MessageType.Info);
    }

    // ---------------------------------------------------------------------------------

    private void GenerateColliders()
    {
        if (targetObject == null)
        {
            Debug.LogError("Seleziona un Oggetto Target.");
            return;
        }

        if (columns > 26)
        {
            Debug.LogError("Il numero di colonne è troppo grande per il naming A-Z.");
            return;
        }

        // 1. OTTENRE I LIMITE (BOUNDS) DEL TARGET
        Bounds bounds = GetObjectBounds(targetObject);
        if (bounds.size == Vector3.zero)
        {
            Debug.LogError($"Impossibile determinare i limiti (Bounds) dell'oggetto '{targetObject.name}'.");
            return;
        }

        // Calcola il nome della cella di inizio (Start Cell Name)
        // Inizio: Colonna A (indice 0), Riga 1 (indice 0)
        char startColChar = (char)('A' + 0);
        int startRowNumber = 0 + 1;
        string startCellName = $"{startColChar}{startRowNumber}"; // Esempio: A1

        // Calcola il nome della cella di fine (End Cell Name)
        // Fine: Ultima Colonna (indice columns - 1), Ultima Riga (indice rows - 1)
        char endColChar = (char)('A' + columns - 1);
        int endRowNumber = rows; // rows è il numero totale di righe (es. 6), che è l'indice j massimo + 1
        string endCellName = $"{endColChar}{endRowNumber}"; // Esempio: I6, D3, Z10

        // Inizio del gruppo Undo
        Undo.SetCurrentGroupName(UndoGroupName);
        int undoGroup = Undo.GetCurrentGroup();

        // Pulisci prima di generare
        CleanupPreviousCollidersInternal();

        // Calcolo delle dimensioni della singola cella nello spazio locale
        Vector3 localSize = targetObject.transform.InverseTransformVector(bounds.size);

        Vector3 cellLocalScale = new Vector3(localSize.x / columns, localSize.y, localSize.z / rows);

        // Calcolo del punto di partenza locale (angolo minimo)
        Vector3 localMin = targetObject.transform.InverseTransformPoint(bounds.min);

        // 2. CICLO PER CREARE I COLLIDER
        for (int i = 0; i < columns; i++) // i = Colonna (Asse X) -> Lettere
        {
            for (int j = 0; j < rows; j++) // j = Riga (Asse Z) -> Numeri
            {
                // *** LOGICA DI NOMINAZIONE ALFANUMERICA ***
                char columnChar = (char)('A' + i);
                int rowNumber = j + 1;
                string cellName = $"{columnChar}{rowNumber}";
                // *******************************************

                // Calcolo della posizione centrale locale della cella
                Vector3 localCenter = new Vector3(
                    localMin.x + (i * cellLocalScale.x) + (cellLocalScale.x / 2f),
                    localMin.y + (cellLocalScale.y / 2f),
                    localMin.z + (j * cellLocalScale.z) + (cellLocalScale.z / 2f)
                );

                // Creazione del GameObject figlio
                GameObject cellGO = new GameObject(cellName);
                cellGO.transform.parent = targetObject.transform;

                // 3. Impostazione della Trasformazione e Collider
                cellGO.transform.localPosition = localCenter;
                cellGO.transform.localRotation = Quaternion.identity;
                cellGO.transform.localScale = Vector3.one;

                BoxCollider boxCol = cellGO.AddComponent<BoxCollider>();
                boxCol.center = Vector3.zero;
                boxCol.size = cellLocalScale;

                // Registra la creazione per l'Undo
                Undo.RegisterCreatedObjectUndo(cellGO, UndoGroupName);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"Generati {columns * rows} Box Collider con nomi da {startCellName} a {endCellName} per: {targetObject.name}");

        // Segna la scena come modificata
        EditorUtility.SetDirty(targetObject);
    }

    // ---------------------------------------------------------------------------------

    // Funzione helper per trovare i limiti dell'oggetto
    private Bounds GetObjectBounds(GameObject obj)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        BoxCollider boxCol = obj.GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            return boxCol.bounds;
        }

        return new Bounds();
    }

    // Funzione interna per la pulizia (chiamata da Genera)
    private void CleanupPreviousCollidersInternal()
    {
        if (targetObject == null)
        {
            return;
        }

        var childrenToDestroy = new System.Collections.Generic.List<GameObject>();

        for (int i = targetObject.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = targetObject.transform.GetChild(i);

            // Cerca BoxCollider generati con naming "0x0" (modulare) o "A1" (alfanumerico)
            bool isGeneratedCollider = child.GetComponent<BoxCollider>() != null && (child.name.Contains("x") || (child.name.Length >= 2 && char.IsLetter(child.name[0]) && char.IsDigit(child.name[1])));

            if (isGeneratedCollider)
            {
                childrenToDestroy.Add(child.gameObject);
            }
        }

        foreach (var child in childrenToDestroy)
        {
            Undo.DestroyObjectImmediate(child);
        }
    }

    // Funzione esterna per la pulizia (chiamata dal pulsante)
    private void CleanupPreviousColliders()
    {
        if (targetObject == null)
        {
            Debug.LogError("Seleziona l'Oggetto Target per la pulizia.");
            return;
        }

        Undo.SetCurrentGroupName("Pulisci Collider Griglia");
        int initialChildCount = targetObject.transform.childCount;

        CleanupPreviousCollidersInternal();

        int finalChildCount = targetObject.transform.childCount;
        int count = initialChildCount - finalChildCount;

        if (count > 0)
        {
            Debug.Log($"Pulizia completata: rimossi {count} Box Collider figli da {targetObject.name}.");
        }
        else
        {
            Debug.LogWarning($"Nessun Box Collider figlio generato trovato su {targetObject.name}.");
        }
    }
}
