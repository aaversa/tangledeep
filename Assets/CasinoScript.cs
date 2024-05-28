using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

public enum CasinoGameType { SLOTS, BLACKJACK, CEELO, COUNT };
public enum GameResults { LOSE, WIN, TIE, COUNT };
public enum CardSuit { CLUBS, SPADES, DIAMONDS, HEARTS };
public enum CardFace { ACE, TWO, THREE, FOUR, FIVE, SIX, SEVEN, EIGHT, NINE, TEN, JACK, QUEEN, KING };
public enum PokerHands { HIGHCARD, ONEPAIR, TWOPAIR, THREEOFAKIND, STRAIGHT, FLUSH, FULLHOUSE, FOUROFAKIND, STRAIGHTFLUSH, ROYALFLUSH, COUNT };

public enum CasinoBetType { GOLD, TOKENS, COUNT };

public enum CeeloScore { AUTOLOSE, AUTOWIN, POINT, TRIPLE, REROLL, PISS, COUNT };

public class PlayingCard
{
    public CardSuit suit;
    public CardFace face;

    public static List<PlayingCard> theDeck;
    public static bool initialized = false;
    public static string[] faceNames;
    public static string[] suitNames;
    public static string[] handNames;
    public static string[] handEffects;

    const int kMinimumCardValueForGamblerRank2 = 7; //face value, not position in array

    static List<CardSuit> suitsContained;
    static int[] countFaces;
    static bool[] handsPossible;

    public void ReturnToDeck()
    {
        theDeck.Add(this);
    }

    public static void Reset()
    {
        initialized = false;
    }

    public static PlayingCard DrawCard()
    {
        if (!initialized)
        {
            CreateDeck();
            initialized = true;            
        }
        theDeck.Shuffle();
        if (theDeck.Count == 0)
        {
            CreateDeck();
            initialized = true;
        }
        PlayingCard returnPC = theDeck[0];
        theDeck.Remove(returnPC);
        return returnPC;
    }

    public static PlayingCard DrawSpecificCard(CardSuit suit, CardFace face)
    {
        if (!initialized)
        {
            CreateDeck();
        }

        PlayingCard cardToDraw = null;
        foreach(PlayingCard pc in theDeck)
        {
            if ((pc.suit == suit) && (pc.face == face))
            {
                cardToDraw = pc;
                break;
            }
        }
        if (cardToDraw != null)
        {
            theDeck.Remove(cardToDraw);
            return cardToDraw;
        }

        cardToDraw = new PlayingCard();
        cardToDraw.face = face;
        cardToDraw.suit = suit;
        Debug.Log("Couldn't find card in deck: " + suit + " " + face + ", so we're creating it.");
        return cardToDraw;

    }

