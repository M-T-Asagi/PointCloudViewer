using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ジェネリックを隠すために継承してしまう
/// [System.Serializable]を書くのを忘れない
/// </summary>
[System.Serializable]
public class IndexedGameObjects : Serialize.TableBase<IndexedVector3, List<GameObject>, IndexedGmaeObjectsPair>
{
}

/// <summary>
/// ジェネリックを隠すために継承してしまう
/// [System.Serializable]を書くのを忘れない
/// </summary>
[System.Serializable]
public class IndexedGmaeObjectsPair : Serialize.KeyAndValue<IndexedVector3, List<GameObject>>
{

    public IndexedGmaeObjectsPair(IndexedVector3 key, List<GameObject> value) : base(key, value)
    {

    }
}