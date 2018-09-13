using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Threading.Tasks;

public class PointCloudPTSViewer : MonoBehaviour
{
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

    [SerializeField]
    float sizeScale = 0.0001f;
    [SerializeField]
    TextAsset pointsData;
    [SerializeField]
    Material mat;
    [SerializeField]
    Text textArea;
    [SerializeField]
    ProgressBarManager pbManager;
    [SerializeField]
    GameObject canvas;

    CloudPoint[] points;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    Mesh mesh;
    String text;
    int pointNum = -1;
    int processedPointNum = 0;
    bool meshCreated = false;
    bool destroyed = false;

    // Use this for initialization
    void Start()
    {
        mesh = null;
        if ((meshFilter = GetComponent<MeshFilter>()) == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if ((meshRenderer = GetComponent<MeshRenderer>()) == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.material = mat;

        text = pointsData.text;
        Task setPointsTask = Task.Run(SetPoints);
    }

    // Update is called once per frame
    void Update()
    {
        if (pointNum >= 0 && pointNum == processedPointNum && !meshCreated)
        {
            CreateMesh();
            canvas.SetActive(false);
            meshCreated = true;
        }

        if (!meshCreated && pointNum >= 0)
        {
            pbManager.UpdateState(processedPointNum / pointNum);
            textArea.text = processedPointNum + " /\n" + pointNum;
        }

    }

    async Task SetPoints()
    {
        StringReader reader = new StringReader(text);
        string fl = reader.ReadLine();
        pointNum = Int32.Parse(fl);

        points = new CloudPoint[pointNum];
        for (int i = 0; i < pointNum; i++)
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
            Debug.Log(points[i].ToString());

            if (destroyed) break;
        }
    }

    void CreateMesh()
    {
        mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Length];
        Color[] colors = new Color[points.Length];
        int[] indeces = new int[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = points[i].point;
            colors[i] = points[i].color;
            indeces[i] = i;
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.SetIndices(indeces, MeshTopology.Points, 0);
        meshFilter.mesh = mesh;
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}
