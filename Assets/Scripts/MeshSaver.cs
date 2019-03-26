using System;
using UnityEngine;
using UnityEditor;

public class MeshSaver : MonoBehaviour
{

    [SerializeField]
    string dirNameHeader = "";

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
        Debug.Log("Process saving " + childCount + "meshes");

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
        string _dirName = dirNameHeader + "-" + time;
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
        for (int i = 0; i < targetMeshes.Length; i++)
        {
            Mesh mesh = targetMeshes[i];
            AssetDatabase.CreateAsset(mesh, path + "/mesh-" + i + ".asset");
            AssetDatabase.SaveAssets();
        }
        Debug.Log("Finish saving " + targetMeshes.Length + "meshes");
    }

    void SavePrefabToAsset()
    {
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path + "/meshes.prefab");
        AssetDatabase.SaveAssets();
    }
}