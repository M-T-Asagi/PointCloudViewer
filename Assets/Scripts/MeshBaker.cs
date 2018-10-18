using System;
using System.Threading.Tasks;
using UnityEngine;

public class MeshBaker : MonoBehaviour
{
    public class FinishBakingArgs : EventArgs
    {
    }

    public class FinishGenerateArgs : EventArgs
    {
        public Mesh mesh;

        public FinishGenerateArgs(Mesh _mesh)
        {
            mesh = _mesh;
        }
    }

    [SerializeField]
    GameObject prefab;

    [SerializeField]
    bool recenter = true;

    Vector3[] verticesBuff;
    Color[] colorsBuff;
    int[] indecesBuff;

    bool process;

    Vector3? center = null;

    ParallelOptions options;
    Transform meshesRoot = null;

    public EventHandler<FinishBakingArgs> finishBaking;
    public EventHandler<FinishGenerateArgs> finishGenerate;

    // Update is called once per frame
    void Update()
    {
        if (process)
        {
            if (recenter)
                meshesRoot.position = -center.Value;

            GenerateMeshes();
            process = false;
        }
    }

    public void SetUp()
    {
        meshesRoot = new GameObject().transform;
        meshesRoot.SetParent(transform);
        meshesRoot.localPosition = Vector3.zero;
        meshesRoot.localRotation = Quaternion.identity;

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;
    }

    public void SetPoints(CloudPoint[] _points)
    {
        CloudPoint[] points = new CloudPoint[_points.Length];
        Array.Copy(_points, points, _points.Length);
        GenerateMeshStuffs(points);
        process = true;
    }

    async void GenerateMeshStuffs(CloudPoint[] points)
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
        GenerateMeshes();
    }

    void GenerateMeshes()
    {
        Debug.Log("creating child objects start!");
        Mesh mesh = new Mesh();
        mesh.vertices = verticesBuff;
        mesh.colors = colorsBuff;
        mesh.SetIndices(indecesBuff, MeshTopology.Points, 0);

        finishGenerate?.Invoke(this, new FinishGenerateArgs(mesh));

        Cleanup();
    }

    public void BakeMeshChildToNewObject(Mesh mesh)
    {
        GameObject child = Instantiate(prefab, meshesRoot);
        child.GetComponent<MeshFilter>().sharedMesh = mesh;

        finishBaking?.Invoke(this, new FinishBakingArgs());
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