    public static int GetGamblerLevel()
    {
        if (GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.GAMBLER && !RandomJobMode.IsCurrentGameInRandomJobMode())
        {
            return 0;
        }
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("gamblercards3"))
        {
            return 2;
        }
        if (GameMasterScript.heroPCActor.myStats.CheckHasStatusName("gamblercards2"))
        {
            return 1;
        }
        return 0;
    }

    public static void ReturnCard(PlayingCard pc)
    {
        theDeck.Add(pc);
    }

    public static void DiscardAndRedrawHand()
    {
        foreach(PlayingCard pc in GameMasterScript.heroPCActor.gamblerHand)
        {
            ReturnCard(pc);
        }
        for (int i = 0; i < 5; i++)
        {
            GameMasterScript.heroPCActor.DrawWildCard(redrawingHand:true);
        }        
        UIManagerScript.singletonUIMS.RefreshGamblerHandDisplay(true, true);
    }


    public static bool IsCardOkForHand(PlayingCard pc)
    {
        int gLevel = GetGamblerLevel();
        List<PlayingCard> hand = GameMasterScript.heroPCActor.gamblerHand;
        
        //Gambler level 1 -- increased chance of a flush.
        //if the player already has a card in hand, card[0] determines what suit cannot be drawn
        //for the rest of the hand. This gives us the effect of reducing suits from 1 in 4 to 1 in 3,
        //while still allowing lots of natural hands to show up.
        if (gLevel >= 1 && hand != null && hand.Count > 0 )
        {
            //first card
            PlayingCard firstCard = hand[0];

            //that card determines the suit we won't allow
            CardSuit rejectedSuit = (CardSuit) (((int) firstCard.suit + 1) % 4);
            CardSuit secondaryRejectedSuit = (CardSuit)(((int)firstCard.suit + 2) % 4); // further increase flush chance.

            if ( pc.suit == rejectedSuit )
            { 
                //this card is not ok.
                return false;
            }
            if (UnityEngine.Random.Range(0,1f) <= 0.33f && (int)pc.face != (int)hand[0].face)
            {
                return false;
            }
        }

        //Gambler level 2 -- drop anything that isn't an ace, and less than kMinimumCardValueForGamblerRank2
        if ((gLevel >= 2) && 
            pc.face != CardFace.ACE &&
            ((int)pc.face) < kMinimumCardValueForGamblerRank2)
        {
            return false;
        }


        return true;
    }

    public static void RefreshDeck()
    {
        if (!initialized)
        {
            CreateDeck();
        }

        //No longer modifying the deck based on the player's hand,
        //instead we will modify what we draw!
        #region old and busted card logic
        /*
        foreach(PlayingCard pc in theDeck)
        {
            if (gamblerLevel >= 1)
            {
                if ((int)pc.suit > (int)CardSuit.SPADES)
                {
                    cardsToRemove.Add(pc);
                    //Debug.Log("Removing " + pc.face + " " + pc.suit);
                    continue;
                }
            }

            if (gamblerLevel >= 2)
            {
                if (((int)pc.face < 6) && ((int)pc.face != 0))
                {
                    cardsToRemove.Add(pc);
                    //Debug.Log("Removing " + pc.face + " " + pc.suit);
                }
            }
        }

        foreach(PlayingCard pc in cardsToRemove)
        {
            theDeck.Remove(pc);
        }
        */
#endregion

        //Remove cards from the deck that are already in the player's hand
        foreach(PlayingCard pc in GameMasterScript.heroPCActor.gamblerHand)
        {
            theDeck.Remove(pc);
        }

        //Debug.Log(theDeck.Count);
        theDeck.Shuffle();
        
    }

    public PokerHands EvaluatePlayerHand(bool sortAceHigh = false)
    {
        List<PlayingCard> hand = GameMasterScript.heroPCActor.gamblerHand;
        PokerHands bestHand = PokerHands.HIGHCARD;
        PokerHands alternateHand = PokerHands.HIGHCARD;

        if (hand.Count == 1)
        {
            bestHand = PokerHands.HIGHCARD;
        }

        hand.Sort((a, b) => (a.CompareTo(b)));

        if (hand.Count == 5)
        {
            if (hand[0].face == (int)CardFace.ACE)
            {
                if (sortAceHigh)
                {
                    PlayingCard aceToRemove = hand[0];
                    hand.Remove(aceToRemove);
                    hand.Add(aceToRemove);
                }
                else
                {
                    // We have to try the ace in both positions.
                    alternateHand = EvaluatePlayerHand(true);
                }
            }
        }
        
        for (int i = 0; i < countFaces.Length; i++)
        {
            countFaces[i] = 0;
        }

        for (int i = 0; i < handsPossible.Length; i++)
        {
            handsPossible[i] = false;
        }        

        for (int i = 0; i < handsPossible.Length; i++)
        {
            handsPossible[i] = true;
        }

        if (hand.Count < 5)
        {
            handsPossible[(int)PokerHands.STRAIGHTFLUSH] = false;
            handsPossible[(int)PokerHands.ROYALFLUSH] = false;
            handsPossible[(int)PokerHands.STRAIGHT] = false;
            handsPossible[(int)PokerHands.FLUSH] = false;
            handsPossible[(int)PokerHands.FULLHOUSE] = false;
        }
        if (hand.Count < 4)
        {
            handsPossible[(int)PokerHands.FOUROFAKIND] = false;
            handsPossible[(int)PokerHands.TWOPAIR] = false;
        }
        if (hand.Count < 3)
        {
            handsPossible[(int)PokerHands.THREEOFAKIND] = false;
        }
        if (hand.Count < 2)
        {
            return PokerHands.HIGHCARD;
        }

        int previousFaceValue = (int)hand[0].face;

        //List<CardSuit> suitsContained = new List<CardSuit>();
        suitsContained.Clear();

        for (int i = 0; i < hand.Count; i++)
        {
            PlayingCard pc = hand[i];
            countFaces[(int)pc.face] += 1;
            if (!suitsContained.Contains(pc.suit))
            {
                suitsContained.Add(pc.suit);
            }
            if ((int)pc.face < (int)CardFace.TEN)
            {
                handsPossible[(int)PokerHands.ROYALFLUSH] = false;
            }
            if (suitsContained.Count > 1)
            {
                handsPossible[(int)PokerHands.FLUSH] = false;
                handsPossible[(int)PokerHands.STRAIGHTFLUSH] = false;
                handsPossible[(int)PokerHands.ROYALFLUSH] = false;
            }
            /* if (suitsContained.Count > 2)
            {
                handsPossible[(int)PokerHands.FULLHOUSE] = false;
            } */
            if (handsPossible[(int)PokerHands.STRAIGHT])
            {
                bool aceHighStraight = false;
                if (i == 4 && previousFaceValue == (int)CardFace.KING) // 5th card
                {
                    if (pc.face ==  CardFace.ACE)
                    {
                        aceHighStraight = true;
                    }
                }
                if ((int)pc.face != previousFaceValue + 1 && i > 0 && !aceHighStraight)
                {
                    handsPossible[(int)PokerHands.STRAIGHT] = false;
                    handsPossible[(int)PokerHands.STRAIGHTFLUSH] = false;
                    handsPossible[(int)PokerHands.ROYALFLUSH] = false;
                }
                previousFaceValue = (int)pc.face;
            }
        }

        if (handsPossible[(int)PokerHands.ROYALFLUSH])
        {
            return PokerHands.ROYALFLUSH;
        }

        int numPairs = 0;
        bool threeOfAKind = false;

        for (int i = 0; i < countFaces.Length; i++)
        {
            if (countFaces[i] == 4)
            {
                return PokerHands.FOUROFAKIND;
            }
            if (countFaces[i] == 3)
            {
                threeOfAKind = true;
            }
            if (countFaces[i] == 2)
            {
                numPairs++;
            }
        }

        if (handsPossible[(int)PokerHands.STRAIGHTFLUSH])
        {
            return PokerHands.STRAIGHTFLUSH;
        }

        //if (suitsContained.Count == 2)
        {
            if (threeOfAKind && numPairs == 1)
            {
                return PokerHands.FULLHOUSE;
            }
        }

        if (suitsContained.Count == 1 && handsPossible[(int)PokerHands.FLUSH])
        {
            return PokerHands.FLUSH;
        }

        if (handsPossible[(int)PokerHands.STRAIGHT])
        {
            return PokerHands.STRAIGHT;
        }

        if ((int)alternateHand >= (int)PokerHands.STRAIGHT)
        {
            return PokerHands.STRAIGHT;
        }

        if (threeOfAKind)
        {
            return PokerHands.THREEOFAKIND;
        }

        if (numPairs == 2)
        {
            return PokerHands.TWOPAIR;
        }

        if (numPairs == 1)
        {
            return PokerHands.ONEPAIR;
        }

        return bestHand;
    }

    public int CompareTo(PlayingCard compareCard)
    {
        if ((int)face > (int)compareCard.face)
        {
            //Debug.Log(face + " follows " + compareCard.face);
            return 1;
        }
        if ((int)face < (int)compareCard.face)
        {
            //Debug.Log(face + " is less than " + compareCard.face);
            return -1;
        }
        //Debug.Log(face + " is same as " + compareCard.face);
        return 0;
    }

    public static void CreateDeck()
    {
        if (suitsContained == null)
        {
            suitsContained = new List<CardSuit>();
        }

        if (countFaces == null)
        {
            countFaces = new int[13];
        }

        if (handsPossible == null)
        {
            handsPossible = new bool[(int)PokerHands.COUNT];
        }
        
        if (faceNames == null)
        {
            faceNames = new string[13];
            faceNames[(int) CardFace.ACE] = StringManager.GetString("card_name_ace");
            faceNames[(int)CardFace.TWO] = "2";
            faceNames[(int)CardFace.THREE] = "3";
            faceNames[(int)CardFace.FOUR] = "4";
            faceNames[(int)CardFace.FIVE] = "5";
            faceNames[(int)CardFace.SIX] = "6";
            faceNames[(int)CardFace.SEVEN] = "7";
            faceNames[(int)CardFace.EIGHT] = "8";
            faceNames[(int)CardFace.NINE] = "9";
            faceNames[(int)CardFace.TEN] = "10";
            faceNames[(int)CardFace.JACK] = StringManager.GetString("card_name_jack"); 
            faceNames[(int)CardFace.QUEEN] = StringManager.GetString("card_name_queen"); 
            faceNames[(int)CardFace.KING] = StringManager.GetString("card_name_king"); 
        }

        if (suitNames == null)
        {
            suitNames = new string[4];
            suitNames[(int)CardSuit.CLUBS] = StringManager.GetString("card_suit_clubs"); 
            suitNames[(int)CardSuit.HEARTS] = StringManager.GetString("card_suit_hearts");
            suitNames[(int)CardSuit.SPADES] = StringManager.GetString("card_suit_spades");
            suitNames[(int)CardSuit.DIAMONDS] = StringManager.GetString("card_suit_diamonds");
        }

        if (handNames == null)
        {
            handNames = new string[(int)PokerHands.COUNT];
            for (int i = 0; i < handNames.Length; i++)
            {
                handNames[i] = StringManager.GetString("poker_" + ((PokerHands)i).ToString().ToLowerInvariant());
            }
        }

        if (handEffects == null)
        {
            // #todo - Load from misc_strings
            handEffects = new string[(int)PokerHands.COUNT];
            for (int i = 0; i < handEffects.Length; i++)
            {
                handEffects[i] = StringManager.GetString("poker_handeffects_" + ((PokerHands)i).ToString().ToLowerInvariant());
            }
            // These effects are actually the same~
            handEffects[(int)PokerHands.FULLHOUSE] = StringManager.GetString("poker_handeffects_royalflush");
            handEffects[(int)PokerHands.STRAIGHTFLUSH] = StringManager.GetString("poker_handeffects_royalflush");
            handEffects[(int)PokerHands.FOUROFAKIND] = StringManager.GetString("poker_handeffects_royalflush");

        }

        theDeck = new List<PlayingCard>();
        int startI = 0;
        int startX = 0;

        for (int i = startI; i < 4; i++) // Suits
        {
            for (int x = startX; x < 13; x++) // Cards
            {
                PlayingCard pc = new PlayingCard();
                pc.suit = (CardSuit)i;
                pc.face = (CardFace)x;
                theDeck.Add(pc);
            }
        }

        initialized = true;
        RefreshDeck();
    }

}

