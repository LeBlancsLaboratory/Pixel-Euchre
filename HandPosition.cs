using Godot;
using System;
using EuchreObjects;

public partial class HandPosition : Node2D
{
	private Card occupyingCard;

	private bool mouseOverlapping = false;

	private int positionInHand;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetPosInHand(int pos) {
		positionInHand = pos;
	}

	public int GetPosInHand() {
		return positionInHand;
	}
	
	public void AcceptNewCard(Card newCard) {
		occupyingCard = newCard;
		occupyingCard.SetPosition(Position);
		occupyingCard.SetCurrentHandPos(this);
	}

	public Card ClearCard() {
		Card oldCard = occupyingCard;
		occupyingCard = null;
		return oldCard; // COULD BE NULL
	}

	public Card GetCard() {
		return occupyingCard;
	}

	public bool HitTest() {
		return mouseOverlapping;
	}

	private void OnMouseEnter() {
		mouseOverlapping = true;
	}

	private void OnMouseExit() {
		mouseOverlapping = false;
	}


}
