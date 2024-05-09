using Godot;

namespace MyMotherToldMe.UI.EndSceneMenu
{
    public partial class EndSceneController : Panel
    {
        [Signal] public delegate void BackButtonPressedEventHandler();
        [Export] private NodePath _backToMainNodePath;
    
        private Button _backToMainButton;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _backToMainButton = GetNode<Button>(_backToMainNodePath);
            _backToMainButton.Connect("button_up", new Callable(this, nameof(OnBackButtonPressed)));
        }

        private void OnBackButtonPressed()
        {
            EmitSignal(SignalName.BackButtonPressed);
        }
    }
}
