using UnityEngine;
using System.Collections.Generic;

/** Summary
 * 
 * Copied by https://qiita.com/k_yanase/items/fb64ccfe1c14567a907d from @k_yanase
 * 
 **/

namespace Serialize
{
    /// <summary>
    /// テーブルの管理クラス
    /// </summary>
    [System.Serializable]
    public class TableBase<TKey, TValue, Type> where Type : KeyAndValue<TKey, TValue>
    {
        [SerializeField]
        private List<Type> list;
        private Dictionary<TKey, TValue> table;

        public TableBase(List<Type> _list = null)
        {
            if (_list != null)
                list = new List<Type>(_list);
            else
                list = new List<Type>();
        }

        public Dictionary<TKey, TValue> GetTable()
        {
            if (table == null)
            {
                table = ConvertListToDictionary(list);
            }
            return table;
        }

        /// <summary>
        /// Editor Only
        /// </summary>
        public List<Type> GetList()
        {
            return list;
        }

        public void Update(TKey key, TValue value)
        {
            KeyAndValue<TKey, TValue> keyAndValue = new KeyAndValue<TKey, TValue>(key, value);
            if (!GetTable().ContainsKey(key))
            {
                // TODO: fix it 
                // InvalidCastException: Specified cast is not valid.
                list.Add((Type)new KeyAndValue<TKey, TValue>(key, value));
            }
            else
            {
                Dictionary<TKey, TValue> _table = new Dictionary<TKey, TValue>(GetTable());
                _table[key] = value;
                list = ConvertDictionaryToList(_table);
            }
        }

        static Dictionary<TKey, TValue> ConvertListToDictionary(List<Type> list)
        {
            Dictionary<TKey, TValue> dic = new Dictionary<TKey, TValue>();
            foreach (KeyAndValue<TKey, TValue> pair in list)
            {
                dic.Add(pair.Key, pair.Value);
            }
            return dic;
        }

        static List<Type> ConvertDictionaryToList(Dictionary<TKey, TValue> dict)
        {
            List<Type> list = new List<Type>();
            foreach(KeyValuePair<TKey, TValue> kvp in dict)
            {
                list.Add((Type)new KeyAndValue<TKey, TValue>(kvp.Key, kvp.Value));
            }
            return list;
        }
    }

    /// <summary>
    /// シリアル化できる、KeyValuePair
    /// </summary>
    [System.Serializable]
    public class KeyAndValue<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public KeyAndValue(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
        public KeyAndValue(KeyValuePair<TKey, TValue> pair)
        {
            Key = pair.Key;
            Value = pair.Value;
        }


    }
}