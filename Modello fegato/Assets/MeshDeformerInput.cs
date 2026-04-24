using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEditor.ShaderGraph.Legacy;

public class MeshDeformerInput : MonoBehaviour
{
    [Header("Forza")]
    public float force = 100000f;
    public float maxAppliedForce = 5000f;

    [Header("Oggetti da deformare")]
    public List<GameObject> deformables = new List<GameObject>();

    // Cache per gestire le proprietà di ogni oggetto
    private Dictionary<GameObject, MeshDeformer> deformerCache = new Dictionary<GameObject, MeshDeformer>();
    private Dictionary<GameObject, float> heightCache = new Dictionary<GameObject, float>();

    // Riferimento all'oggetto attualmente toccato
    private GameObject currentActiveSilicone;

    private Vector3 phalanxDirection;
    private Vector3 colliderStartPosition;
    private Vector3 hitPoint;
    private Vector3 surfaceNormal;
    private bool isTouching = false;
    private float travelDistance = 0f;
    private float maxTravelDistance = 0f;
    private float normalDirectionTravel = 0f;
    private float appliedForce = 0f;
    private NearestCollisionTracker closestCell;
    private float realHeightMM = 40f;
    private float referenceHeightUnity = 0f; // Usata per il calcolo attuale basato sull'oggetto attivo

    public float indentazione = 0f;
    private float maxIndentazione = 0f;
    public float P;
    private float lam;
    private float l1;
    private float term_diff;
    private float t;
    private float blend;
    private float blend0;

    void Start()
    {
        // Inizializzazione cache per ogni oggetto nella lista
        foreach (GameObject def in deformables)
        {
            if (def != null)
            {
                // Cache Deformer
                deformerCache[def] = def.GetComponent<MeshDeformer>();

                // Cache Altezza
                BoxCollider box = def.GetComponent<BoxCollider>();
                if (box != null)
                {
                    heightCache[def] = box.size.y * def.transform.lossyScale.y;
                }
                else
                {
                    heightCache[def] = def.GetComponent<Collider>().bounds.size.y;
                }
            }
        }

        closestCell = GetComponent<NearestCollisionTracker>();
        UpdatePhalanxDirection();
    }

    void Update()
    {
        UpdatePhalanxDirection();

        if (isTouching && currentActiveSilicone != null)
        {
            // Usa il nome dell'oggetto attivo per i calcoli
            string cellaCorrente = closestCell.closestObject != null ? closestCell.closestObject.name : "Unknown";
            
            CalcolaP(cellaCorrente);
            UpdateForceFromTravel();
            ApplyDeformation();
        }
        else
        {
            travelDistance = 0f;
            indentazione = 0f;
            appliedForce = 0f;
            P = 0f;
        }
    }

    private void CalcolaP(string nomeCella)
    {
        Debug.Log("Contatto con " + nomeCella);
        var dati = GestoreDatiRandom.Instance.OttieniValori(nomeCella);
        //dati.knee = 666f;
        //dati.y0 = 666f;
        //dati.m1 = 666f;
        //dati.m2 = 666f;
        //dati.k = 666f;
        if (indentazione >= 25f) return;

        t = (indentazione - dati.knee) / dati.k;
        blend = 1f / (1f + Mathf.Exp(-t));
        P = dati.m1 * indentazione + blend * ((dati.m2 - dati.m1) * (indentazione - dati.knee) + dati.y0);
        blend0 = 1f / (1f + Mathf.Exp(dati.knee / dati.k));
        P -= blend0 * dati.y0;
        Debug.Log($"P = {P}kPa per la cella {nomeCella} di parametri knee = {dati.knee}, y0 = {dati.y0}, m1 = {dati.m1}, m2 = {dati.m2} e k = {dati.k}");
        // Debug logging opzionale
        // Debug.Log($"P = {P}kPa per la cella {nomeCella}");
    }

    private void UpdatePhalanxDirection()
    {
        if (transform.childCount == 0) return;
        phalanxDirection = transform.GetChild(0).position - transform.position;
        phalanxDirection.Normalize();
    }

    private void UpdateForceFromTravel()
    {
        // Usa l'altezza corretta basata sull'oggetto attualmente toccato
        if (currentActiveSilicone != null && heightCache.ContainsKey(currentActiveSilicone))
        {
            referenceHeightUnity = heightCache[currentActiveSilicone];
        }

        normalDirectionTravel = Vector3.Dot(transform.position - colliderStartPosition, surfaceNormal);
        travelDistance = Vector3.Distance(colliderStartPosition, transform.position);

        if (referenceHeightUnity > 0.0001f)
        {
            indentazione = (travelDistance / referenceHeightUnity) * realHeightMM;
        }
        else
        {
            indentazione = travelDistance * 1000f;
        }

        if (indentazione > maxIndentazione)
        {
            maxIndentazione = indentazione;
            maxTravelDistance = travelDistance;
        }

        float rawForce = force * travelDistance;
        appliedForce = Mathf.Min(rawForce, maxAppliedForce);
    }

    private void ApplyDeformation()
    {
        // Verifica che l'oggetto attivo sia nel dizionario e abbia un deformer
        if (currentActiveSilicone == null || !deformerCache.ContainsKey(currentActiveSilicone) || deformerCache[currentActiveSilicone] == null)
            return;

        if (appliedForce <= 0f) return;

        deformerCache[currentActiveSilicone].AddDeformingForce(hitPoint, phalanxDirection * appliedForce);
    }

    void OnTriggerEnter(Collider other)
    {
        // Controlla se l'oggetto colliso è tra quelli nella lista
        if (!deformables.Contains(other.gameObject)) return;

        currentActiveSilicone = other.gameObject; // Imposta l'oggetto attivo
        colliderStartPosition = transform.position;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, phalanxDirection, out hit, 100f))
        {
            hitPoint = hit.point;
            surfaceNormal = hit.normal * 0.0005f;
        }
        isTouching = true;
    }

    void OnTriggerExit(Collider other)
    {
        // Controlla se è l'oggetto attivo che sta uscendo
        if (currentActiveSilicone == other.gameObject)
        {
            isTouching = false;
            currentActiveSilicone = null; // Resetta l'oggetto attivo
            appliedForce = 0f;
            travelDistance = 0f;
        }
    }
}