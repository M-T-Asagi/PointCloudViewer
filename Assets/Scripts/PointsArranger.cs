using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PointsArranger : MonoBehaviour
{
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

    public EventHandler<FinishProcessArgs> finishProcess;

    public int ProcessedPointCount { get; private set; }
    public int AllPointCount { get; private set; }

    ParallelOptions options;

    bool destroyed = false;

    // Use this for initialization
    void Start()
    {
        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = maxThreadNum;
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
        CloudPoint[] cloudPoints = (CloudPoint[])points.Clone();
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
        Dictionary<IndexedVector3, CenteredPoints> chunkedPoints = new Dictionary<IndexedVector3, CenteredPoints>();

        ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        Debug.Log("Start chunking.");

        Parallel.For(0, points.Length, options, (i, loopState) =>
        {
            IndexedVector3 index = new IndexedVector3(
                Mathf.RoundToInt(points[i].point.x / chunkSize), Mathf.RoundToInt(points[i].point.y / chunkSize), Mathf.RoundToInt(points[i].point.z / chunkSize));

            bool generateNewChunk = false;
            bool addNewPoint = false;

            rwlock.EnterUpgradeableReadLock();
            try
            {
                if (!chunkedPoints.ContainsKey(index))
                {
                    generateNewChunk = true;
                    addNewPoint = true;
                } else if(!chunkedPoints[index].points.Contains(points[i]))
                {
                    addNewPoint = true;
                }

                if (generateNewChunk)
                {
                    rwlock.EnterWriteLock();
                    try
                    {
                        chunkedPoints.Add(index, new CenteredPoints(new List<CloudPoint>(), Vector3.zero));
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
                if(addNewPoint)
                {
                    rwlock.EnterWriteLock();
                    try
                    {
                        chunkedPoints[index].points.Add(points[i]);
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

        ProcessedPointCount = 0;
        AllPointCount = chunkedPoints.Count;

        Parallel.ForEach(chunkedPoints, options, (item, loopState) =>
        {
            
            Vector3 _center = Vector3.zero;
            for (int i = 0; i < item.Value.points.Count; i++)
            {
                _center += item.Value.points[i].point;
            }

            _center /= (float)item.Value.points.Count;
            for (int i = 0; i < item.Value.points.Count; i++)
            {
                CloudPoint _buffPoint = item.Value.points[i];
                _buffPoint.point -= _center;
                chunkedPoints[item.Key].points[i] = _buffPoint;
            }

            ProcessedPointCount++;
            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });

        Debug.Log("Finish one of processes.");

        finishProcess?.Invoke(this, new FinishProcessArgs(chunkedPoints));
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}