public class CasinoScript { // was monobehavior, not necessary

    public static int playerBet;
    public static int moneyWon;
    public static CasinoGameType curGameType;
    public static CasinoGame curGame;
    public static float totalNumChips;
    public static CasinoBetType playerBetType;

    public static void SetGame(CasinoGameType cgt)
    {
        switch(cgt)
        {
            case CasinoGameType.SLOTS:
                curGame = new SlotsGame();                                
                break;
            case CasinoGameType.BLACKJACK:
                curGame = new BlackjackGame();
                break;
            case CasinoGameType.CEELO:
                curGame = new CeeloGame();
                break;
        }
        curGameType = cgt;
        curGame.UpdateHeader();
        moneyWon = 0;
        //Debug.Log("Rset player bet");
        playerBet = 0;
    }

    public static void PlayerWonGame(int goldWon, TextMeshProUGUI text)
    {
        playerBetType = CasinoBetType.TOKENS; // just do this for now, #todo make a feature where player can choose what to win?

        switch(playerBetType)
        {
            case CasinoBetType.GOLD:
                StringManager.SetTag(0, goldWon.ToString());
                GameMasterScript.heroPCActor.ChangeMoney(goldWon, doNotAlterFromGameMods: true);
                text.text = UIManagerScript.greenHexColor + StringManager.GetString("casino_win") + "</color> <color=yellow>" + StringManager.GetString("casino_win_gold") + "</color>";
                break;
            case CasinoBetType.TOKENS:
                int numTokens = CasinoScript.AddChipsToPool(playerBet);
                StringManager.SetTag(0, numTokens.ToString());
                text.text = UIManagerScript.greenHexColor + StringManager.GetString("casino_win") + "</color> <color=yellow>" + StringManager.GetString("casino_win_tokens") + "</color>";
                break;
        }        
    }

    public static int AddChipsToPool(int moneyBase)
    {
        float numChips = moneyBase / 100f;
        //Debug.Log("From " + moneyBase + " we earn " + numChips);
        totalNumChips += numChips;
        if (totalNumChips < 1f) return 0;
        int chipsForPlayer = Mathf.FloorToInt(totalNumChips);
        totalNumChips -= (float)chipsForPlayer;

        //Debug.Log("Which means we now earn " + totalNumChips);

        Item chips = LootGeneratorScript.CreateItemFromTemplateRef("item_casinochip", 1.0f, 0f, false);
        Consumable chipCon = chips as Consumable;
        chipCon.Quantity = chipsForPlayer;
        GameMasterScript.heroPCActor.myInventory.AddItem(chipCon, true);
        StringManager.SetTag(0, chipsForPlayer.ToString());

        GameLogScript.LogWriteStringRef("log_casino_earnchips");
        return chipsForPlayer;
    }

    public static void PlayCurrentGame(int bet)
    {
        //Debug.Log("BB is now " + bet);
        playerBet = bet;        
        curGame.PlayGame();
        curGame.UpdateHeader();
    }

    public static void SetBet(int bet)
    {
        playerBet = bet;
        curGame.UpdateHeader();
        //Debug.Log("Player bet is now: " + bet);        
    }

    public static void TakeAction(int action)
    {
        switch(curGameType)
        {
            case CasinoGameType.BLACKJACK:
                BlackjackGame bjg = curGame as BlackjackGame;
                switch(action)
                {
                    case 0:
                        bjg.HitMe();
                        break;
                    case 1:
                        bjg.Stand();
                        break;
                }
                break;
            case CasinoGameType.CEELO:
                CeeloGame clg = curGame as CeeloGame;
                clg.RollPlayerDice();
                break;
        }
    }
}

public class CasinoGame
{
    public virtual int PlayGame()
    {
        return 0;
    }

