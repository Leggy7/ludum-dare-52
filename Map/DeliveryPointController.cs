using Godot;
using MyMotherToldMe.Player;

namespace MyMotherToldMe.Map
{
    public partial class DeliveryPointController : Area2D
    {
        [Signal]
        public delegate void DeliveryPointReachedEventHandler();
        
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
        }

        private void OnBodyEntered(Node2D body)
        {
            EmitSignal(SignalName.DeliveryPointReached);
        }
    }
}
