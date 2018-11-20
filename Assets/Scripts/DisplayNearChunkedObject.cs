using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayNearChunkedObject : MonoBehaviour
{
    [SerializeField]
    ChunkedMeshesManager chunkedMeshesManager;

    [SerializeField]
    float displayDistance;

    [SerializeField]
    Transform eye;

    Transform thisTransform;
    float chunkSize;
    int chunkedDisplayDistance;
    List<GameObject> lastDisplayed;

    private void Start()
    {
        thisTransform = transform;
        chunkSize = chunkedMeshesManager.chunkSize;
        chunkedDisplayDistance = Mathf.FloorToInt(displayDistance / chunkSize);
        lastDisplayed = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 inversedPosition = thisTransform.InverseTransformPoint(eye.position);
        IndexedVector3 indexedPosition = new IndexedVector3(0, 0, 0);
        indexedPosition.x = Mathf.FloorToInt(inversedPosition.x / chunkSize);
        indexedPosition.y = Mathf.FloorToInt(inversedPosition.y / chunkSize);
        indexedPosition.z = Mathf.FloorToInt(inversedPosition.z / chunkSize);

        IndexedVector3 lower = new IndexedVector3(0, 0, 0);
        lower.x = indexedPosition.x - chunkedDisplayDistance;
        lower.y = indexedPosition.y - chunkedDisplayDistance;
        lower.z = indexedPosition.z - chunkedDisplayDistance;

        IndexedVector3 higher = new IndexedVector3(0, 0, 0);
        higher.x = indexedPosition.x + chunkedDisplayDistance;
        higher.y = indexedPosition.y + chunkedDisplayDistance;
        higher.z = indexedPosition.z + chunkedDisplayDistance;

        List<GameObject> displayedObject = chunkedMeshesManager.GetChunkedObjectRange(lower, higher);

        for (int i = 0; i < displayedObject.Count; i++)
        {
            if (lastDisplayed.Contains(displayedObject[i]))
            {
                lastDisplayed.Remove(displayedObject[i]);
            }
            else
            {
                displayedObject[i].GetComponent<MeshRenderer>().enabled = true;
            }
        }

        for (int i = 0; i < lastDisplayed.Count; i++)
        {
            lastDisplayed[i].GetComponent<MeshRenderer>().enabled = false;
        }

        lastDisplayed = new List<GameObject>(displayedObject);
    }
}