    public virtual void UpdateHeader()
    {

    }
}

public class CeeloGame : CasinoGame
{
    public int[] houseDiceRolls;
    public int[] playerDiceRolls;
    public int houseScore;
    public int playerScore;

    public const float CHANCE_PISS = 0.02f;
    

    public CeeloGame()
    {
        houseDiceRolls = new int[3];
        playerDiceRolls = new int[3];
    }

    public class ScorePackage
    {
        public CeeloScore scoreType;
        public int scoreValue;

        public ScorePackage()
        {
            scoreType = CeeloScore.COUNT;
            scoreValue = 0;
        }
    }

    public override void UpdateHeader()
    {
        StringManager.SetTag(0, CasinoScript.playerBet.ToString());
        string text = StringManager.GetString("casino_ceelo_introtext") + " " + StringManager.GetString("casino_bet");
        StringManager.SetTag(0, GameMasterScript.heroPCActor.GetMoney().ToString());
        text += " (<color=yellow>" + StringManager.GetString("ui_shop_money_normal") + "</color>)";
        UIManagerScript.blackjackHeader.text = text;
    }

    public void RollPlayerDice()
    {
        int[] playerRolls = DoRolls();

        ScorePackage sp = EvaluateScore(playerRolls, false);

        string playerText = StringManager.GetString("casino_ceelo_playerrolls") + " " + UIManagerScript.cyanHexColor + GetDiceDisplay(playerRolls) + " </color>";
        UIManagerScript.blackjackPlayerHand.text = playerText;

        switch (sp.scoreType)
        {
            case CeeloScore.AUTOLOSE:
                UIManagerScript.blackjackPlayerHand.text += UIManagerScript.redHexColor + " " + StringManager.GetString("casino_instant_lose");
                GameOver(GameResults.LOSE);
                break;
            case CeeloScore.PISS:
                UIManagerScript.blackjackPlayerHand.text += UIManagerScript.redHexColor + " " + StringManager.GetString("casino_ceelo_dice_off_table");
                playerText = StringManager.GetString("casino_ceelo_playerrolls") + " " + UIManagerScript.lightPurpleHexColor + "[X][X][X]" + "</color>";
                GameOver(GameResults.LOSE);
                break;
            case CeeloScore.AUTOWIN:
                UIManagerScript.blackjackPlayerHand.text += UIManagerScript.greenHexColor + " " + StringManager.GetString("casino_instant_win");
                GameOver(GameResults.WIN);
                break;
            case CeeloScore.POINT:
                playerScore = sp.scoreValue;
                UIManagerScript.blackjackPlayerHand.text += UIManagerScript.orangeHexColor + " " + StringManager.GetString("casino_score") + " " + playerScore + " </color>";
                if (playerScore > houseScore)
                {
                    GameOver(GameResults.WIN);
                }
                else if (playerScore == houseScore)
                {
                    GameOver(GameResults.TIE);
                }
                else
                {
                    GameOver(GameResults.LOSE);
                }
                break;
            case CeeloScore.REROLL:
                UIManagerScript.blackjackPlayerHand.text += " <color=yellow>" + StringManager.GetString("casino_ceelo_reroll") + " </color>";
                break;
        }
    }

    public int[] DoRolls()
    {
        int[] rolls = new int[3];
        for (int i = 0; i < 3; i++)
        {
            rolls[i] = UnityEngine.Random.Range(1, 7);
        }
        return rolls;
    }

    // This starts the game.
    public override int PlayGame()
    {

        UIManagerScript.blackjackResults.text = StringManager.GetString("casino_ceelo_waitrolls");

        // Dealer rolls first.
        int[] dealerRolls = DoRolls();

        ScorePackage dealerSP = EvaluateScore(dealerRolls, true);

        while (dealerSP.scoreType == CeeloScore.REROLL)
        {
            dealerRolls = DoRolls();
            dealerSP = EvaluateScore(dealerRolls,true);
        }

        UIManagerScript.blackjackDealerHand.text = StringManager.GetString("casino_ceelo_dealerrolls") + " <color=yellow>" + GetDiceDisplay(dealerRolls) + "</color>";
        UIManagerScript.blackjackPlayerHand.text = StringManager.GetString("casino_ceelo_playerrolls") + " ????";

        bool includeScore = true;

        if (dealerSP.scoreType == CeeloScore.AUTOWIN)
        {
            UIManagerScript.blackjackDealerHand.text += UIManagerScript.greenHexColor + " " + StringManager.GetString("casino_instant_win");
            GameOver(GameResults.LOSE);
            includeScore = false;
        }
        else if (dealerSP.scoreType == CeeloScore.AUTOLOSE)
        {
            UIManagerScript.blackjackDealerHand.text += UIManagerScript.redHexColor + " " + StringManager.GetString("casino_instant_lose");
            GameOver(GameResults.WIN);
            includeScore = false;
        }
        else
        {
        }

        houseScore = dealerSP.scoreValue;
        if (includeScore)
        {
            UIManagerScript.blackjackDealerHand.text += UIManagerScript.orangeHexColor + " " + StringManager.GetString("casino_score") + ": " + houseScore + "</color>";
        }        

        return 0;
    }

    public string GetDiceDisplay(int[] rolls)
    {
        string builder = "";
        for (int i = 0; i < rolls.Length; i++)
        {
            builder += "[" + rolls[i] + "]";
        }
        return builder;
    }

    public void GameOver(GameResults result)
    {
        UIManagerScript.SetCeeloPlaying(false);
        switch (result)
        {
            case GameResults.LOSE:
                // You lose!
                UIManagerScript.PlaySound("CasinoLose");
                UIManagerScript.blackjackResults.text = UIManagerScript.redHexColor + StringManager.GetString("casino_lose") + "</color>";
                break;
            case GameResults.TIE:
                UIManagerScript.PlaySound("CasinoWin");
                GameMasterScript.heroPCActor.ChangeMoney(CasinoScript.playerBet, doNotAlterFromGameMods: true);
                UIManagerScript.blackjackResults.text = "<color=yellow>" + StringManager.GetString("casino_tie") + "</color>";
                break;
            case GameResults.WIN:
                UIManagerScript.PlaySound("CasinoWin");

                CasinoScript.PlayerWonGame(CasinoScript.playerBet * 2, UIManagerScript.blackjackResults);

                /* int moneyBase = CasinoScript.playerBet; // was 2x prior to new token system                                                
                //GameMasterScript.heroPCActor.ChangeMoney(moneyBase);
                int numTokens = CasinoScript.AddChipsToPool(moneyBase);
                StringManager.SetTag(0, numTokens.ToString());
                UIManagerScript.blackjackResults.text = UIManagerScript.greenHexColor + StringManager.GetString("casino_win") + "</color> <color=yellow>" + StringManager.GetString("casino_win_tokens") + "</color>";
                */
                break;
        }
        UpdateHeader();
    }

