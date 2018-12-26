using System;
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
        Transforming,
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
    PtsToCloudPointConverter converter;
    [SerializeField]
    PointsArranger arranger;
    [SerializeField]
    PointsSlicer slicer;
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

    List<IndexedVector3> chunkedPointKeys;
    Dictionary<IndexedVector3, List<CenteredPoints>> chunkedPoints;
    List<CenteredMesh> chunkedMeshes;
    IndexedVector3 cubingProcessingIndex;

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

        ChunkedMeshesManager chunkedMeshesManager = meshesRoot.AddComponent<ChunkedMeshesManager>();
        chunkedMeshesManager.chunkSize = arranger.ChunkSize;
        chunkedMeshesManager.chunksParent = meshesRoot;
        chunkedMeshesManager.indexedObjects = new IndexedGameObjects();

        converter.processUp += ProcessUp;
        converter.allProcessUp += AllProcessUp;
        arranger.finishArranging += ArrangingProcessUp;
        arranger.finishProcess += ChunkingProcessUp;
        slicer.finishProcess += SlicerProcessUp;
        cuber.finish += CubingProcessUp;
        baker.finishBaking += MeshesBaked;
        saver.finishSaving += MeshesSaved;

        CallConverterProcess();
    }

    void CallConverterProcess()
    {
        stateNow = State.Converting;
        converter.Process();
    }

    void ProcessUp(object sender, PtsToCloudPointConverter.ProcessUpArgs args)
    {
        CallCollecting(args.cloudPoints);
    }

    async void CallCollecting(CloudPoint[] _points)
    {
        stateNow = State.Collecting;
        CloudPoint[] points = (CloudPoint[])_points.Clone();

        subCount = 0;
        subAll = points.Length;

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
        CallSlicingProcess(new Dictionary<IndexedVector3, CenteredPoints>(args.chunkedPoints));
    }

    void CallSlicingProcess(Dictionary<IndexedVector3, CenteredPoints> points)
    {
        stateNow = State.Slicing;
        slicer.Process(points, points.Values.Count);
    }

    void SlicerProcessUp(object sender, PointsSlicer.FinishSlicingEventArgs args)
    {
        chunkedMeshes = new List<CenteredMesh>();
        chunkedPoints = new Dictionary<IndexedVector3, List<CenteredPoints>>(args.points);
        Debug.Log("add arranged " + chunkedPoints.Count + "points!");
        chunkedPointKeys = new List<IndexedVector3>(chunkedPoints.Keys);
        CallPointsToCube();
    }

    void CallPointsToCube()
    {
        stateNow = State.Cubing;
        CheckAndRemoveZeroItemChunkedPointKey();
        if (chunkedPointKeys.Count <= 0)
        {
            stateNow = State.Transforming;
            return;
        }

        cubingProcessingIndex = chunkedPointKeys[0];
        Debug.Log("---State in cubing!---");
        Debug.Log("Reaming point nums: " + chunkedPoints.Count);
        Debug.Log("Processed chunks num: " + chunkedMeshes.Count);
        Debug.Log("Chunked points center: " + chunkedPoints[cubingProcessingIndex][0].center);
        Debug.Log("-----------------------");

        cuber.Process(chunkedPoints[cubingProcessingIndex][0].points.ToArray(), cubeSize);
    }

    void CheckAndRemoveZeroItemChunkedPointKey()
    {
        Debug.Log("CheckAndRemoveZeroItemChunkedPointKey is called.");
        if (chunkedPointKeys.Count <= 0 || chunkedPoints[chunkedPointKeys[0]].Count > 0)
        {
            Debug.Log("return from CheckAndRemoveZeroItemChunkedPointKey.");
            return;
        }
        else
        {
            chunkedPointKeys.RemoveAt(0);
            CheckAndRemoveZeroItemChunkedPointKey();
        }
    }

    void CubingProcessUp(object sender, PointsToCube.FinishGeneratingEventArgs args)
    {
        Debug.Log("Cubing process up!");
        center += chunkedPoints[cubingProcessingIndex][0].center;
        chunkedMeshes.Add(new CenteredMesh(args.generatedMeshes[0], chunkedPoints[cubingProcessingIndex][0].center));
        bakeCount++;

        chunkedPoints[cubingProcessingIndex].RemoveAt(0);
        if (chunkedPoints[cubingProcessingIndex].Count <= 0)
            CallMeshBake();
        else
            CallPointsToCube();
    }

    void CallMeshBake()
    {
        Debug.Log("Bake the mesh!");
        baker.SetMeshToBake(chunkedMeshes);
    }

    void MeshesBaked(object sender, MeshBaker.FinishBakingArgs args)
    {
        stateNow = State.Saving;

        chunkedMeshes.Clear();

        ChunkedMeshesManager chunkedMeshesManager = meshesRoot.GetComponent<ChunkedMeshesManager>();
        chunkedMeshesManager.indexedObjects.Update(cubingProcessingIndex, new List<GameObject>(args.gameObjects));
        CallPointsToCube();
    }

    void MeshesSaved(object sender, EventArgs args)
    {
        pbManager.Finish();
    }

    // Update is called once per frame
    void Update()
    {
        stateText.text = "State now:\n    " + stateNow.ToString();
        if (stateNow == State.Transforming)
        {
            meshesRoot.transform.position = -(center / (float)bakeCount);
            stateNow = State.Saving;
        }
        else if (stateNow == State.Saving)
        {
            saver.Process(meshesRoot);
        }

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
            case State.Slicing:
                pbManager.UpdateState((float)slicer.ProcessedVertexCount / (float)slicer.VertexCount);
                pbManager.UpdateStateText(slicer.ProcessedVertexCount + " /\n" + slicer.VertexCount);
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
