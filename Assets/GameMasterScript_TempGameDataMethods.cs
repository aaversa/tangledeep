using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript : MonoBehaviour
{
    Dictionary<string, int> dictTempGameData;
    Dictionary<int, int> dictTempIntData;
    Dictionary<string, string> dictTempStringData;
    Dictionary<string, float> dictTempFloatData;
    Dictionary<string, GameObject> dictTempGameObjects;

    Dictionary<string, Queue<string>> queueTempStringData;
    Dictionary<string, Queue<int>> queueTempGameData;

    public void QueueTempStringData(string key, string value)
    {
        if (!queueTempStringData.ContainsKey(key)) // "legfound_sprite"        
        {
            queueTempStringData.Add(key, new Queue<string>());
        }

        queueTempStringData[key].Enqueue(value);
    }

    public void QueueTempGameData(string key, int value)
    {
        if (!queueTempGameData.ContainsKey(key)) // "legfound_id"        
        {
            queueTempGameData.Add(key, new Queue<int>()); // no queue exists? create it
        }

        queueTempGameData[key].Enqueue(value);
    }

    public void SetTempStringData(string key, string value)
    {
        if (dictTempStringData.ContainsKey(key))
        {
            dictTempStringData[key] = value;
        }
        else
        {
            dictTempStringData.Add(key, value);
        }
    }

    public void SetTempFloatData(string key, float value)
    {
        if (dictTempFloatData.ContainsKey(key))
        {
            dictTempFloatData[key] = value;
        }
        else
        {
            dictTempFloatData.Add(key, value);
        }
    }

    public void SetTempIntData(int key, int value)
    {
        if (dictTempIntData.ContainsKey(key))
        {
            dictTempIntData[key] = value;
        }
        else
        {
            dictTempIntData.Add(key, value);
        }
    }

    public void SetTempGameObject(string key, GameObject go)
    {
        if (dictTempGameObjects.ContainsKey(key))
        {
            dictTempGameObjects[key] = go;
        }
        else
        {
            dictTempGameObjects.Add(key, go);
        }
    }

    public GameObject ReadTempGameObject(string key)
    {
        GameObject go = null;
        dictTempGameObjects.TryGetValue(key, out go);
        return go;
    }

    public void RemoveTempGameObject(string key)
    {
        if (dictTempGameObjects.ContainsKey(key))
        {
            dictTempGameObjects.Remove(key);
        }
    }

    public void SetTempGameData(string key, int value)
    {
        if (dictTempGameData.ContainsKey(key))
        {
            dictTempGameData[key] = value;
        }
        else
        {
            dictTempGameData.Add(key, value);
        }
    }

    public string ReadTempStringData(string key)
    {
        if (dictTempStringData.ContainsKey(key))
        {
            return dictTempStringData[key];
        }
        else
        {
            return "";
        }
    }

    public float ReadTempFloatData(string key)
    {
        if (dictTempFloatData.ContainsKey(key))
        {
            return dictTempFloatData[key];
        }
        else
        {
            return 0f;
        }
    }

    public string DequeueTempStringData(string key)
    {
        if (queueTempStringData.ContainsKey(key))
        {
            if (queueTempStringData[key].Count > 0)
            {
                return queueTempStringData[key].Dequeue();
            }
        }
        return "";
    }

    public int DequeueTempGameData(string key)
    {
        if (queueTempGameData.ContainsKey(key))
        {
            if (queueTempGameData[key].Count > 0)
            {
                return queueTempGameData[key].Dequeue();
            }
        }
        return -1;
    }

    public int ReadTempGameData(string key)
    {
        if (dictTempGameData.ContainsKey(key))
        {
            return dictTempGameData[key];
        }
        else
        {
            return -1;
        }
    }

    public int ReadTempIntData(int key)
    {
        if (dictTempIntData.ContainsKey(key))
        {
            return dictTempIntData[key];
        }
        else
        {
            return -1;
        }
    }

}
