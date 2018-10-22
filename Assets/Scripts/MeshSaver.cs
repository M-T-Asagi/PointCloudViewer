using System;
using UnityEngine;
using UnityEditor;

public class MeshSaver : MonoBehaviour
{

    [SerializeField]
    string fileNameHeader = "";

    const bool saveMesh = true;

    GameObject prefabRoot = null;
    bool process = false;
    string path;

    Mesh[] targetMeshes = null;

    public EventHandler<EventArgs> finishSaving;

    public void Process(Mesh[] meshes, GameObject root = null)
    {
        targetMeshes = meshes;
        prefabRoot = root;
        Setup();
    }

    public void Process(GameObject objectRoot)
    {
        int childCount = objectRoot.transform.childCount;
        targetMeshes = new Mesh[childCount];
        for (int i = 0; i < childCount; i++)
        {
            targetMeshes[i] = objectRoot.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh;
        }
        prefabRoot = objectRoot;
        Setup();
    }

    void Setup()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/SavedMeshes"))
            AssetDatabase.CreateFolder("Assets/Resources", "SavedMeshes");

        string time = DateTime.Now.ToFileTimeUtc().ToString();
        string _dirName = fileNameHeader + "-" + time;
        path = "Assets/Resources/SavedMeshes/" + _dirName;

        AssetDatabase.CreateFolder("Assets/Resources/SavedMeshes", _dirName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        process = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!process)
            return;

        SaveMeshAll();
        if (prefabRoot != null)
            SavePrefabToAsset();

        finishSaving?.Invoke(this, new EventArgs());
        process = false;
    }

    void SaveMeshAll()
    {
        int count = 0;

        foreach (Mesh mesh in targetMeshes)
        {
            AssetDatabase.CreateAsset(mesh, path + "/mesh-" + count + ".asset");
            AssetDatabase.SaveAssets();
            count++;
        }
    }

    void SavePrefabToAsset()
    {
        PrefabUtility.CreatePrefab(path + "/meshes.prefab", prefabRoot);
        AssetDatabase.SaveAssets();
    }
}