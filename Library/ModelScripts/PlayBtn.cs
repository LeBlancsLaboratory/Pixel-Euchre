using Godot;
using System;

public partial class PlayBtn : Button
{
    public override void _Pressed()
    {
        base._Pressed();

        GetTree().ChangeSceneToFile("res://Library/ScreenScenes/game screen.tscn");
    }
}
