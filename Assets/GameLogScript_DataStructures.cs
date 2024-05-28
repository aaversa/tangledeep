using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameLogScript
{
    public class DividerDataPack
    {
        public int turnInserted;
        public GameObject obj;
    }
    public struct LogSpacingAndFontInfo
    {
        public int iLineSpacing;
        public int iFontSize;
        public int iMaxLinesOnScreen;
        public LogSpacingAndFontInfo(int spacing, int size, int maxOnScreen)
        {
            iLineSpacing = spacing;
            iFontSize = size;
            iMaxLinesOnScreen = maxOnScreen;
        }
    }
}