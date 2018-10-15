using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MeshSaver : MonoBehaviour
{
    [SerializeField]
    bool saveMesh = true;

    [SerializeField]
    bool savePrefab = true;

    [SerializeField]
    PointCloudPTSViewer pointCloudPTSViewer;

    bool process = false;
    string dirName;

    private void OnValidate()
    {
        if (savePrefab)
            saveMesh = true;
    }

    // Use this for initialization
    void Start()
    {
        if (!saveMesh)
            Destroy(this);

        pointCloudPTSViewer.processUp += StartProcessSetUp;
    }

    // Update is called once per frame
    void Update()
    {
        if (!process)
            return;

        SaveMeshAll();
        if (savePrefab)
            SavePrefab();

        process = false;
        pointCloudPTSViewer.processUp -= StartProcessSetUp;
        Destroy(this);
    }

    void SaveMeshAll()
    {
        int count = 0;
        foreach (Transform child in transform.GetChild(0))
        {
            AssetDatabase.CreateAsset(child.gameObject.GetComponent<MeshFilter>().mesh, dirName + "/mesh-" + count + ".asset");
            AssetDatabase.SaveAssets();
            count++;
        }
    }

    void SavePrefab()
    {
        PrefabUtility.CreatePrefab(dirName + "/meshes.prefab", transform.GetChild(0).gameObject);
        AssetDatabase.SaveAssets();

    }

    void StartProcessSetUp(object sender, PointCloudPTSViewer.ProcessUpArgs args)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/SavedMeshes"))
            AssetDatabase.CreateFolder("Assets/Resources", "SavedMeshes");

        string time = DateTime.Now.ToFileTimeUtc().ToString();
        dirName = "Assets/Resources/SavedMeshes/" + time;

        AssetDatabase.CreateFolder("Assets/Resources/SavedMeshes", time);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        process = true;
    }
}
