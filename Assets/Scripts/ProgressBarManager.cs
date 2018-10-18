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

    [SerializeField]
    float state = 0;
    public float State { get { return state; } }

    Text stateText;
    RectTransform rTransform;
    float max;

    // Use this for initialization
    void Start()
    {
        rTransform = progressingBar.rectTransform;
        max = baseBar.rectTransform.sizeDelta.x;
        stateText = transform.Find("StateText").GetComponent<Text>();
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

    public void UpdateStateText(string text)
    {
        stateText.text = text;
    }

    public void Finish()
    {
        Destroy(gameObject);
    }
}
