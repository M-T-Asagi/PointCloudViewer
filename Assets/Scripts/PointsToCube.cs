using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

public class PointsToCube : MonoBehaviour
{
    public class FinishGeneratingEventArgs : EventArgs
    {
        public Mesh[] generatedMeshes;

        public FinishGeneratingEventArgs(Mesh[] _generatedMshes)
        {
            generatedMeshes = (Mesh[])_generatedMshes.Clone();
        }
    }

    [SerializeField]
    Mesh mesh;
    [SerializeField]
    int maxThreadNum = 4;

    public EventHandler<FinishGeneratingEventArgs> finish;

    public int ProcessedStuffingChunkCount { get; private set; }
    public int AllChunkCount { get; private set; }

    public int ProcessedStuffingPointsCount { get; private set; }
    public int AllOfStuffingPointsCount { get; private set; }

    List<Vector3> prefabPoints;
    List<int> prefabTriangles;
    float size;

    bool generating = false;
    bool destroyed = false;

    List<MeshStuff> meshStuffs;
    ParallelOptions options;

    // Use this for initialization
    void Start()
    {
        GeneratePrefavPoints();
        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = maxThreadNum;

        ProcessedStuffingChunkCount = 0;
        AllChunkCount = 0;

        ProcessedStuffingPointsCount = 0;
        AllOfStuffingPointsCount = 0;
    }

    void GeneratePrefavPoints()
    {
        prefabPoints = new List<Vector3>(mesh.vertices);
        prefabTriangles = new List<int>(mesh.triangles);
        Vector3 center = Vector3.zero;
        float max = 0;

        for (int i = 0; i < prefabPoints.Count; i++)
        {
            center += prefabPoints[i];
        }

        center /= (float)prefabPoints.Count;

        for (int i = 0; i < prefabPoints.Count; i++)
        {
            prefabPoints[i] -= center;
        }

        for (int i = 0; i < prefabPoints.Count; i++)
        {
            if (prefabPoints[i].x > max) max = Mathf.Abs(prefabPoints[i].x);
            if (prefabPoints[i].y > max) max = Mathf.Abs(prefabPoints[i].y);
            if (prefabPoints[i].z > max) max = Mathf.Abs(prefabPoints[i].z);
        }

        Debug.Log("-----Generated prefab points------");

        for (int i = 0; i < prefabPoints.Count; i++)
        {
            prefabPoints[i] /= max * 2f;
            Debug.Log(prefabPoints[i].ToString());
        }

        Debug.Log("-----------");
    }

    public void Process(CloudPoint[] _points, float _size)
    {
        _Process(new List<CloudPoint[]>() { (CloudPoint[])_points.Clone() }, _size);
    }

    public void Process(List<CloudPoint[]> _points, float _size)
    {
        _Process(new List<CloudPoint[]>(_points), _size);
    }

    void _Process(List<CloudPoint[]> _points, float _size)
    {
        Debug.Log("cuing Process is called!");
        size = _size;
        ProcessedStuffingChunkCount = 0;
        AllChunkCount = _points.Count;
        CallGenerateStuff(new List<CloudPoint[]>(_points));
    }

    async void CallGenerateStuff(List<CloudPoint[]> points)
    {
        Debug.Log("created cubed meshes stuffs.");
        await Task.Run(() => GenerateStuff(points));
        generating = true;
    }

    void GenerateStuff(List<CloudPoint[]> points)
    {
        meshStuffs = new List<MeshStuff>();
        foreach (CloudPoint[] buffPoints in points)
        {
            ProcessedStuffingPointsCount = 0;
            AllOfStuffingPointsCount = buffPoints.Length;
            Vector3[] vertices = new Vector3[buffPoints.Length * prefabPoints.Count];
            Color[] colors = new Color[buffPoints.Length * prefabPoints.Count];
            int[] triangles = new int[buffPoints.Length * prefabTriangles.Count];

            Parallel.For(0, buffPoints.Length, options, (i, loopState) =>
            {
                try
                {
                    {
                        Vector3[] _points = GeneratePointedVertex(buffPoints[i].point);
                        for (int k = 0; k < _points.Length; k++)
                            vertices[i * prefabPoints.Count + k] = _points[k];
                    }
                    {
                        int[] _triangles = GenerateTriangles(i);
                        for (int k = 0; k < _triangles.Length; k++)
                            triangles[i * prefabTriangles.Count + k] = _triangles[k];

                    }
                    {
                        Color[] _colors = GenerateColor(buffPoints[i].color);
                        for (int k = 0; k < _colors.Length; k++)
                            colors[i * prefabPoints.Count + k] = _colors[k];
                    }
                    ProcessedStuffingPointsCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Debug.LogException(e);
                    Debug.LogError("Generating cubed mesh stuffs process is Dead!!!!!!!!!!!!!");
                }

                if (destroyed)
                {
                    loopState.Stop();
                    return;
                }
            });

            meshStuffs.Add(new MeshStuff(Vector3.zero, vertices, colors, triangles, new int[0]));
            ProcessedStuffingChunkCount++;
        }
    }

    Vector3[] GeneratePointedVertex(Vector3 point)
    {
        Vector3[] pointsBuff = new Vector3[prefabPoints.Count];
        for (int i = 0; i < prefabPoints.Count; i++)
        {
            pointsBuff[i] = prefabPoints[i] * size + point;
        }
        return pointsBuff;
    }

    Color[] GenerateColor(Color color)
    {
        Color[] colors = new Color[prefabPoints.Count];
        for (int i = 0; i < prefabPoints.Count; i++)
        {
            colors[i] = color;
        }
        return colors;
    }

    int[] GenerateTriangles(int index)
    {
        int[] triangles = new int[prefabTriangles.Count];
        for (int i = 0; i < prefabTriangles.Count; i++)
        {
            triangles[i] = prefabTriangles[i] + index * prefabPoints.Count;
        }
        return triangles;
    }

    // Update is called once per frame
    void Update()
    {
        if (generating)
        {
            GenerateMeshes();
        }
    }

    void GenerateMeshes()
    {
        Debug.Log("Start generate meshes! / " + meshStuffs.Count); ;
        Mesh[] meshes = new Mesh[meshStuffs.Count];

        for (int i = 0; i < meshStuffs.Count; i++)
        {
            meshes[i] = _GenerateMeshes(meshStuffs[i]);
        }
        Debug.Log("Finished generate" + meshStuffs.Count + " meshes!");

        meshStuffs.Clear();
        generating = false;
        finish?.Invoke(this, new FinishGeneratingEventArgs(meshes));
    }

    Mesh _GenerateMeshes(MeshStuff stuff)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = (Vector3[])stuff.vertices.Clone();
        mesh.triangles = (int[])stuff.triangles.Clone();
        mesh.colors = (Color[])stuff.colors.Clone();
        return mesh;
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
