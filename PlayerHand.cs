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
	private HandPosition lastEntered;
	private HandPosition startingPosition;
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
			handPositions[i].SetOccupantPosition(); // call this manually now
			SetCardRotationInHandArea(newCard);
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
				TestPositionEntered();
			}
		}
	}

	private void TestPositionEntered() {
		for (int i = handPositions.Count - 1; i >= 0; i--) {
			if (handPositions[i].HitTest()) {
				if (lastEntered.GetPosInHand() != i) {
					var card = handPositions[i].ClearCard();
					lastEntered.AcceptNewCard(card);
					// animate to new pos
					Tween tween = GetTree().CreateTween();
					tween.SetParallel();
					tween.TweenProperty(card.GetModel(), "position", lastEntered.Position, 0.1);
					tween.TweenProperty(card.GetModel(), "rotation", CalcCardRotationFromPosition(lastEntered.Position), 0.1);

					lastEntered = handPositions[i];
					RefreshCardDrawOrder();
				}
				break;
			}
		}
	}

	private void HitLogic() {
		for (int i = cardsInHand.Count - 1; i >= 0; i--) {
			if (cardsInHand[i].HitTest()) {
				dragging = true;
				draggingCard = cardsInHand[i];
				if (draggingCard.GetPosition() != null) {

					cardsInHand.RemoveAt(i);
					cardsInHand.Add(draggingCard);

					draggingCardOffset = (Vector2)draggingCard.GetPosition() - GetLocalMousePosition();
					if (draggingCard.GetCurrentHandPos() != null && draggingCard.GetCurrentHandPos().GetPosInHand() != -1) {
						lastEntered = draggingCard.GetCurrentHandPos();
						startingPosition = lastEntered;
						lastEntered.ClearCard();
					}
				}
				RefreshCardDrawOrder();
				break;
			}
		}
	}

	private void HandleArea2DInput(Node viewport, InputEvent inputEvent, long shapeIdx) {
		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left) {
			if (inputEvent.IsPressed()) {
				if (!dragging) {
					HitLogic();
				}
			} else {
				StopDragging();
			}
		}
	}


	private void StopDragging() {
		if (draggingCard != null) {
			lastEntered.AcceptNewCard(draggingCard);
			lastEntered.SetOccupantPosition();
			lastEntered = null;
			startingPosition = null;
			dragging = false;
			draggingCard = null;
			draggingCardOffset = defaultOffset;
			// TODO: test snap or return to original position

			AlignCardIndexToHandPosition();
			RefreshCardDrawOrder();
		}
	}

	private void AlignCardIndexToHandPosition() {
		cardsInHand = new List<Card>(); // shouldn't be expensive.
		foreach (HandPosition pos in handPositions) {
			cardsInHand.Add(pos.GetCard());
		}
		if (dragging) {
			cardsInHand.Add(draggingCard);
		}
	}

	private void RefreshCardDrawOrder() {
		for (int i = cardsInHand.Count - 1; i >= 0; i--) {
			cardsInHand[i].GetModel().ZIndex = i + 2;
			SetCardRotationInHandArea(cardsInHand[i]);
		}
	}


}
