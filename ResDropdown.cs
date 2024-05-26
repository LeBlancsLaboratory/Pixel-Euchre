using Godot;
using System;

public partial class ResDropdown : OptionButton
{

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Window displayWindow = GetWindow();
		string resolutionString = displayWindow.Size.X.ToString() + 'x' +  displayWindow.Size.Y.ToString();

		bool inList = false;
		
		for (int i = 0; i < this.ItemCount; i++) {
			string eq = GetItemText(idx: i);
			if (eq == resolutionString) {
				this.Selected = i;
				inList = true;
				break;
			}
		}

		if (!inList) {
			this.Selected = -1;
		}

	}

	public void OnItemSelected(int idx) {
		string res = GetItemText(idx);
		string[] splitRes = res.Split("x");

		int x = splitRes[0].ToInt(); int y = splitRes[1].ToInt();

		GetWindow().Size = new Vector2I(x: x, y: y);
		GetTree().ReloadCurrentScene();
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
