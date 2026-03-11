using System.Collections.Generic;
using UnityEngine;

public class NearestCollisionTracker : MonoBehaviour
{
    private List<GameObject> potentialCollisions = new List<GameObject>();

    public GameObject closestObject = null;

    private const string ignore = "Silicone 6.000";

    void OnTriggerEnter(Collider other)
    {
        if (other.name == ignore)
        {
            return;
        }

        if (!potentialCollisions.Contains(other.gameObject))
        {
            potentialCollisions.Add(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (potentialCollisions.Contains(other.gameObject))
        {
            potentialCollisions.Remove(other.gameObject);
        }
    }

    void Update()
    {
        FindClosestCell();
    }

    private void FindClosestCell()
    {
        if (potentialCollisions.Count == 0)
        {
            closestObject = null;
            return;
        }

        GameObject nearest = null;
        float minDistance = float.MaxValue;
        Vector3 myPosition = transform.position;

        for (int i = potentialCollisions.Count - 1; i >= 0; i--)
        {
            GameObject obj = potentialCollisions[i];

            if (obj == null)
            {
                potentialCollisions.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(myPosition, obj.transform.position);

            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = obj;
            }
        }

        closestObject = nearest;
    }

    /*
    void OnGUI()
    {
        GUI.color = Color.black;

        GUI.Label(new Rect(10, 10, 300, 20), "Cella più vicina:");
        if (closestObject != null)
        {
            GUI.Label(new Rect(10, 30, 300, 20), $"- {closestObject.name}");
        }
        else
        {
            GUI.Label(new Rect(10, 30, 300, 20), "- Nessuna");
        }

        string nomiSovrapposti = "";
        foreach (GameObject obj in potentialCollisions)
        {
            nomiSovrapposti += obj.name + ", "; 
        }

        GUI.Label(new Rect(10, 60, 800, 20), $"Contatto con {potentialCollisions.Count} oggetti: [ {nomiSovrapposti} ]");
    }
    */

}
