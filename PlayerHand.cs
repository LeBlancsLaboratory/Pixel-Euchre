using EuchreObjects;
using Godot;
using System;
using System.Collections;

using System.Collections.Generic;


public partial class PlayerHand : Control
{
	private const double CARD_TWEEN_INTERVAL = 0.15;
	private bool inPlayerHand = false;
	private bool inPlayerDiscard = false;
	private float leftBoundary;
	private float rightBoundary;
	private float centerX;
	private float centerY;
	private static Vector2 defaultOffset = new(0, 0);
	private Card draggingCard;
	private bool dragging = false;
	private Vector2 draggingCardOffset;
	private HandPosition lastEntered;
	private HandPosition startingPosition;
	// collection of cards in the client player's hand.
	// the index order of this also represents the draw order. drawn from left to right
	private List<Card> cardsInHand = new();
	private List<HandPosition> handPositions = new();
	private PlayerDiscard discard;

	private HandPosition templatePosition;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		CollisionShape2D handPhysBoundary = (CollisionShape2D)FindChild("PlayerHandBoundary").FindChild("HandOutline");
		Vector2 hbPosition = handPhysBoundary.Shape.GetRect().Position;
		Vector2 hbSize = handPhysBoundary.Shape.GetRect().Size;
		leftBoundary = hbPosition.X;
		rightBoundary = hbPosition.X + hbSize.X;
		centerX = hbPosition.X + (hbSize.X / 2);
		centerY = hbPosition.Y + (hbSize.Y / 2);

		discard = (PlayerDiscard)GetParent().FindChild("PlayerDiscard");

		templatePosition = (HandPosition)GD.Load<PackedScene>("res://hand_position.tscn").Instantiate(); // should get garbage collected?

