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

    }

    [SerializeField]
    int maxThreadNum = 4;

    int countAll = 0;
    public int CountAll { get { return countAll; } }

    int processedCount = 0;
    public int ProcessedCount { get { return processedCount; } }

    float cubeSize;

    public void Process(CloudPoint[] _points, float _cubeSize)
    {
        cubeSize = _cubeSize;
        CallCollecting(_points);
    }


    async void CallCollecting(CloudPoint[] _points)
    {
        CloudPoint[] points = (CloudPoint[])_points.Clone();

        countAll = points.Length;
        processedCount = 0;

        await Task.Run(() => Collecting(points));
    }

    void Collecting(CloudPoint[] points)
    {
        ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        Parallel.For(0, points.Length, options, (i, loopState) =>
        {
            IndexedVector3 newIndex = new IndexedVector3(
                Mathf.RoundToInt(points[i].point.x / cubeSize),
                Mathf.RoundToInt(points[i].point.y / cubeSize),
                Mathf.RoundToInt(points[i].point.z / cubeSize)
                );

            bool canAdd = false;
            rwlock.EnterUpgradeableReadLock();

            try
            {
                canAdd = !collectedPoints.ContainsKey(newIndex);
                if (canAdd)
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

            subCount++;

            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });

        Debug.Log("Collected " + points.Length + "points to " + collectedPoints.Count);
    }
}
