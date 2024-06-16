using EuchreObjects;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Resolvers;

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
	// collection of cards in the client player's hand.
	// the index order of this also represents the draw order. drawn from left to right
	private List<Card> cardsInHand = new List<Card>();

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

		CardModel templateCard = (CardModel)GD.Load<PackedScene>("res://card_model.tscn").Instantiate(); // should get garbage collected?

		float halfCardWidth = ((CollisionShape2D)templateCard.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.X / 2; // yuck
		float fifteenthCardHeight = ((CollisionShape2D)templateCard.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.Y / 15; // yuck still

		float newCardPosX = halfCardWidth * -2;

		for (int i = 0; i < 5; i++) {
			CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://card_model.tscn").Instantiate();
			AddChild(newCardModel);

			float newY = centerY;
			switch (Math.Abs(i - 2)) {
				case 1:
					newY = centerY + fifteenthCardHeight / 2;
					break;
				case 2:
					newY = (float)(centerY + fifteenthCardHeight * 1.5);
					break;
			}

			newCardModel.Position = new Vector2(newCardPosX, newY);
			newCardPosX += halfCardWidth;

			newCardModel.Visible = true;
			Card newCard = new Card(EuchreEnums.Suit.Clubs, 7, "7", newCardModel);

			setCardRotationInHandArea(newCard);

			cardsInHand.Add(newCard);
		}

		RefreshCardDrawOrder();
    }

	private void setCardRotationInHandArea(Card card) {
		float newRotation = new();
		if (card.GetModel().Position.X > centerX) {
			newRotation = (float)(Math.PI / 2 / rightBoundary) * (float)(card.GetModel().Position.X / 2);
		} else if (card.GetModel().Position.X < centerX) {
			newRotation = (float)(Math.PI / 2 / leftBoundary) * -(float)(card.GetModel().Position.X / 2);
		}

		card.SetRotationRad(newRotation);
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (dragging && draggingCard != null) {
			Vector2 newPosition = GetLocalMousePosition() + draggingCardOffset;
			if (newPosition.X <= leftBoundary || newPosition.X >= rightBoundary || newPosition.Y <= upperBoundary || newPosition.Y >= lowerBoundary) {
				StopDragging();
			} else {
				draggingCard.SetPosition(newPosition);

				setCardRotationInHandArea(draggingCard);
			}
		}
	}

	private void HitLogic() {
		int idx = 0;
		foreach (Card card in cardsInHand) {
			if (card.HitTest()) {
				draggingCard = card;
				if (draggingCard.GetPosition() != null) {
					draggingCardOffset = (Vector2)draggingCard.GetPosition() - GetLocalMousePosition();
				}

				Card newTop = cardsInHand[idx];
				cardsInHand.RemoveAt(idx);
				cardsInHand.Add(newTop);
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
					HitLogic();
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

		for (int i = cardsInHand.Count - 1; i >= 0; i--) {
			cardsInHand[i].GetModel().ZIndex = newZIndex;
			newZIndex--;
		}
	}


}
