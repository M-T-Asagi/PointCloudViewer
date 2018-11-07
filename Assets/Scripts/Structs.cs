using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct CenteredPoints
{
    public List<CloudPoint> points;
    public Vector3 center;

    public CenteredPoints(List<CloudPoint> _points, Vector3 _center)
    {
        points = new List<CloudPoint>(_points);
        center = _center;
    }
}

[System.Serializable]
public struct CenteredMesh
{
    public Mesh mesh;
    public Vector3 center;

    public CenteredMesh(Mesh _mesh, Vector3 _center)
    {
        mesh = _mesh;
        center = _center;
    }
}

[System.Serializable]
public struct MeshStuff
{
    public Vector3 center;
    public Vector3[] vertices;
    public Color[] colors;
    public int[] indeces;
    public int[] triangles;

    public MeshStuff(Vector3 _center, Vector3[] _vertices, Color[] _colors, int[] _triangles, int[] _indeces)
    {
        center = _center;
        vertices = (Vector3[])_vertices.Clone();
        colors = (Color[])_colors.Clone();
        triangles = (int[])_triangles.Clone();
        indeces = (int[])_indeces.Clone();
    }
}