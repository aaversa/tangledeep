using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_InteractableConsoleFinalArea : MonoBehaviour
{
    [Header("Sprites to use to build animations")]
    public Texture2D texNormal;
    private Sprite[] spritesNormal;

    public Texture2D texAlartSymbol;
    private Sprite[] spritesAlartSymbol;

    public Texture2D texAlartText;
    private Sprite[] spritesAlartText;

    public Animatable myAnimatable;

    [Header("Dialog Info")]
    [Tooltip("What meta flag am I checking to see if I should be upset or not?")]
    public string FlagToCheckForAlertStatus;

    // 0 == alert/bad, 1 == everything is ok.
    private int iAlertStatus = -999;
    private ImpactCoroutineWatcher currentAnimRoutineWatcher;

    [Tooltip("What dialog branch should I activate when I am bumped into?")]
    public string DialogStartBranch;

    private NPC myNPC;

    private SpriteRenderer[] srInKids;

    void Start()
    {
        BuildAnimations();
        srInKids = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (myNPC == null)
        {
            myNPC = GetComponent<Movable>().GetOwner() as NPC;
            myNPC.strOverrideConversationStartingBranch = DialogStartBranch;
        }

        //check alart update
        int iNewStatus = Math.Max(GameMasterScript.heroPCActor.ReadActorData(FlagToCheckForAlertStatus), MetaProgressScript.ReadMetaProgress(FlagToCheckForAlertStatus));
        if (iNewStatus != iAlertStatus)
        {
            iAlertStatus = iNewStatus;
            if (currentAnimRoutineWatcher != null)
            {
                currentAnimRoutineWatcher.StopCoroutine();
            }

            if (iAlertStatus < 1)
            {
                currentAnimRoutineWatcher = new ImpactCoroutineWatcher();
                StartCoroutine(currentAnimRoutineWatcher.StartCoroutine(LoopAlartAnims()));
            }
            else
            {
                currentAnimRoutineWatcher = new ImpactCoroutineWatcher();
                StartCoroutine(currentAnimRoutineWatcher.StartCoroutine(LoopNormalAnims()));
            }
        }

        //fade according to our parent
        for (int t = 1; t < srInKids.Length; t++)
        {
            //srInKids[t].color = srInKids[0].color;
            srInKids[t].enabled = srInKids[0].enabled;
        }
    }

    IEnumerator LoopAlartAnims()
    {
        while (true)
        {
            switch (UnityEngine.Random.Range(0, 2))
            {
                case 1:
                    myAnimatable.SetAnim("alart_symbol");
                    yield return new WaitForSeconds( 7.0f);
                    break;
                case 0:
                    myAnimatable.SetAnim("alart_text");
                    yield return new WaitForSeconds(3.0f);
                    break;
            }

        }
    }

    //Yes, this is bad right now, but we might eventually
    //have more than one normal anim, for looks.
    IEnumerator LoopNormalAnims()
    {
        while (true)
        {
            myAnimatable.SetAnim("normal");
            yield return null;
        }
    }


    void BuildAnimations()
    {
        spritesNormal = Resources.LoadAll<Sprite>("NPCs/Spritesheets/" + texNormal.name);
        spritesAlartSymbol = Resources.LoadAll<Sprite>("NPCs/Spritesheets/" + texAlartSymbol.name);
        spritesAlartText = Resources.LoadAll<Sprite>("NPCs/Spritesheets/" + texAlartText.name);

        var normalAnim = new zirconAnim();
        var alartSymbolAnim = new zirconAnim();
        var alartTextAnim = new zirconAnim();

        //normal anim
        var animFrames = new List<zirconAnim.AnimationFrameData>();
        zirconAnim.AnimationFrameData fram;

        //0-7 draw first line
        for (int t = 0; t < 8; t++)
        {
            fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spritesNormal[t];
            fram.spriteTime = 0.05f;
            fram.opacity = 1.0f;
            animFrames.Add(fram);
        }

        //loop frames 8 and 9 for a bit
        for (int t = 0; t < 12; t++)
        {
            fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spritesNormal[8];
            fram.spriteTime = 0.2f;
            fram.opacity = 1.0f;
            animFrames.Add(fram);

            fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spritesNormal[9];
            fram.spriteTime = 0.2f;
            fram.opacity = 1.0f;
            animFrames.Add(fram);
        }

        //add the rest
        for (int t = 10; t < 37; t++)
        {
            fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spritesNormal[t];
            fram.spriteTime = 0.05f;
            fram.opacity = 1.0f;
            animFrames.Add(fram);
        }

        normalAnim.animName = "normal";
        normalAnim.setSprite(animFrames);
        normalAnim.completionLogic = "Loop";
        normalAnim.startOpacity = 1.0f;
        normalAnim.startScale = 1.0f;

        myAnimatable.myAnimations.Add(normalAnim);

        //alart symbol anim
        animFrames = new List<zirconAnim.AnimationFrameData>();
        
        for (int t = 0; t < 6; t++)
        {
            fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spritesAlartSymbol[t];
            fram.spriteTime = 0.05f;
            fram.opacity = 1.0f;
            animFrames.Add(fram);
        }
        alartSymbolAnim.animName = "alart_symbol";
        alartSymbolAnim.setSprite(animFrames);
        alartSymbolAnim.completionLogic = "Loop";
        alartSymbolAnim.startOpacity = 1.0f;
        alartSymbolAnim.startScale = 1.0f;
        myAnimatable.myAnimations.Add(alartSymbolAnim);


        //alart text anim
        animFrames = new List<zirconAnim.AnimationFrameData>();

        for (int t = 0; t < 12; t++)
        {
            fram = new zirconAnim.AnimationFrameData();
            fram.mySprite = spritesAlartText[t];
            fram.spriteTime = 0.08f;
            fram.opacity = 1.0f;
            animFrames.Add(fram);
        }

        alartTextAnim.animName = "alart_text";
        alartTextAnim.setSprite(animFrames);
        alartTextAnim.completionLogic = "Loop";
        alartTextAnim.startOpacity = 1.0f;
        alartTextAnim.startScale = 1.0f;

        myAnimatable.myAnimations.Add(alartTextAnim);
    }

}
