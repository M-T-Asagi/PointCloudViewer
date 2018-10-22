using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MeshBaker : MonoBehaviour
{
    public class MeshStuff
    {
        public Vector3 center;
        public Vector3[] vertices;
        public Color[] colors;
        public int[] indeces;

        public MeshStuff(Vector3 _center, Vector3[] _vertices, Color[] _colors, int[] _indeces)
        {
            center = _center;
            vertices = (Vector3[])_vertices.Clone();
            colors = (Color[])_colors.Clone();
            indeces = (int[])_indeces.Clone();
        }
    }

    public class FinishBakingArgs : EventArgs
    {
    }

    public class FinishGenerateArgs : EventArgs
    {
        public Vector3[] centers;
        public Mesh[] meshes;

        public FinishGenerateArgs(Vector3[] _centers, Mesh[] _meshes)
        {
            centers = (Vector3[])_centers.Clone();
            meshes = (Mesh[])_meshes.Clone();
        }
    }

    [SerializeField]
    GameObject prefab;

    [SerializeField]
    bool recenter = true;

    List<MeshStuff> meshStuffs;

    bool generate = false;
    bool bake = false;
    bool destroyed = false;

    Vector3? center = null;

    ParallelOptions options;
    Transform meshesRoot = null;

    Mesh[] meshesBuff;
    Vector3[] meshesCenter;

    public EventHandler<FinishBakingArgs> finishBaking;
    public EventHandler<FinishGenerateArgs> finishGenerate;

    // Update is called once per frame
    void Update()
    {
        if (generate)
        {
            GenerateMeshes();
            generate = false;
        }
        if (bake)
        {
            BakingMeshToNewObject();
            bake = false;
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

    public void SetPoints(CloudPoint[] _points, Vector3? _center = null)
    {
        List<CloudPoint[]> points = new List<CloudPoint[]>() { (CloudPoint[])_points.Clone() };
        List<Vector3> _centers = new List<Vector3>() { _center.HasValue ? _center.Value : Vector3.zero };
        GenerateMeshStuffs(points, _centers);
    }

    public void SetPoints(List<CloudPoint[]> _points, List<Vector3> _centers = null)
    {
        List<CloudPoint[]> points = new List<CloudPoint[]>(_points);
        GenerateMeshStuffs(points, _centers);
    }

    async void GenerateMeshStuffs(List<CloudPoint[]> points, List<Vector3> centers)
    {
        Debug.Log("creating meshes start!");
        await Task.Run(() =>
        {
            meshStuffs = new List<MeshStuff>();

            Parallel.For(0, points.Count, options, (i, loopState) =>
            {
                int pointCount = points[i].Length;
                Vector3[] _vertices = new Vector3[pointCount];
                Color[] _colors = new Color[pointCount];
                int[] _indeces = new int[pointCount];

                for (int k = 0; k < pointCount; k++)
                {
                    _vertices[k] = points[i][k].point;
                    _colors[k] = points[i][k].color;
                    _indeces[k] = k;
                }

                lock (Thread.CurrentContext)
                    meshStuffs.Add(new MeshStuff(centers[i], _vertices, _colors, _indeces));

                if (destroyed)
                {
                    loopState.Stop();
                    return;
                }
            });
        });
        generate = true;
    }

    void GenerateMeshes()
    {
        Debug.Log("creating child objects start!");

        Vector3[] centers = new Vector3[meshStuffs.Count];
        Mesh[] meshes = new Mesh[meshStuffs.Count];
        for (int i = 0; i < meshes.Length; i++)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = meshStuffs[i].vertices;
            mesh.colors = meshStuffs[i].colors;
            mesh.SetIndices(meshStuffs[i].indeces, MeshTopology.Points, 0);
            meshes[i] = mesh;

            centers[i] = meshStuffs[i].center;
            if (center.HasValue)
                center = (center + centers[i]) / 2f;
            else
                center = centers[i];
        }

        finishGenerate?.Invoke(this, new FinishGenerateArgs(centers, meshes));
        Cleanup();
    }

    public void SetMeshToBake(Vector3[] centers, Mesh[] meshes)
    {
        meshesBuff = (Mesh[])meshes.Clone();
        meshesCenter = (Vector3[])centers.Clone();
        bake = true;
    }

    private void BakingMeshToNewObject()
    {
        for (int i = 0; i < meshesBuff.Length; i++)
        {
            GameObject child;
            if (meshesRoot)
            {
                child = Instantiate(prefab, meshesRoot);
                child.transform.localPosition = meshesCenter[i];
            }
            else
            {
                child = Instantiate(prefab);
                child.transform.position = meshesCenter[i];
            }

            child.GetComponent<MeshFilter>().sharedMesh = meshesBuff[i];
        }

        meshesRoot.position = -center.Value;

        meshesBuff = null;
        finishBaking?.Invoke(this, new FinishBakingArgs());
    }

    private void OnDestroy()
    {
        destroyed = true;
        Cleanup();
    }

    void Cleanup()
    {
        meshStuffs = null;
    }
}
