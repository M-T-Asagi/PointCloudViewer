using UnityEngine;
using UnityEngine.UI;

public class ProgressBarManager : MonoBehaviour
{

    [SerializeField]
    Image baseBar;
    [SerializeField]
    Image progressingBar;
    [SerializeField]
    Text stateText;

    [SerializeField]
    float state = 0;
    public float State { get { return state; } }

    RectTransform rTransform;
    float max;
    float height;
    string stateTextsText = "";

    // Use this for initialization
    void Start()
    {
        rTransform = progressingBar.rectTransform;
        max = baseBar.rectTransform.sizeDelta.x;
        height = baseBar.rectTransform.sizeDelta.y;
        Debug.Log(baseBar.rectTransform.sizeDelta);
        _UpdateState();
        rTransform.anchoredPosition = new Vector3(-baseBar.rectTransform.sizeDelta.x, -baseBar.rectTransform.sizeDelta.y, 0);
    }

    private void Update()
    {
        _UpdateState();
    }

    void _UpdateState()
    {
        Vector2 sizeDelta = rTransform.sizeDelta;
        sizeDelta.x = max * state;
        sizeDelta.y = height;
        rTransform.sizeDelta = sizeDelta;
        stateText.text = stateTextsText;
    }

    public void UpdateState(float _state)
    {
        state = _state;
        if (state < 0)
            state = 0;
        else if (state > 1)
            state = 1;
    }

    public void UpdateStateText(string text)
    {
        stateTextsText = text;
    }

    public void Finish()
    {
        Destroy(gameObject);
    }
}