		float halfPosWidth = ((CollisionShape2D)templatePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.X / 2; // yuck
		float fifteenthPosHeight = ((CollisionShape2D)templatePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.Y / 15; // yuck still

		float newHandPositionPosX = halfPosWidth * -2;


		// this section makes sense for instantiation. cards dont except for testing
		PackedScene handPosScene = GD.Load<PackedScene>("res://hand_position.tscn");
		for (int i = 0; i < 5; i++) {
			HandPosition newHandPos = (HandPosition)handPosScene.Instantiate();
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

			Card newCard = new(EuchreEnums.Suit.Clubs, 7, "7");
			newCard.SetModel(newCardModel);

			cardsInHand.Add(newCard);

			handPositions[i].AcceptNewCard(newCard);
			handPositions[i].SetOccupantPosition(); // call this manually now
			SetCardRotationInHandArea(newCard);
		}

		RefreshCardDrawOrder();
    }

	public void AcceptCard(Card newCard) {
		if (cardsInHand.Count < 5) {
			if (newCard.GetModel() == null) {
				CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://card_model.tscn").Instantiate();
				newCard.SetModel(newCardModel);
				// TODO: Some logic to set images on card model
				newCardModel.Visible = true;
			}
		}
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

	private bool IsPositionInPlayerHand(Vector2 pos) {
		if (!inPlayerHand && !inPlayerDiscard) {
			return false;
		}
		return true;
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (dragging && draggingCard != null) {
			Vector2 newPosition = GetLocalMousePosition() + draggingCardOffset;
			if (!IsPositionInPlayerHand(newPosition)) {
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
				var lastPos = lastEntered.GetPosInHand();
				if (lastPos != i) {
					var incr = lastPos < i ? 1 : -1;
					HandPosition nextFilled = lastEntered;

					for (int j = lastPos + incr; j != i + incr; j += incr) {
						var card = handPositions[j].ClearCard();

						nextFilled.AcceptNewCard(card);
						// animate to new pos
						Tween tween = GetTree().CreateTween();
						tween.SetParallel();
						tween.TweenProperty(
							card.GetModel(), 
							"position", 
							nextFilled.Position, 
							CARD_TWEEN_INTERVAL
						);
						tween.TweenProperty(
							card.GetModel(), 
						    "rotation", 
					        CalcCardRotationFromPosition(nextFilled.Position), 
						    CARD_TWEEN_INTERVAL
						);

						if (j != i) { nextFilled = handPositions[j]; }
					}

					lastEntered = handPositions[i];
					AlignCardIndexToHandPosition();
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

	private void HandleHandAreaInput(Node viewport, InputEvent inputEvent, long shapeIdx) {
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

	private void HandleDiscardAreaInput(Node viewport, InputEvent inputEvent, long shapeIdx) {
		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left) {
			if (!inputEvent.IsPressed() && draggingCard != null) {
				StopDragging(true); // in the future this will be a discard function
				RemakeHandPositionsFromCardIndex();
			}
		}
	}

	private void DiscardToGamePile(Card card) {
		RemoveChild(card.GetModel());
		cardsInHand.Remove(card);
		discard.DiscardToGamePile(card);
	}

	/// <summary>
	/// Stops dragging operation on player hand. If in discard area, discard.
	/// </summary>
	/// <param name="discard">boolean indicator for discard area</param>
	private void StopDragging(bool discard = false) {
		if (draggingCard != null) {
			lastEntered.AcceptNewCard(draggingCard);
			lastEntered.SetOccupantPosition();
			lastEntered = null;
			startingPosition = null;
			dragging = false;

			if (discard) {
				DiscardToGamePile(draggingCard);
			}

			draggingCard = null;
			draggingCardOffset = defaultOffset;

			AlignCardIndexToHandPosition();
			RefreshCardDrawOrder();
		}
	}

	/// <summary>
	/// Remake the position objects according to the size of the hand after a card is added or discarded.
	/// Align cards in hand to new objects.
	/// </summary>
	private void RemakeHandPositionsFromCardIndex() {

		// foreach(HandPosition pos in handPositions) {
		// 	RemoveChild(pos);
		// }
		// handPositions = new(); // reset hand positions ... could be done with what we've got, clear positions, reassign ?? add when needed ??

		// // load the hand scene
		// PackedScene handPosScene = GD.Load<PackedScene>("res://hand_position.tscn");

		// // calculate arbitrary values to place cards with arbitrarily
		// float halfPosWidth = ((CollisionShape2D)templatePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.X / 2;
		// float fifteenthPosHeight = ((CollisionShape2D)templatePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.Y / 15;

		// switch (cardsInHand.Count) {
		//  case 1:
		//		break;
		//	case 2:
		//		break;
		// 	case 3:
		// 		int offsetXMultiplier = -1;
		// 		for (int i = 0; i < 3; i++) {
		// 			float newPosX = centerX + (halfPosWidth * offsetXMultiplier);
		// 			float newPosY = centerY + (fifteenthPosHeight * offsetXMultiplier);
					
		// 			HandPosition newHandPos = (HandPosition)handPosScene.Instantiate();
		// 			newHandPos.Position = new Vector2(newPosX, newPosY);
		// 			newHandPos.Rotation = CalcCardRotationFromPosition(newHandPos.Position);

		// 			newHandPos.AcceptNewCard(cardsInHand[i]);
		// 			newHandPos.SetOccupantPosition();
		// 		}
		// 		break;
		// 	case 4:
				
		// 		break;
		// 	case 5:
		// 		break;
		// }

		// the below is for reference when building this function
		// for (int i = 0; i < 5; i++) {
		// 	HandPosition newHandPos = (HandPosition)GD.Load<PackedScene>("res://hand_position.tscn").Instantiate();
		// 	newHandPos.SetPosInHand(i);
		// 	AddChild(newHandPos);

		// 	float newY = centerY;
		// 	switch (Math.Abs(i - 2)) {
		// 		case 1:
		// 			newY = centerY + fifteenthPosHeight / 2;
		// 			break;
		// 		case 2:
		// 			newY = (float)(centerY + fifteenthPosHeight * 1.5); // i mean this is fully arbitrary... how will it look scaled?
		// 			break;
		// 	}

		// 	newHandPos.Position = new Vector2(newHandPositionPosX, newY);
		// 	newHandPositionPosX += halfPosWidth;

		// 	newHandPos.Rotation = CalcCardRotationFromPosition(newHandPos.Position);

		// 	handPositions.Add(newHandPos);
		// }
	}

	private void AlignCardIndexToHandPosition() {
		cardsInHand = new(); // shouldn't be expensive.
		foreach (HandPosition pos in handPositions) {
			cardsInHand.Add(pos.GetCard());
		}
		if (dragging) {
			cardsInHand.Add(draggingCard);
		}
	}

	private void RefreshCardDrawOrder() {
		for (int i = cardsInHand.Count - 1; i >= 0; i--) {
			if (cardsInHand[i] != null && cardsInHand[i].GetModel() != null) {
				cardsInHand[i].GetModel().ZIndex = i + 2;
				SetCardRotationInHandArea(cardsInHand[i]);
			}
		}
	}

	private void playerHandEntered() { inPlayerHand = true; }
	private void playerHandExited() { inPlayerHand = false; }
	private void playerDiscardEntered() { inPlayerDiscard = true; }
	private void playerDiscardExited() { inPlayerDiscard = false; }

}
