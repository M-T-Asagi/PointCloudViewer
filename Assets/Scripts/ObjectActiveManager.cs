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

    bool lastState;

    // Use this for initialization
    void Start()
    {
        lastState = active;
        target.SetActive(active);
    }

    // Update is called once per frame
    void Update()
    {
        if (lastState != active)
        {
            target.SetActive(active);
            lastState = active;
        }
    }
}
