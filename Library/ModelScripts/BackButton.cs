using Godot;
using System;

public partial class BackButton : TextureButton
{
    public override void _Ready()
    {
        
    }
    public void OnPressed()
    {
        base._Pressed();

        GetTree().ChangeSceneToFile("res://Library/ScreenScenes/main menu.tscn");
    }
}
