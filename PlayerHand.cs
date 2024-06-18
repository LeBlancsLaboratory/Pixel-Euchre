using EuchreObjects;
using Godot;
using System;

using System.Collections.Generic;


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
	private HandPosition lastSnappedTo;
	// collection of cards in the client player's hand.
	// the index order of this also represents the draw order. drawn from left to right
	private List<Card> cardsInHand = new List<Card>();

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

		HandPosition templatePosition = (HandPosition)GD.Load<PackedScene>("res://hand_position.tscn").Instantiate(); // should get garbage collected?

		float halfPosWidth = ((CollisionShape2D)templatePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.X / 2; // yuck
		float fifteenthPosHeight = ((CollisionShape2D)templatePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.Y / 15; // yuck still

		float newHandPositionPosX = halfPosWidth * -2;


		// this section makes sense for instantiation. cards dont except for testing
		for (int i = 0; i < 5; i++) {
			HandPosition newHandPos = (HandPosition)GD.Load<PackedScene>("res://hand_position.tscn").Instantiate();
			newHandPos.SetPosInHand(i);
			AddChild(newHandPos);

			float newY = centerY;
			switch (Math.Abs(i - 2)) {
				case 1:
					newY = centerY + fifteenthPosHeight / 2;
					break;
				case 2:
					newY = (float)(centerY + fifteenthPosHeight * 1.5); // i mean this is fully arbitrary... how will it look scaled?
					break;
			}

			newHandPos.Position = new Vector2(newHandPositionPosX, newY);
			newHandPositionPosX += halfPosWidth;

			newHandPos.Rotation = CalcCardRotationFromPosition(newHandPos.Position);

			handPositions.Add(newHandPos);
		}

		for (int i = 0; i < 5; i++) {
			CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://card_model.tscn").Instantiate();
			AddChild(newCardModel);
			
			newCardModel.Visible = true;

			Card newCard = new Card(EuchreEnums.Suit.Clubs, 7, "7");
			newCard.SetModel(newCardModel);

			cardsInHand.Add(newCard);

			handPositions[i].AcceptNewCard(newCard);
		}

		RefreshCardDrawOrder();
    }

	private void SetCardRotationInHandArea(Card card) {
		card.SetRotationRad(CalcCardRotationFromPosition(card.GetModel().Position));
	}

	private float CalcCardRotationFromPosition(Vector2 position) {
		float newRotation = new();
		if (position.X > centerX) {
			newRotation = (float)(Math.PI / 2 / rightBoundary) * (float)(position.X / 2);
		} else if (position.X < centerX) {
			newRotation = (float)(Math.PI / 2 / leftBoundary) * -(float)(position.X / 2);
		}

		return newRotation;
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (dragging && draggingCard != null) {
			Vector2 newPosition = GetLocalMousePosition() + draggingCardOffset;
			if (newPosition.X <= leftBoundary || newPosition.X >= rightBoundary || newPosition.Y <= upperBoundary || newPosition.Y >= lowerBoundary) {
				StopDragging();
			} else {
				draggingCard.SetPosition(newPosition);
				SetCardRotationInHandArea(draggingCard);

				TestSnap();
			}
		}
	}

	private void TestSnap() {
		for (int i = handPositions.Count - 1; i >= 0; i--) {
			if (handPositions[i].HitTest()) {
				var oldCard = handPositions[i].ClearCard();
				if (handPositions[i].GetPosInHand() > lastSnappedTo.GetPosInHand()) {
					handPositions[i - 1].AcceptNewCard(oldCard);
				} else {
					handPositions[i + i].AcceptNewCard(oldCard);
				}

				handPositions[i].AcceptNewCard(draggingCard);
				lastSnappedTo = handPositions[i];
				
				RefreshCardDrawOrder();
				break;
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
					lastSnappedTo = draggingCard.GetCurrentHandPos();
				}
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


	private void StopDragging() {
		dragging = false;
		lastSnappedTo.AcceptNewCard(draggingCard);
		draggingCard = null;
		draggingCardOffset = defaultOffset;
		// TODO: test snap or return to original position
	}

	private void RefreshCardDrawOrder() {
		int newZIndex = 2;

		foreach(HandPosition position in handPositions) {
			position.GetCard().GetModel().ZIndex = newZIndex;
			newZIndex++;

			SetCardRotationInHandArea(position.GetCard());
		}
	}


}
