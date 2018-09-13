using System.Collections;
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
    public float State { get; }

    // Use this for initialization
    void Start()
    {
        rTransform = progressingBar.rectTransform;
        max = baseBar.rectTransform.rect.width;
    }

    public void UpdateState(float state)
    {
        if (state < 0)
            state = 0;
        else if (state > 1)
            state = 1;

        Vector2 offsetMax = rTransform.offsetMax;
        offsetMax.x = max * 0.5f - max * state;
        rTransform.offsetMax = offsetMax;
    }
}
