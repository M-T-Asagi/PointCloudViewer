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
    public class TableBase<TKey, TValue, TKaV> where TKaV : KeyAndValue<TKey, TValue>
    {
        [SerializeField]
        private List<TKaV> list;
        private Dictionary<TKey, TValue> table;

        public TableBase(List<TKaV> _list = null)
        {
            if (_list != null)
                list = new List<TKaV>(_list);
            else
                list = new List<TKaV>();
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
        public List<TKaV> GetList()
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
                list.Add(ConvertValueWithKeyToKeyAndValue(key, value));
            }
            else
            {
                Dictionary<TKey, TValue> _table = new Dictionary<TKey, TValue>(GetTable());
                _table[key] = value;
                list = ConvertDictionaryToList(_table);
            }
        }

        public static TKaV ConvertValueWithKeyToKeyAndValue(TKey key, TValue value)
        {
            TKaV item = (TKaV)typeof(TKaV).GetConstructor(new System.Type[] { typeof(TKey), typeof(TValue) }).Invoke(new object[] { key, value });
            item.Key = key;
            item.Value = value;
            return item;
        }

        static Dictionary<TKey, TValue> ConvertListToDictionary(List<TKaV> list)
        {
            Dictionary<TKey, TValue> dic = new Dictionary<TKey, TValue>();
            foreach (KeyAndValue<TKey, TValue> pair in list)
            {
                dic.Add(pair.Key, pair.Value);
            }
            return dic;
        }

        static List<TKaV> ConvertDictionaryToList(Dictionary<TKey, TValue> dict)
        {
            List<TKaV> list = new List<TKaV>();
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                list.Add(ConvertValueWithKeyToKeyAndValue(kvp.Key, kvp.Value));
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