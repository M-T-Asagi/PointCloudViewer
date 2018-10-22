using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class CollectingPoints : MonoBehaviour
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

    GameObject meshesRoot;
    Dictionary<IndexedVector3, Color> collectedPoints;
    ParallelOptions options;

    bool allProcessIsUp = false;
    bool destroyed = false;

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
        CloudPoint[] points = new CloudPoint[_points.Length];
        Array.Copy(_points, points, _points.Length);
        await Task.Run(() => Collecting(points));
        CallConverterProcess();
    }

    void Collecting(CloudPoint[] points)
    {
        Parallel.For(0, points.Length, options, (i, loopState) =>
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
        CloudPoint[] points = new CloudPoint[collectedPoints.Count];
        int count = 0;

        foreach (KeyValuePair<IndexedVector3, Color> point in collectedPoints)
        {
            points[count] = new CloudPoint(point.Key.ToVector3(), 1, point.Value);
            count++;
        }

        arranger.Process(points);
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
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
