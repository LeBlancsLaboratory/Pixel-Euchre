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
	}

	public override string ToString()
	{
		return "Player: " + this.name;
	}
}
