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

    bool generate = false;
    bool bake = false;

    Vector3? center = null;

    ParallelOptions options;
    Transform meshesRoot = null;

    Mesh meshBuff;

    public EventHandler<FinishBakingArgs> finishBaking;
    public EventHandler<FinishGenerateArgs> finishGenerate;

    // Update is called once per frame
    void Update()
    {
        if (generate)
        {
            if (recenter)
                meshesRoot.position = -center.Value;

            GenerateMeshes();
            generate = false;
        }
    }

    public void SetUp(Transform _root = null)
    {
        if (_root)
        {
            meshesRoot = _root;
            meshesRoot.localPosition = Vector3.zero;
            meshesRoot.localRotation = Quaternion.identity;
        }

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;
    }

    public void SetPoints(CloudPoint[] _points)
    {
        CloudPoint[] points = new CloudPoint[_points.Length];
        Array.Copy(_points, points, _points.Length);
        GenerateMeshStuffs(points);

        generate = true;
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

    public void SetMeshToBake(Mesh mesh)
    {
        meshBuff = mesh;
        bake = true;
    }

    private void BakingMeshToNewObject()
    {
        GameObject child;
        if (meshesRoot)
            child = Instantiate(prefab, meshesRoot);
        else
            child = Instantiate(prefab);

        child.GetComponent<MeshFilter>().sharedMesh = meshBuff;

        meshBuff = null;
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
