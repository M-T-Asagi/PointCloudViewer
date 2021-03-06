﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MeshBaker : MonoBehaviour
{
    public class FinishBakingArgs : EventArgs
    {
        public List<GameObject> gameObjects;

        public FinishBakingArgs(List<GameObject> _gameObjects)
        {
            gameObjects = new List<GameObject>(_gameObjects);
        }
    }

    public class FinishGenerateArgs : EventArgs
    {
        public List<CenteredMesh> meshes;

        public FinishGenerateArgs(List<CenteredMesh> _meshes)
        {
            meshes = new List<CenteredMesh>(_meshes);
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

    ParallelOptions options;
    Transform meshesRoot = null;

    List<CenteredMesh> meshes;

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
        }

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;
    }

    public void SetPoints(CloudPoint[] _points, Vector3? _center = null)
    {
        List<CloudPoint[]> points = new List<CloudPoint[]>() { (CloudPoint[])_points.Clone() };
        List<Vector3> _centers = new List<Vector3>() { _center.HasValue ? _center.Value : Vector3.zero };
        ConvertPointsToCenteredPoints(points, _centers);
    }

    public void SetPoints(List<CloudPoint[]> _points, List<Vector3> _centers = null)
    {
        List<CloudPoint[]> points = new List<CloudPoint[]>(_points);
        ConvertPointsToCenteredPoints(points, (_centers != null ? _centers : new List<Vector3>()));
    }

    void ConvertPointsToCenteredPoints(List<CloudPoint[]> _points, List<Vector3> _centers)
    {
        List<CenteredPoints> points = new List<CenteredPoints>();
        for (int i = 0; i < _points.Count; i++)
        {
            points.Add(new CenteredPoints(new List<CloudPoint>(_points[i]), (_centers.Count > i ? _centers[i] : Vector3.zero)));
        }
        GenerateMeshStuffs(points);
    }

    public void SetPoints(List<CenteredPoints> _points)
    {
        GenerateMeshStuffs(new List<CenteredPoints>(_points));
    }

    async void GenerateMeshStuffs(List<CenteredPoints> points)
    {
        Debug.Log("creating meshes start!");
        await Task.Run(() =>
        {
            meshStuffs = new List<MeshStuff>();

            Parallel.For(0, points.Count, options, (i, loopState) =>
            {
                int pointCount = points[i].points.Count;
                Vector3[] _vertices = new Vector3[pointCount];
                Color[] _colors = new Color[pointCount];
                int[] _indeces = new int[pointCount];

                for (int k = 0; k < pointCount; k++)
                {
                    _vertices[k] = points[i].points[k].point;
                    _colors[k] = points[i].points[k].color;
                    _indeces[k] = k;
                }

                lock (Thread.CurrentContext)
                    meshStuffs.Add(new MeshStuff(points[i].center, _vertices, _colors, new int[0], _indeces));

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

        List<CenteredMesh> generatedMeshes = new List<CenteredMesh>();

        for (int i = 0; i < meshStuffs.Count; i++)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = meshStuffs[i].vertices;
            mesh.colors = meshStuffs[i].colors;
            mesh.SetIndices(meshStuffs[i].indeces, MeshTopology.Points, 0);
            generatedMeshes.Add(new CenteredMesh(mesh, meshStuffs[i].center));
        }

        finishGenerate?.Invoke(this, new FinishGenerateArgs(generatedMeshes));
        Cleanup();
    }

    public void SetMeshToBake(List<CenteredMesh> _meshes)
    {
        meshes = new List<CenteredMesh>(_meshes);
        bake = true;
    }

    private void BakingMeshToNewObject()
    {
        List<GameObject> objects = new List<GameObject>();
        for (int i = 0; i < meshes.Count; i++)
        {
            GameObject child;
            if (meshesRoot)
            {
                child = Instantiate(prefab, meshesRoot);
                child.transform.localPosition = meshes[i].center;
            }
            else
            {
                child = Instantiate(prefab);
                child.transform.position = meshes[i].center;
            }

            child.GetComponent<MeshFilter>().sharedMesh = meshes[i].mesh;
            objects.Add(child);
        }

        meshes = null;
        finishBaking?.Invoke(this, new FinishBakingArgs(objects));
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
