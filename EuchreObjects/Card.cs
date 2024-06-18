namespace EuchreObjects;

using EuchreEnums;
using Godot;
using System;
using System.Dynamic;


public class Card
{
	private Image cardFront;
	private Image cardBack;
	private CardModel cardModel;
	private Suit suit;
	private int rank;
	private string name;

	private HandPosition currentPos;

	// private HandPosition home;
	
	public Card(Suit suit, int rank, string name) {
		// TODO: logic for setting image (and i have to create the images)
		this.suit = suit;
		this.rank = rank;
		this.name = name;
	}

	public Card(Suit suit, int rank, string name, CardModel cardModel) {
		this.suit = suit;
		this.rank = rank;
		this.name = name;
		this.cardModel = cardModel;
	}

    public override int GetHashCode() {
        return HashCode.Combine(suit, rank);
    }

	/// <summary>
	/// Check to see if two cards share a suit and rank. 
	/// This would indicate the same object in a 52 card deck.
	/// </summary>
	/// <param name="other"></param>
	/// <returns>True if both rank and suit are equal</returns>
	public bool fullyEquals(Card other) {
		return this.suit == other.suit && this.rank == other.rank;
	}

	public Suit GetSuit() {
		return suit;
	}

	public int GetRank() {
		return rank;
	}

	public string GetName() {
		return name;
	}

	public override string ToString() {
		return name + " of " + this.suit.ToString();
	}

	public void SetModel(CardModel newModel) {
		cardModel = newModel;
	}

	public CardModel GetModel() {
		return cardModel;
	}

	public bool HitTest() {
		return cardModel.HitTest();
	}

	public Vector2? GetPosition() {
		if (cardModel != null) {
			return cardModel.Position;
		}
		return null;
	}

	public void SetPosition(Vector2 newPosition) {
		if (cardModel != null) {
			cardModel.Position = newPosition;
		}
	}

	public void SetPosition(float newX, float newY) {
		Vector2 newPosition = new Vector2(newX, newY);
		if (cardModel != null) {
			cardModel.Position = newPosition;
		}
	}

	public float? GetRotationDeg() {
		if (cardModel != null) {
			return cardModel.RotationDegrees;
		}
		return null;
	}

	public void SetRotationDeg(float newRot) {
		if (cardModel != null) {
			cardModel.RotationDegrees = newRot;
		}
	}

	public float? GetRotationRad() {
		if (cardModel != null) {
			return cardModel.Rotation;
		}
		return null;
	}

	public void SetRotationRad(float newRot) {
		if (cardModel != null) {
			cardModel.Rotation = newRot;
		}
	}

	public void SetCurrentHandPos(HandPosition pos) {
		if (pos != null) {
			currentPos = pos;
		}
	}

	public HandPosition GetCurrentHandPos() {
		return currentPos;
	}

	// public void SetHomeInHand(HandPosition handPosition) {
	// 	home = handPosition;
	// }

	

	// public HandPosition GetHomeInHand() {
	// 	return home;
	// }
}
