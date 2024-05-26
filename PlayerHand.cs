using EuchreObjects;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Resolvers;

/// <summary>
/// Denotes a fixed hand position from within the PlayerHand Control
/// </summary>
public class HandPosition {
	private Vector2 position;
	private float rotation; // rotation as radians
	private Card occupiedBy;

	public HandPosition(Vector2 position, float rotation) {
		this.position = position;
		this.rotation = rotation;
	}

	public HandPosition(Vector2 position, float rotation, Card occupant) {
		this.position = position;
		this.rotation = rotation;
		occupiedBy = occupant; // occupiedBy is still a member, occupant makes more sense to me as a param name
	}

	public void acceptTenant(Card newOccupant) {
		// assume eviction prior to new tenant
		occupiedBy = newOccupant;
		occupiedBy.SetRotationRad(rotation);
		occupiedBy.SetPosition(position);
	}

	public Card evictTenant() {
		var oldTenant = occupiedBy;
		occupiedBy = null;
		return oldTenant;
	}
	
	public void ResetTenantOrientation() {
		occupiedBy.SetRotationRad(rotation);
		occupiedBy.SetPosition(position);
	}

	public bool CanSnap(Card card) {
		// figure out a tolerance for snappage
		return false;
	}
}

public partial class PlayerHand : Control
{
	private float upperBoundary;
	private float leftBoundary;
	private float rightBoundary;
	private float lowerBoundary;
	private float centerX;
	private float centerY;
	private static Vector2 defaultOffset = new Vector2(0, 0);
	private Card draggingCard;
	private bool dragging = false;
	private Vector2 draggingCardOffset;
	// the hand position to return to if new snap fails
	private HandPosition draggingLastHandPosition;
	// collection of cards in the client player's hand.
	// the index order of this also represents the draw order. drawn from left to right
	private List<Card> cardsInHand = new List<Card>();
	// left to right hand positions. automated to set card position for snapping
	private List<HandPosition> handPositions = new List<HandPosition>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		CollisionShape2D handPhysBoundary = (CollisionShape2D)FindChild("PlayerHandBoundary").FindChild("HandOutline");
		Vector2 hbPosition = handPhysBoundary.Shape.GetRect().Position;
		Vector2 hbSize = handPhysBoundary.Shape.GetRect().Size;
		upperBoundary = hbPosition.Y;
		lowerBoundary = hbPosition.Y + hbSize.Y;
		leftBoundary = hbPosition.X;
		rightBoundary = hbPosition.X + hbSize.X;
		centerX = hbPosition.X + (hbSize.X / 2);
		centerY = hbPosition.Y + (hbSize.Y / 2);

		for (int i = 0; i < 5; i++) {
			CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://card_model.tscn").Instantiate();
			AddChild(newCardModel);

			newCardModel.Position = new Vector2((float)centerX, (float)centerY);

			newCardModel.Visible = true;
			Card newCard = new Card(EuchreEnums.Suit.Clubs, 7, "7", newCardModel);

			cardsInHand.Add(newCard);
		}
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (dragging && draggingCard != null) {
			Vector2 newPosition = GetLocalMousePosition() + draggingCardOffset;
			if (newPosition.X <= leftBoundary || newPosition.X >= rightBoundary || newPosition.Y <= upperBoundary || newPosition.Y >= lowerBoundary) {
				StopDragging();
			} else {
				draggingCard.SetPosition(newPosition);

				float newRotation = new();
				if (newPosition.X > centerX) {
					newRotation = (float)(Math.PI / rightBoundary) * (float)(newPosition.X / 2);
				} else if (newPosition.X < centerX) {
					newRotation = (float)(Math.PI / leftBoundary) * -(float)(newPosition.X / 2);
				}
				
				draggingCard.SetRotationRad(newRotation);
			}
		}
	}

	private void HitTest() {
		int idx = 0;
		foreach (Card card in cardsInHand) {
			if (card.HitTest()) {
				draggingCard = card;
				if (draggingCard.GetPosition() != null) {
					draggingCardOffset = (Vector2)draggingCard.GetPosition() - GetLocalMousePosition();
				}

				Card newTop = cardsInHand[idx];
				cardsInHand.RemoveAt(idx);
				cardsInHand.Insert(0, newTop);
				RefreshCardDrawOrder();

				break;
			}
			idx++;
		}
	}

	private void HandleArea2DInput(Node viewport, InputEvent inputEvent, long shapeIdx) {
		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left) {
			if (inputEvent.IsPressed()) {
				if (!dragging) {
					HitTest();
				}
				dragging = true;
			} else {
				StopDragging();
			}
		}
	}

	private void ReshuffleHandPositions() {

	}

	private void StopDragging() {
		dragging = false;
		draggingCard = null;
		draggingCardOffset = defaultOffset;
		// TODO: test snap or return to original position
	}

	private void RefreshCardDrawOrder() {
		int newZIndex = cardsInHand.Count + 1;

		foreach (Card card in cardsInHand) {
			card.GetModel().ZIndex = newZIndex;
			newZIndex--;
		}
	}


}
