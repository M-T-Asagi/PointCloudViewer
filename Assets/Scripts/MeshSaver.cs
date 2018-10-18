using System;
using UnityEngine;
using UnityEditor;

public class MeshSaver : MonoBehaviour
{
    [SerializeField]
    bool savePrefab = true;

    public bool SavePrefab
    {
        get { return savePrefab; }
        set { savePrefab = value; }
    }

    [SerializeField]
    string dirName = "";

    const bool saveMesh = true;

    GameObject target;
    bool process = false;
    string path;

    public void Process(GameObject _target)
    {
        target = _target;
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/SavedMeshes"))
            AssetDatabase.CreateFolder("Assets/Resources", "SavedMeshes");

        string time = DateTime.Now.ToFileTimeUtc().ToString();
        string _dirName = dirName + "-" + time;
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
        if (savePrefab)
            SavePrefabToAsset();

        process = false;
    }

    void SaveMeshAll()
    {
        int count = 0;
        foreach (Transform child in target.transform)
        {
            AssetDatabase.CreateAsset(child.gameObject.GetComponent<MeshFilter>().mesh, path + "/mesh-" + count + ".asset");
            AssetDatabase.SaveAssets();
            count++;
        }
    }

    void SavePrefabToAsset()
    {
        PrefabUtility.CreatePrefab(path + "/meshes.prefab", target);
        AssetDatabase.SaveAssets();
    }
}
