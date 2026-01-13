using System;
using System.Collections.Generic;
using Godot;

namespace PixelEuchre.Library.EuchreObjects;

public class Pile : Hand {

    public Pile() : base() {}
    public override string ToString()
    {
        string outString = "In Play:\n";

		foreach (Card card in cards) {
			outString += card + "\n";
		}

		return outString;
    }
}

public class Board
{
    private Pile pile;

    // should i store game rule settings here? would that make any sense?

    /// A list of players whose index position is their 'player number'. 0 and 2 are partners, 1 and 3 are partners.   
    private List<Player> players;

    private int _turn = 0;

    private bool _playing = false;

    private PlayerDiscard _discardArea;

    private Hand _discardPile;

    private Deck _deck;

    public Board(Tuple<Tuple<Player, Player>, Tuple<Player, Player>> teamMates, PlayerDiscard discard)
    {
        players = [
            // I suppose pair 1 will be 0 and 2, pair 2 will be 1 and 3
            teamMates.Item1.Item1,
            teamMates.Item2.Item1,
            teamMates.Item1.Item2,
            teamMates.Item2.Item2,
        ];

        _discardArea = discard;

        _discardArea.SetParentBoard(this);

        _discardPile = new();

        _deck = new Deck(aceHigh: true);
        _deck.Shuffle();
    }

    public void StartGame()
    {
        _playing = true;
    }

    public bool AttemptPlayCard(Card card, int playerId)
    {
        if (_playing && playerId == _turn)
        {
            _discardPile.AcceptCard(card);
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        return _deck.ToString();
    }
} 