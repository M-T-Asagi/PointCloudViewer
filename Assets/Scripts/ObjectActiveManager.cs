using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectActiveManager : MonoBehaviour
{
    [SerializeField]
    GameObject target;
    [SerializeField]
    bool active = true;

    public bool Active { get { return active; } set { active = value; } }

    // Use this for initialization
    void Start()
    {
        SetActiveTarget();
    }

    // Update is called once per frame
    void Update()
    {
        SetActiveTarget();
    }

    void SetActiveTarget()
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
