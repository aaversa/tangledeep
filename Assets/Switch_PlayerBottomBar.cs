using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Switch_PlayerBottomBar : MonoBehaviour
{

    public TextMeshProUGUI playerGold;
    public TextMeshProUGUI playerJP;

    private int cachedMoney;
    private int cachedJP;
    void Start()
    {
        // No need to localize anything... for now?
        cachedMoney = -1;
        cachedJP = -1;
    }

    void Update()
    {
        if (GameMasterScript.actualGameStarted)
        {
            UpdateJPAndGold();
        }
    }
    public void UpdateJPAndGold()
    {
        var newMoney = GameMasterScript.heroPCActor.GetMoney();
        if (newMoney != cachedMoney)
        {
            playerGold.text = newMoney.ToString();
            cachedMoney = newMoney;
        }
        var newJP = (int)GameMasterScript.heroPCActor.GetCurJP();
        if (newJP != cachedJP)
        {
            playerJP.text = newJP.ToString();
            cachedJP = newJP;
        }
    }

    // Reminder of how the array is setup.
    /* 
             hudHotbarAbilities[0] = hudHotbarFlask;
            hudHotbarAbilities[1] = hudHotbarPortal;
            hudHotbarAbilities[2] = hudHotbarSnackBag;
            hudHotbarAbilities[3] = hudHotbarSkill1;
            hudHotbarAbilities[4] = hudHotbarSkill2;
            hudHotbarAbilities[5] = hudHotbarSkill3;
            hudHotbarAbilities[6] = hudHotbarSkill4;
            hudHotbarAbilities[7] = hudHotbarSkill5;
            hudHotbarAbilities[8] = hudHotbarSkill6;
            hudHotbarAbilities[9] = hudHotbarSkill7;
            hudHotbarAbilities[10] = hudHotbarSkill8;
    */

    // Connect all objects
    // Top row: abilities 1-4
    // Bottom row: 5-8
    public static void SetupNavigation()
    {
        UIManagerScript.hudHotbarAbilities[0].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarSkill8;

        // Top row abilities north: Cycle hotbars
        for (int i = 3; i < 7; i++)
        {
            UIManagerScript.hudHotbarAbilities[i].directionalActions[(int)Directions.NORTH] = UIManagerScript.singletonUIMS.CycleHotbars;
            UIManagerScript.hudHotbarAbilities[i].directionalValues[(int)Directions.NORTH] = -1;

            // 2nd hotbar also needs nav, same thing
            UIManagerScript.hudHotbarAbilities[i+8].directionalActions[(int)Directions.NORTH] = UIManagerScript.singletonUIMS.CycleHotbars;
            UIManagerScript.hudHotbarAbilities[i+8].directionalValues[(int)Directions.NORTH] = -1;

            if (i < 6) // Move to the right...
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[i + 1];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[i + 9];
            }
            else // Wrap around
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[3];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[11];
            }

            if (i > 3) // Move to the left...
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[i - 1];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[(i+8) - 1];
            }
            else // Wrap around
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[6];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[14];
            }

            // South moves to the next row of abilities
            UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.SOUTH] = UIManagerScript.hudHotbarAbilities[i + 4];
            UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.SOUTH] = UIManagerScript.hudHotbarAbilities[i + 12];
        }

        // Bottom row abilities NORTH: upper row of abilities
        for (int i = 7; i < 11; i++)
        {
            UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.NORTH] = UIManagerScript.hudHotbarAbilities[i - 4];
            UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.NORTH] = UIManagerScript.hudHotbarAbilities[(i+8) - 4];

            // Bottom row SOUTH: cycle
            UIManagerScript.hudHotbarAbilities[i].directionalActions[(int)Directions.SOUTH] = UIManagerScript.singletonUIMS.CycleHotbars;
            UIManagerScript.hudHotbarAbilities[i].directionalValues[(int)Directions.SOUTH] = 1;

            UIManagerScript.hudHotbarAbilities[i+8].directionalActions[(int)Directions.SOUTH] = UIManagerScript.singletonUIMS.CycleHotbars;
            UIManagerScript.hudHotbarAbilities[i+8].directionalValues[(int)Directions.SOUTH] = 1;

            if (i < 10) // Move to the right...
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[i + 1];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[i + 9];
            }
            else // Go to flask
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[0];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.EAST] = UIManagerScript.hudHotbarAbilities[0];
            }

            if (i > 7) // Move to the left...
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[i - 1];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[(i+8) - 1];
            }
            else // Wrap around
            {
                UIManagerScript.hudHotbarAbilities[i].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[10];
                UIManagerScript.hudHotbarAbilities[i+8].neighbors[(int)Directions.WEST] = UIManagerScript.hudHotbarAbilities[18];
            }
        }

    }
}
