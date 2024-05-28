using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class AARandomFunctionScript : MonoBehaviour
{

    private void Start()
    {
        Debug.Log("Running.");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SpecialFunction(rootPath);
        }
    }

    static string rootPath = "F:/ISWDEV/TokyoStrings/TokyoScoringStrings/Samples";

    void SpecialFunction(string path)
    {
       
        FileInfo[] files = null;
        DirectoryInfo[] subDirs = null;

        DirectoryInfo root = new DirectoryInfo(path);

        // First, process all the files directly under this folder
        try
        {
            files = root.GetFiles("*");
            Debug.Log("Done getting files in " + path + ", Count is " + files.Length);
        }
        // This is thrown if even one of the files requires permissions greater
        // than the application provides.
        catch (UnauthorizedAccessException e)
        {
            // This code just writes out the message and continues to recurse.
            // You may decide to do something different here. For example, you
            // can try to elevate your privileges and access the file again.
            Debug.Log(e.Message);
        }

        subDirs = root.GetDirectories();

        Debug.Log("Done getting directories in " + path + ", count is " + subDirs.Length);

    }
}
