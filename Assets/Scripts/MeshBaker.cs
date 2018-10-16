using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MeshBaker : MonoBehaviour
{
    [SerializeField]
    GameObject prefab;

    [SerializeField]
    PointCloudPTSViewer pTSViewer;

    [SerializeField]
    bool recenter = true;

    Vector3[] verticesBuff;
    Color[] colorsBuff;
    int[] indecesBuff;

    bool process;

    Vector3? center = null;

    ParallelOptions options;

    private void Start()
    {
        GameObject newObj = new GameObject();
        newObj.transform.SetParent(transform);
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.identity;

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;
    }

    // Update is called once per frame
    void Update()
    {
        if (process)
        {
            if (recenter)
                transform.GetChild(0).position = -center.Value;

            CreateChildObject();
            pTSViewer.Restart();
            process = false;
        }
    }

    public void SetPoints(CloudPoint[] points)
    {
        CreateMesh(points);
        process = true;
    }

    async void CreateMesh(CloudPoint[] points)
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

                if (center == null)
                    center = points[i].point;
                else
                    center = (points[i].point + center) / 2f;
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
