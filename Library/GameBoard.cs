using Godot;
using System;
using PixelEuchre.Library.EuchreObjects;

public partial class GameBoard : Control
{
    private Board _board = null;

    public override void _Ready()
    {
        Player playerOne = new Player("player1");
        Player playerTwo = new Player("player2");
        Player playerThree = new Player("player3");
        Player playerFour = new Player("Player4");
        Tuple<Player, Player> teamOne = Tuple.Create(playerOne, playerThree);
        Tuple<Player, Player> teamTwo = Tuple.Create(playerTwo, playerFour);
        Tuple<Tuple<Player, Player>, Tuple<Player, Player>> teams = Tuple.Create(teamOne, teamTwo);
        _board = new(teams);
    }
}

