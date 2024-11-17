using System;
using System.Collections.Generic;
using Godot;

namespace EuchreObjects;

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

public class Board {
    private Pile pile;

    // should i store game rule settings here? would that make any sense?

    /// A list of players whose index position is their 'player number'. 0 and 2 are partners, 1 and 3 are partners.   
    private List<Player> players;

    private int turn = 0;

    private Deck deck;

    public Board(Tuple<Tuple<Player, Player>, Tuple<Player, Player>> teamMates) {
        // I suppose pair 1 will be 0 and 2, pair 2 will be 1 and 3
        players[0] = teamMates.Item1.Item1;
        players[2] = teamMates.Item1.Item2;
        players[1] = teamMates.Item2.Item1;
        players[3] = teamMates.Item2.Item2;

        deck = new Deck(aceHigh: true);
        deck.Shuffle();
    }
} 