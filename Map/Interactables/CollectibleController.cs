using Godot;
using MyMotherToldMe.Player;

namespace MyMotherToldMe.Map.Interactables
{
    public partial class CollectibleController : Area2D
    {
        [Export] private NodePath _spriteNodePath;
        [Export] private NodePath _hintNodePath;
        [Export] private Texture2D _collectedTexture;
        [Signal] public delegate void CollectibleCollectedEventHandler();

        private Sprite2D _sprite;
        private Panel _hint;
        
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>(_spriteNodePath);
            _hint = GetNode<Panel>(_hintNodePath);
            Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
            Connect("body_exited", new Callable(this, nameof(OnBodyExited)));
            UpdateHintVisibility(0);
        }

        public void Harvest()
        {
            Disconnect("body_entered", new Callable(this, nameof(OnBodyEntered)));
            Disconnect("body_exited", new Callable(this, nameof(OnBodyExited)));
            _sprite.Texture = _collectedTexture;
            UpdateHintVisibility(0, 0.5f);
            EmitSignal(SignalName.CollectibleCollected);
        }

        public void OnBodyEntered(PlayerController player)
        {
            if (player.GetType() != typeof(PlayerController)) return;
            UpdateHintVisibility(1, 0.5f);
            player.AddCloseTarget(this);
        }

        public void OnBodyExited(PlayerController player)
        {
            UpdateHintVisibility(0, 0.5f);
            player.RemoveCloseTarget(this);
        }

        private void UpdateHintVisibility(float opacityValue, float duration = 0f)
        {
            var initialValue = _hint.Modulate;
            var finalValue = new Color(initialValue, opacityValue);
            var tweener = CreateTween();
            tweener.TweenProperty(_hint, "modulate", finalValue, duration);
            tweener.Play();
        }
    }
}
