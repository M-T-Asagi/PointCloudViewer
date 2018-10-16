using System;
using UnityEngine;

public class IndexedVector3 : IEquatable<IndexedVector3>
{
    public int x;
    public int y;
    public int z;

    public IndexedVector3(int _x, int _y, int _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
    }

    public override string ToString()
    {
        return "x: " + x + "\ny: " + y + "\nz: " + z;
    }

    bool IEquatable<IndexedVector3>.Equals(IndexedVector3 other)
    {
        if (other == null || x != other.x || y != other.y || z != other.z)
        {
            return false;
        }

        return true;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
