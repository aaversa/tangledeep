using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarGlowComponent : MonoBehaviour
{
    [Header("Glowy Bois")]
    [Tooltip("Please keep ordered from leftmost button to right.")]
    public List<Image> listGlowyBois;
    
    [SerializeField] private Color maxGlowColor;
    [SerializeField] private Color minGlowColor;

    public float fGlowSinPeriod;

    public Color selectedColor;
    public Color onCooldownColor;

    private bool bActive;

	// Use this for initialization
	void Start ()
	{
        //make sure these are chilled out from the get go
	    foreach (var boi in listGlowyBois)
	    {
	        boi.enabled = false;
	    }
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (bActive && GameMasterScript.actualGameStarted)
        {
            //basic ping-pongy glow
            for (int t = 0; t < listGlowyBois.Count; t++)
            {
                //this is the current selected shadowObject
                //current hotbar code starts the skill hotbar stuff at 3
                const int iHotbarOffset = 3;

                //hotbars past 0 increase our count by 8
                int iBarIndex = t + iHotbarOffset + UIManagerScript.GetIndexOfActiveHotbar() * 8;
                var shadowObject = UIManagerScript.hudHotbarAbilities[iBarIndex];

                if (shadowObject == null)
                {
                    continue;
                }

                //here's us!
                var boi = listGlowyBois[t];
                
                //check for the ability assigned to this button. This array does not 
                //use the hotbar offset
                var hbBindable = UIManagerScript.hotbarAbilities[iBarIndex - iHotbarOffset];
                var ability = hbBindable.ability;

                //if the ability is on cooldown, let's glow red.
                if (ability != null && ability.GetCurCooldownTurns() > 0)
                {
                    boi.color = onCooldownColor;
                    continue;
                }

                //this is the color we want to glow if not selected
                Color glowColor = Color.Lerp(minGlowColor, maxGlowColor, Mathf.PingPong(Time.realtimeSinceStartup, fGlowSinPeriod));

                //if there's nothing in here at all, don't glow.
                if ( string.IsNullOrEmpty(hbBindable.GetHotbarActionInfo()))
                {
                    glowColor = Color.clear;
                }
                
                //If our current boi is selected, give it a special glow
                //otherwise use the glow we decided on
                boi.color = UIManagerScript.uiObjectFocus == shadowObject ? selectedColor : glowColor;

            }
        }
       
	}



    /// <summary>
    /// Activate glow routines beep boop. Used when we start targeting the hotbar.
    /// </summary>
    public void StartGlowing()
    {
        if (bActive)
        {
            return;
        }

        bActive = true;
        float fDelay = 0.3f;
        foreach (var boi in listGlowyBois)
        {
            boi.enabled = true;
            var rt = boi.rectTransform;
            rt.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            LeanTween.scale(rt, Vector3.one, 0.5f).setEaseInOutBounce().setDelay(fDelay) ;
            fDelay += 0.02f;
        }

    }

    /// <summary>
    /// Turn off all glowing, usually used when we are no longer targeting the hotbar
    /// </summary>
    public void StopGlowing()
    {
        if (!bActive)
        {
            return;
        }

        bActive = false;
        foreach( var boi in listGlowyBois)
        {
            boi.enabled = false;
        }
    }
}

public partial class UIManagerScript
{
    [Tooltip("Controls the flashy glowy aspect on our hotbars")]
    public HotbarGlowComponent hotbarGlowComponent;
}
