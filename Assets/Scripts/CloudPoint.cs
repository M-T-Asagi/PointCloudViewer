
using UnityEngine;

public struct CloudPoint
{
    public Vector3 point;
    public int intensity;
    public Color color;

    public CloudPoint(Vector3 _point, int _intensity, Color _color)
    {
        point = _point;
        intensity = _intensity;
        color = _color;
    }

    override public string ToString()
    {
        return
            "point: [X: " + point.x + ", Y: " + point.y + ", Z: " + point.z + "]\n" +
            "intensity: " + intensity + "\n" +
            "color: [R: " + color.r + ", G: " + color.g + ", B: " + color.b + "]";
    }
}