using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{

    public static void AlignGameObjectToObject(GameObject thingToAlign, GameObject baseOfAlignment, Directions dir, float xOffset, float yOffset)
    {
        Transform trans = baseOfAlignment.transform;

        float spriteHeight = 0f;
        float spriteWidth = 0f;
        float centerX = 0f;
        float centerY = 0f;

        if (baseOfAlignment.GetComponent<SpriteRenderer>() != null)
        {
            spriteHeight = baseOfAlignment.GetComponent<SpriteRenderer>().sprite.bounds.extents.y;
            spriteWidth = baseOfAlignment.GetComponent<SpriteRenderer>().sprite.bounds.extents.x;
            centerX = baseOfAlignment.GetComponent<SpriteRenderer>().sprite.bounds.center.x;
            centerY = baseOfAlignment.GetComponent<SpriteRenderer>().sprite.bounds.center.y;
        }
        else
        {
        }

        if (baseOfAlignment == GameMasterScript.heroPC)
        {
            spriteHeight = 0.7f;
            spriteWidth = 0.6f;
            centerX = 0f;
            centerY = 0f;
        }
        else
        {
            Movable mv = baseOfAlignment.GetComponent<Movable>();
            if (mv != null)
            {
                if (mv.spriteHeight != 0f)
                {
                    spriteHeight = mv.spriteHeight;
                }
                else
                {
                    spriteHeight = 0.85f;
                }
                if (mv.spriteWidth != 0f)
                {
                    spriteWidth = mv.spriteWidth;
                }
                else
                {
                    spriteWidth = 0.6f;
                }
            }
        }

        Vector3 newPos = Vector3.zero;

        switch (dir)
        {
            case Directions.TRUENEUTRAL:
                newPos.x = trans.position.x + centerX;
                newPos.y = trans.position.y + centerY;
                break;
            case Directions.NEUTRAL:
            case Directions.NORTH:
                newPos.x = trans.position.x + centerX;
                newPos.y = trans.position.y + centerY + spriteHeight + 0.1f;
                break;
            case Directions.NORTHEAST:
                newPos.x = trans.position.x + centerX + spriteWidth;
                newPos.y = trans.position.y - (centerY / 1f) + spriteHeight;
                break;
            case Directions.EAST:
                newPos.x = trans.position.x + centerX + spriteWidth + 0.1f;
                newPos.y = trans.position.y + centerY;
                break;
            case Directions.SOUTHEAST:
                newPos.x = trans.position.x + centerX + spriteWidth;
                newPos.y = trans.position.y + (centerY / 1f) - spriteHeight;
                break;
            case Directions.SOUTH:
                newPos.x = trans.position.x + centerX;
                newPos.y = trans.position.y + (centerY / 1f) - spriteHeight;
                break;
            case Directions.SOUTHWEST:
                newPos.x = trans.position.x - centerX - spriteWidth;
                newPos.y = trans.position.y + (centerY / 1f) - spriteHeight;
                break;
            case Directions.WEST:
                newPos.x = trans.position.x - centerX - spriteWidth - 0.1f;
                newPos.y = trans.position.y + centerY;
                break;
            case Directions.NORTHWEST:
                newPos.x = trans.position.x - centerX - spriteWidth;
                newPos.y = trans.position.y - (centerY / 1f) + spriteHeight;
                break;
        }

        newPos.x += xOffset;
        newPos.y += yOffset;

        // This was working before the below code was added.
        //newPos.x += baseOfAlignment.transform.position.x;
        //newPos.y += baseOfAlignment.transform.position.y;


        // Should this be localPos?
        thingToAlign.transform.position = newPos;

        //Debug.Log(thingToAlign.name + " " + thingToAlign.transform.position + " " + thingToAlign.transform.localPosition + " " + dir);
    }

}