using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class PointCloudPTSViewer : MonoBehaviour
{
    public class ProcessUpArgs : EventArgs
    {
    }

    [SerializeField]
    float sizeScale = 0.0001f;
    [SerializeField]
    int maxPointsNumInAnObject = 300000;
    [SerializeField]
    string filePath;
    [SerializeField]
    MeshBaker meshBaker;
    [SerializeField]
    Text textArea;
    [SerializeField]
    ProgressBarManager pbManager;
    [SerializeField]
    GameObject canvas;

    int pointNum = -1;
    int processedPointNum = 0;
    int processedCount = 0;
    StreamReader reader = null;
    bool continuos = true;
    bool destroy = false;

    ParallelOptions options;

    public EventHandler<ProcessUpArgs> processUp;

    // Use this for initialization
    void Start()
    {
        if (filePath == null || filePath == "")
            return;

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;

        SetupPointScaning(filePath);
    }

    void SetupPointScaning(string path)
    {
        reader = new StreamReader(path);
        string fl = reader.ReadLine();
        pointNum = Int32.Parse(fl);
        Restart();
    }

    // Update is called once per frame
    void Update()
    {
        if (pointNum >= 0)
        {
            pbManager.UpdateState((float)processedPointNum / (float)pointNum);
            textArea.text = processedPointNum + " /\n" + pointNum;
        }
    }

    async void CallSetPoint()
    {
        await Task.Run(() => SetPointsAsync());
        processedCount++;

        Debug.Log("Setting points process is finished!");
        Debug.Log("Processed count : " + processedCount);
    }

    void SetPointsAsync()
    {
        Debug.Log("Scan start!");

        int newPointsArrayCount = Mathf.Min(maxPointsNumInAnObject, pointNum - maxPointsNumInAnObject * (processedCount + 1));
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
                processedPointNum++;
            }
            currentCount++;
        });

        Debug.Log("processed! : " + currentCount + "/" + points.Length);
        meshBaker.SetPoints(points);
    }

    private void OnDestroy()
    {
        destroy = true;
        Cleanup();
    }

    public void Restart()
    {
        if (continuos)
        {
            CallSetPoint();
        }
        else
        {
            processUp?.Invoke(this, new ProcessUpArgs());
            Cleanup();
            canvas.SetActive(false);
        }
    }

    void Cleanup()
    {
        Debug.Log("cleaning up!");

        reader.Close();
        Destroy(this);
    }
}
