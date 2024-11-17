using Godot;
using System;

public partial class CardModel : Node2D
{
	private bool clickable = false;
	private bool inLocalPlayerHand = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public bool HitTest() {
		return clickable;
	}

	public void EnterLocalPlayerHand() {
		inLocalPlayerHand = true;
	}

	public void ExitLocalPlayerHand() {
		inLocalPlayerHand = false;
	}

	private void OnMouseEnter() {
		clickable = true;
	}

	private void OnMouseExit() {
		clickable = false;
	}
}
