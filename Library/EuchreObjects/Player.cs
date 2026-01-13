namespace PixelEuchre.Library.EuchreObjects;

using Godot;
using System;

public class Player
{
	public string name;
	private Hand hand;

	public Player(string name)
	{
		this.name = name;
		hand = new();
	}

	public Hand GetHand()
	{
		if (hand != null)
		{
			return hand;
		}
		return null;
	}

	public override string ToString()
	{
		return "Player: " + this.name;
	}
}
