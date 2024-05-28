using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ElementalAuraManager : MonoBehaviour {

    public SpriteRenderer back;
    public SpriteRenderer front;

    public DamageTypes auraType;

    public const int NUM_FRAMES_IN_AURA = 16;
    public const int NUM_AURA_ELEMENTS = 5;
    public bool auraInitialized;
    public SpriteRenderer followSpriteRenderer;

    void Update()
    {
        if (followSpriteRenderer == null) return;

        back.color = followSpriteRenderer.color;
        front.color = followSpriteRenderer.color;
        back.enabled = followSpriteRenderer.enabled;
        front.enabled = followSpriteRenderer.enabled;
        
    }

    public void StopAndDie()
    {
        auraInitialized = false;
        GameMasterScript.ReturnToStack(gameObject, gameObject.name.Replace("(Clone)", string.Empty));
    }

    public void UpdateSpriteOrder(int baseOrder)
    {
        back.sortingOrder = baseOrder - 1;
        front.sortingOrder = baseOrder + 1;
    }

    public void Initialize(DamageTypes element, SpriteRenderer followSR)
    {
        if (followSR == null) return;
        //if (auraInitialized) return;

        followSpriteRenderer = followSR;
        auraType = element;

        Animatable frontAnim = front.gameObject.GetComponent<Animatable>();
        Animatable backAnim = back.gameObject.GetComponent<Animatable>();        

        for (int i = 0; i < NUM_FRAMES_IN_AURA; i++)
        {
            frontAnim.myAnimations[0].SetSpriteOnly(i, TDVisualEffects.GetElementalAuraSprite(element, true, i));

            backAnim.myAnimations[0].SetSpriteOnly(i, TDVisualEffects.GetElementalAuraSprite(element, false, i));
        }

        backAnim.SetAnim("Default");
        frontAnim.SetAnim("Default");

        gameObject.transform.localScale = Vector3.one;
        front.transform.localScale = Vector3.one;
        back.transform.localScale = Vector3.one;

        auraInitialized = true;
    }
}
