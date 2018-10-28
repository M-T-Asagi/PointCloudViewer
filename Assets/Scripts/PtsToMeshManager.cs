using UnityEngine;
using System.Collections.Generic;

public class PtsToMeshManager : MonoBehaviour
{
    [SerializeField]
    string filePath;
    [SerializeField]
    PtsToCloudPointConverter converter;
    [SerializeField]
    MeshBaker baker;
    [SerializeField]
    MeshSaver saver;
    [SerializeField]
    ProgressBarManager pbManager;

    GameObject meshesRoot;
    List<Mesh> meshes;

    bool allProcessIsUp = false;

    // Use this for initialization
    void Start()
    {
        converter.SetupPointScaning(filePath);

        meshesRoot = new GameObject();
        meshesRoot.transform.parent = transform;
        baker.SetUp(meshesRoot.transform);

        Debug.Log("Converter totatlSectionCount : " + converter.TotalSectionCount);

        meshes = new List<Mesh>();

        converter.processUp += ProcessUp;
        converter.allProcessUp += AllProcessUp;
        baker.finishGenerate += FinishGenerateMeshes;
        baker.finishBaking += FinishBakingMeshes;

        CallConverterProcess();
    }

    // Update is called once per frame
    void Update()
    {
        if (!allProcessIsUp && converter.TotalPointCount >= 0)
        {
            pbManager.UpdateState((float)converter.ProcessedPointCount / (float)converter.TotalPointCount);
            pbManager.UpdateStateText(converter.ProcessedPointCount + " /\n" + converter.TotalPointCount);
        }
    }

    void CallConverterProcess()
    {
        converter.Process();
    }

    void ProcessUp(object sender, PtsToCloudPointConverter.ProcessUpArgs args)
    {
        baker.SetPoints(args.cloudPoints);
    }

    void FinishGenerateMeshes(object sender, MeshBaker.FinishGenerateArgs args)
    {
        Debug.Log("FinishGenerateMeshes + Processed count : " + converter.ProcessedSectionCount);
        meshes.Add(args.meshes[0].mesh);
        baker.SetMeshToBake(args.meshes);
    }

    void FinishBakingMeshes(object sender, MeshBaker.FinishBakingArgs args)
    {
        CallConverterProcess();
    }

    void AllProcessUp(object sender, PtsToCloudPointConverter.AllProcessUpArgs args)
    {
        pbManager.Finish();
        saver.Process(meshes.ToArray(), meshesRoot);

        allProcessIsUp = true;
    }
}
