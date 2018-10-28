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

    float chunkSize;
    int chunkedDisplayDistance;
    IndexedVector3 position;
    IndexedVector3 lower;
    IndexedVector3 higher;
    List<GameObject> lastDisplayed;

    private void Start()
    {
        chunkSize = chunkedMeshesManager.chunkSize;
        chunkedDisplayDistance = Mathf.FloorToInt(displayDistance / chunkSize);
        position = new IndexedVector3(0, 0, 0);
        lower = new IndexedVector3(0, 0, 0);
        higher = new IndexedVector3(0, 0, 0);
        lastDisplayed = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        position.x = Mathf.FloorToInt(eye.position.x / chunkSize);
        position.y = Mathf.FloorToInt(eye.position.y / chunkSize);
        position.z = Mathf.FloorToInt(eye.position.z / chunkSize);
        lower.x = position.x - chunkedDisplayDistance;
        lower.y = position.y - chunkedDisplayDistance;
        lower.z = position.z - chunkedDisplayDistance;
        higher.x = position.x + chunkedDisplayDistance;
        higher.y = position.y + chunkedDisplayDistance;
        higher.z = position.z + chunkedDisplayDistance;

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
