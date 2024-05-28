using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugMonsterInfo : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro txtDisplay;

    private Monster myMonster;

    private bool bActive;

    private List<string> strAdditionalMessages;

    void Awake()
    {
        strAdditionalMessages = new List<string>();
    }

    void Update()
    {
        if (txtDisplay == null)
        {
            //weird
            txtDisplay = GetComponentInChildren<TextMeshPro>();
            return;
        }

        if (!bActive && DebugConsole.IsOpen)
        {
            bActive = true;
            txtDisplay.enabled = true;
        }
        else if (bActive && !DebugConsole.IsOpen)
        {
            bActive = false;
            txtDisplay.enabled = false;
        }

        if (!bActive)
        {
            return;
        }

        //We're active, so talk about our monster info
        if (myMonster != null)
        {
            UpdateMonsterInfoDisplay();
        }


    }

    void UpdateMonsterInfoDisplay()
    {
#if UNITY_EDITOR
        //What are we thinking?
        string strAIState = myMonster.myBehaviorState.ToString();
        string strDisplay = strAIState;
        foreach (string s in strAdditionalMessages)
        {
            strDisplay += "\n" + s;
        }
        txtDisplay.text = strDisplay;

        //Who needs a beatdown?
        Actor targetActor = myMonster.myTarget;
        if (targetActor != null && targetActor.objectSet )
        {
            Color drawColor = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.timeSinceLevelLoad, 0.5f));
            Vector2 vTarget = targetActor.GetObject().transform.position;
            Debug.DrawLine(transform.position, vTarget,drawColor);

            //draw aggression arrows because Jim is old and can't
            //really suss out these lines otherwise
            float fArrowDist = (Time.timeSinceLevelLoad % 0.5f) / 0.5f;
            Vector2 vToTarget = vTarget - (Vector2)transform.position;

            //arrow starts here
            Vector2 vArrowCore = (Vector2)transform.position + vToTarget * fArrowDist;

            //arrow edges
            Vector2 vEdge1 = GameMasterScript.Rotate2DVector(vToTarget.normalized * 0.2f, 135.0f);
            Vector2 vEdge2 = GameMasterScript.Rotate2DVector(vToTarget.normalized * 0.2f, 225.0f);
            
            Debug.DrawLine(vArrowCore, vArrowCore + vEdge1, drawColor);
            Debug.DrawLine(vArrowCore, vArrowCore + vEdge2, drawColor);
        }

        //Where are we headed next?
        Vector2 vDest = myMonster.myTargetTile;
        if (vDest != Vector2.zero)
        {
            Debug.DrawLine(transform.parent.position, vDest, Color.green);
        }

        //If we have an anchor, we do so in DrawGizmos
#endif
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        try
        {
            if (!bActive || myMonster == null)
            {
                return;
            }

            //Do we have an anchor?
            if (myMonster.anchor != null && myMonster.anchor.GetObject() != null)
            {
                Vector2 vMyPos = myMonster.GetObject().transform.position;
                Vector2 vAnchorPos = myMonster.anchor.GetObject().transform.position;
                Vector2 vTowardAnchor = vAnchorPos - vMyPos;

                float fChainDistance = vTowardAnchor.magnitude; //pop pop
                vTowardAnchor.Normalize();

                const float fLinkSize = 0.3f;
                float fDistanceTraveled = 0f;
                bool bBigLink = true;
                while (fDistanceTraveled < fChainDistance)
                {
                    float fRaidus = bBigLink ? 0.2f : 0.05f;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(vMyPos + vTowardAnchor * fDistanceTraveled, fRaidus);
                    fDistanceTraveled += fLinkSize;
                    bBigLink = !bBigLink;
                }
            }
        }
        catch
        {
            if (myMonster != null)
            {
                Debug.Log("Something weird going on with the anchor for actor " + myMonster.actorRefName + " " + myMonster.actorUniqueID + ", but it's just for debug drawing and editor only.");
            }
        }
    }
#endif

    public void SetMonster(Monster mon)
    {
        myMonster = mon;
    }

    public void AddAdditionalMessage(string strMsg)
    {
        if (!strAdditionalMessages.Contains(strMsg))
        {
            strAdditionalMessages.Add(strMsg);
        }
    }

    public void ClearAdditionalMessages()
    {
        strAdditionalMessages.Clear();
    }
}
