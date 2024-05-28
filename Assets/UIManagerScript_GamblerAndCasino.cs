using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System;

public partial class UIManagerScript
{


    public void TryEnableGamblerHand()
    {
        if (cardsInUIArea.Count > 0)
        {
            gamblerHand.SetActive(true);
        }
    }

    public void DisableGamblerHand()
    {
        if (uiPlayerSkillInfo.text != "")
        {
            gamblerHand.SetActive(false);
        }
    }

    public void RefreshGamblerHandDisplay(bool clearHand, bool onlyClear)
    {
        if (!PlayingCard.initialized) // bad code, don't do this here
        {
            PlayingCard.CreateDeck();
            PlayingCard.initialized = true;
        }
        if (clearHand)
        {
            foreach (GameObject playingCard in gamblerHandObjects)
            {
                GameMasterScript.ReturnToStack(playingCard, playingCard.name.Replace("(Clone)", String.Empty));
                //Debug.Log("Destroying " + playingCard.name);
            }
            gamblerHandObjects.Clear();
            cardsInUIArea.Clear();
        }

        if (onlyClear) return;

        if (GameMasterScript.heroPCActor.gamblerHand.Count == 0)
        {
            gamblerHand.SetActive(false);
        }
        else
        {
            gamblerHand.SetActive(true);
            foreach (PlayingCard pc in GameMasterScript.heroPCActor.gamblerHand)
            {
                if (cardsInUIArea.Contains(pc))
                {
                    continue;
                }
                int index = ((int)pc.suit) * 13 + (int)pc.face;
                Sprite nSprite = playingCardSprites[index];
                //GameObject card = (GameObject)Instantiate(Resources.Load("PokerCard"));
                GameObject card = GameMasterScript.TDInstantiate("PokerCard");
                card.transform.SetParent(gamblerHand.transform);
                card.transform.localScale = Vector3.one;
                Image img = card.GetComponent<Image>();
                img.sprite = nSprite;
                cardsInUIArea.Add(pc);
                gamblerHandObjects.Add(card);
            }
        }
    }

    public string ReturnGamblerHandText()
    {
        PokerHands hand = PlayingCard.theDeck[0].EvaluatePlayerHand();
        string txt = PlayingCard.handNames[(int)hand] + ": " + PlayingCard.handEffects[(int)hand];
        return txt;
    }

    public void GetPlayerGamblerHandText()
    {
        if (abilityTargeting && abilityInTargeting.refName != "skill_wildcards")
        {
            return;
        }
        if (GameMasterScript.heroPCActor.gamblerHand.Count > 0)
        {
            ShowGenericInfoBar();
            forceShowInfoBar = true;
            PokerHands hand = PlayingCard.theDeck[0].EvaluatePlayerHand();
            SetInfoText(PlayingCard.handNames[(int)hand] + ": " + PlayingCard.handEffects[(int)hand]);
        }
    }

    public void ExitCasino()
    {
        CloseSlotsGame();
        CloseBlackjackGame();
    }

    public static void CloseSlotsGame()
    {
        casinoGameOpen = false;
        slotsGame.SetActive(false);
        singletonUIMS.DisableCursor();
    }

    public static void CloseBlackjackGame()
    {
        casinoGameOpen = false;
        blackjackGame.SetActive(false);
        singletonUIMS.DisableCursor();
    }
}
