namespace EuchreObjects;

using Godot;
using System;
using System.Collections.Generic;

public class Hand
{
	protected List<Card> cards;
	public Hand(){}

	/// <summary>
	/// Accept a card into this hand.
	/// </summary>
	/// <param name="newCard">Card to accept into this hand.</param>
	public void AcceptCard(Card newCard) {
		cards.Add(newCard);
	}

	/// <summary>
	/// Accept a variable number of cards into this hand.
	/// </summary>
	/// <param name="newCards">A list of a variable number of cards to accept into this hand.</param>
	public void AcceptCards(List<Card> newCards) {
		foreach (Card card in newCards) {
			AcceptCard(card);
		}
	}

	/// <returns>Number of cards in this hand.</returns>
	public int CardsLeft() {
		return cards.Count;
	}

	/// <summary>
	/// Discard a card from this hand.
	/// </summary>
	/// <param name="toDiscard">
	/// The card to discard from this hand. If not specified the last card added is discarded.
	/// </param>
	/// <returns>Discarded card.</returns>
	public Card Discard(Card toDiscard = null) {
		if (toDiscard != null) {
			if (cards.Remove(toDiscard)) {
				return toDiscard;
			}
			return null;
		}

		Card lastCard = cards[CardsLeft() - 1];
		cards.Remove(lastCard);
		return lastCard;
	}

    public override string ToString() {
        string outString = "Hand:\n";

		foreach (Card card in cards) {
			outString += card + "\n";
		}

		return outString;
    }
}
