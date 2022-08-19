using brock.Blackjack;
using System;
using System.Collections.Generic;

public class BlackjackGameResult
{
	public long Id;
	public string Username;
	public float Wager;
	public List<Card> PlayerHand;
	public ushort PlayerScore;
    public List<Card> DealerHand;
	public ushort DealerScore;
}
