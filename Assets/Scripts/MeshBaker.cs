using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MeshBaker : MonoBehaviour
{
    public class MeshStuff
    {
        public Vector3[] vertices;
        public Color[] colors;
        public int[] indeces;

        public MeshStuff(Vector3[] _vertices, Color[] _colors, int[] _indeces)
        {
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
        public Mesh[] meshes;

        public FinishGenerateArgs(Mesh[] _meshes)
        {
            meshes = new Mesh[_meshes.Length];
            Array.Copy(_meshes, meshes, _meshes.Length);
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
        if (recenter && center.HasValue)
            meshesRoot.position = -center.Value;
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
        List<CloudPoint[]> points = new List<CloudPoint[]>() { (CloudPoint[])_points.Clone() };
        GenerateMeshStuffs(points);
    }

    public void SetPoints(List<CloudPoint[]> _points)
    {
        List<CloudPoint[]> points = new List<CloudPoint[]>(_points);
        GenerateMeshStuffs(points);
    }

    async void GenerateMeshStuffs(List<CloudPoint[]> points)
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
                    meshStuffs.Add(new MeshStuff(_vertices, _colors, _indeces));

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

        Mesh[] meshes = new Mesh[meshStuffs.Count];
        for (int i = 0; i < meshes.Length; i++)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = meshStuffs[i].vertices;
            mesh.colors = meshStuffs[i].colors;
            mesh.SetIndices(meshStuffs[i].indeces, MeshTopology.Points, 0);
            meshes[i] = mesh;
        }
        finishGenerate?.Invoke(this, new FinishGenerateArgs(meshes));

        Cleanup();
    }

    public void SetMeshToBake(Mesh[] meshes)
    {
        meshesBuff = (Mesh[])meshes.Clone();
        bake = true;
    }

    private void BakingMeshToNewObject()
    {
        for (int i = 0; i < meshesBuff.Length; i++)
        {
            GameObject child;
            if (meshesRoot)
                child = Instantiate(prefab, meshesRoot);
            else
                child = Instantiate(prefab);

            child.GetComponent<MeshFilter>().sharedMesh = meshesBuff[i];
        }

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
