using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPReceive : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5052;
    public bool startReceiving = true;
    public string dataString;

    [Serializable]
    public class HandData
    {
        public string gesture;
        public float wrist_x;
        public float wrist_y;
    }

    [Serializable]
    public class TelemetryPayload
    {
        public HandData[] hands; 
    }

    public TelemetryPayload currentData = new TelemetryPayload();

    void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (startReceiving)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] dataByte = client.Receive(ref anyIP); // Initial grab

                // PIPELINE OVERRIDE: THE BUFFER FLUSH
                // Drains the traffic jam. Unity will now only read real-time coordinates.
                while (client.Available > 0)
                {
                    dataByte = client.Receive(ref anyIP);
                }

                dataString = Encoding.UTF8.GetString(dataByte);
                currentData = JsonUtility.FromJson<TelemetryPayload>(dataString);
            }
            catch (Exception)
            {
                // Silently handle expected thread terminations on exit
            }
        }
    }

    void OnDisable()
    {
        startReceiving = false;
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}