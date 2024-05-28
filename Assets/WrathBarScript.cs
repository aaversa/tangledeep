using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WrathBarScript : MonoBehaviour {

    public SpriteRenderer[] wrathCounters;
    

	public void SetWrathCharges(int num)
    {
        if (num < 0) num = 0;
        if (num > 5) num = 5;

        for (int i = 0; i < 5; i++)
        {
            wrathCounters[i].color = UIManagerScript.transparentColor;
        }

        for (int i = 0; i < 5; i++)
        {
            if (i < num)
            {
                wrathCounters[i].color = Color.white;
            }         
        }
    }

    public void UpdateWrathCount(int amount)
    {
        if (!GameMasterScript.gameLoadSequenceCompleted) return;        
        SetWrathCharges(amount);
    }

    public void ToggleWrathBar(bool state)
    {
        gameObject.SetActive(state);
        if (state && wrathCounters[0] == null)
        {
            Debug.Log("Wrath counter 1 is null");
        }
    }
}
