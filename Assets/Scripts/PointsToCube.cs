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

    Vector3[] prefabPoints;
    int[] prefabTriangles;
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
        prefabPoints = (Vector3[])mesh.vertices.Clone();
        prefabTriangles = (int[])mesh.triangles.Clone();
        Vector3 center = Vector3.zero;
        float max = 0;

        for (int i = 0; i < prefabPoints.Length; i++)
        {
                center += prefabPoints[i];
        }

        center /= (float)prefabPoints.Length;

        for (int i = 0; i < prefabPoints.Length; i++)
        {
            prefabPoints[i] -= center;
        }

        for (int i = 0; i < prefabPoints.Length; i++)
        {
            if (prefabPoints[i].x > max) max = Mathf.Abs(prefabPoints[i].x);
            if (prefabPoints[i].y > max) max = Mathf.Abs(prefabPoints[i].y);
            if (prefabPoints[i].z > max) max = Mathf.Abs(prefabPoints[i].z);
        }

        Debug.Log("-----Generated prefab points------");

        for (int i = 0; i < prefabPoints.Length; i++)
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
        Vector3[] vertices = new Vector3[prefabPoints.Length * _points.Length];
        int[] triangles = new int[prefabTriangles.Length * _points.Length];
        Color[] colors = new Color[prefabPoints.Length * _points.Length];

        Debug.Log("Process " + _points.Length + " points!");

        for (int i = 0; i < _points.Length; i++)
        {
            Array.Copy(GeneratePointedVertex(_points[i].point), 0, vertices, i * prefabPoints.Length, prefabPoints.Length);
            Array.Copy(prefabTriangles, 0, triangles, i * prefabTriangles.Length, prefabTriangles.Length);
            Array.Copy(GenerateColor(_points[i].color), 0, colors, i * prefabPoints.Length, prefabPoints.Length);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        return mesh;
    }

    Vector3[] GeneratePointedVertex(Vector3 point)
    {
        Vector3[] pointsBuff = (Vector3[])prefabPoints.Clone();
        for (int i = 0; i < pointsBuff.Length; i++)
        {
            pointsBuff[i] = pointsBuff[i] * size + point;
        }
        return pointsBuff;
    }

    Color[] GenerateColor(Color color)
    {
        Color[] colors = new Color[prefabPoints.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }
        return colors;
    }
}
