using UnityEngine;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class PtsToCloudPointConverter : MonoBehaviour
{
    [Serializable]
    public class Axis3
    {
        public Axis x = Axis.X;
        public Axis y = Axis.Y;
        public Axis z = Axis.Z;
    }

    public enum Axis
    {
        X = 0,
        Y,
        Z,
        minusX,
        minusY,
        minusZ,

        itemCount
    }

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
    [SerializeField]
    int maxThreadNum = 4;
    [SerializeField]
    Axis3 axis;

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
        options.MaxDegreeOfParallelism = maxThreadNum;
    }

    public void SetupPointScaning(string path)
    {
        reader = new StreamReader(path);
        string fl = reader.ReadLine();
        totalPointCount = Int32.Parse(fl);
        totalSectionCount = Mathf.CeilToInt(totalPointCount / maxPointsNumInAnObject);
    }

    public bool Process()
    {
        if (continuos)
        {
            CallSetPoint();
            return true;
        }
        else
        {
            allProcessUp?.Invoke(this, new AllProcessUpArgs());
            Cleanup();
            return false;
        }
    }

    async void CallSetPoint()
    {
        await Task.Run(() => SetPointsAsync());

        Debug.Log("Setting points process is finished!");
        Debug.Log("Processed count : " + processedSectionCount);
    }

    void SetPointsAsync()
    {
        Debug.Log("Scan start!");

        int newPointsArrayCount = Mathf.Min(maxPointsNumInAnObject, totalPointCount - maxPointsNumInAnObject * processedSectionCount);
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
                        PointConvertWithAxis(new Vector3(
                            float.Parse(data[0]) * sizeScale,
                            float.Parse(data[1]) * sizeScale,
                            float.Parse(data[2]) * sizeScale
                        )),
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

            processedPointCount++;
            currentCount++;

            if (destroy)
            {
                loopState.Stop();
                return;
            }
        });

        Debug.Log("process up! : " + currentCount + "/" + points.Length);
        processUp?.Invoke(this, new ProcessUpArgs(points));
        processedSectionCount++;
    }

    Vector3 PointConvertWithAxis(Vector3 _point)
    {
        Vector3 point = new Vector3();

        switch (axis.x)
        {
            case Axis.minusX:
                point.x = -_point.x;
                break;
            case Axis.Y:
                point.x = _point.y;
                break;
            case Axis.minusY:
                point.x = -_point.y;
                break;
            case Axis.Z:
                point.x = _point.z;
                break;
            case Axis.minusZ:
                point.x = -_point.z;
                break;
            default:
                point.x = _point.x;
                break;
        }

        switch (axis.y)
        {
            case Axis.X:
                point.y = _point.x;
                break;
            case Axis.minusX:
                point.y = -_point.x;
                break;
            case Axis.minusY:
                point.y = -_point.y;
                break;
            case Axis.Z:
                point.y = _point.z;
                break;
            case Axis.minusZ:
                point.y = -_point.z;
                break;
            default:
                point.y = _point.y;
                break;
        }

        switch (axis.z)
        {
            case Axis.X:
                point.z = _point.x;
                break;
            case Axis.minusX:
                point.z = -_point.x;
                break;
            case Axis.Y:
                point.z = _point.y;
                break;
            case Axis.minusY:
                point.z = -_point.y;
                break;
            case Axis.minusZ:
                point.z = -_point.z;
                break;
            default:
                point.z = _point.z;
                break;
        }

        return point;
    }

    void Cleanup()
    {
        if (reader != null)
            reader.Close();

        Debug.Log("cleaning up!");
    }

    private void OnDestroy()
    {
        destroy = true;
        Cleanup();
    }
}
