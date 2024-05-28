using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.Serialization;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Globalization;
using Debug = UnityEngine.Debug;

public partial class StringManager
{
    public static void ClearTags()
    {
        for (int i = 0; i < mergeTags.Length; i++)
        {
            mergeTags[i] = String.Empty;
        }
    }

    public static string GetTag(int index)
    {
        if (index >= mergeTags.Length) return String.Empty;
        return mergeTags[index];
    }

    public static void SetTag(int index, string content)
    {
        if (index >= mergeTags.Length) return;
        mergeTags[index] = content;
    }

    public static void ClearAndSetTag(int index, string content)
    {
        if (index >= mergeTags.Length) return;
        ClearTags();
        mergeTags[index] = content;
    }

    public static string[] GetCopyOfCurrentMergeTags()
    {
        string[] copyOfTags = new string[mergeTags.Length];
        for (int i = 0; i < copyOfTags.Length; i++)
        {
            copyOfTags[i] = mergeTags[i];
        }
        return copyOfTags;
    }
}