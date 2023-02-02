using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

#if WINDOWS_UWP
using NetworkCommunication;
#else
using System.Net.Sockets;
using System.Threading;
#endif

public class PointCloudReceiver : MonoBehaviour
{
#if WINDOWS_UWP
    TransferSocket socket;
#else
    TcpClient socket = null;
#endif
    private int port = -1;

    PointCloudRenderer pointCloudRenderer;
    bool bReadyForNextFrame = true;
    bool bConnected = false;

    // private System.DateTime time = System.DateTime.Now;

    // threaded receiver
    float[] vertices;
    byte[] colors;
    private Thread receiverThread = null;
    bool pendingRender = false;
    bool pendingDestroy = false;

    //public GameObject ConnectionHandlerPrefab;

    public int multiID = -1;
    private Vector3 position;
    private Vector3 localPosition;

    void Start()
    {
        port = Constants.GetPortByMultiID(multiID);  // TODO single mode cannot run

        Debug.Log(string.Format("MultiID client {0} is trying to connect to server {1} : port {2}\n", multiID, Constants.serverHostName, port));

        pointCloudRenderer = GetComponent<PointCloudRenderer>();

        // store transform
        position = transform.position;
        localPosition = transform.localPosition;
        //Pose thisPose = new Pose();
        //thisPose.position = transform.position;  // + transform.localPosition;  // adding this will not get the right outcome
        //thisPose.rotation = transform.rotation;
        //MultiRenderer.playerPoseList[multiID] = thisPose;

        receiverThread = new Thread(ThreadReceiver);
        receiverThread.Start();
    }

    void Update()
    {
        if (!bConnected)
            return;

        //if (NRInput.IsTouching()) return;  // If touching trackpad, do not render

        // ThreadReceiver keepalive (unused yet)
        //if (receiverThread == null || !receiverThread.IsAlive)
        //{
        //    if (receiverThread != null)
        //        receiverThread.Abort();
        //    receiverThread = new Thread(ThreadReceiver);
        //    receiverThread.Start();
        //}

        // a lot of code removed here
        // receive in thread

        if (pendingRender)
        {
            if (multiID == -1)  // TODO
                pointCloudRenderer.Render(vertices, colors);
            bReadyForNextFrame = true;
            pendingRender = false;
        }

        if (pendingDestroy)
        {
            Debug.Log("initiate destruction");

            // duplicated code
            if (multiID != -1 && multiID < Constants.ArrayCount)
            {
                //Constants.Vertices[multiID] = new[] { 0f, 0f, 0f };
                //Constants.Colors[multiID] = new[] { (byte)0, (byte)0, (byte)0 };
                Constants.Vertices[multiID] = null;
                Constants.Colors[multiID] = null;

                //(GameObject.Find("/MultiRenderer").GetComponent<MultiRenderer>()).Render(null, null, multiID);
                if (MultiRenderer.instance != null)
                    MultiRenderer.instance.Render(null, null, multiID);

                Debug.Log(string.Format("Set multID={0} to null", multiID));
            }

            //if (socket != null)
            //    socket.Close();

            Destroy(gameObject);
        }
    }

