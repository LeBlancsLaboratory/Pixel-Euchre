using Godot;
using System;
using EuchreObjects;
using System.Xml.Serialization;

public partial class HandPosition : Node2D
{
	private Card occupyingCard;

	private bool mouseOverlapping = false;

	private int positionInHand = -1;

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
		occupyingCard.SetCurrentHandPos(this);
	}

	public void SetOccupantPosition() {
		if (occupyingCard != null) {
			occupyingCard.SetPosition(Position);
		}
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
