using System;
using UnityEngine;
using UnityEditor;

public class MeshSaver : MonoBehaviour
{
    [SerializeField]
    bool saveMesh = true;

    public bool SaveMesh
    {
        get { return saveMesh; }
        set { saveMesh = value; }
    }

    [SerializeField]
    bool savePrefab = true;

    public bool SavePrefab
    {
        get { return saveMesh; }
        set { saveMesh = value; }
    }

    GameObject target;
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
            AssetDatabase.CreateAsset(child.gameObject.GetComponent<MeshFilter>().mesh, dirName + "/mesh-" + count + ".asset");
            AssetDatabase.SaveAssets();
            count++;
        }
    }

    void SavePrefabToAsset()
    {
        PrefabUtility.CreatePrefab(dirName + "/meshes.prefab", target);
        AssetDatabase.SaveAssets();
    }

    public void StartProcessSetUp(GameObject _target)
    {
        target = _target;
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
