using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PointsArranger : MonoBehaviour
{
    public class FinishProcessArgs : EventArgs
    {
        public Dictionary<IndexedVector3, List<CloudPoint>> chunkedPositions;

        public FinishProcessArgs(Dictionary<IndexedVector3, List<CloudPoint>> _chunkedPositions)
        {
            chunkedPositions = new Dictionary<IndexedVector3, List<CloudPoint>>(_chunkedPositions);
        }
    }

    [SerializeField]
    float chunkSize = 10f;
    public float ChunkSize { get { return chunkSize; } }

    public EventHandler<FinishProcessArgs> finishProcess;

    public int ProcessedPointCount { get; private set; }
    public int AllPointCount { get; private set; }

    Dictionary<IndexedVector3, List<CloudPoint>> chunkedPositions;
    ParallelOptions options;

    bool destroyed = false;

    // Use this for initialization
    void Start()
    {
        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;
        chunkedPositions = new Dictionary<IndexedVector3, List<CloudPoint>>();
    }

    public void Setup(GameObject parentObject)
    {
        parentObject.AddComponent<ChunkedMeshesManager>();
    }

    public void Process(Vector3[] _vertices, Color[] _colors)
    {
        Vector3[] vertices = new Vector3[_vertices.Length];
        Array.Copy(_vertices, vertices, _vertices.Length);
        Color[] colors = new Color[_colors.Length];
        Array.Copy(_colors, colors, _colors.Length);
        SetPointsWithVertices(vertices, colors);
    }

    async void SetPointsWithVertices(Vector3[] vertices, Color[] colors)
    {
        await Task.Run(() =>
        {
            return ConvertingProcess(vertices, colors);
        }).ContinueWith((res) =>
        {
            SetPointsAsync(res.Result);
        });
    }

    CloudPoint[] ConvertingProcess(Vector3[] vertices, Color[] colors)
    {
        CloudPoint[] cloudPoints = new CloudPoint[vertices.Length];
        Parallel.For(0, vertices.Length, options, (i, loopState) =>
        {
            lock (Thread.CurrentContext)
                cloudPoints[i] = new CloudPoint(vertices[i], 1, colors[i]);

            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });
        return cloudPoints;
    }

    public void Process(CloudPoint[] points)
    {
        CloudPoint[] cloudPoints = new CloudPoint[points.Length];
        Array.Copy(points, cloudPoints, points.Length);
        SetPoints(cloudPoints);
    }

    async void SetPoints(CloudPoint[] points)
    {
        AllPointCount = points.Length;
        ProcessedPointCount = 0;
        await Task.Run(() => SetPointsAsync(points));
    }

    void SetPointsAsync(CloudPoint[] points)
    {
        Debug.Log("Start chunking.");

        Parallel.For(0, points.Length, options, (i, loopState) =>
        {
            try
            {
                IndexedVector3 index = new IndexedVector3(
                    Mathf.RoundToInt(points[i].point.x / chunkSize), Mathf.RoundToInt(points[i].point.y / chunkSize), Mathf.RoundToInt(points[i].point.z / chunkSize));

                if (!chunkedPositions.ContainsKey(index))
                {
                    lock (Thread.CurrentContext)
                        chunkedPositions[index] = new List<CloudPoint>();
                }

                if (!chunkedPositions[index].Contains(points[i]))
                {
                    lock (Thread.CurrentContext)
                        chunkedPositions[index].Add(new CloudPoint(points[i].point - (index.ToVector3() * chunkSize), 1, points[i].color));
                }

                lock (Thread.CurrentContext)
                    ProcessedPointCount++;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogException(e);
                Debug.LogError("Arrangin points process is Dead!!!!!!!!!!!!!");
            }

            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });

        Debug.Log("Finish one of processes.");

        finishProcess?.Invoke(this, new FinishProcessArgs(chunkedPositions));
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}