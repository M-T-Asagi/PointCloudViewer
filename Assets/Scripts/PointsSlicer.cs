using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

public class PointsSlicer : MonoBehaviour
{
    public class FinishSlicingEventArgs : EventArgs
    {
        public Dictionary<IndexedVector3, List<CenteredPoints>> points;

        public FinishSlicingEventArgs(Dictionary<IndexedVector3, List<CenteredPoints>> _points)
        {
            points = new Dictionary<IndexedVector3, List<CenteredPoints>>(_points);
        }
    }

    public EventHandler<FinishSlicingEventArgs> finishProcess;

    [SerializeField]
    int maxVertexCountInAMesh = 64000;

    [SerializeField]
    int maxThreadNum = 3;

    ParallelOptions options;

    bool destroyed = false;

    int vertexCount = 0;
    public int VertexCount
    {
        get { return vertexCount; }
    }

    int processedvertexCount = 0;
    public int ProcessedVertexCount
    {
        get { return processedvertexCount; }
    }

    public void Process(Dictionary<IndexedVector3, CenteredPoints> _points, int maxPointCountInAMesh)
    {
        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = maxThreadNum;
        CallSliceChunk(new Dictionary<IndexedVector3, CenteredPoints>(_points), maxPointCountInAMesh);
    }

    async void CallSliceChunk(Dictionary<IndexedVector3, CenteredPoints> _points, int maxPointCountInAMesh)
    {
        await Task.Run(() => SliceChunk(new Dictionary<IndexedVector3, CenteredPoints>(_points), maxPointCountInAMesh));
    }

    void SliceChunk(Dictionary<IndexedVector3, CenteredPoints> _points, int maxPointCountInAMesh)
    {
        Debug.Log("Slicing process is started.");

        vertexCount = 0;

        Dictionary<IndexedVector3, List<CenteredPoints>> chunkedPoints = new Dictionary<IndexedVector3, List<CenteredPoints>>();

        int maxPointsInAMesh = Mathf.FloorToInt((float)maxVertexCountInAMesh / (float)maxPointCountInAMesh);

        Parallel.ForEach(_points, options, (item, loopState) =>
        {
            try
            {
                lock (Thread.CurrentContext)
                    vertexCount += item.Value.points.Count;

                Debug.Log("foring " + item.Value.points.Count + " points to " + Mathf.CeilToInt((float)item.Value.points.Count / (float)maxPointsInAMesh) + "objects(an object contains " + maxPointsInAMesh + " points).");

                List<CenteredPoints> buffCenteredPoints = new List<CenteredPoints>();

                int _processedVertexCount = 0;

                for (int i = 0; i < Mathf.CeilToInt((float)item.Value.points.Count / (float)maxPointsInAMesh); i++)
                {
                    CenteredPoints newPoints = new CenteredPoints();
                    newPoints.center = item.Value.center;
                    newPoints.points = new List<CloudPoint>(item.Value.points.GetRange(i * maxPointsInAMesh, Mathf.Min(item.Value.points.Count - i * maxPointsInAMesh, maxPointsInAMesh)));
                    buffCenteredPoints.Add(newPoints);

                    _processedVertexCount++;

                    if (destroyed)
                        break;
                }

                if (!destroyed && buffCenteredPoints.Count > 0)
                    lock (Thread.CurrentContext)
                    {
                        processedvertexCount += _processedVertexCount;
                        chunkedPoints.Add(item.Key, new List<CenteredPoints>(buffCenteredPoints));
                    }


                if (destroyed)
                {
                    loopState.Stop();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("Slicing process is Dead!!!!!!!!!!!!!");
            }
        });

        Debug.Log("Slicing process is finished.");

        if (finishProcess != null)
            finishProcess.Invoke(this, new FinishSlicingEventArgs(chunkedPoints));
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
