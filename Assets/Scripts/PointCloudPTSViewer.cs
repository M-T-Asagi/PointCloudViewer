using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Threading.Tasks;

public class PointCloudPTSViewer : MonoBehaviour
{
    const int MAX_LIST_LENGTH = 500000;

    struct CloudPoint
    {
        public Vector3 point;
        public int intensity;
        public Color color;

        public CloudPoint(Vector3 _point, int _intensity, Color _color)
        {
            point = _point;
            intensity = _intensity;
            color = _color;
        }

        override public string ToString()
        {
            return
                "point: [X: " + point.x + ", Y: " + point.y + ", Z: " + point.z + "]\n" +
                "intensity: " + intensity + "\n" +
                "color: [R: " + color.r + ", G: " + color.g + ", B: " + color.b + "]";
        }
    }

    enum State
    {
        Setup = 0,
        WaitStartScan,
        Scanning,
        WaitStartCreateMeshes,
        CreatingMeshes,
        WaitStartBaking,
        Baking,
        FinishedBaking,
        FinishedAllProcess,
        Destroyed,

        ItemNum
    }

    [SerializeField]
    float sizeScale = 0.0001f;
    [SerializeField]
    string filePath;
    [SerializeField]
    GameObject prefab;
    [SerializeField]
    Text textArea;
    [SerializeField]
    ProgressBarManager pbManager;
    [SerializeField]
    GameObject canvas;

    CloudPoint[] points;

    int pointNum = -1;
    int processedPointNum = 0;
    StreamReader reader = null;

    State state = 0;
    bool continuos = false;

    Vector3[] verticesBuff;
    Color[] colorsBuff;
    int[] indecesBuff;

    // Use this for initialization
    void Start()
    {
        if (filePath == null || filePath == "")
            return;

        SetupPointScaning(filePath);
    }

    void SetupPointScaning(string path)
    {
        reader = new StreamReader(path);
        string fl = reader.ReadLine();
        pointNum = Int32.Parse(fl);
        state++;
    }

    // Update is called once per frame
    void Update()
    {
        if (pointNum >= 0)
        {
            pbManager.UpdateState((float)processedPointNum / (float)pointNum);
            textArea.text = processedPointNum + " /\n" + pointNum;
        }

        switch (state)
        {
            case State.WaitStartScan:
                Task.Run(() => SetPointsAsync()).ContinueWith((Task t) =>
                {
                    Debug.Log("Setting points process is finished!");
                    state++;
                });
                state++;
                break;
            case State.Scanning:
                break;
            case State.WaitStartCreateMeshes:
                CreateMesh();
                state++;
                break;
            case State.CreatingMeshes:
                break;
            case State.WaitStartBaking:
                CreateChildObject();
                break;
            case State.Baking:
                state++;
                break;
            case State.FinishedBaking:
                if (continuos)
                {
                    state = State.WaitStartScan;
                }
                else
                {
                    state = State.FinishedAllProcess;
                }
                break;
            case State.FinishedAllProcess:
                Cleanup();
                canvas.SetActive(false);
                break;
        }
    }

    void SetPointsAsync()
    {
        Debug.Log("Scan start!");

        int newPointsArrayCount = Mathf.Min(MAX_LIST_LENGTH, pointNum - processedPointNum);
        continuos = (newPointsArrayCount >= MAX_LIST_LENGTH);

        points = new CloudPoint[newPointsArrayCount];
        ParallelOptions options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;

        Parallel.For(0, newPointsArrayCount, options, async (i, loopState) =>
        {
            string _read = await reader.ReadLineAsync();
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
            processedPointNum++;

            if (state == State.Destroyed)
            {
                loopState.Stop();
                Cleanup();
                return;
            }
        });
    }


    async void CreateMesh()
    {
        Debug.Log("creating meshes start!");
        await Task.Run(() =>
        {
            int _pointCount = points.Length;

            verticesBuff = new Vector3[_pointCount];
            colorsBuff = new Color[_pointCount];
            indecesBuff = new int[_pointCount];

            for (int i = 0; i < _pointCount; i++)
            {
                verticesBuff[i] = points[i].point;
                colorsBuff[i] = points[i].color;
                indecesBuff[i] = i;
            }
            state++;
        });
    }

    void CreateChildObject()
    {
        Debug.Log("creating child objects start!");
        Mesh mesh = new Mesh();
        mesh.vertices = verticesBuff;
        mesh.colors = colorsBuff;
        mesh.SetIndices(indecesBuff, MeshTopology.Points, 0);

        GameObject child = Instantiate(prefab, transform);
        child.GetComponent<MeshFilter>().sharedMesh = mesh;
        state++;
    }

    private void OnDestroy()
    {
        state = State.Destroyed;
    }

    void Cleanup()
    {
        Debug.Log("cleaning up!");
        verticesBuff = null;
        colorsBuff = null;
        indecesBuff = null;
        reader.Close();
    }

    public void Build(string filepath)
    {
        if (state == State.Setup || state == State.FinishedAllProcess)
        {
            state = State.Setup;
            SetupPointScaning(filepath);
        }

    }
}
