using Godot;
using System;

public partial class ExitBtn : Button
{
    public override void _Pressed()
    {
        base._Pressed();

		GetTree().Quit();
    }
}
