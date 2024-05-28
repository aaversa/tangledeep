using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRepoolSelf : MonoBehaviour
{
    public float timeUntilRepool = 5f;

    public string poolName;

    float timeAtEnable;
     
    bool waitingToGetRepooled;

    void OnEnable()
    {
        timeAtEnable = Time.time;
        waitingToGetRepooled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!waitingToGetRepooled) return;

        if (Time.time - timeAtEnable >= timeUntilRepool)
        {
            waitingToGetRepooled = false;
            GameMasterScript.ReturnToStack(gameObject, poolName);
        }
    }
}
