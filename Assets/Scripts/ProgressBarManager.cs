﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarManager : MonoBehaviour
{

    [SerializeField]
    Image baseBar;
    [SerializeField]
    Image progressingBar;

    RectTransform rTransform;
    float max;

    [SerializeField]
    float state = 0;
    public float State { get { return state; } }

    // Use this for initialization
    void Start()
    {
        rTransform = progressingBar.rectTransform;
        max = baseBar.rectTransform.sizeDelta.x;
    }

    public void UpdateState(float _state)
    {
        state = _state;
        if (state < 0)
            state = 0;
        else if (state > 1)
            state = 1;

        Vector2 sizeDelta = rTransform.sizeDelta;
        sizeDelta.x = max * state;
        rTransform.sizeDelta = sizeDelta;
    }
}
