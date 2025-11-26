namespace PixelEuchre.Library.EuchreObjects;

using Enums;
using Godot;
using System;
using System.Collections.Generic;



public class Deck
{
	private List<Card> cards = new();
	private string deckName = "";
	private Random random = new Random();

	public Deck(bool aceHigh = false) {
		// do i want to load images for the card here or in the card constructor?
		int lowestRank = aceHigh ? 1 : 2;
		int highestRank = aceHigh ? 13 : 14;

		Suit[] suits = {Suit.Diamonds, Suit.Hearts, Suit.Spades, Suit.Spades};

		foreach(Suit suit in suits) {
			for (int i = lowestRank; i <= highestRank; i++) {
				string cardName;
				switch(i) {
					case 1:
						cardName = "Ace";
						break;
					case 11:
						cardName = "Jack";
						break;
					case 12:
						cardName = "Queen";
						break;
					case 13:
						cardName = "King";
						break;
					case 14:
						cardName = "Ace";
						break;
					default:
						cardName = i.ToString();
						break;
				}

				deckName = DeckNames.GildedDeck.ToString();

				Card newCard = new(rank: i, suit: suit, name: cardName);

				//TODO: add texture to card front and back
				/* example: 

				Image cardImageFront = GD.Load<Image>("res://Images/seven of clubs.png");
				ImageTexture cardTextureFront = ImageTexture.CreateFromImage(cardImageFront);
				((Sprite2D)newCardModel.FindChild("Sprite2D")).Texture = cardTextureFront;

				*/

				cards.Add(newCard);
			}
		}
	}

	/// <summary>
	/// Accept a card into this hand
	/// </summary>
	/// <param name="card">Card to add to this hand</param>
	public void AcceptCard(Card card) {
		cards.Add(card);
	}

	public void Shuffle() {
		for (int i = 0; i < CardsLeft(); i++) {
			int x = random.Next(1, CardsLeft() - 1);
			// simply swap the two
			(cards[x], cards[i]) = (cards[i], cards[x]);
		}
	}

	/// <returns>Number of cards left in the deck</returns>
	public int CardsLeft() {
		return cards.Count;
	}

	public List<Card> DealToHand(int numCards = 1, Hand hand = null) {
		List<Card> cardsList = new List<Card>();

		for (int i = 0; i < numCards; i++) {
			cardsList.Add(cards[CardsLeft() - 1]);
			cards.RemoveAt(CardsLeft() - 1);
		}
		
		if (hand != null) {
			hand.AcceptCards(cardsList);
		}

		return cardsList;
	}
	public List<Card> DealToPlayer(int numCards, Player player) {
		return new List<Card>();
	}

    public override string ToString() {
		string outString = "Cards in Deck:\n";

        foreach (Card card in cards) {
			outString += card + "\n"; // tostring not necessary in concatenation
		}

		return outString;
    }

}