    // Evaluates the array of three dice rolls and checks if a pair exists alongside the given nonPairNumber
    // We don't know if the nonPairNumber exists in the array. If it doesn't, return false immediately.
    private bool CheckForPair(int[] rolls, int nonPairNumber)
    {
        if (rolls[0] == rolls[1])
        {
            return nonPairNumber == rolls[2];
        }
        if (rolls[1] == rolls[2])
        {
            return nonPairNumber == rolls[0];
        }
        return false;
    }

    // Evaluates the array of three dice rolls and checks if a pair exists alongside the given nonPairNumber
    // We don't know if the nonPairNumber exists in the array. If it doesn't, return false immediately.
    public bool CheckForPair_OLD(int[] rolls, int nonPairNumber)
    {
        int npIndex = Array.IndexOf(rolls, nonPairNumber);

        if (npIndex < 0) return false; // We don't have the nonPairNumber. 

        int pairNum = -1;

        for (int i = 0; i < rolls.Length; i++)
        {
            if (i == npIndex) continue;
            if (pairNum == -1)
            {
                pairNum = rolls[i];
            }
            else if (rolls[i] == pairNum)
            {
                // We have a ONE and a pair of something not-one. That's a loss.
                return true;
            }
        }
        return false;
    }

    public ScorePackage EvaluateScore(int[] rolls, bool dealerRoll)
    {
        Array.Sort(rolls); // Sorts rolls low to high

        ScorePackage sp = new ScorePackage();

        //4,5,6 is a win!
        if (rolls[0] == 4 && rolls[1] == 5 && rolls[2] == 6)
        {
            // Auto win!
            sp.scoreType = CeeloScore.AUTOWIN;
            sp.scoreValue = 999;
            return sp;
        }

        //1,2,3 is lose
        if (rolls[0] == 1 && rolls[1] == 2 & rolls[2] == 3)
        {
            // Auto loss!
            sp.scoreType = CeeloScore.AUTOLOSE;
            sp.scoreValue = 0;
            return sp;
        }

        if (rolls[0] == rolls[1] && rolls[1] == rolls[2])
        {
            // Triple
            sp.scoreType = CeeloScore.AUTOWIN; // This is a triple, but it's also an auto win...
            sp.scoreValue = rolls[0];
            return sp;
        }

        //The dealer wins on any pair + a 6
        if (dealerRoll)
        {
            // This makes it pretty rough on the player.
            if (CheckForPair(rolls, 6))
            {
                sp.scoreType = CeeloScore.AUTOWIN;
                sp.scoreValue = 999;
                return sp;
            }
        }
        else
        {
            //if you aren't a gambler, there's a 2% chance you get a PISS.
            if (UnityEngine.Random.Range(0,1f) <= CHANCE_PISS && GameMasterScript.heroPCActor.myJob.jobEnum != CharacterJobs.GAMBLER)
            {
                sp.scoreType = CeeloScore.PISS;
                sp.scoreValue = 0;
                return sp;
            }
        }

        //If you roll a pair and 1, you auto lose.
        if (CheckForPair(rolls, 1))
        {
            sp.scoreType = CeeloScore.AUTOLOSE;
            sp.scoreValue = 0;
            return sp;
        }

        // Search for pairs.
        List<int> checkedRolls = new List<int>(3);
        int pairValue = 0;
        for (int i = 0; i < rolls.Length; i++)
        {
            if (checkedRolls.Contains(rolls[i]))
            {
                // We have a pair!
                pairValue = rolls[i];
            }
            checkedRolls.Add(rolls[i]);
        }

        //if you have no pairs, didn't roll 1,2,3 or 4,5,6, then just reroll.
        if (pairValue == 0)
        {
            sp.scoreType = CeeloScore.REROLL;
            return sp;
        }

        //If you have a pair, your score value is the number that isn't in the pair.
        for (int i = 0; i < rolls.Length; i++)
        {
            if (rolls[i] != pairValue)
            {
                sp.scoreValue = rolls[i];
                sp.scoreType = CeeloScore.POINT;
                return sp;
            }
        }

        //how does the code get here
        sp.scoreType = CeeloScore.REROLL;
        return sp;
    }

}

public class BlackjackGame : CasinoGame
{
    public static List<int> deckOfCards;
    public static string[] cardNames;
    // 1 is ace, 11 is jack, 12 is queen, 13 is king

    public List<int> dealerHand;
    public List<int> playerHand;
    public Stack<int> currentDeck;

    int dealerScore;
    int playerScore;
    public GameResults result;

    public BlackjackGame()
    {
        if (deckOfCards == null)
        {
            deckOfCards = new List<int>();

            for (int i = 1; i < 14; i++)
            {
                deckOfCards.Add(i);
                deckOfCards.Add(i);
                deckOfCards.Add(i);
                deckOfCards.Add(i);
            }

            //todo: find out if this needs to be localized
            cardNames = new string[13];
            cardNames[0] = "A";
            cardNames[1] = "2";
            cardNames[2] = "3";
            cardNames[3] = "4";
            cardNames[4] = "5";
            cardNames[5] = "6";
            cardNames[6] = "7";
            cardNames[7] = "8";
            cardNames[8] = "9";
            cardNames[9] = "10";
            cardNames[10] = "J";
            cardNames[11] = "Q";
            cardNames[12] = "K";
        }        

        dealerHand = new List<int>();
        playerHand = new List<int>();
        currentDeck = new Stack<int>();
        ShuffleNewDeck();
    }

    public override void UpdateHeader()
    {
        StringManager.SetTag(0, CasinoScript.playerBet.ToString());
        string text = StringManager.GetString("casino_blackjack_introtext") + " " + StringManager.GetString("casino_bet");
        StringManager.SetTag(0, GameMasterScript.heroPCActor.GetMoney().ToString());
        text += " (<color=yellow>" + StringManager.GetString("ui_shop_money_normal") + "</color>)";
        UIManagerScript.blackjackHeader.text = text;
    }

