using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PointsArranger : MonoBehaviour
{
    public class FinishArrangingArgs : EventArgs
    {
        public Dictionary<IndexedVector3, List<CloudPoint>> arrangedPoints;

        public FinishArrangingArgs(Dictionary<IndexedVector3, List<CloudPoint>> _arrangedPoints)
        {
            arrangedPoints = new Dictionary<IndexedVector3, List<CloudPoint>>(_arrangedPoints);
        }
    }

    public class FinishProcessArgs : EventArgs
    {
        public Dictionary<IndexedVector3, CenteredPoints> chunkedPoints;

        public FinishProcessArgs(Dictionary<IndexedVector3, CenteredPoints> _chunkedPoints)
        {
            chunkedPoints = new Dictionary<IndexedVector3, CenteredPoints>(_chunkedPoints);
        }
    }

    [SerializeField]
    float chunkSize = 10f;
    public float ChunkSize { get { return chunkSize; } }

    [SerializeField]
    int maxThreadNum = 4;

    public EventHandler<FinishArrangingArgs> finishArranging;
    public EventHandler<FinishProcessArgs> finishProcess;

    public int ProcessedPointCount { get; private set; }
    public int AllPointCount { get; private set; }

    public int ProcessedChunkedCount { get; private set; }
    public int AllChunkCount { get; private set; }

    ParallelOptions options;

    bool destroyed = false;

    // Use this for initialization
    void Start()
    {
        ProcessedPointCount = 0;
        ProcessedChunkedCount = 0;
        AllPointCount = 0;
        AllChunkCount = 0;
        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = maxThreadNum;
    }

    public void Setup(GameObject parentObject)
    {
        parentObject.AddComponent<ChunkedMeshesManager>();
    }

    public void ProcessArranging(Vector3[] _vertices, Color[] _colors)
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
            Arranging(res.Result);
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

    public void ProcessArranging(CloudPoint[] points)
    {
        CloudPoint[] cloudPoints = (CloudPoint[])points.Clone();
        SetPointsForArranging(cloudPoints);
    }

    async void SetPointsForArranging(CloudPoint[] points)
    {
        AllPointCount = points.Length;
        ProcessedPointCount = 0;
        await Task.Run(() => Arranging(points));
    }

    void Arranging(CloudPoint[] points)
    {
        Dictionary<IndexedVector3, List<CloudPoint>> arrangedPoints = new Dictionary<IndexedVector3, List<CloudPoint>>();

        ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        Debug.Log("Start Arranging " + points.Length + " counts.");

        Parallel.For(0, points.Length, options, (i, loopState) =>
        {
            IndexedVector3 index = new IndexedVector3(
                Mathf.RoundToInt(points[i].point.x / chunkSize), Mathf.RoundToInt(points[i].point.y / chunkSize), Mathf.RoundToInt(points[i].point.z / chunkSize));

            bool generateNewChunk = false;

            rwlock.EnterUpgradeableReadLock();
            try
            {
                if (!arrangedPoints.ContainsKey(index))
                {
                    generateNewChunk = true;
                }

                if (generateNewChunk)
                {
                    rwlock.EnterWriteLock();
                    try
                    {
                        arrangedPoints.Add(index, new List<CloudPoint>());
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        Debug.LogException(e);
                        Debug.LogError("Arranging points process is Dead!!!!!!!!!!!!!");
                    }
                    finally
                    {
                        rwlock.ExitWriteLock();
                    }
                }
                arrangedPoints[index].Add(points[i]);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogException(e);
                Debug.LogError("Arranging points process is Dead!!!!!!!!!!!!!");
            }
            finally
            {
                rwlock.ExitUpgradeableReadLock();
            }

            ProcessedPointCount++;
            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });
        Debug.Log("finish arranging.");
        finishArranging?.Invoke(this, new FinishArrangingArgs(arrangedPoints));
    }

    public void ProcessChunking(Dictionary<IndexedVector3, List<CloudPoint>> _arrangedPoints)
    {
        Dictionary<IndexedVector3, List<CloudPoint>> arrangedPoints = new Dictionary<IndexedVector3, List<CloudPoint>>(_arrangedPoints);
        CallChunking(arrangedPoints);
    }

    async void CallChunking(Dictionary<IndexedVector3, List<CloudPoint>> arrangedPoints)
    {
        ProcessedChunkedCount = 0;
        AllChunkCount = arrangedPoints.Count;
        await Task.Run(() => Chunking(arrangedPoints));
    }

    void Chunking(Dictionary<IndexedVector3, List<CloudPoint>> arrangedPoints)
    {
        Debug.Log("Start adding points to chunks.");

        Dictionary<IndexedVector3, CenteredPoints> chunkedPoints = new Dictionary<IndexedVector3, CenteredPoints>();
        foreach (KeyValuePair<IndexedVector3, List<CloudPoint>> item in arrangedPoints)
        {
            Vector3 _center = Vector3.zero;

            ProcessedPointCount = 0;
            AllPointCount = item.Value.Count;
            Parallel.For(0, item.Value.Count, options, (i, loopState) =>
            {
                try
                {
                    _center += item.Value[i].point;
                    ProcessedPointCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Debug.LogException(e);
                    Debug.LogError("Calucuation center process in chunking process is Dead!!!!!!!!!!!!!");
                }


                if (destroyed)
                {
                    loopState.Stop();
                    return;
                }
            });

            _center /= (float)item.Value.Count;
            List<CloudPoint> newPoints = new List<CloudPoint>();
            ProcessedPointCount = 0;

            Parallel.For(0, item.Value.Count, options, (i, loopState) =>
            {
                try
                {
                    lock (Thread.CurrentContext)
                        newPoints.Add(new CloudPoint(item.Value[i].point - _center, item.Value[i].intensity, item.Value[i].color));

                    ProcessedPointCount++;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError("Cetering process in chunking process is Dead!!!!!!!!!!!!!");
                }

                if (destroyed)
                {
                    loopState.Stop();
                    return;
                }
            });

            chunkedPoints.Add(item.Key, new CenteredPoints(newPoints, _center));
            ProcessedChunkedCount++;
        }

        Debug.Log("Finish arranging to " + chunkedPoints.Count + " processes.");
        finishProcess?.Invoke(this, new FinishProcessArgs(chunkedPoints));
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}