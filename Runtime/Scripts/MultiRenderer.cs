using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MultiRenderer : MonoBehaviour
{
    public int maxChunkSize = 65535;
    // public int maxNumPoints = 50000;
    public float pointSize = 0.005f;
    public GameObject pointCloudElem;
    public Material pointCloudMaterial;
    private readonly List<List<GameObject>> elemsList = new List<List<GameObject>>();
    [HideInInspector]
    public static readonly Pose[] playerPoseList = new Pose[Constants.NumClients];

    public static MultiRenderer instance = null;

    int iterCount = 0;

    public static readonly bool[] flip = new bool[Constants.NumClients];
    private static readonly bool[] flop = new bool[Constants.NumClients];
    //private static readonly float[] lastUpdateTime = new float[NumClients];

    private static readonly Material[] materials = new Material[Constants.NumClients];

    void Start()
    {
        instance = this;

        for (int i = 0; i < Constants.NumClients; i++)
        {
            elemsList.Add(new List<GameObject>());
            // duplicate N material containing shaders with different coord offsets
            materials[i] = new Material(pointCloudMaterial);
        }

        UpdatePointSize();
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            UpdatePointSize();
            transform.hasChanged = false;
        }

        /*
         * FPS / DeltaTime
         * 62.5 / 0.016
         * 50 / 0.02
         * 40 / 0.25
         * 30 / 0.0333...
         */
        if (Time.smoothDeltaTime >= 0.033)
        {
            //Debug.Log("Skip frame");
            return;
        }

        try
        {
            //if (lastUpdateTime[iterCount] + 8000 < Time.time)
            //{
            //    // destroy gameobj
            //    GameObject obj = GameObject.Find("/GameObject(Clone)/PointCloudRenderer1(Clone)");
            //    if (obj != null)
            //    {
            //        DestroyImmediate(obj);
            //        Debug.LogWarning("Destroy upon MultiRender timeout");
            //    }

            //    // update time
            //    lastUpdateTime[iterCount] = Time.time;

            //    // turn into finally
            //    return;
            //}

            // render ONLY ONE pointcloud each time
            if (Constants.Vertices.Count > iterCount && flip[iterCount] != flop[iterCount])
            {
                Render(Constants.Vertices[iterCount], Constants.Colors[iterCount], iterCount);

                // update flip-flop flag
                flop[iterCount] = !flop[iterCount];

                //lastUpdateTime[iterCount] = Time.time;
            }  // else save frame; do not update time
        }
        finally
        {
            iterCount += 1;
            if (iterCount >= Constants.NumClients)  // range within 0 1 2
                iterCount = 0;
        }
    }

    void UpdatePointSize()
    {
        // comment out as the original material is not used by any instance
        //pointCloudMaterial.SetFloat("_PointSize", pointSize * transform.localScale.x);
        for (int i = 0; i < materials.Length; i++)
            materials[i].SetFloat("_PointSize", pointSize * transform.localScale.x);
    }

    public void Render(float[] arrVertices, byte[] arrColors, int elemsIdx)
    {
        int nPoints, nChunks;
        if (arrVertices == null || arrColors == null)
        {
            nPoints = 0;
            nChunks = 0;
        }
        else
        {
            nPoints = arrVertices.Length / 3;
            nChunks = 1 + nPoints / maxChunkSize;
        }

        // makes elems has Count=nChunks
        if (elemsList[elemsIdx].Count < nChunks)
            AddElems(nChunks - elemsList[elemsIdx].Count, elemsIdx);
        if (elemsList[elemsIdx].Count > nChunks)
            RemoveElems(elemsList[elemsIdx].Count - nChunks, elemsIdx);

        int offset = 0;
        Pose targetPose = playerPoseList[Array.IndexOf(PositionManager.PositionData, elemsIdx)];
        for (int i = 0; i < nChunks; i++)
        {
            int nPointsToRender = Math.Min(maxChunkSize, nPoints - offset);

            ElemRenderer renderer = elemsList[elemsIdx][i].GetComponent<ElemRenderer>();  // TODO no indexof
            // update transform
            if (renderer.transform.localPosition != targetPose.position)
            {
                renderer.transform.localPosition = targetPose.position;

                // Quest patch: manually offsets to shaders
                // ~~NOTE: below method also works on Windows, but the above one cannot work on Quest 2~~
                // UPDATE: the above method is good to go EVERYWHERE
//#if !UNITY_EDITOR
//                materials[elemsIdx].SetVector("_Offset", new Vector4(-targetPose.position.x, -targetPose.position.y, -targetPose.position.z));
//                elemsList[elemsIdx][i].GetComponent<MeshRenderer>().material = materials[elemsIdx];
//#endif
            }
            if (renderer.transform.localRotation != targetPose.rotation)
                renderer.transform.localRotation = targetPose.rotation * Quaternion.Euler(-180, 0, 0);  // TODO patch
            renderer.UpdateMesh(arrVertices, arrColors, nPointsToRender, offset);

            offset += nPointsToRender;

            // if (offset >= maxNumPoints) break;
        }
        //Debug.Log(offset);
    }

    void AddElems(int nElems, int elemsIdx)
    {
        for (int i = 0; i < nElems; i++)
        {
            GameObject newElem = GameObject.Instantiate(pointCloudElem);
            newElem.transform.parent = transform;
            // #if UNITY_EDITOR
            newElem.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            // #else
            //             newElem.transform.localPosition = new Vector3(0.0f, 0.0f, 0.2f);
            // #endif
            //newElem.transform.localRotation = Quaternion.identity;
            newElem.transform.localRotation = Quaternion.Euler(0, 180, 180);  // Fix rotation
            newElem.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            elemsList[elemsIdx].Add(newElem);
        }
    }

    void RemoveElems(int nElems, int elemsIdx)
    {
        for (int i = 0; i < nElems; i++)
        {
            Destroy(elemsList[elemsIdx][0]);
            elemsList[elemsIdx].Remove(elemsList[elemsIdx][0]);
        }
    }
}