    void ShuffleNewDeck()
    {
        currentDeck.Clear();
        deckOfCards.Shuffle();
        for (int i = 0; i < deckOfCards.Count; i++)
        {            
            currentDeck.Push(deckOfCards[i]);
        }
    }

    void BuildDealerHand()
    {
        dealerHand.Clear();
        while (dealerScore <= 16)
        {
            int pick = currentDeck.Pop();
            dealerHand.Add(pick);
            dealerScore = EvaluateHandScore(dealerHand);
        }

        string dealerText = StringManager.GetString("casino_blackjack_dealerhand") + " ";

        for (int i = 0; i < dealerHand.Count; i++)
        {
            dealerText += "[" + cardNames[dealerHand[i]-1] + "] ";
        }

        UIManagerScript.blackjackDealerHand.text = dealerText;
    }

    int EvaluateHandScore(List<int> hand)
    {
        int score = 0;
        int acesBuffered = 0;
        foreach(int card in hand)
        {
            switch(card)
            {
                case 1:
                    acesBuffered++;
                    break;
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                    score += card;
                    break;
                case 11:
                case 12:
                case 13:
                    score += 10;
                    break;
            }
        }

        score = RecursiveAceLogic(acesBuffered, score);
        return score;

        /*
        if (acesBuffered == 0)
        {
            return score;
        }


        else 
        {
            for (int i = 0; i < acesBuffered; i++)
            {
                if (score <= 10)
                {
                    score += 11;
                }
                else
                {
                    score += 1;
                }
            }
        }

        return score;
        */
    }

    //every ACE can be 1 or 11. Calculate all the possible outcomes and pick the one that is closest to 21 without going over.
    int RecursiveAceLogic(int iNumAces, int iCurrentScore)
    {
        iNumAces--;
        if (iNumAces < 0)
        {
            return iCurrentScore;
        }

        int iHighScore = RecursiveAceLogic(iNumAces, iCurrentScore + 11);
        int iLowScore = RecursiveAceLogic(iNumAces, iCurrentScore + 1);

        return GetBestBlackjackScore(iHighScore, iLowScore);
    }

    int GetBestBlackjackScore(int a, int b)
    {
        int valA = 21 - a;
        int valB = 21 - b;

        //If a score is >=0 that means it is 21 or less.
        //We want the smallest score that is also not negative
        if (valA >= 0 && (valB < 0 || valA <= valB))
        {
            return a;
        }
        if (valB >= 0 && (valA < 0 || valB <= valA))
        {
            return b;
        }

        //both scores are negative, rip.
        return a;
    }


    void DealToPlayer(int cards)
    {
        for (int i = 0; i < cards; i++)
        {
            playerHand.Add(currentDeck.Pop());
        }

        playerScore = EvaluateHandScore(playerHand);

        string playerText = StringManager.GetString("casino_blackjack_playerhand") + " ";

        for (int i = 0; i < playerHand.Count; i++)
        {
            playerText += "[" + cardNames[playerHand[i]-1] + "] ";
        }

        UIManagerScript.blackjackPlayerHand.text = playerText;

    }

    public override int PlayGame()
    {
        dealerScore = 0;
        playerScore = 0;
        ShuffleNewDeck();
        playerHand.Clear();
        dealerHand.Clear();
        //BuildDealerHand();
        UIManagerScript.blackjackPlayerHand.text = StringManager.GetString("casino_blackjack_playerhand") + " ????";
        UIManagerScript.blackjackDealerHand.text = StringManager.GetString("casino_blackjack_dealerhand") + " ????";
        if (dealerScore > 21)
        {
            GameOver(GameResults.WIN);
            return 1;
        }
        DealToPlayer(2);
        if (playerScore > 21)
        {
            // Lose
            GameOver(GameResults.LOSE);
            return 0;
        }
        else if (playerScore == 21)
        {
            UIManagerScript.blackjackResults.text = StringManager.GetString("casino_blackjack_max"); // This text won't actually appear. Oops.
            GameOver(GameResults.WIN);            
            return 0;
        }
        UIManagerScript.blackjackResults.text = StringManager.GetString("casino_blackjack_prompt");
        return 0;
    }

    public void HitMe()
    {
        DealToPlayer(1);
        playerScore = EvaluateHandScore(playerHand);

        string playerText = StringManager.GetString("casino_blackjack_playerhand") + " ";

        for (int i = 0; i < playerHand.Count; i++)
        {
            playerText += "[" + cardNames[playerHand[i]-1] + "] ";
        }

        UIManagerScript.blackjackPlayerHand.text = playerText;

        if (playerScore > 21)
        {
            // Lose
            GameOver(GameResults.LOSE);
        }
    }

    public void Stand()
    {
        BuildDealerHand();
        if (dealerScore > 21)
        {
            GameOver(GameResults.WIN);
        }
        else if (playerScore < dealerScore)
        {
            // Lose
            GameOver(GameResults.LOSE);
        }
        else if (playerScore == dealerScore)
        {
            GameOver(GameResults.TIE);
        }
        else if (playerScore > dealerScore)
        {
            GameOver(GameResults.WIN);
        }
    }

    public void GameOver(GameResults result) {
        UIManagerScript.SetBlackjackPlaying(false);
        switch (result)
        {
            case GameResults.LOSE:
                // You lose!
                UIManagerScript.PlaySound("CasinoLose");
                UIManagerScript.blackjackResults.text = UIManagerScript.redHexColor + StringManager.GetString("casino_lose") + "</color>";
                break;
            case GameResults.TIE:
                UIManagerScript.PlaySound("CasinoWin");
                GameMasterScript.heroPCActor.ChangeMoney(CasinoScript.playerBet, doNotAlterFromGameMods:true);
                UIManagerScript.blackjackResults.text = "<color=yellow>" + StringManager.GetString("casino_tie") + "</color>";
                break;
            case GameResults.WIN:
                UIManagerScript.PlaySound("CasinoWin");
                int moneyBase = (int)(CasinoScript.playerBet); // was 1.5x in old system
                if (GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.GAMBLER)
                {
                    moneyBase = (int)(moneyBase * 1.15f);
                }
                //GameMasterScript.heroPCActor.ChangeMoney(moneyBase);
                int tokensWon = CasinoScript.AddChipsToPool(moneyBase);
                StringManager.SetTag(0, tokensWon.ToString());
                UIManagerScript.blackjackResults.text = UIManagerScript.greenHexColor + StringManager.GetString("casino_win") + "</color> <color=yellow>" + StringManager.GetString("casino_win_tokens") + "</color>";
                
                break;
        }
        UpdateHeader();
    }


}

