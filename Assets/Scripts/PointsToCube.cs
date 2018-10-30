using UnityEngine;
using System;
using System.Collections.Generic;

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

    public EventHandler<FinishGeneratingEventArgs> finish;

    List<Vector3> prefabPoints;
    List<int> prefabTriangles;
    float size;

    bool generating = false;

    List<CloudPoint[]> points;

    // Use this for initialization
    void Start()
    {
        GeneratePrefavPoints();
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
        points = new List<CloudPoint[]>(_points);
        size = _size;
        generating = true;
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
        Debug.Log("Start generate meshes! / " + points.Count);
        Mesh[] meshes = new Mesh[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            meshes[i] = _GenerateMeshes(points[i]);
        }
        Debug.Log("Finished generate" + points.Count + " meshes!");

        generating = false;
        finish?.Invoke(this, new FinishGeneratingEventArgs(meshes));
    }

    Mesh _GenerateMeshes(CloudPoint[] _points)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        Debug.Log("Process " + _points.Length + " points!");

        for (int i = 0; i < _points.Length; i++)
        {
            vertices.AddRange(GeneratePointedVertex(_points[i].point));
            triangles.AddRange(GenerateTriangles(i));
            colors.AddRange(GenerateColor(_points[i].color));
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();

        return mesh;
    }

    List<Vector3> GeneratePointedVertex(Vector3 point)
    {
        List<Vector3> pointsBuff = new List<Vector3>();
        for (int i = 0; i < prefabPoints.Count; i++)
        {
            pointsBuff.Add(prefabPoints[i] * size + point);
        }
        return pointsBuff;
    }

    List<Color> GenerateColor(Color color)
    {
        List<Color> colors = new List<Color>();
        for (int i = 0; i < prefabPoints.Count; i++)
        {
            colors.Add(color);
        }
        return colors;
    }

    List<int> GenerateTriangles(int index)
    {
        List<int> triangles = new List<int>();
        for (int i = 0; i < prefabTriangles.Count; i++)
        {
            triangles.Add(prefabTriangles[i] + index * prefabPoints.Count);
        }
        return triangles;
    }
}
