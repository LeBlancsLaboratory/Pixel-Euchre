using EuchreObjects;
using Godot;
using System;

using System.Collections.Generic;

// this script is something else man

public partial class PlayerHand : Control
{
	private const double CARD_TWEEN_INTERVAL = 0.15;
	/// <summary>
	/// 
	/// </summary>
	private bool discardActive = false;
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

	private HandPosition negativePosition;
	// collection of cards in the client player's hand.
	// the index order of this also represents the draw order. drawn from left to right
	private List<Card> cardsInHand = new();
	private List<HandPosition> handPositions = new();
	private PlayerDiscard discard;

	public void StartTurn() {
		discardActive = true;
	}

	public void EndTurn() {
		discardActive = false;
	}

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

		List<Card> cardsForTesting = new();

		for (int i = 0; i < 5; i++) {
			CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://Library/ModelScenes/card_model.tscn").Instantiate();
			newCardModel.Visible = true;

			Card newCard = new(EuchreEnums.Suit.Clubs, 7, "7");
			newCard.SetModel(newCardModel);

			cardsForTesting.Add(newCard);
		}

		AcceptCard(cardsForTesting);

		StartTurn();
    }

	public bool AcceptCard(Card newCard) {
		if (cardsInHand.Count < 5) {
			if (newCard.GetModel() == null) {
				CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://Library/ModelScenes/card_model.tscn").Instantiate();
				newCard.SetModel(newCardModel);
				// TODO: Some logic to set images on card model
				newCardModel.Visible = true;
			}

			cardsInHand.Add(newCard);
			AddChild(newCard.GetModel());
			RemakeHandPositionsFromCardIndex();
			RefreshCardDrawOrder();
		}
		return cardsInHand.Count < 5;
	}

	// always use this for adding multiple, tweening gets funky otherwise due to remaking hand pos every iter
	public bool AcceptCard(List<Card> newCards) {
		if (cardsInHand.Count + newCards.Count <= 5) {
			foreach (Card card in newCards) {
				if (card.GetModel() == null) {
					CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://Library/ModelScenes/card_model.tscn").Instantiate();
					card.SetModel(newCardModel);
					// TODO: Some logic to set images on card model
					newCardModel.Visible = true;					
				}
				cardsInHand.Add(card);
				AddChild(card.GetModel());
			}

			RemakeHandPositionsFromCardIndex();
			RefreshCardDrawOrder();
		}
		return cardsInHand.Count + newCards.Count <= 5;
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

	/// <summary>
	/// Tweens a card to a new position and calculated rotation.
	/// </summary>
	/// <param name="card">Card to tween</param>
	/// <param name="position">Position to tween to</param>
	/// <param name="interval">Interval at which card tweens in seconds</param>
	/// <param name="parallel">Bool representing whether tweener will await tween to finish
	private async void TweenCardToNewPosition(Card card, Vector2 position, double interval=-1, bool wait=false) {
		if (interval < 0) {interval = CARD_TWEEN_INTERVAL;}
		Tween tween = GetTree().CreateTween();
		tween.SetParallel();
		tween.TweenProperty(
			card.GetModel(), 
			"position", 
			position, 
			interval
		);
		tween.TweenProperty(
			card.GetModel(), 
			"rotation", 
			CalcCardRotationFromPosition(position), 
			interval
		);
		if (wait) {
			await ToSignal(tween, Tween.SignalName.Finished);
		}
	}

	/// <summary>
	/// Tweens a list of cards to a new position and calculated rotation.
	/// </summary>
	/// <param name="cards">Cards to tween</param>
	/// <param name="positions">Positions to tween cards to</param>
	/// <param name="interval">Interval at which cards tween in seconds</param>
	/// <param name="wait">Bool representing whether tweener will await tweens to finish
	private async void TweenCardToNewPosition(List<Card> cards, List<HandPosition> positions, double interval=-1, bool wait=false) {
		if (interval < 0) {interval = CARD_TWEEN_INTERVAL;}
		for (int i = 0; i < cards.Count; i++) {
			Tween tween = GetTree().CreateTween();
			tween.SetParallel();
			tween.TweenProperty(
				cards[i].GetModel(), 
				"position", 
				positions[i].Position, 
				interval
			);
			tween.TweenProperty(
				cards[i].GetModel(), 
				"rotation", 
				CalcCardRotationFromPosition(positions[i].Position), 
				interval
			);
			if (wait) {
				await ToSignal(tween, Tween.SignalName.Finished);
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
						TweenCardToNewPosition(card, nextFilled.Position);

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
		if (card.GetCurrentHandPos() != null && card.GetCurrentHandPos().GetPosInHand() != -1) {
			// in euchre the turn would end after a single discard
			EndTurn();

			card.SetCurrentHandPos(negativePosition);

			AlignCardIndexToHandPosition();
			RemoveChild(card.GetModel());

			cardsInHand.Remove(card);

			discard.DiscardToGamePile(card);

			RemakeHandPositionsFromCardIndex();
			RefreshCardDrawOrder();
		}
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
	/// Animate cards to new hand positions.
	/// </summary>
	private void RemakeHandPositionsFromCardIndex() {
		// load the hand pos scene
		PackedScene handPosScene = GD.Load<PackedScene>("res://Library/ModelScenes/hand_position.tscn");

		if (negativePosition == null) {
			negativePosition = (HandPosition)handPosScene.Instantiate();
		}

		// reset handpositions in place.
		// STEP 1: align count of handpositions with that of cards
		// STEP 2: update card references on handpositions and vice versa (acceptnewcard handles this)
		// O(n) (thank you sebnem) (doesn't matter because we're max of 5 object references)
		int increment = handPositions.Count < cardsInHand.Count ? 1 : -1;

		for (int i = handPositions.Count; i != cardsInHand.Count; i += increment) {
			if (increment < 0) {
				RemoveChild(handPositions[handPositions.Count - 1]);
				handPositions.RemoveAt(handPositions.Count - 1);
			} else {
				HandPosition newHandPos = (HandPosition)handPosScene.Instantiate();
				handPositions.Add(newHandPos);
				AddChild(newHandPos);
			}
		}

		for (int i = 0; i < cardsInHand.Count; i++) {
			HandPosition handPos = handPositions[i];
			handPos.SetPosInHand(i);
			handPos.AcceptNewCard(cardsInHand[i]);
		}

		// calculate arbitrary values to place cards with arbitrarily
		float halfPosWidth = ((CollisionShape2D)negativePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.X / 2;
		float fifteenthPosHeight = ((CollisionShape2D)negativePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.Y / 15;

		// if you notice a pattern and can refactor please do im just fucking with hardcoded values so it renders in a way i like
		HandPosition currHandPos;
		switch (cardsInHand.Count) {
		    case 1:
				currHandPos = handPositions[0];
				currHandPos.Position = new Vector2(centerX, centerY);

				currHandPos.SetOccupantPosition();
				break;
			case 2:
				int xMult = -1;
				for (int i = 0; i < 2; i++) {
					float newPosX = centerX + (halfPosWidth / 2 * xMult);
					float newPosY = centerY;
					
					Vector2 newPos = new Vector2(newPosX, newPosY);
					
					currHandPos = handPositions[i];

					currHandPos.Position = newPos;
					currHandPos.Rotation = CalcCardRotationFromPosition(currHandPos.Position);

					xMult += 2;
				}
				break;
			case 3:
				int offsetMultiplier = -1;
				for (int i = 0; i < 3; i++) {
					float newPosX = centerX + (halfPosWidth * offsetMultiplier);
					
					float newPosY = centerY;
					if (i == 0 || i == 2) {
						newPosY += (float)(fifteenthPosHeight / 3);
					}

					Vector2 newPos = new Vector2(newPosX, newPosY);

					currHandPos = handPositions[i];
					currHandPos.Position = newPos;
					currHandPos.Rotation = CalcCardRotationFromPosition(currHandPos.Position);

					offsetMultiplier++;
				}
				break;
			case 4:
				float case4XLeftBound = (float)(halfPosWidth * -1.5);
				float case4XRightBound = (float)(halfPosWidth * 1.5);
				
				float case4XPos = (float)(halfPosWidth * -1.5);
				for (int i = 0; i < 4; i++) {
					currHandPos = handPositions[i];

					float newY = centerY;

					if (i == 0 || i == 3) {
						newY += (float)(fifteenthPosHeight / 1.5);
					}

					Vector2 newPos = new Vector2(case4XPos, newY);

					currHandPos.Position = newPos;
					currHandPos.Rotation = CalcCardRotationFromPosition(currHandPos.Position);

					case4XPos += halfPosWidth;
				}
				break;
			case 5:
				float newHandPositionPosX2 = halfPosWidth * -2;
				for (int i = 0; i < 5; i++) {
					currHandPos = handPositions[i];

					float newY = centerY;
					switch (Math.Abs(i - 2)) {
						case 1:
							newY = (float)(centerY + fifteenthPosHeight / 3);
							break;
						case 2:
							newY = (float)(centerY + fifteenthPosHeight * 1.25);
							break;
					}

					Vector2 newPos = new Vector2(newHandPositionPosX2, newY);

					currHandPos.Position = newPos;
					currHandPos.Rotation = CalcCardRotationFromPosition(currHandPos.Position);

					newHandPositionPosX2 += halfPosWidth;
				}
				break;
		}

		TweenCardToNewPosition(cardsInHand, handPositions, wait: false);
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
	private void playerDiscardEntered() { inPlayerDiscard = true && discardActive; }
	private void playerDiscardExited() { inPlayerDiscard = false; }
}
