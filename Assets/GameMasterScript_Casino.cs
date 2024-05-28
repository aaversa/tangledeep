using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameMasterScript
{
    public void StartCasino(CasinoGameType whichGame, int startBet)
    {
        CasinoScript.SetGame(whichGame);

        switch (whichGame)
        {
            case CasinoGameType.SLOTS:
                CasinoScript.SetBet(startBet);
                UIManagerScript.OpenSlotsGame();
                break;
            case CasinoGameType.BLACKJACK:
                CasinoScript.SetBet(startBet);
                UIManagerScript.OpenBlackjackGame();
                break;
            case CasinoGameType.CEELO:
                CasinoScript.SetBet(startBet);
                UIManagerScript.OpenCeeloGame();
                break;
        }
    }

    public void PlayCasinoGameWithSelectedBet()
    {
        PlayCasinoGame(CasinoScript.playerBet);
    }

    public void PlayCasinoGame(int bet)
    {
        if (heroPCActor.GetMoney() < bet)
        {
            UIManagerScript.PlaySound("Error");
            GameLogScript.LogWriteStringRef("log_error_notenoughmoney");
            return;
        }

        heroPCActor.ChangeMoney(-1 * bet);

        if (CasinoScript.curGameType == CasinoGameType.BLACKJACK)
        {
            UIManagerScript.SetBlackjackPlaying(true);
        }
        else if (CasinoScript.curGameType == CasinoGameType.CEELO)
        {
            UIManagerScript.SetCeeloPlaying(true);
        }

        CasinoScript.PlayCurrentGame(bet);
    }

    public void TakeCasinoAction(int action)
    {
        CasinoScript.TakeAction(action);
    }
}
