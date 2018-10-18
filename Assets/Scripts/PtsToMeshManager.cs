using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    bool allProcessIsUp = false;

    // Use this for initialization
    void Start()
    {
        converter.SetupPointScaning(filePath);
        baker.SetUp();

        converter.processUp += ProcessUp;
        converter.allProcessUp += AllProcessUp;
    }

    // Update is called once per frame
    void Update()
    {
        if (converter.TotalPointCount >= 0)
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
        baker.SetMeshToBake(args.mesh);
    }

    void FinishBakingMeshes(object sender, MeshBaker.FinishBakingArgs args)
    {
        saver.
    }

    void AllProcessUp(object sender, PtsToCloudPointConverter.AllProcessUpArgs args)
    {
        allProcessIsUp = true;
        pbManager.Finish();
    }
}