public class SlotsGame : CasinoGame
{
    public string[] slotSymbolRef;
    public int[] slotSymbolValue;
    public float[] slotSymbolProbability;
    public int[] slotResults;

    public SlotsGame()
    {
        slotSymbolRef = new string[5];
        slotSymbolRef[0] = "assorteditems_560";
        slotSymbolRef[1] = "assorteditems_561";
        slotSymbolRef[2] = "assorteditems_562";
        slotSymbolRef[3] = "assorteditems_563";
        slotSymbolRef[4] = "assorteditems_564";

        slotSymbolValue = new int[5];
        slotSymbolValue[0] = (int)(1.5f * CasinoScript.playerBet);
        slotSymbolValue[1] = (int)(3f * CasinoScript.playerBet);
        slotSymbolValue[2] = (int)(6f * CasinoScript.playerBet);
        slotSymbolValue[3] = (int)(12f * CasinoScript.playerBet);
        slotSymbolValue[4] = (int)(25f * CasinoScript.playerBet);

        slotSymbolProbability = new float[5];
        slotSymbolProbability[0] = 0.53f;
        slotSymbolProbability[1] = 0.765f;
        slotSymbolProbability[2] = 0.89f;
        slotSymbolProbability[3] = 0.95f;
        slotSymbolProbability[4] = 1f;

        slotResults = new int[3];
    }

    public void RefreshSlotRewards()
    {
        slotSymbolValue[0] = 3 * CasinoScript.playerBet;
        slotSymbolValue[1] = 6 * CasinoScript.playerBet;
        slotSymbolValue[2] = 12 * CasinoScript.playerBet;
        slotSymbolValue[3] = 24 * CasinoScript.playerBet;
        slotSymbolValue[4] = 50 * CasinoScript.playerBet;

        if (GameMasterScript.heroPCActor.myJob.jobEnum == CharacterJobs.GAMBLER)
        {
            for (int i = 0; i < slotSymbolValue.Length; i++)
            {
                slotSymbolValue[i] = (int)((float)slotSymbolValue[i] * 1.15f);
            }
        }
    }

    public override void UpdateHeader()
    {
        StringManager.SetTag(0, CasinoScript.playerBet.ToString());
        string text = StringManager.GetString("casino_slots_introtext") + " " + StringManager.GetString("casino_bet");
        StringManager.SetTag(0, GameMasterScript.heroPCActor.GetMoney().ToString());
        text += " (<color=yellow>" + StringManager.GetString("ui_shop_money_normal") + "</color>)";
        UIManagerScript.slotsHeader.text = text;
    }

    public override int PlayGame()
    {
        float[] rolls = new float[3];
        rolls[0] = UnityEngine.Random.Range(0, 1f);
        rolls[1] = UnityEngine.Random.Range(0, 1f);
        rolls[2] = UnityEngine.Random.Range(0, 1f);

        slotResults[0] = 0;
        slotResults[1] = 0;
        slotResults[2] = 0;

        for (int i = 0; i < 3; i++)
        {
            for (int x = 0; x < slotSymbolProbability.Length; x++)
            {
                if (x == 0)
                {
                    if ((rolls[i] >= 0.0f) && (rolls[i] <= slotSymbolProbability[0]))
                    {
                        slotResults[i] = x;
                        break;
                    }
                }
                else
                {
                    if ((rolls[i] >= slotSymbolProbability[x-1]) && (rolls[i] <= slotSymbolProbability[x]))
                    {
                        slotResults[i] = x;
                        break;
                    }
                }
            }
        }

        string[] slotSymbols = new string[3];
        for (int i = 0; i < 3; i++)
        {
            slotSymbols[i] = slotSymbolRef[slotResults[i]];
        }        

        UIManagerScript.SlotsUpdateImages(slotSymbols);

        RefreshSlotRewards();

        int amountWon = 0;

        if ((slotResults[0] == slotResults[1]) && (slotResults[1] == slotResults[2]))
        {
            amountWon = slotSymbolValue[slotResults[0]];
            //GameMasterScript.heroPCActor.ChangeMoney(amountWon);
            int tokensWon = CasinoScript.AddChipsToPool(amountWon);
            StringManager.SetTag(0, tokensWon.ToString());
            UIManagerScript.SlotsUpdateText(UIManagerScript.greenHexColor + StringManager.GetString("casino_win") + "</color> <color=yellow>" + StringManager.GetString("casino_win_tokens") + "</color>");
            UIManagerScript.RefreshPlayerStats();
            UIManagerScript.PlaySound("CasinoWin");            
            UpdateHeader();
            return amountWon;
        }
        else
        {
            amountWon = 0;
            UIManagerScript.SlotsUpdateText(UIManagerScript.redHexColor + StringManager.GetString("casino_lose") + "</color>");
            UIManagerScript.PlaySound("CasinoLose");
            UpdateHeader();
            return 0;
        }

        //Shep: Since the if and else above both return 0, this can never be reached
        //return 0;
    }
    
}

public partial class UIManagerScript
{
    public static void OpenCeeloGame()
    {
        ClearAllDialogOptions();

        allUIObjects.Clear();
        allUIObjects.Add(blackjackPlayGame);
        allUIObjects.Add(blackjackExit);
        allUIObjects.Add(blackjackHit);

        dialogObjects.Add(blackjackPlayGame.gameObj);
        dialogObjects.Add(blackjackExit.gameObj);
        dialogObjects.Add(blackjackHit.gameObj);

        dialogUIObjects.Add(blackjackPlayGame);
        dialogUIObjects.Add(blackjackExit);
        dialogUIObjects.Add(blackjackHit);

        blackjackPlayGame.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("casino_ceelo_start");        
        blackjackHit.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("casino_ceelo_roll");

        SetListOffset(0);

        casinoGameOpen = true;
        blackjackGame.SetActive(true);
        blackjackHit.gameObj.SetActive(false);
        blackjackStay.gameObj.SetActive(false);

        blackjackHit.enabled = true;
        blackjackStay.enabled = true;

        blackjackPlayGame.gameObj.SetActive(true);
        blackjackPlayGame.enabled = true;
        blackjackExit.gameObj.SetActive(true);
        blackjackExit.enabled = true;

        blackjackPlayerHand.text = StringManager.GetString("casino_ceelo_playerrolls") + " ????";
        blackjackDealerHand.text = StringManager.GetString("casino_ceelo_dealerrolls") + " ????";
        blackjackResults.text = StringManager.GetString("casino_generic_waiting");

        ShowDialogMenuCursor();
        ChangeUIFocusAndAlignCursor(blackjackPlayGame);

    }

