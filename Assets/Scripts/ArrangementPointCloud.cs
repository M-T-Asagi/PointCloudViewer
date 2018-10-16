using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ArrangementPointCloud : MonoBehaviour
{
    [SerializeField]
    List<Mesh> meshes;

    [SerializeField]
    float chunkSize = 0.1f;

    [SerializeField]
    GameObject prefab;

    [SerializeField]
    MeshSaver meshSaver;

    Dictionary<IndexedVector3, GameObject> generated;
    Dictionary<IndexedVector3, List<CloudPoint>> buffPos;

    int processedCount = 0;

    ParallelOptions options;

    bool processed = false;
    bool destroyed = false;

    // Use this for initialization
    void Start()
    {
        GameObject child = new GameObject();
        child.transform.SetParent(transform);
        child.AddComponent<ChunkedMeshesManager>();

        options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 4;

        generated = new Dictionary<IndexedVector3, GameObject>();

        CallSetPoint(meshes[processedCount].vertexCount, meshes[processedCount].vertices, meshes[processedCount].colors);
    }

    // Update is called once per frame
    void Update()
    {
        if (!processed)
            return;

        foreach (KeyValuePair<IndexedVector3, List<CloudPoint>> obj in buffPos)
        {
            Vector3 chunkOrigin = obj.Key.ToVector3() * chunkSize;
            if (!generated.ContainsKey(obj.Key))
            {
                generated[obj.Key] = Instantiate(prefab, transform.GetChild(0));
                generated[obj.Key].transform.localPosition = chunkOrigin;
            }

            MeshFilter filter = generated[obj.Key].GetComponent<MeshFilter>();

            int oldVertexCount = (filter.mesh != null) ? filter.mesh.vertexCount : 0;
            int newVertexCount = oldVertexCount + obj.Value.Count;

            Vector3[] vertices = new Vector3[newVertexCount];
            Color[] colors = new Color[newVertexCount];
            int[] indices = new int[newVertexCount];

            if (filter.mesh != null)
            {
                Array.Copy(filter.mesh.vertices, vertices, oldVertexCount);
                Array.Copy(filter.mesh.colors, colors, oldVertexCount);
                for (int i = 0; i < oldVertexCount; i++)
                    indices[i] = i;
            }

            for (int i = 0; i < obj.Value.Count; i++)
            {
                vertices[oldVertexCount + i] = obj.Value[i].point - chunkOrigin;
                colors[oldVertexCount + i] = obj.Value[i].color;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            filter.sharedMesh = mesh;
        }

        processed = false;

        if (processedCount < meshes.Count)
        {
            CallSetPoint(meshes[processedCount].vertexCount, meshes[processedCount].vertices, meshes[processedCount].colors);
        }
        else
        {
            GameObject child = transform.GetChild(0).gameObject;
            ChunkedMeshesManager m = child.GetComponent<ChunkedMeshesManager>();
            m.chunkSize = chunkSize;
            m.chunksParent = child;
            meshSaver.StartProcessSetUp(transform.GetChild(0).gameObject);
        }
    }

    async void CallSetPoint(int verticesCount, Vector3[] vertices, Color[] colors)
    {
        Debug.Log("Start chunking.");
        await Task.Run(() => SetPointsAsync(verticesCount, vertices, colors));
        Debug.Log("Finish one of processes.");
        processed = true;
        processedCount++;
    }

    void SetPointsAsync(int verticesCount, Vector3[] vertices, Color[] colors)
    {
        buffPos = new Dictionary<IndexedVector3, List<CloudPoint>>();
        Parallel.For(0, verticesCount, options, (i, loopState) =>
        {
            Vector3 vertex = vertices[i];
            Color color = colors[i];
            IndexedVector3 index = new IndexedVector3(
                Mathf.RoundToInt(vertex.x / chunkSize), Mathf.RoundToInt(vertex.y / chunkSize), Mathf.RoundToInt(vertex.z / chunkSize));

            if (!buffPos.ContainsKey(index))
            {
                lock (Thread.CurrentContext)
                    buffPos[index] = new List<CloudPoint>();
            }

            lock (Thread.CurrentContext)
                buffPos[index].Add(new CloudPoint(vertex, 1, color));

            if (destroyed)
            {
                loopState.Stop();
                return;
            }
        });
    }

    private void OnDestroy()
    {
        destroyed = true;
    }
}