using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class PtsToCloudPointConverter : MonoBehaviour
{
    public class AllProcessUpArgs : EventArgs
    {
    }

    public class ProcessUpArgs : EventArgs
    {
        public CloudPoint[] cloudPoints;
        public ProcessUpArgs(CloudPoint[] _cloudPoints)
        {
            cloudPoints = new CloudPoint[_cloudPoints.Length];
            Array.Copy(_cloudPoints, cloudPoints, _cloudPoints.Length);
        }
    }

    [SerializeField]
    float sizeScale = 0.0001f;
    [SerializeField]
    int maxPointsNumInAnObject = 300000;

    int totalPointCount = -1;
    public int TotalPointCount { get { return totalPointCount; } }
    int processedPointCount = 0;
    public int ProcessedPointCount { get { return processedPointCount; } }
    int processedSectionCount = 0;
    public int ProcessedSectionCount { get { return processedSectionCount; } }
    int totalSectionCount = 0;
    public int TotalSectionCount { get { return totalSectionCount; } }

    StreamReader reader = null;
    bool continuos = true;
    bool destroy = false;

    ParallelOptions options;

    public EventHandler<AllProcessUpArgs> allProcessUp;
    public EventHandler<ProcessUpArgs> processUp;

    // Use this for initialization
    void Start()
    {
        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;
    }

    public void SetupPointScaning(string path)
    {
        reader = new StreamReader(path);
        string fl = reader.ReadLine();
        totalPointCount = Int32.Parse(fl);
    }

    public void StartProcessing()
    {
        if (continuos)
        {
            CallSetPoint();
        }
        else
        {
            allProcessUp?.Invoke(this, new AllProcessUpArgs());
            Cleanup();
        }
    }

    async void CallSetPoint()
    {
        await Task.Run(() => SetPointsAsync());
        processedSectionCount++;

        Debug.Log("Setting points process is finished!");
        Debug.Log("Processed count : " + processedSectionCount);
    }

    void SetPointsAsync()
    {
        Debug.Log("Scan start!");

        int newPointsArrayCount = Mathf.Min(maxPointsNumInAnObject, totalPointCount - maxPointsNumInAnObject * (processedSectionCount + 1));
        Debug.Log("new points array count : " + newPointsArrayCount);

        continuos = (newPointsArrayCount >= maxPointsNumInAnObject);

        CloudPoint[] points = new CloudPoint[newPointsArrayCount];

        int currentCount = 0;
        Parallel.For(0, newPointsArrayCount, options, (i, loopState) =>
        {
            try
            {
                string _read;
                lock (Thread.CurrentContext)
                    _read = reader.ReadLine();
                if (_read == null || _read == "")
                {
                    Debug.LogError("reading failed!");
                }
                else
                {
                    string[] data = _read.Split(' ');
                    points[i] = new CloudPoint(
                        new Vector3(
                            float.Parse(data[0]) * sizeScale,
                            float.Parse(data[1]) * sizeScale,
                            float.Parse(data[2]) * sizeScale
                        ),
                        Int32.Parse(data[3]),
                        new Color(
                            float.Parse(data[4]) / 255f,
                            float.Parse(data[5]) / 255f,
                            float.Parse(data[6]) / 255f
                        ));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogException(e);
                Debug.LogError("Dead!!!!!!!!!!!!!");
            }


            if (destroy)
            {
                loopState.Stop();
                return;
            }

            lock (Thread.CurrentContext)
            {
                processedPointCount++;
            }
            currentCount++;
        });

        Debug.Log("process up! : " + currentCount + "/" + points.Length);
        processUp?.Invoke(this, new ProcessUpArgs(points));
    }

    void Cleanup()
    {
        Debug.Log("cleaning up!");

        reader.Close();
        if (!destroy)
            Destroy(this);
    }

    private void OnDestroy()
    {
        destroy = true;
        Cleanup();
    }
}