    public static void SetCeeloPlaying(bool state)
    {
        SetListOffset(0);
        if (state)
        {
            
            blackjackHit.gameObj.SetActive(true);
            blackjackHit.enabled = true;

            blackjackPlayGame.gameObj.SetActive(false);
            blackjackPlayGame.enabled = false;

            blackjackExit.gameObj.SetActive(false);
            blackjackExit.enabled = false;

            ChangeUIFocus(blackjackHit);
            AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
        }
        else
        {
            blackjackHit.gameObj.SetActive(false);
            blackjackHit.enabled = false;

            blackjackPlayGame.gameObj.SetActive(true);
            blackjackPlayGame.enabled = true;

            blackjackExit.gameObj.SetActive(true);
            blackjackExit.enabled = true;

            ChangeUIFocus(blackjackPlayGame);
            AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
        }
    }

    public static void OpenBlackjackGame()
    {
        ClearAllDialogOptions();

        allUIObjects.Clear();
        allUIObjects.Add(blackjackPlayGame);
        allUIObjects.Add(blackjackExit);
        allUIObjects.Add(blackjackHit);
        allUIObjects.Add(blackjackStay);

        //dialogObjects.Clear();
        dialogObjects.Add(blackjackPlayGame.gameObj);
        dialogObjects.Add(blackjackExit.gameObj);
        dialogObjects.Add(blackjackHit.gameObj);
        dialogObjects.Add(blackjackStay.gameObj);

        //dialogUIObjects.Clear();
        dialogUIObjects.Add(blackjackPlayGame);
        dialogUIObjects.Add(blackjackExit);
        dialogUIObjects.Add(blackjackHit);
        dialogUIObjects.Add(blackjackStay);

        blackjackPlayGame.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("casino_blackjack_start");
        blackjackHit.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("casino_blackjack_hit");
        blackjackStay.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetString("casino_blackjack_stay");

        SetListOffset(0);
        //listArrayIndexPosition = 0;

        casinoGameOpen = true;
        blackjackGame.SetActive(true);

        blackjackPlayGame.gameObj.SetActive(true);
        blackjackPlayGame.enabled = true;
        blackjackExit.gameObj.SetActive(true);
        blackjackExit.enabled = true;

        blackjackHit.gameObj.SetActive(false);
        blackjackStay.gameObj.SetActive(false);
        blackjackPlayerHand.text = StringManager.GetString("casino_blackjack_playerhand") + " ?????";
        blackjackDealerHand.text = StringManager.GetString("casino_blackjack_dealerhand") + " ?????";
        blackjackResults.text = StringManager.GetString("casino_generic_waiting");

        blackjackHit.enabled = true;
        blackjackStay.enabled = true;

        ShowDialogMenuCursor();
        ChangeUIFocusAndAlignCursor(blackjackPlayGame);
    }

    public static void SetBlackjackPlaying(bool state)
    {
        SetListOffset(0);

        //listArrayIndexPosition = 0;
        if (state)
        {
            blackjackHit.gameObj.SetActive(true);
            blackjackHit.enabled = true;

            blackjackStay.gameObj.SetActive(true);
            blackjackStay.enabled = true;

            blackjackPlayGame.gameObj.SetActive(false);
            blackjackPlayGame.enabled = false;

            blackjackExit.gameObj.SetActive(false);
            blackjackExit.enabled = false;


            ChangeUIFocus(blackjackHit);
            AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
        }
        else
        {
            blackjackHit.gameObj.SetActive(false);
            blackjackHit.enabled = false;

            blackjackStay.gameObj.SetActive(false);
            blackjackStay.enabled = false;

            blackjackPlayGame.gameObj.SetActive(true);
            blackjackPlayGame.enabled = true;

            blackjackExit.gameObj.SetActive(true);
            blackjackExit.enabled = true;

            ChangeUIFocus(blackjackPlayGame);
            AlignCursorPos(uiObjectFocus.gameObj, -5f, -4f, false);
        }
    }

    public static void OpenSlotsGame()
    {
        ClearAllDialogOptions();

        allUIObjects.Clear();
        allUIObjects.Add(slotsPlayGame);
        allUIObjects.Add(slotsExit);

        //dialogObjects.Clear();
        dialogObjects.Add(slotsPlayGame.gameObj);
        dialogObjects.Add(slotsExit.gameObj);

        //dialogUIObjects.Clear();
        dialogUIObjects.Add(slotsPlayGame);
        dialogUIObjects.Add(slotsExit);

        SetListOffset(0);
        //listArrayIndexPosition = 0;

        casinoGameOpen = true;
        slotsGame.SetActive(true);
        StringManager.SetTag(0, CasinoScript.playerBet.ToString());
        slotsPlayGame.gameObj.GetComponentInChildren<TextMeshProUGUI>().text = StringManager.GetExcitedString("dialog_slotmachine_intro_btn_0");
        slotsBet.text = StringManager.GetString("casino_bet");
        ShowDialogMenuCursor();
        ChangeUIFocusAndAlignCursor(slotsPlayGame);
    }

    public static void SlotsUpdateImages(string[] slotResultsImageRefs)
    {
        slotsImage1.sprite = LoadSpriteFromDict(dictItemGraphics, slotResultsImageRefs[0]);
        slotsImage2.sprite = LoadSpriteFromDict(dictItemGraphics, slotResultsImageRefs[1]);
        slotsImage3.sprite = LoadSpriteFromDict(dictItemGraphics, slotResultsImageRefs[2]);
    }

    public static void SlotsUpdateText(string newText)
    {
        slotsBet.text = newText;
    }
}
