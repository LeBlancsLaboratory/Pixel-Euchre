using Godot;
using System;

public partial class OptBtn : Button
{
    public override void _Pressed()
    {
        base._Pressed();

		GetTree().ChangeSceneToFile("res://Library/ScreenScenes/options.tscn");
    }
}
