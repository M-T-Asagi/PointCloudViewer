﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PtsToCubingManager : MonoBehaviour
{
    public enum State
    {
        Settings = 0,
        Converting,
        Collecting,
        Restoring,
        Arranging,
        Chunking,
        Slicing,
        Cubing,
        Generating,
        Baking,
        Saving,

        ItemNum
    }

    [SerializeField]
    float cubeSize = 0.01f;
    [SerializeField]
    string filePath;
    [SerializeField]
    int maxThreadNum = 3;
    [SerializeField]
    int maxVertexCountInAMesh = 64000;
    [SerializeField]
    PtsToCloudPointConverter converter;
    [SerializeField]
    PointsArranger arranger;
    [SerializeField]
    MeshBaker baker;
    [SerializeField]
    MeshSaver saver;
    [SerializeField]
    PointsToCube cuber;
    [SerializeField]
    ProgressBarManager pbManager;
    [SerializeField]
    ObjectActiveManager pbManagerActiveManager;
    [SerializeField]
    ProgressBarManager subpbManager;
    [SerializeField]
    ObjectActiveManager subPBManagerActiveManager;
    [SerializeField]
    Text stateText;

    GameObject meshesRoot;
    Dictionary<IndexedVector3, Color> collectedPoints;
    ParallelOptions options;

    Dictionary<IndexedVector3, List<CenteredPoints>> chunkedPoints;
    List<CenteredMesh> chunkedMeshes;

    List<IndexedVector3> everCubed;
    Vector3 processingChunkCenter;

    State stateNow = 0;

    bool allProcessIsUp = false;
    bool destroyed = false;

    int subCount = 0;
    int subAll = 0;

    int bakeCount = 0;
    Vector3 center = Vector3.zero;

    // Use this for initialization
    void Start()
    {
        collectedPoints = new Dictionary<IndexedVector3, Color>();

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = maxThreadNum;

        converter.SetupPointScaning(filePath);

        meshesRoot = new GameObject();
        meshesRoot.transform.parent = transform;
        baker.SetUp(meshesRoot.transform);

        converter.processUp += ProcessUp;
        converter.allProcessUp += AllProcessUp;
        arranger.finishArranging += ArrangingProcessUp;
        arranger.finishProcess += ChunkingProcessUp;
        cuber.finish += CubingProcessUp;
        baker.finishBaking += MeshesBaked;

        CallConverterProcess();
    }

    void CallConverterProcess()
    {
        converter.Process();
        stateNow = State.Converting;
    }

    void ProcessUp(object sender, PtsToCloudPointConverter.ProcessUpArgs args)
    {
        CallCollecting(args.cloudPoints);
    }

    async void CallCollecting(CloudPoint[] _points)
    {
        CloudPoint[] points = (CloudPoint[])_points.Clone();

        subCount = 0;
        subAll = points.Length;

        stateNow = State.Collecting;

        await Task.Run(() => Collecting(points));
        CallConverterProcess();
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

    void AllProcessUp(object sender, PtsToCloudPointConverter.AllProcessUpArgs args)
    {
        CallRestoringPoints();
    }

    async void CallRestoringPoints()
    {
        stateNow = State.Restoring;

        List<CloudPoint> t = await Task.Run(() => RestoringCollectedPointsToPoints());
        collectedPoints.Clear();
        CallArrange(t.ToArray());
    }

    List<CloudPoint> RestoringCollectedPointsToPoints()
    {
        List<CloudPoint> points = new List<CloudPoint>();
        subCount = 0;
        subAll = collectedPoints.Count;
        Parallel.ForEach(collectedPoints, options, (point, loopState) =>
        {
            try
            {
                lock (Thread.CurrentContext)
                {
                    points.Add(new CloudPoint(point.Key.ToVector3() * cubeSize, 1, point.Value));
                    subCount++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogException(e);
                Debug.LogError("Bundling points process is Dead!!!!!!!!!!!!!");
            }

            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });
        return points;
    }

    void CallArrange(CloudPoint[] _points)
    {
        stateNow = State.Arranging;
        CloudPoint[] points = (CloudPoint[])_points.Clone();
        arranger.ProcessArranging(points);
    }

    void ArrangingProcessUp(object sender, PointsArranger.FinishArrangingArgs args)
    {
        Debug.Log("Receive finished arranging event.");
        CallChunking(args.arrangedPoints);
    }

    void CallChunking(Dictionary<IndexedVector3, List<CloudPoint>> _points)
    {
        stateNow = State.Chunking;
        Dictionary<IndexedVector3, List<CloudPoint>> points = new Dictionary<IndexedVector3, List<CloudPoint>>(_points);
        arranger.ProcessChunking(points);
    }

    void ChunkingProcessUp(object sender, PointsArranger.FinishProcessArgs args)
    {
        CallSliceChunk(new Dictionary<IndexedVector3, CenteredPoints>(args.chunkedPoints));
    }

    async void CallSliceChunk(Dictionary<IndexedVector3, CenteredPoints> _points)
    {
        chunkedPoints = new Dictionary<IndexedVector3, List<CenteredPoints>>();
        await Task.Run(() => SliceChunk(new Dictionary<IndexedVector3, CenteredPoints>(_points)));
        everCubed = new List<IndexedVector3>(chunkedPoints.Keys);
        chunkedMeshes = new List<CenteredMesh>();
        Debug.Log("add arranged " + chunkedPoints.Count + "points!");
        CallPointsToCube();
    }

    void SliceChunk(Dictionary<IndexedVector3, CenteredPoints> _points)
    {
        int maxPointsInAMesh = Mathf.FloorToInt((float)maxVertexCountInAMesh / (float)cuber.PrefabMeshesVerticesCount);
        Parallel.ForEach(_points, options, (item, loopState) =>
        {
            List<CenteredPoints> buffCenteredPoints = new List<CenteredPoints>();
            for (int i = 0; i < Mathf.FloorToInt((float)item.Value.points.Count / (float)maxPointsInAMesh); i++)
            {
                CenteredPoints newPoints = new CenteredPoints();
                newPoints.center = item.Value.center;
                newPoints.points = new List<CloudPoint>(item.Value.points.GetRange(i * maxPointsInAMesh, Mathf.Min(item.Value.points.Count - i * maxPointsInAMesh, maxPointsInAMesh)));
                buffCenteredPoints.Add(newPoints);
            }
            chunkedPoints.Add(item.Key, new List<CenteredPoints>(buffCenteredPoints));
        });
    }

    void CallPointsToCube()
    {
        stateNow = State.Cubing;
        IndexedVector3 process = everCubed[0];
        Debug.Log("---State in cubing!---");
        Debug.Log("Reaming point nums: " + everCubed.Count);
        Debug.Log("Processed chunks num: " + chunkedMeshes.Count);
        Debug.Log("Will process point nums: " + chunkedPoints[process][0].points.Count);
        Debug.Log("Chunked points center: " + chunkedPoints[process][0].center);
        Debug.Log("-----------------------");
        processingChunkCenter = chunkedPoints[process][0].center;
        cuber.Process(chunkedPoints[process][0].points.ToArray(), cubeSize);
        chunkedPoints[process].RemoveAt(0);
        if (chunkedPoints[process].Count == 0)
            everCubed.RemoveAt(0);
    }

    void CubingProcessUp(object sender, PointsToCube.FinishGeneratingEventArgs args)
    {
        Debug.Log("Cubing process up!");
        center += processingChunkCenter;
        bakeCount++;

        chunkedMeshes.Add(new CenteredMesh(args.generatedMeshes[0], processingChunkCenter));
        if (everCubed.Count > 0)
            CallPointsToCube();
        else
            CallMeshBake();
    }

    void CallMeshBake()
    {
        Debug.Log("Bake the mesh!");
        baker.Center = center / (float)bakeCount;
        baker.SetMeshToBake(chunkedMeshes);
    }

    void MeshesBaked(object sender, MeshBaker.FinishBakingArgs args)
    {
        stateNow = State.Saving;

        ChunkedMeshesManager chunkedMeshesManager = meshesRoot.AddComponent<ChunkedMeshesManager>();
        chunkedMeshesManager.chunkSize = arranger.ChunkSize;
        chunkedMeshesManager.chunksParent = meshesRoot;

        saver.Process(meshesRoot);
    }

    void MeshesSaved(object sender, EventArgs args)
    {
        pbManager.Finish();
    }

    // Update is called once per frame
    void Update()
    {
        stateText.text = "State now:\n    " + stateNow.ToString();
        UpdateMainProgressBar();
        UpdateSubProgressbar();
    }

    void UpdateMainProgressBar()
    {
        pbManagerActiveManager.Active = true;
        switch (stateNow)
        {
            case State.Converting:
                pbManager.UpdateState((float)converter.ProcessedPointCount / (float)converter.TotalPointCount);
                pbManager.UpdateStateText(converter.ProcessedPointCount + " /\n" + converter.TotalPointCount);
                break;
            case State.Collecting:
                break;
            case State.Restoring:
                pbManager.UpdateState((float)subCount / (float)subAll);
                pbManager.UpdateStateText(subCount + " /\n" + subAll);
                break;
            case State.Arranging:
                pbManager.UpdateState((float)arranger.ProcessedPointCount / (float)arranger.AllPointCount);
                pbManager.UpdateStateText(arranger.ProcessedPointCount + " /\n" + arranger.AllPointCount);
                break;
            case State.Chunking:
                pbManager.UpdateState((float)arranger.ProcessedChunkedCount / (float)arranger.AllChunkCount);
                pbManager.UpdateStateText(arranger.ProcessedChunkedCount + " /\n" + arranger.AllChunkCount);
                break;
            case State.Cubing:
                pbManager.UpdateState((float)cuber.ProcessedStuffingChunkCount / (float)cuber.AllChunkCount);
                pbManager.UpdateStateText(cuber.ProcessedStuffingChunkCount + " /\n" + cuber.AllChunkCount);
                break;
            default:
                pbManagerActiveManager.Active = false;
                break;
        }
    }

    void UpdateSubProgressbar()
    {
        subPBManagerActiveManager.Active = true;
        switch (stateNow)
        {
            case State.Collecting:
                subpbManager.UpdateState((float)subCount / (float)subAll);
                subpbManager.UpdateStateText(subCount + "/\n" + subAll);
                break;
            case State.Chunking:
                subpbManager.UpdateState((float)arranger.ProcessedPointCount / (float)arranger.AllPointCount);
                subpbManager.UpdateStateText(arranger.ProcessedPointCount + " /\n" + arranger.AllPointCount);
                break;
            case State.Cubing:
                pbManager.UpdateState((float)cuber.ProcessedStuffingPointsCount / (float)cuber.AllOfStuffingPointsCount);
                pbManager.UpdateStateText(cuber.ProcessedStuffingPointsCount + " /\n" + cuber.AllOfStuffingPointsCount);
                break;
            default:
                subPBManagerActiveManager.Active = false;
                break;
        }
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
