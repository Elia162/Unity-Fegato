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

    [Header("Oggetto da deformare")]
    public GameObject silicone;
    private MeshDeformer deformer;

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
    private float referenceHeightUnity = 0f;
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
        if (silicone != null)
        {
            deformer = silicone.GetComponent<MeshDeformer>();
        }

        BoxCollider box = silicone.GetComponent<BoxCollider>();
        if (box != null)
        {
            referenceHeightUnity = box.size.y * silicone.transform.lossyScale.y;
        }
        else
        {
            referenceHeightUnity = silicone.GetComponent<Collider>().bounds.size.y;
        }

        closestCell = GetComponent<NearestCollisionTracker>();

        UpdatePhalanxDirection();
    }

    void Update()
    {
        UpdatePhalanxDirection();

        if (closestCell.closestObject == null)
        {
            //Debug.Log("Non sto toccando nessuna cella");
        }
     
        if (isTouching)
        {
            string cellaCorrente = closestCell.closestObject.name;
            CalcolaP(cellaCorrente);
            //CalcolaPYeoh(cellaCorrente);

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

    /*
    private void CalcolaPYeoh(string nomeCella)
    {
        var dati = GestoreDatiYeohRandom.Instance.OttieniValori(nomeCella);

        if (indentazione >= 25f)
        {
            return;
        }

        lam = 1f + indentazione / realHeightMM;
        l1 = lam * lam + 2f / lam;
        term_diff = lam - 1f / (lam * lam);
        P = 2f * term_diff * (dati.C1 + 2f * dati.C2 * (l1 - 3f) + 3 * dati.C3 * (l1 - 3f) * (l1 - 3f));

        Debug.Log(closestCell.closestObject.name);
        Debug.Log("----------");
        //Debug.Log(dati.C1 + ", " + dati.C2 + ", " + dati.C3);
        Debug.Log($"P = {P}kPa per la cella {nomeCella} di parametri C1 = {dati.C1}, C2 = {dati.C2} e C3 = {dati.C3}");
    }
    */
   
    private void CalcolaP(string nomeCella)
    {
        var dati = GestoreDatiRandom.Instance.OttieniValori(nomeCella);

        if (indentazione >= 25f)
        {
            return;
        }
        t = (indentazione - dati.knee) / dati.k;
        blend = 1f / (1f + Mathf.Exp(-t));
        P = dati.m1 * indentazione + blend * ((dati.m2 - dati.m1) * (indentazione - dati.knee) + dati.y0);
        blend0 = 1f / (1f + Mathf.Exp(dati.knee / dati.k));
        P -= blend0 * dati.y0;

        Debug.Log(closestCell.closestObject.name);
        //Debug.Log("----------");
        Debug.Log($"P = {P}kPa per la cella {nomeCella} di parametri knee = {dati.knee}, y0 = {dati.y0}, m1 = {dati.m1}, m2 = {dati.m2} e k = {dati.k}");
    }

    private void UpdatePhalanxDirection()
    {
        if (transform.childCount == 0)
        {
            return;
        }

        phalanxDirection = transform.GetChild(0).position - transform.position;
        phalanxDirection.Normalize();
    }

    private void UpdateForceFromTravel()
    {
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
        if (deformer == null || !isTouching)
        {
            return;
        }

        if (appliedForce <= 0f)
        {
            return;
        }

        deformer.AddDeformingForce(hitPoint, phalanxDirection * appliedForce);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != silicone)
        {
            return;
        }

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
        if (other.gameObject != silicone)
        {
            return;
        }

        isTouching = false;
        appliedForce = 0f;
        travelDistance = 0f;
    }


    void OnGUI() 
    {
        GUI.color = Color.black;

        //GUI.Label(new Rect(10, 100, 400, 30), $"Indentazione: {indentazione:F5}mm");
        //GUI.Label(new Rect(10, 175, 400, 30), $"Indentazione massima: {maxIndentazione:F5}mm");
    }
}
