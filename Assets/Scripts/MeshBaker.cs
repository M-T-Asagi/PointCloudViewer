using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public class MeshBaker : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;

    [SerializeField]
    PointCloudPTSViewer pTSViewer;

    Vector3[] verticesBuff;
    Color[] colorsBuff;
    int[] indecesBuff;

    bool process;

    private void Start()
    {
        GameObject newObj = new GameObject();
        newObj.transform.SetParent(transform);
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.identity;
    }

    // Update is called once per frame
    void Update()
    {
        if (process)
        {
            CreateChildObject();
            pTSViewer.Restart();
            process = false;
        }
    }

    public void SetPoints(PointCloudPTSViewer.CloudPoint[] points)
    {
        CreateMesh(points);
        process = true;
    }

    async void CreateMesh(PointCloudPTSViewer.CloudPoint[] points)
    {
        Debug.Log("creating meshes start!");
        await Task.Run(() =>
        {
            int _pointCount = points.Length;

            verticesBuff = new Vector3[_pointCount];
            colorsBuff = new Color[_pointCount];
            indecesBuff = new int[_pointCount];

            for (int i = 0; i < _pointCount; i++)
            {
                verticesBuff[i] = points[i].point;
                colorsBuff[i] = points[i].color;
                indecesBuff[i] = i;
            }
        });
    }

    void CreateChildObject()
    {
        Debug.Log("creating child objects start!");
        Mesh mesh = new Mesh();
        mesh.vertices = verticesBuff;
        mesh.colors = colorsBuff;
        mesh.SetIndices(indecesBuff, MeshTopology.Points, 0);

        GameObject child = Instantiate(prefab, transform.GetChild(0));
        child.GetComponent<MeshFilter>().sharedMesh = mesh;

        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    void Cleanup()
    {
        verticesBuff = null;
        colorsBuff = null;
        indecesBuff = null;
    }
}