    public void Connect(string IP)
    {
        try
        {
#if WINDOWS_UWP
            socket = new NetworkCommunication.TransferSocket(IP, port);
#else
            // https://stackoverflow.com/a/17118710/8448191
            TcpClient tmpClient = new TcpClient();
            var result = tmpClient.BeginConnect(IP, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(1800);
            if (!success)
            {
                throw new Exception("TCP failed to handshake");
            }
            tmpClient.EndConnect(result);

            //socket = new TcpClient(IP, port);  // Eric1932: socket can also encounter errors; 
            socket = tmpClient;

            // eric code
            // shorten socket timeout
            //socket.ReceiveTimeout = 500;
            //socket.SendTimeout = 500;
            //socket.NoDelay = true;
#endif
            bConnected = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning(String.Format("TCP handshake error: {0}", e));

            // duplicated code
            if (multiID != -1 && multiID < Constants.ArrayCount)
            {
                //Constants.Vertices[multiID] = new[] { 0f, 0f, 0f };
                //Constants.Colors[multiID] = new[] { (byte)0, (byte)0, (byte)0 };
                Constants.Vertices[multiID] = null;
                Constants.Colors[multiID] = null;

                (GameObject.Find("/MultiRenderer").GetComponent<MultiRenderer>()).Render(null, null, multiID);

                Debug.Log(string.Format("Set multID={0} to null", multiID));
            }

            if (socket != null)
                socket.Close();

            // stop thread
            if (receiverThread != null)
                receiverThread.Abort();

            Destroy(gameObject);
        }
    }

    //Frame receiving for the editor
#if WINDOWS_UWP
#else
    void RequestFrame()
    {
        byte[] byteToSend = new byte[1];
        byteToSend[0] = 0;

        socket.GetStream().Write(byteToSend, 0, 1);
    }

    int ReadInt()
    {
        byte[] buffer = new byte[4];
        int nRead = 0;
        while (nRead < 4)
            nRead += socket.GetStream().Read(buffer, nRead, 4 - nRead);

        return BitConverter.ToInt32(buffer, 0);
    }

    bool ReceiveFrame(out float[] lVertices, out byte[] lColors)
    {
        IAsyncResult result;
        bool success;

        // int nPointsToRead = ReadInt();
        byte[] buffer0 = new byte[4];
        {
            result = socket.GetStream().ReadAsync(buffer0, 0, 4);
            success = result.AsyncWaitHandle.WaitOne(5000);
            if (!success)
            {
                //throw new Exception("pointcloud transmission error: get data length");

                lVertices = null;
                lColors = null;
                return false;
            }
        }
        int nPointsToRead = BitConverter.ToInt32(buffer0, 0);

        lVertices = new float[3 * nPointsToRead];
        short[] lShortVertices = new short[3 * nPointsToRead];
        lColors = new byte[3 * nPointsToRead];


        int nBytesToRead = sizeof(short) * 3 * nPointsToRead;
        int nBytesRead = 0;
        byte[] buffer = new byte[nBytesToRead];

        while (nBytesRead < nBytesToRead)
        {
            //nBytesRead += socket.GetStream().Read(buffer, nBytesRead, Math.Min(nBytesToRead - nBytesRead, 64000));

            result = socket.GetStream().ReadAsync(buffer, nBytesRead, Math.Min(nBytesToRead - nBytesRead, 64000));
            success = result.AsyncWaitHandle.WaitOne(5000);
            if (!success)
                //throw new Exception("pointcloud transmission error: receive verts");
                return false;
            else
                nBytesRead += ((System.Threading.Tasks.Task<int>)result).Result;
        }

        System.Buffer.BlockCopy(buffer, 0, lShortVertices, 0, nBytesToRead);

        for (int i = 0; i < lShortVertices.Length; i++)
            lVertices[i] = lShortVertices[i] / 1000.0f;

        nBytesToRead = sizeof(byte) * 3 * nPointsToRead;
        nBytesRead = 0;
        buffer = new byte[nBytesToRead];

        while (nBytesRead < nBytesToRead)
        {
            //nBytesRead += socket.GetStream().Read(buffer, nBytesRead, Math.Min(nBytesToRead - nBytesRead, 64000));

            result = socket.GetStream().ReadAsync(buffer, nBytesRead, Math.Min(nBytesToRead - nBytesRead, 64000));
            success = result.AsyncWaitHandle.WaitOne(5000);
            if (!success)
                //throw new Exception("pointcloud transmission error: receive colors");
                return false;
            else
                nBytesRead += ((System.Threading.Tasks.Task<int>)result).Result;
        }

        System.Buffer.BlockCopy(buffer, 0, lColors, 0, nBytesToRead);

        return true;
    }
#endif

    void ThreadReceiver()
    {
        while (true)
        {
            if (socket == null  // if socket not connected || pending render -> do not receive
                || pendingRender
                || Time.smoothDeltaTime >= 0.033)
                continue;

            // eric code
            try
            {
                if (bReadyForNextFrame)  // TODO = pendingRender??
                {
                    //Debug.Log("Requesting frame");
                    // TimeSpan ts = System.DateTime.Now.Subtract(time);
                    // if (ts.Seconds < 0.2) {
                    //     return;
                    // }

#if WINDOWS_UWP
                    socket.RequestFrame();
                    socket.ReceiveFrameAsync();
#else
                    RequestFrame();
#endif
                    bReadyForNextFrame = false;
                }

#if WINDOWS_UWP
                if (socket.GetFrame(out vertices, out colors))
#else
                if (ReceiveFrame(out vertices, out colors))
#endif
                {
                    pendingRender = true;

                    if (multiID != -1 && multiID < Constants.ArrayCount)
                    {
                        if (vertices != null)  // TODO needed?
                        {
                            Constants.Vertices[multiID] = vertices;
                            Constants.Colors[multiID] = colors;
                            // invalidate buffers
                            vertices = null;
                            colors = null;

                            MultiRenderer.flip[multiID] = !MultiRenderer.flip[multiID];
                        }
                    }
                } else
                {
                    Debug.Log("PointCloudReceiver:ReceiveFrame() returns an invalid result");
                    pendingDestroy = true;
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(String.Format("socket receiving frame error! destroy self; port {0}; multiID {1}",
                    port, multiID));
                //Debug.LogError(String.Format(new System.Diagnostics.StackTrace().ToString()));
                Debug.LogException(e);

                // custom error handler
                //ConnectionHandlerPrefab.GetComponent<MyConnectionHandler>().setPrefabActive(false);

                // in case of multi-targets
                // push empty array to flush display
                if (multiID != -1 && multiID < Constants.ArrayCount)
                {
                    Constants.Vertices[multiID] = null;
                    Constants.Colors[multiID] = null;

                    //(GameObject.Find("/MultiRenderer").GetComponent<MultiRenderer>()).Render(null, null, multiID);

                    Debug.Log(string.Format("Set multID={0} to null", multiID));
                }

                // destroy pointcloudrenderer
                pendingDestroy = true;
                break;  // kill self (thread)
                //return;
            }
        }
    }
}
