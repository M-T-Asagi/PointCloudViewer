using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CollectingPointsManager : MonoBehaviour
{
    public enum State
    {
        Settings = 0,
        Converting,
        Collecting,
        Bundling,
        Arranging,
        Generating,
        Baking,
        Saving,

        ItemNum
    }

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

    State stateNow = 0;

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
        Parallel.For(0, points.Length, options, (i, loopState) =>
        {
            try
            {
                IndexedVector3 newIndex = new IndexedVector3(
                    Mathf.RoundToInt(points[i].point.x / cubeSize),
                    Mathf.RoundToInt(points[i].point.y / cubeSize),
                    Mathf.RoundToInt(points[i].point.z / cubeSize)
                    );
                lock (Thread.CurrentContext)
                {
                    if (!collectedPoints.ContainsKey(newIndex))
                    {

                        collectedPoints.Add(newIndex, points[i].color);
                    }
                }
                subCount++;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogException(e);
                Debug.LogError("Collecting process is Dead!!!!!!!!!!!!!");
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
        CallBundlingPoints();
    }

    async void CallBundlingPoints()
    {
        stateNow = State.Bundling;

        List<CloudPoint> t = await Task.Run(() => BundlingPoints());
        CallArrange(t.ToArray());
    }

    List<CloudPoint> BundlingPoints()
    {
        List<CloudPoint> points = new List<CloudPoint>();
        subCount = 0;
        subAll = collectedPoints.Count;
        Parallel.ForEach(collectedPoints, options, (point, loopState) =>
        {
            try
            {
                lock (Thread.CurrentContext)
                    points.Add(new CloudPoint(point.Key.ToVector3(), 1, point.Value));
                lock (Thread.CurrentContext)
                    subCount++;
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
        arranger.Process(points);
    }

    void ArrangingProcessUp(object sender, PointsArranger.FinishProcessArgs args)
    {
        CallBakerSetPoints(new List<CenteredPoints>(args.chunkedPoints.Values));
    }

    void CallBakerSetPoints(List<CenteredPoints> points)
    {
        stateNow = State.Generating;
        baker.SetPoints(points);
    }

    void MeshesGenerated(object sender, MeshBaker.FinishGenerateArgs args)
    {
        stateNow = State.Baking;
        baker.SetMeshToBake(args.meshes);
    }

    void MeshesBaked(object sender, MeshBaker.FinishBakingArgs args)
    {
        stateNow = State.Saving;
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
            case State.Bundling:
                pbManager.UpdateState((float)subCount / (float)subAll);
                pbManager.UpdateStateText(subCount + " /\n" + subAll);
                break;
            case State.Arranging:
                pbManager.UpdateState((float)arranger.ProcessedPointCount / (float)arranger.AllPointCount);
                pbManager.UpdateStateText(arranger.ProcessedPointCount + " /\n" + arranger.AllPointCount);
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
