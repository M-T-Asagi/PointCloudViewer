using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PointsCollector : MonoBehaviour
{
    public class FinishProcessArgs : EventArgs
    {
        public Dictionary<IndexedVector3, Color> collectedPoints;

        public FinishProcessArgs(Dictionary<IndexedVector3, Color> _collectedPoints)
        {
            collectedPoints = new Dictionary<IndexedVector3, Color>(_collectedPoints);
        }

    }

    [SerializeField]
    int maxThreadNum = 4;

    public EventHandler<FinishProcessArgs> finishCollectingProcess;

    int countAll = 0;
    public int CountAll { get { return countAll; } }

    int processedCount = 0;
    public int ProcessedCount { get { return processedCount; } }

    bool destroyed = false;
    ParallelOptions options;
    float cubeSize;

    public void CollectingProcess(Dictionary<IndexedVector3, Color> _collectedPoints, CloudPoint[] _points, float _cubeSize)
    {
        Dictionary<IndexedVector3, Color> collectedPopints = new Dictionary<IndexedVector3, Color>(_collectedPoints);

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = maxThreadNum;

        cubeSize = _cubeSize;
        CallCollecting(collectedPopints, _points);
    }

    async void CallCollecting(Dictionary<IndexedVector3, Color> collectedPoints, CloudPoint[] _points)
    {
        CloudPoint[] points = (CloudPoint[])_points.Clone();

        countAll = points.Length;
        processedCount = 0;

        await Task.Run(() => Collecting(collectedPoints, points));
    }

    void Collecting(Dictionary<IndexedVector3, Color> collectedPoints, CloudPoint[] points)
    {
        ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        Parallel.For(0, points.Length, options, (i, loopState) =>
        {
            IndexedVector3 newIndex = new IndexedVector3(
                Mathf.RoundToInt(points[i].point.x / cubeSize),
                Mathf.RoundToInt(points[i].point.y / cubeSize),
                Mathf.RoundToInt(points[i].point.z / cubeSize));

            rwlock.EnterUpgradeableReadLock();

            try
            {
                if (!collectedPoints.ContainsKey(newIndex))
                {
                    rwlock.EnterWriteLock();
                    try
                    {
                        collectedPoints.Add(newIndex, points[i].color);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        Debug.LogException(e);
                        Debug.LogError("Collecting process is Dead!!!!!!!!!!!!!");
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
                Debug.LogError("Collecting process is Dead!!!!!!!!!!!!!");
            }
            finally
            {
                rwlock.ExitUpgradeableReadLock();
            }

            processedCount++;

            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });

        Debug.Log("Collected " + points.Length + "points to " + collectedPoints.Count);

        if (finishCollectingProcess != null)
            finishCollectingProcess.Invoke(this, new FinishProcessArgs(collectedPoints));
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
