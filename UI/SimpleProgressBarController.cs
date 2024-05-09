using Godot;

namespace MyMotherToldMe.UI;

public partial class SimpleProgressBarController : HSlider
{
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Value <= 0)
        {
            Modulate = new Color(Modulate, 0);
        }
        else
        {
            Modulate = new Color(Modulate, 1);
        }
    }
}