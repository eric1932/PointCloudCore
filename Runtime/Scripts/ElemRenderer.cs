using UnityEngine;
using System.Collections;

public class ElemRenderer : MonoBehaviour
{
    Mesh mesh;

    public void UpdateMesh(float[] arrVertices, byte[] arrColors, int nPointsToRender, int nPointsRendered)
    {
        int nPoints;

        if (arrVertices == null || arrColors == null)
            nPoints = 0;
        else
            nPoints = System.Math.Min(nPointsToRender, (arrVertices.Length / 3) - nPointsRendered);
        nPoints = System.Math.Min(nPoints, 65535);

        float[] normalizedColors = System.Array.ConvertAll(arrColors, x => x / 256f);

        Vector3[] points = new Vector3[nPoints];
        Color[] colors = new Color[nPoints];
        int[] indices = new int[nPoints];
        for (int i = 0; i < nPoints; i++)
        {
            int ptIdx = 3 * (nPointsRendered + i);

            points[i] = new Vector3(arrVertices[ptIdx + 0], arrVertices[ptIdx + 1], -arrVertices[ptIdx + 2]);
            //colors[i] = new Color((float)arrColors[ptIdx + 0] / 256.0f, (float)arrColors[ptIdx + 1] / 256.0f, (float)arrColors[ptIdx + 2] / 256.0f, 1.0f);
            colors[i] = new Color(normalizedColors[ptIdx + 0], normalizedColors[ptIdx + 1], normalizedColors[ptIdx + 2], 1.0f);
            indices[i] = i;
        }

        if (mesh != null)
            Destroy(mesh);
        mesh = new Mesh();
        mesh.vertices = points;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
