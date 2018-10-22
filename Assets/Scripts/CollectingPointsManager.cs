using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class CollectingPointsManager : MonoBehaviour
{
    [SerializeField]
    float cubeSize = 0.1f;
    [SerializeField]
    string filePath;
    [SerializeField]
    PtsToCloudPointConverter converter;
    [SerializeField]
    PointsArranger arranger;
    [SerializeField]
    MeshBaker baker;
    [SerializeField]
    MeshSaver saver;
    [SerializeField]
    ProgressBarManager pbManager;
    [SerializeField]
    ObjectActiveManager subPBManagerActiveManager;
    [SerializeField]
    ProgressBarManager subpbManager;

    GameObject meshesRoot;
    Dictionary<IndexedVector3, Color> collectedPoints;
    ParallelOptions options;

    bool allProcessIsUp = false;
    bool destroyed = false;

    int subCount = 0;
    int subAll = 0;

    // Use this for initialization
    void Start()
    {
        collectedPoints = new Dictionary<IndexedVector3, Color>();

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;

        converter.SetupPointScaning(filePath);

        meshesRoot = new GameObject();
        meshesRoot.transform.parent = transform;
        baker.SetUp(meshesRoot.transform);

        converter.processUp += ProcessUp;
        converter.allProcessUp += AllProcessUp;
        arranger.finishProcess += ArrangingProcessUp;
        baker.finishGenerate += MeshesGenerated;
        baker.finishBaking += MeshesBaked;

        CallConverterProcess();
    }

    void CallConverterProcess()
    {
        converter.Process();
    }

    void ProcessUp(object sender, PtsToCloudPointConverter.ProcessUpArgs args)
    {
        CallCollecting(args.cloudPoints);
    }

    async void CallCollecting(CloudPoint[] _points)
    {
        CloudPoint[] points = (CloudPoint[])_points.Clone();

        subPBManagerActiveManager.Active = true;
        subCount = 0;
        subAll = points.Length;

        await Task.Run(() => Collecting(points));
        subPBManagerActiveManager.Active = false;
        CallConverterProcess();
    }

    void Collecting(CloudPoint[] points)
    {
        Parallel.For(0, points.Length, options, (i, loopState) =>
        {
            try
            {
                IndexedVector3 newIndex = new IndexedVector3(
                    Mathf.RoundToInt(points[i].point.x / cubeSize),
                    Mathf.RoundToInt(points[i].point.y / cubeSize),
                    Mathf.RoundToInt(points[i].point.z / cubeSize)
                    );

                if (!collectedPoints.ContainsKey(newIndex))
                {
                    lock (Thread.CurrentContext)
                        collectedPoints.Add(newIndex, points[i].color);
                }

                lock (Thread.CurrentContext)
                    subCount++;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogException(e);
                Debug.LogError("Dead!!!!!!!!!!!!!");
            }

            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });
    }

    void AllProcessUp(object sender, PtsToCloudPointConverter.AllProcessUpArgs args)
    {
        CallArranging();
    }

    void CallArranging()
    {
        Debug.Log("Called!!!");
        ConvertCollectedPointToCloudPoint();
    }

    async void ConvertCollectedPointToCloudPoint()
    {
        subPBManagerActiveManager.Active = true;
        List<CloudPoint> t = await Task.Run(() => ConvertingCollectedPointToCloudPointProcess());
        subPBManagerActiveManager.Active = false;
        arranger.Process(t.ToArray());
    }

    Task<List<CloudPoint>> ConvertingCollectedPointToCloudPointProcess()
    {
        Debug.Log("nonononononono");
        return new Task<List<CloudPoint>>(() => _ConvertingCollectedPointToCloudPointProcess());
    }

    List<CloudPoint> _ConvertingCollectedPointToCloudPointProcess()
    {
        Debug.Log("apoapoapoapoapoapoa");
        List<CloudPoint> points = new List<CloudPoint>();
        subCount = 0;
        subAll = collectedPoints.Count;
        Parallel.ForEach(collectedPoints, options, (point, loopState) =>
        {
            try
            {
                lock (Thread.CurrentContext)
                    points.Add(new CloudPoint(point.Key.ToVector3(), 1, point.Value));
                subCount++;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogException(e);
                Debug.LogError("Dead!!!!!!!!!!!!!");
            }


            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });
        return points;
    }

    void ArrangingProcessUp(object sender, PointsArranger.FinishProcessArgs args)
    {
        List<CloudPoint[]> points = new List<CloudPoint[]>();
        List<Vector3> centers = new List<Vector3>();
        foreach (KeyValuePair<IndexedVector3, List<CloudPoint>> val in args.chunkedPositions)
        {
            CloudPoint[] _points = val.Value.ToArray();
            centers.Add(val.Key.ToVector3() * arranger.ChunkSize);
            points.Add(_points);
        }

        baker.SetPoints(points, centers);
    }

    void MeshesGenerated(object sender, MeshBaker.FinishGenerateArgs args)
    {
        baker.SetMeshToBake(args.centers, args.meshes);
    }

    void MeshesBaked(object sender, MeshBaker.FinishBakingArgs args)
    {
        saver.Process(meshesRoot);
    }

    void MeshesSaved(object sender, EventArgs args)
    {
        pbManager.Finish();
    }

    // Update is called once per frame
    void Update()
    {
        if (!allProcessIsUp && converter.TotalPointCount >= 0 && pbManager != null)
        {
            pbManager.UpdateState((float)converter.ProcessedPointCount / (float)converter.TotalPointCount);
            pbManager.UpdateStateText(converter.ProcessedPointCount + " /\n" + converter.TotalPointCount);
        }
        if (subPBManagerActiveManager.Active)
        {
            subpbManager.UpdateState((float)subCount / (float)subAll);
            subpbManager.UpdateStateText(subCount + "/\n" + subAll);
        }
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
