using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System;

public enum TDAnimTypes { WALKTOP, WALK, WALKSIDE, IDLETOP, IDLE, IDLESIDE, ATTACKTOP, ATTACK, ATTACKSIDE, USEITEM,
    TAKEDAMAGE, COUNT }

public class PlayerMods_Spritesheet {

    public string refName;
    public string spritesheetName;
    public int columns;
    public int rows;
    public string fileName;
    public Sprite[] choppedSprites;
    public Dictionary<TDAnimTypes, TDPlayerAnimData> dictAnimFrameData;
    public CharacterJobs replaceJob;
    public string replaceRef;
    public bool isPartialSheet;

    public PlayerMods_Spritesheet()
    {
        refName = "";
        replaceRef = "";
        dictAnimFrameData = new Dictionary<TDAnimTypes, TDPlayerAnimData>();
        spritesheetName = "";
        replaceJob = CharacterJobs.COUNT;
        isPartialSheet = false;

    }

    public TDPlayerAnimData GetAnimData(string animName)
    {
        List<string> alternates = new List<string>()
        {
            "Default",
            "default",
            "Idle",
            "idle"
        };

        string animNameLower = animName.ToLowerInvariant();        

        foreach (TDAnimTypes tType in dictAnimFrameData.Keys)
        {
            string tTypeLower = tType.ToString().ToLowerInvariant();

            if (tTypeLower == animNameLower)// || alternates.Contains(tTypeLower))
            {
                //Debug.Log("Searching for animation name: " + animName + ", we have found " + tType);
                return dictAnimFrameData[tType];
            }
        }
        return null;
    }

    public bool HasAnimData(string animName)
    {
        if (GetAnimData(animName) != null)
        {
            return true;
        }
        return false;
    }

    public void ChopUpSpritesheetFromPath(string path)
    {
        Sprite spr = null;

        byte[] getImageBytes = System.IO.File.ReadAllBytes(path);
        Texture2D basicTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        basicTexture.filterMode = FilterMode.Point;
        basicTexture.LoadImage(getImageBytes);
        fileName = path;

        float baseWidth = basicTexture.width / columns;
        float baseHeight = basicTexture.height / rows;

        //Debug.Log("Expected width x height of " + refName + ": " + baseWidth + "," + baseHeight);

        int numSprite = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                spr = Sprite.Create(basicTexture,
                    new Rect(x * baseWidth, y * baseHeight, baseWidth, baseHeight), 
                    new Vector2(0.5f, 0.5f), 32f);

                choppedSprites[(y * columns) + x] = spr;
                //Debug.Log("Sprite number " + numSprite + " from " + (x * baseWidth) + "," + (y * baseHeight) + " at index " + ((y * columns) + x));
                numSprite++;          
            }
        }        
    }

    public bool ConnectChoppedSpritesToDict()
    {
        foreach(TDAnimTypes tdType in dictAnimFrameData.Keys)
        {
            TDPlayerAnimData animData = dictAnimFrameData[tdType];
            foreach(TDPlayerAnimData.FrameData fd in animData.frames)
            {                
                fd.spr = choppedSprites[fd.spriteIndexInSheet];
                //Debug.Log(fileName + " For " + tdType + " connect " + fd.spr.name + " " + fd.spriteIndexInSheet);
            }
        }
        return true;
    }

    public bool ReadFromXml(string path)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;

        string fText = File.ReadAllText(path);

        if (string.IsNullOrEmpty(fText))
        {
            Debug.Log("No text in " + fText);
            return false;
        }

        XmlReader reader = XmlReader.Create(new StringReader(fText), settings);

        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {            
            switch (reader.Name.ToLowerInvariant())
            {                
                case "refname":
                    refName = reader.ReadElementContentAsString();
                    break;
                case "partialsheet":
                    isPartialSheet = GameMasterScript.simpleBool[reader.ReadElementContentAsInt()];
                    break;
                case "spritesheetname":
                    spritesheetName = reader.ReadElementContentAsString();
                    break;
                case "replacejob":
                    replaceJob = (CharacterJobs)Enum.Parse(typeof(CharacterJobs), reader.ReadElementContentAsString().ToUpperInvariant());
                    break;
                case "replaceref":
                case "replacemonster":
                case "replacenpc":
                case "replacemapobject":
                case "replacebattlefx":
                    replaceRef = reader.ReadElementContentAsString();
                    break;               
                case "numrows":
                    rows = reader.ReadElementContentAsInt();
                    break;
                case "numcolumns":
                    columns = reader.ReadElementContentAsInt();
                    break;
                case "animdata":
                    TDPlayerAnimData afd = new TDPlayerAnimData();
                    if (afd.ReadFromXml(reader))
                    {
                        foreach(TDAnimTypes aType in afd.animTypes)
                        {
                            if (dictAnimFrameData.ContainsKey(aType))
                            {
                                Debug.LogError("WARNING! Cannot add certain anim frame data in " + refName + " as the type " + aType + " already exists for this spritesheet.");
                            }
                            else
                            {
                                dictAnimFrameData.Add(aType, afd);
                            }
                        }
                    }
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        if (rows > 0 && columns > 0)
        {
            choppedSprites = new Sprite[columns * rows];
        }
        else
        {
            Debug.LogError("Spritesheet data " + refName + " did not have columns/rows defined, cannot import. " + rows + " " + columns);
            return false;
        }

        return true;
    }
}

public class TDPlayerAnimData
{
    public List<FrameData> frames;
    public List<TDAnimTypes> animTypes;

    public TDPlayerAnimData()
    {
        animTypes = new List<TDAnimTypes>();
        frames = new List<FrameData>();
    }

    public bool ReadFromXml(XmlReader reader)
    {
        reader.ReadStartElement();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name.ToLowerInvariant())
            {
                case "animtype":
                    animTypes.Add((TDAnimTypes)Enum.Parse(typeof(TDAnimTypes), reader.ReadElementContentAsString().ToUpperInvariant()));
                    break;
                case "framedata":
                    FrameData fd = new FrameData();
                    if (fd.ReadFromXml(reader))
                    {
                        frames.Add(fd);
                    }
                    //Debug.Log(fd.spriteIndexInSheet + " is for " + animTypes[0]);
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        reader.ReadEndElement();
        return true;
    }

    public class FrameData
    {
        public int spriteIndexInSheet;
        public float frameTime;
        public Sprite spr;

        public bool ReadFromXml(XmlReader reader)
        {
            reader.ReadStartElement();

            while(reader.NodeType != XmlNodeType.EndElement)
            {
                switch(reader.Name.ToLowerInvariant())
                {
                    case "spriteindex":
                        spriteIndexInSheet = reader.ReadElementContentAsInt();
                        break;
                    case "frametime":
                        string txt = reader.ReadElementContentAsString();
                        frameTime = CustomAlgorithms.TryParseFloat(txt);
                        break;
                    default:
                        reader.Read();
                        break;
                }
            }
            reader.ReadEndElement();
            return true;
        }
    }
}
