using UnityEngine;
using System.Collections.Generic;

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