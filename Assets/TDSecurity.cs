using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.IO;
using System;

public class TDSecurity {

    static bool initialized;

#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
    static RIPEMD160 hasher;
#endif

    public static void Initialize()
    {
        if (!initialized)
        {
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
            hasher = RIPEMD160Managed.Create();
            initialized = true;
#endif
        }
    }

    public static bool CheckIfHashesMatch(string path)
    {
        string mdPath = CustomAlgorithms.GetPersistentDataPath() + "/md" + GameStartData.saveGameSlot + ".dat";
        if (!File.Exists(mdPath))
        {
            Debug.Log("No hash.");
            return false;
        }

        List<byte> byteReadList = new List<byte>();

        Stream mdStream = File.Open(mdPath, FileMode.Open);        
        BinaryReader br = new BinaryReader(mdStream);
        byte[] md = null;

        int bufferSize = 4096;
        using (var ms = new MemoryStream())
        {
            byte[] buffer = new byte[bufferSize];
            int count;
            while ((count = br.Read(buffer, 0, buffer.Length)) != 0)
            {
                ms.Write(buffer, 0, count);
            }
               
            md = ms.ToArray();
        }
        
        mdStream.Close();

        byte[] baseArray = GetHashValueForFile(path);

        if (baseArray == null)
        {
            Debug.Log(path + " has null hash.");
            return false;
        }
        byte[] compare = new byte[baseArray.Length + 4];
        for (int i = 0; i < GameLogScript.pKey.Length; i++)
        {
            compare[i] = GameLogScript.pKey[i];
        }
        for (int i = 4; i < baseArray.Length + 4; i++)
        {
            compare[i] = baseArray[i - 4];
        }        

        if (md == null)
        {
            Debug.Log("MD is null.");
            return false;
        }

        if (md.Length != compare.Length)
        {
            Debug.Log(md.Length + " does not match " + compare.Length);
            return false;
        }

        for (int i = 0; i < compare.Length; i++)
        {
            //Debug.Log(compare[i] + " vs " + md[i]);
            if (compare[i] != md[i])
            {
                return false;
            }
        }

        return true;

    }

    static byte[] GetHashValueForFile(string path)
    {
#if !UNITY_SWITCH && !UNITY_PS4 && !UNITY_XBOXONE && !UNITY_ANDROID
        if (!File.Exists(path))
        {
            Debug.Log("Doesn't exist: " + path);
            return null;
        }
        Initialize();
        FileStream fStream = File.Open(path, FileMode.Open);
        fStream.Position = 0;        
        byte[] hashValue = hasher.ComputeHash(fStream);
        fStream.Close();
        return hashValue;
#else
        return null;
#endif
    }

    public static void UpdateFileHash(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }        
        byte[] hashValue = GetHashValueForFile(path);
 
        string mdPath = CustomAlgorithms.GetPersistentDataPath() + "/md" + GameStartData.saveGameSlot + ".dat";
        if (File.Exists(mdPath))
        {
            File.Delete(mdPath);
        }

        FileStream createFileHash = File.Open(mdPath, FileMode.Create);
        BinaryWriter bw = new BinaryWriter(createFileHash);
        //bw.Write((short)hashValue.Length);
        for (int i = 0; i < GameLogScript.pKey.Length; i++)
        {
            bw.Write((byte)GameLogScript.pKey[i]);
        }
        
        for (int i = 0; i < hashValue.Length; i++)
        {
            bw.Write((byte)hashValue[i]);
        }

        //Debug.Log("Wrote 4 bytes and then " + hashValue.Length);
        bw.Close();
        createFileHash.Close();
    }

}
