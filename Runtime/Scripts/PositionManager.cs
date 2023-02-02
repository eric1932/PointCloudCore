using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Proyecto26;
using System;
using System.Linq;

public class PositionManager : MonoBehaviour
{
    private int MyPlayerID = Constants.MyOnlineID;
    [HideInInspector]
    public static int[] PositionData { get; private set; } = new int[] { 0, 1, 2 };  // default value in case of net err
    [HideInInspector]
    public static bool DataChanged = false;
    // left, mid, right
    public float UpdateInterval = 5;
    private float LastUpdatTime = 0;
    // something like 0: {1: 0, 2: 1, 3: 2}; 1: {0: 0, 2: 1, 3: 2}
    // remote ID -> local ID mapping
    private readonly Dictionary<int, int> map = new Dictionary<int, int>();

    // Start is called before the first frame update
    void Start()
    {
        int localIndex = 0;
        for (int remoteID = 0; remoteID < Constants.NumClients + 1; ++remoteID)
            if (remoteID != MyPlayerID)
                map[remoteID] = localIndex++;


        // initialize PositionData
        switch (MyPlayerID)
        {
            case 0:
                //PositionData = new int[] { 0, 1, 2 };
                break;
            case 1:
                PositionData = new int[] { 0, 2, 1 };
                break;
            case 2:
                PositionData = new int[] { 2, 0, 1 };
                break;
            case 3:
                PositionData = new int[] { 2, 1, 0 };
                break;
            default:
                throw new ArgumentException("Map not configured for this playerID");
        }

        fetchOnlineData();

        // debug
        // print Dict<int, int>
        //var lines = map.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
        //Debug.Log(string.Join(Environment.NewLine, lines));
    }

    // Update is called once per frame
    void Update()
    {
        if (LastUpdatTime + UpdateInterval < Time.time && !MyConnectionHandler.AllInstancesOffline())
        {
            fetchOnlineData();
            LastUpdatTime = Time.time;
        }
    }

    private void fetchOnlineData()
    {
        RestClient.Get(string.Format("{0}/config/{1}", Constants.APIHostname, MyPlayerID)).Then(response =>
        {
            Debug.Log(string.Format("API position return: {0}\n", response.Text));
            int[] newData = remoteIdxToLocalIdx(StringToIntArray(response.Text));
            Debug.Log(string.Format("Translated position: [{0}, {1}, {2}]\n", newData[0], newData[1], newData[2]));

            if (!Enumerable.SequenceEqual(PositionData, newData))
            {
                PositionData = newData;
                DataChanged = true;
            }
        }).Catch(exception => { Debug.LogException(exception); });
    }

    public int[] remoteIdxToLocalIdx(int[] remotePosition)
    {
        //return Array.ConvertAll(remotePosition, delegate (int i) { return PositionData[i]; });
        int[] r = new int[remotePosition.Length];
        for (int i = 0; i < r.Length; ++i)
        {
            r[i] = map[remotePosition[i]];
        }
        return r;
    }

    public static int[] StringToIntArray(string input)
    {
        return Array.ConvertAll(input.Replace("[", "").Replace("]", "").Split(','), int.Parse);
    }
}
