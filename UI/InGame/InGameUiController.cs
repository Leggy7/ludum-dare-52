using Godot;

namespace MyMotherToldMe.UI.InGame
{
    public partial class InGameUiController : CanvasLayer
    {
        [Signal] public delegate void MenuOpenedEventHandler();
        [Signal] public delegate void MenuClosedEventHandler();
        [Export] private NodePath _harvestInfoNodePath;
        [Export] private NodePath _cogwheelNodePath;
        [Export] private PackedScene _settingsScene;

        private HarvestInfoUiController _harvestInfoUiController;
        private Button _cogwheelController;
        private SoundSettingsController _soundSettingsController;

        private bool _menuIsOpen;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _harvestInfoUiController = GetNode<HarvestInfoUiController>(_harvestInfoNodePath);
            _cogwheelController = GetNode<Button>(_cogwheelNodePath);
            _soundSettingsController = _settingsScene.Instantiate<SoundSettingsController>();
            AddChild(_soundSettingsController);
            _soundSettingsController.Visible = false;

            _cogwheelController.Connect("button_up", new Callable(this, nameof(OpenSettings)));
        }

        public void InitializeMaxGoal(int amount)
        {
            _harvestInfoUiController.GoalToReach = amount;
            _harvestInfoUiController.UpdateGoalLabel(0);
        }

        public void UpdateScore(int value)
        {
            _harvestInfoUiController.UpdateGoalLabel(value);
        }

        private void OpenSettings()
        {
            _soundSettingsController.Visible = _menuIsOpen = !_menuIsOpen;
            if (_menuIsOpen) EmitSignal(SignalName.MenuOpened);
            if (!_menuIsOpen) EmitSignal(SignalName.MenuClosed);
            _cogwheelController.FocusMode = Control.FocusModeEnum.None;
        }
    }
}
