using Godot;

namespace MyMotherToldMe.UI.InGame
{
    public partial class HarvestInfoUiController : Control
    {
        [Export] private NodePath _goalLabelNodePath;
        [Export] private int _max;

        public int GoalToReach
        {
            get => _max;
            set => _max = value;
        }
        
        private Label _goalLabel;
        
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _goalLabel = GetNode<Label>(_goalLabelNodePath);
        }

        public void UpdateGoalLabel(int value)
        {
            _goalLabel.Text = $"{value}/{GoalToReach}";
        }
    }
}
