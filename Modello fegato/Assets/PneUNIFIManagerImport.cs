/**
 * modified form Ardity (Serial Communication for Arduino + Unity)
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using System;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UIElements;


public class PneUNIFIManager : MonoBehaviour
{
    [Header("Serial parameters")]
    public string arduinoPort = "COM3";
    public int baudRate = 115200;
    public float updateSeconds = 1f; // Intervallo di 1 secondo

    [Header("Pressure settings")]
    [Range(0.0f, 50f)]
    public float maxPumpPressure = 20f;

    [Range(0.0f, 40f)]
    public float maxBubblePressure = 15f;

    [Range(0.0f, 30.0f)]
    public float minBubblePressure = 2f;

    [Header("Bubbles' Pressure")]
    [Range(0f, 50f)] // Intervallo del slider
    public float pThumb;
    [Range(0f, 50f)] // Intervallo del slider
    public float pIndex;
    [Range(0f, 50f)] // Intervallo del slider
    public float pMiddle;

    // Variabili per le pressioni precedenti
    private float prevPThumb, prevPIndex, prevPMiddle;
    public string prevMessage;
    // GameObjet sulle tre dita che interagiascono con gli oggetti
    [Header("Spheres on the fingers")]
    [Tooltip("Pallins sull'indice")]
    public GameObject index;
    [Tooltip("Pallina su pollice")]
    public GameObject thumb;
    [Tooltip("Pallina sul medio")]
    public GameObject middle;

    private GameObject handRoot;

    void Start()
    {  
        pThumb = 0f;
        prevPThumb = 0f;
        pIndex = 0f;
        prevPIndex = 0f;
        pMiddle = 0f;
        prevPMiddle = 0f;
        prevMessage = "";

        handRoot = GetRootParentNotActive(index.transform);
        Debug.Log("Il parent ultimo è: " + handRoot.name);

        string[] ports = SerialPort.GetPortNames();
        // Mostra ogni nome della porta alla console
        if (ports.Length > 0)
        {
            Debug.Log("The following serial ports were found:");
            foreach (string port in ports)
            {
                Debug.Log(port);
            }
            // Si connette alla prima porta supponendo che sia Arduino
            arduinoPort = ports[0];
            Connect(arduinoPort);
        }
        else
        {
            Debug.Log("No ports were found:");
        }

    }

    void Update()
    {
        // Se esistono gli indentatori, recupero le pressioni
        pIndex = minBubblePressure;
        pMiddle = minBubblePressure;
        pThumb = minBubblePressure;

        //Debug.Log(handRoot.activeSelf);

        if (handRoot.activeSelf)
        {
            if (index)
            {
                pIndex = Mathf.Clamp(index.GetComponent<MeshDeformerInput>().P, minBubblePressure, maxBubblePressure);
            }
            if (thumb)
            {
                pThumb = Mathf.Clamp(thumb.GetComponent<MeshDeformerInput>().P, minBubblePressure, maxBubblePressure);
            }
            if (middle)
            {
                pMiddle = Mathf.Clamp(middle.GetComponent<MeshDeformerInput>().P, minBubblePressure, maxBubblePressure);
            }
        }
        //invio i valori di pressione
        //timer += Time.deltaTime;
        //if (timer >= updateSeconds)
        //Debug.Log("Running");
            SendPressures(pIndex, pThumb, pMiddle);
       
    }

    //------------------------------------------------------
    // Funzione che invia il valore solo se almeno una delle pressioni è cambiata
    //------------------------------------------------------
    private void SendPressures(float pIndex, float pThumb, float pMiddle)
    {
        
        // Taglio alla prima cifra decimale
        pThumb = (float)Math.Truncate(pThumb * 10) / 10;
        pIndex = (float)Math.Truncate(pIndex * 10) / 10;
        pMiddle = (float)Math.Truncate(pMiddle * 10) / 10;
        // Tronca i valori di pressione alla soglia massima e minimaw
        //pThumb = Mathf.Clamp(pThumb, minBubblePressure, maxBubblePressure);
        //pIndex = Mathf.Clamp(pIndex, minBubblePressure, maxBubblePressure);
        //pMiddle = Mathf.Clamp(pMiddle, minBubblePressure, maxBubblePressure);


        // Se non ci sono nuovi valori non inviare per non intasare il buffer
        
            
        
        
            // Creo il messaggio da inviare 
            string toSend = pThumb + "," + pIndex + "," + pMiddle ;

            
        if (false)
        {
            Debug.Log("no new pressure values to send");
        }
        else
        {
            // Lo invio
            SendSerialMessage(toSend);
            //Debug.Log(toSend);
            // Aggiorno i valori precedenti
            prevPThumb = pThumb;
            prevPIndex = pIndex;
            prevPMiddle = pMiddle;
            prevMessage = toSend;
        }
    }

    //--------------------------------------------------
    // Connessione
    //---------------------------------------------------
    private static SerialPort pneUnifi; // Nome porta seriale
    private static bool connected = false; // Per sapere se connessione è stata stabilita o meno

    public void Connect(string port)
    {
        if (!connected)
        {
            Debug.Log("connect to " + port);
            pneUnifi = new SerialPort(port, baudRate);// Definisce la connessione
            try // Controlla se la connessione funziona
            {
                pneUnifi.Open(); // Apre la comunicazione seriale
                pneUnifi.WriteLine("p." + maxPumpPressure);
                Debug.Log("Pump Enabled - " + maxPumpPressure.ToString() + " kPa set");
                pneUnifi.WriteLine("c1");
                Debug.Log("Control Enabled - Maximum bubble press " + maxBubblePressure.ToString() + " kPa");
                connected = true; 
                                  // Disabilita dropdown menu


            }
            catch (System.Exception e) // In caso di errore di connessione
            {
                Debug.LogWarning("connection error " + e);
            }
        }


    }

    //----------------------------------------------------------------------------------------------------------------
    // Chiusura connessione
    //--------------------------------------------------------------------------------------------------------------------
    public static void CloseConnection()
    {
        if (connected)
        {
            pneUnifi.Close();
            Debug.Log("Connection closed");
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    // Invio messaggio seriale e pulizia buffer
    //-------------------------------------------------------------------------------------------------------------------------------
    public static void SendSerialMessage(string message)
    {
        if (connected)
        {
            //print(message);
            pneUnifi.WriteLine(message); // Invia il messaggio sulla seriale
            pneUnifi.DiscardOutBuffer(); // Pulisce i buffer per aumentare la velocità di comunicazione
            pneUnifi.DiscardInBuffer(); // Pulisce i buffer per aumentare la velocità di comunicazione
        }
        else
        {
            //Debug.LogWarning("Device NOT connected!");
        }


    }

    //--------------------------------------------------------------------
    // Chiude la connessione alla chiusura della scena
    //--------------------------------------------------------------------
    private void OnDestroy()
    {
        if (connected)
        {
            pneUnifi.DiscardOutBuffer();
            SendSerialMessage("0,0,0");
            SendSerialMessage("c0");
            CloseConnection();
        }
    }

    GameObject GetRootParentNotActive(Transform obj)
    {
        while (obj.parent != null)
        {
            obj = obj.parent;
            if (obj.gameObject.activeSelf==false)
            {
                return obj.gameObject;
            }
        }
        return obj.gameObject;
    }

}
