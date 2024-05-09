using Godot;

namespace MyMotherToldMe
{
    public partial class MainMenuController : Panel
    {
        [Signal] public delegate void PlayButtonPressedEventHandler();
        [Signal] public delegate void QuitButtonPressedEventHandler();
    
        [Export] private NodePath _playButtonNodePath; 
        [Export] private NodePath _quitButtonNodePath;

        private Button _playButton;
        private Button _quitButton;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _playButton = GetNode<Button>(_playButtonNodePath);
            _quitButton = GetNode<Button>(_quitButtonNodePath);

            _quitButton.Pressed += QuitToDesktop;
            _playButton.Pressed += BeginAdventure;
            GD.Print($"{nameof(MainMenuController)} successfully initialized.");
        }

        private void BeginAdventure()
        {
            GD.Print($"{nameof(BeginAdventure)} called.");
            EmitSignal(SignalName.PlayButtonPressed);
        }

        private void QuitToDesktop()
        {
            GD.Print($"{nameof(QuitToDesktop)} called.");
            EmitSignal(SignalName.QuitButtonPressed);
        }
    }
}
