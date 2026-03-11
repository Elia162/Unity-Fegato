using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridGeneratorRuntime : MonoBehaviour
{
    [Header("Impostazioni Griglia")]
    [Tooltip("Numero di divisioni lungo l'asse X (Lettere: A, B, C...).")]
    [Range(1, 26)]
    public int columns = 9;

    [Tooltip("Numero di divisioni lungo l'asse Z (Numeri: 1, 2, 3...).")]
    [Min(1)]
    public int rows = 6;

    [Header("Visualizzazione")]
    public bool showGridGizmos = true;
    public Color gridColor = Color.white;
    public Color labelColor = Color.white;

    [Range(0.005f, 3f)]
    public float lineThickness = 2f;

    [Range(8, 40)]
    public int fontSize = 16;

    [Tooltip("Distanza delle etichette dal bordo della griglia")]
    [Range(0.1f, 5f)]
    public float labelOffset = 0.5f; // Nuova variabile per controllare la distanza

    private void OnDrawGizmos()
    {
        if (!showGridGizmos)
        {
            return;
        }

        DrawGridInternal();
    }

    private void DrawGridInternal()
    {
#if UNITY_EDITOR
        // 1. TROVIAMO LE DIMENSIONI LOCALI
        Vector3 localCenter = Vector3.zero;
        Vector3 localSize = Vector3.one;
        bool foundBounds = false;

        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            localCenter = boxCol.center;
            localSize = boxCol.size;
            foundBounds = true;
        }
        else
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                localCenter = meshFilter.sharedMesh.bounds.center;
                localSize = meshFilter.sharedMesh.bounds.size;
                foundBounds = true;
            }
        }

        if (!foundBounds)
        {
            return;
        }

        // 2. SETUP VARIABILI
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix; // Usiamo la matrice per le linee

        // Calcoli geometrici locali
        Vector3 localStartPoint = localCenter - (localSize * 0.5f);
        float cellWidth = localSize.x / columns;
        float cellLength = localSize.z / rows;
        float yTop = localStartPoint.y + localSize.y;

        // Limiti locali per posizionamento testo
        float xMin = localStartPoint.x;
        float xMax = localStartPoint.x + localSize.x;
        float zMin = localStartPoint.z;
        float zMax = localStartPoint.z + localSize.z;

        Handles.color = gridColor;

        // 3. DISEGNO GRIGLIA (LINEE)
        // Verticali
        /*
        for (int i = 0; i <= columns; i++)
        {
            float xPos = xMin + (i * cellWidth);
            Vector3 start = new Vector3(xPos, yTop, zMin);
            Vector3 end = new Vector3(xPos, yTop, zMax);
            Handles.DrawLine(transform.TransformPoint(start), transform.TransformPoint(end), lineThickness);
        }
        // Orizzontali
        for (int j = 0; j <= rows; j++)
        {
            float zPos = zMin + (j * cellLength);
            Vector3 start = new Vector3(xMin, yTop, zPos);
            Vector3 end = new Vector3(xMax, yTop, zPos);
            Handles.DrawLine(transform.TransformPoint(start), transform.TransformPoint(end), lineThickness);
        }
        */

        // 4. DISEGNO ETICHETTE (TESTO)
        GUIStyle style = new GUIStyle();
        style.normal.textColor = labelColor;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // --- LETTERE (Colonne A, B...) ---
        for (int i = 0; i < columns; i++)
        {
            char c = (char)('A' + i);
            float xCenter = xMin + (i * cellWidth) + (cellWidth * 0.5f);

            // Basso (Z Minima)
            Vector3 posBottom = new Vector3(xCenter, yTop, zMin - labelOffset);
            Handles.Label(transform.TransformPoint(posBottom), c.ToString(), style);

            // Alto (Z Massima) 
            Vector3 posTop = new Vector3(xCenter, yTop, zMax + labelOffset);
            Handles.Label(transform.TransformPoint(posTop), c.ToString(), style);
        }

        // --- NUMERI (Righe 1, 2...) ---
        for (int j = 0; j < rows; j++)
        {
            int num = j + 1;
            float zCenter = zMin + (j * cellLength) + (cellLength * 0.5f);

            // Sinistra (x Minima)
            Vector3 posLeft = new Vector3(xMin - labelOffset, yTop, zCenter);
            Handles.Label(transform.TransformPoint(posLeft), num.ToString(), style);

            // Destra (x Massima) 
            Vector3 posRight = new Vector3(xMax + labelOffset, yTop, zCenter);
            Handles.Label(transform.TransformPoint(posRight), num.ToString(), style);
        }

        Gizmos.matrix = originalMatrix;
#endif
    }
}
