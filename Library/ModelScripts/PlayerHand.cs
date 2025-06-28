using System;
using System.Collections.Generic;
using Godot;
using PixelEuchre.Library.EuchreObjects;

namespace PixelEuchre.Library.ModelScripts;
// this script is something else man

public partial class PlayerHand : Control
{
	private const double CARD_TWEEN_INTERVAL = 0.15;
	/// <summary>
	/// 
	/// </summary>
	private bool _discardActive = false;
	private bool _inPlayerHand = false;
	private bool _inPlayerDiscard = false;
	private float _leftBoundary;
	private float _rightBoundary;
	private float _centerX;
	private float _centerY;
	private static readonly Vector2 DefaultOffset = new(0, 0);
	private Card _draggingCard;
	private bool _dragging = false;
	private Vector2 _draggingCardOffset;
	private HandPosition _lastEntered;
	private HandPosition _startingPosition;

	private HandPosition _negativePosition;
	// collection of cards in the client player's hand.
	// the index order of this also represents the draw order. drawn from left to right
	private List<Card> _cardsInHand = new();
	private readonly List<HandPosition> _handPositions = new();
	private PlayerDiscard _discard;

	private void StartTurn() {
		_discardActive = true;
	}

	private void EndTurn() {
		discardActive = false;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		CollisionShape2D handPhysBoundary = (CollisionShape2D)FindChild("PlayerHandBoundary").FindChild("HandOutline");
		Vector2 hbPosition = handPhysBoundary.Shape.GetRect().Position;
		Vector2 hbSize = handPhysBoundary.Shape.GetRect().Size;
		_leftBoundary = hbPosition.X;
		_rightBoundary = hbPosition.X + hbSize.X;
		_centerX = hbPosition.X + (hbSize.X / 2);
		_centerY = hbPosition.Y + (hbSize.Y / 2);

		_discard = (PlayerDiscard)GetParent().FindChild("PlayerDiscard");

		List<Card> cardsForTesting = new();

		for (int i = 0; i < 5; i++) {
			CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://Library/ModelScenes/card_model.tscn").Instantiate();
			newCardModel.Visible = true;

			Card newCard = new(PixelEuchre.Enums.Suit.Clubs, 7, "7");
			newCard.SetModel(newCardModel);

			cardsForTesting.Add(newCard);
		}

		AcceptCard(cardsForTesting);

		StartTurn();
	}

	public bool AcceptCard(Card newCard) {
		if (_cardsInHand.Count < 5) {
			if (newCard.GetModel() == null) {
				CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://Library/ModelScenes/card_model.tscn").Instantiate();
				newCard.SetModel(newCardModel);
				// TODO: Some logic to set images on card model
				newCardModel.Visible = true;
			}

			_cardsInHand.Add(newCard);
			AddChild(newCard.GetModel());
			RemakeHandPositionsFromCardIndex();
			RefreshCardDrawOrder();
		}
		return _cardsInHand.Count < 5;
	}

	// always use this for adding multiple, tweening gets funky otherwise due to remaking hand pos every iter
	private bool AcceptCard(List<Card> newCards) {
		if (_cardsInHand.Count + newCards.Count > 5) return _cardsInHand.Count + newCards.Count <= 5;
		foreach (Card card in newCards) {
			if (card.GetModel() == null) {
				CardModel newCardModel = (CardModel)GD.Load<PackedScene>("res://Library/ModelScenes/card_model.tscn").Instantiate();
				card.SetModel(newCardModel);
				// TODO: Some logic to set images on card model
				newCardModel.Visible = true;					
			}
			_cardsInHand.Add(card);
			AddChild(card.GetModel());
		}

		RemakeHandPositionsFromCardIndex();
		RefreshCardDrawOrder();
		return _cardsInHand.Count + newCards.Count <= 5;
	}

	private void SetCardRotationInHandArea(Card card) {
		card.SetRotationRad(CalcCardRotationFromPosition(card.GetModel().Position));
	}

	private float CalcCardRotationFromPosition(Vector2 position) {
		float newRotation = 0;
		if (position.X > _centerX) {
			newRotation = (float)(Math.PI / 2 / _rightBoundary) * (float)(position.X / 2);
		} else if (position.X < _centerX) {
			newRotation = (float)(Math.PI / 2 / _leftBoundary) * -(float)(position.X / 2);
		}		

		return newRotation;
	}

