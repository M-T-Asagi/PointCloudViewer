using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkedMeshesManager : MonoBehaviour
{
    public float chunkSize;
    public GameObject chunksParent;
    public IndexedGameObjects indexedObjects;

    public List<GameObject> GetChunkedObjectRange(IndexedVector3 lower, IndexedVector3 higher)
    {
        List<GameObject> returner = new List<GameObject>();

        if (lower.x > higher.x || lower.y > higher.y || lower.z > higher.z)
            throw new System.Exception("Cannot set lower params over higher params.");
        else if (higher.x < lower.x || higher.y < lower.y || higher.z < lower.z)
            throw new System.Exception("Cannot set higher params under lower params.");

        for (int x = lower.x; x <= higher.x; x++)
        {
            for (int y = lower.y; y <= higher.y; y++)
            {
                for (int z = lower.z; z <= higher.z; z++)
                {
                    IndexedVector3 index = new IndexedVector3(x, y, z);
                    if (indexedObjects.GetTable().ContainsKey(index))
                    {
                        List<GameObject> objectBuffs = new List<GameObject>(indexedObjects.GetTable()[index]);
                        returner.AddRange(objectBuffs);
                    }
                }
            }
        }

        return returner;
    }
}
