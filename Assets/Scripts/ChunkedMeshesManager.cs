﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkedMeshesManager : MonoBehaviour
{
    public float chunkSize;
    public GameObject chunksParent;
    public IndexedGameObjects indexedObjects;

    private void Start()
    {
    }

    public List<GameObject> GetChunkedObjectRange(IndexedVector3 lower, IndexedVector3 higher)
    {
        List<GameObject> returner = new List<GameObject>();

        if (lower.x > higher.x || lower.y > higher.y || lower.z > higher.z)
            throw new System.Exception("Cannot set lower params over higher params.");
        else if (higher.x < lower.x || higher.y < lower.y || higher.z < lower.z)
            throw new System.Exception("Cannot set higher params under lower params.");

        for (int _x = 0; _x < higher.x - lower.x; _x++)
        {
            for (int _y = 0; _y < higher.y - lower.y; _y++)
            {
                for (int _z = 0; _z < higher.z - lower.z; _z++)
                {
                    IndexedVector3 index = new IndexedVector3(_x + lower.x, _y + lower.y, _z + lower.z);
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