	private bool IsPositionInPlayerHand(Vector2 pos)
	{
		return _inPlayerHand || _inPlayerDiscard;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		switch (_dragging)
		{
			case true when _draggingCard != null:
			{
				Vector2 newPosition = GetLocalMousePosition() + _draggingCardOffset;
				if (!IsPositionInPlayerHand(newPosition)) {
					StopDragging();
				} else {
					_draggingCard.SetPosition(newPosition);
					SetCardRotationInHandArea(_draggingCard);
					TestPositionEntered();
				}

				break;
			}
		}
	}

	/// <summary>
	/// Tweens a card to a new position and calculated rotation.
	/// </summary>
	/// <param name="card">Card to tween</param>
	/// <param name="position">Position to tween to</param>
	/// <param name="interval">Interval at which card tweens in seconds</param>
	/// <param name="wait">Bool representing whether tweener will await tween to finish
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
		for (int i = _handPositions.Count - 1; i >= 0; i--) {
			if (_handPositions[i].HitTest()) {
				var lastPos = _lastEntered.GetPosInHand();
				if (lastPos != i) {
					var incr = lastPos < i ? 1 : -1;
					HandPosition nextFilled = _lastEntered;

					for (int j = lastPos + incr; j != i + incr; j += incr) {
						var card = _handPositions[j].ClearCard();

						nextFilled.AcceptNewCard(card);
						// animate to new pos
						TweenCardToNewPosition(card, nextFilled.Position);

						if (j != i) { nextFilled = _handPositions[j]; }
					}

					_lastEntered = _handPositions[i];
					AlignCardIndexToHandPosition();
					RefreshCardDrawOrder();
				}
				break;
			}
		}
	}

	private void HitLogic() {
		for (int i = _cardsInHand.Count - 1; i >= 0; i--) {
			if (_cardsInHand[i].HitTest()) {
				_dragging = true;
				_draggingCard = _cardsInHand[i];
				if (_draggingCard.GetPosition() != null) {

					_cardsInHand.RemoveAt(i);
					_cardsInHand.Add(_draggingCard);

					_draggingCardOffset = (Vector2)_draggingCard.GetPosition() - GetLocalMousePosition();
					if (_draggingCard.GetCurrentHandPos() != null && _draggingCard.GetCurrentHandPos().GetPosInHand() != -1) {
						_lastEntered = _draggingCard.GetCurrentHandPos();
						_startingPosition = _lastEntered;
						_lastEntered.ClearCard();
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
				if (!_dragging) {
					HitLogic();
				}
			} else {
				StopDragging();
			}
		}
	}

	private void HandleDiscardAreaInput(Node viewport, InputEvent inputEvent, long shapeIdx) {
		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left) {
			if (!inputEvent.IsPressed() && _draggingCard != null) {
				StopDragging(true); // in the future this will be a discard function
				RemakeHandPositionsFromCardIndex();
			}
		}
	}

	private void DiscardToGamePile(Card card) {
		if (card.GetCurrentHandPos() != null && card.GetCurrentHandPos().GetPosInHand() != -1) {
			// in euchre the turn would end after a single discard
			// EndTurn();

			card.SetCurrentHandPos(_negativePosition);

			AlignCardIndexToHandPosition();
			RemoveChild(card.GetModel());

			cardsInHand.Remove(card);

			_discard.DiscardToGamePile(card);

			RemakeHandPositionsFromCardIndex();
			RefreshCardDrawOrder();
		}
	}

	/// <summary>
	/// Stops dragging operation on player hand. If in discard area, discard.
	/// </summary>
	/// <param name="discard">boolean indicator for discard area</param>
	private void StopDragging(bool discard = false) {
		if (_draggingCard != null) {
			_lastEntered.AcceptNewCard(_draggingCard);
			_lastEntered.SetOccupantPosition();
			_lastEntered = null;
			_startingPosition = null;
			_dragging = false;

			if (discard) {
				DiscardToGamePile(_draggingCard);
			}

			_draggingCard = null;
			_draggingCardOffset = DefaultOffset;

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

		if (_negativePosition == null) {
			_negativePosition = (HandPosition)handPosScene.Instantiate();
		}

		// reset handpositions in place.
		// STEP 1: align count of handpositions with that of cards
		// STEP 2: update card references on handpositions and vice versa (acceptnewcard handles this)
		// O(n) (thank you sebnem) (doesn't matter because we're max of 5 object references)
		int increment = _handPositions.Count < _cardsInHand.Count ? 1 : -1;

		for (int i = _handPositions.Count; i != _cardsInHand.Count; i += increment) {
			if (increment < 0) {
				RemoveChild(_handPositions[_handPositions.Count - 1]);
				_handPositions.RemoveAt(_handPositions.Count - 1);
			} else {
				HandPosition newHandPos = (HandPosition)handPosScene.Instantiate();
				_handPositions.Add(newHandPos);
				AddChild(newHandPos);
			}
		}

		for (int i = 0; i < _cardsInHand.Count; i++) {
			HandPosition handPos = _handPositions[i];
			handPos.SetPosInHand(i);
			handPos.AcceptNewCard(_cardsInHand[i]);
		}

		// calculate arbitrary values to place cards with arbitrarily
		float halfPosWidth = ((CollisionShape2D)_negativePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.X / 2;
		float fifteenthPosHeight = ((CollisionShape2D)_negativePosition.FindChild("Area2D").FindChild("CollisionShape2D")).Shape.GetRect().Size.Y / 15;

		// if you notice a pattern and can refactor please do im just fucking with hardcoded values so it renders in a way i like
		HandPosition currHandPos;
		switch (_cardsInHand.Count) {
			case 1:
				currHandPos = _handPositions[0];
				currHandPos.Position = new Vector2(_centerX, _centerY);

				currHandPos.SetOccupantPosition();
				break;
			case 2:
				int xMult = -1;
				for (int i = 0; i < 2; i++) {
					float newPosX = _centerX + (halfPosWidth / 2 * xMult);
					float newPosY = _centerY;
					
					Vector2 newPos = new Vector2(newPosX, newPosY);
					
					currHandPos = _handPositions[i];

					currHandPos.Position = newPos;
					currHandPos.Rotation = CalcCardRotationFromPosition(currHandPos.Position);

					xMult += 2;
				}
				break;
			case 3:
				int offsetMultiplier = -1;
				for (int i = 0; i < 3; i++) {
					float newPosX = _centerX + (halfPosWidth * offsetMultiplier);
					
					float newPosY = _centerY;
					if (i == 0 || i == 2) {
						newPosY += (float)(fifteenthPosHeight / 2);
					}

					Vector2 newPos = new Vector2(newPosX, newPosY);

					currHandPos = _handPositions[i];
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
					currHandPos = _handPositions[i];

					float newY = _centerY;

					if (i == 0 || i == 3) {
						newY += fifteenthPosHeight;
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
					currHandPos = _handPositions[i];

					float newY = _centerY;
					switch (Math.Abs(i - 2)) {
						case 1:
							newY = (float)(_centerY + fifteenthPosHeight / 2);
							break;
						case 2:
							newY = (float)(_centerY + fifteenthPosHeight * 2);
							break;
					}

					Vector2 newPos = new Vector2(newHandPositionPosX2, newY);

					currHandPos.Position = newPos;
					currHandPos.Rotation = CalcCardRotationFromPosition(currHandPos.Position);

					newHandPositionPosX2 += halfPosWidth;
				}
				break;
		}

		TweenCardToNewPosition(_cardsInHand, _handPositions, wait: false);
	}

	private void AlignCardIndexToHandPosition() {
		_cardsInHand = new(); // shouldn't be expensive.
		foreach (HandPosition pos in _handPositions) {
			_cardsInHand.Add(pos.GetCard());
		}
		if (_dragging) {
			_cardsInHand.Add(_draggingCard);
		}
	}

	private void RefreshCardDrawOrder() {
		for (int i = _cardsInHand.Count - 1; i >= 0; i--) {
			if (_cardsInHand[i] != null && _cardsInHand[i].GetModel() != null) {
				_cardsInHand[i].GetModel().ZIndex = i + 2;
				SetCardRotationInHandArea(_cardsInHand[i]);
			}
		}
	}

	private void playerHandEntered() { _inPlayerHand = true; }
	private void playerHandExited() { _inPlayerHand = false; }
	private void playerDiscardEntered() { _inPlayerDiscard = true && _discardActive; }
	private void playerDiscardExited() { _inPlayerDiscard = false; }
}