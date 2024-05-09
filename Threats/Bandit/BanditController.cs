using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MyMotherToldMe.Player;

namespace MyMotherToldMe.Threats.Bandit
{
    public partial class BanditController : CharacterBody2D, IHittable
    {
        [Export] private float _speed;
        [Export] private NodePath _hatSpriteNodePath;
        [Export] private NodePath _faceSpriteNodePath;
        [Export] private NodePath _legsSpriteNodePath;
        [Export] private NodePath _knifeSpriteNodePath;
        [Export] private NodePath _animatorNodePath;
        [Export] private NodePath _walkingDustParticleNodePath;
        [Export] private NodePath _bloodSpillParticleNodePath;
        [Export] private NodePath _perceptionAreaNodePath;
        [Export] private NodePath _attackSliderNodePath;
        [Export] private NodePath _audioPlayerNodePath;
        [Export] private float _attackDistance;
        [Export] private float _attackCooldown;
        [Export] private float _stabDistance;

        private Sprite2D _hatSprite;
        private Sprite2D _faceSprite;
        private Sprite2D _legsSprite;
        private Node2D _knifeSprite;
        private Area2D _perceptionArea;
        private AnimationPlayer _animator;
        private GpuParticles2D _walkingDustParticle;
        private GpuParticles2D _bloodSpillParticle;
        private HSlider _attackCooldownSlider;
        private RandomNumberGenerator _randomGenerator;
        private PlayerController _target;
        private AudioStreamPlayer2D _audioPlayer;
        private bool _isStriking;
        private bool _isWarmingUp;
        private bool _dead;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private Vector2 Direction { get; set; }
    
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            Direction = Vector2.Right;
            
            _hatSprite = GetNode<Sprite2D>(_hatSpriteNodePath);
            _faceSprite = GetNode<Sprite2D>(_faceSpriteNodePath);
            _legsSprite = GetNode<Sprite2D>(_legsSpriteNodePath);
            _knifeSprite = GetNode<Node2D>(_knifeSpriteNodePath);
            _animator = GetNode<AnimationPlayer>(_animatorNodePath);
            _perceptionArea = GetNode<Area2D>(_perceptionAreaNodePath);
            _walkingDustParticle = GetNode<GpuParticles2D>(_walkingDustParticleNodePath);
            _bloodSpillParticle = GetNode<GpuParticles2D>(_bloodSpillParticleNodePath);
            _attackCooldownSlider = GetNode<HSlider>(_attackSliderNodePath);
            _audioPlayer = GetNode<AudioStreamPlayer2D>(_audioPlayerNodePath);
            
            _randomGenerator = new RandomNumberGenerator();
            _randomGenerator.Seed = (ulong)GetHashCode();
            _hatSprite.Modulate = RandomColor();
            _legsSprite.Modulate = RandomColor();
            _attackCooldownSlider.Modulate = new Color(_attackCooldownSlider.Modulate, 0);

            _perceptionArea.Connect("body_entered", new Callable(this, nameof(TargetInSight)));
            _perceptionArea.Connect("body_exited", new Callable(this, nameof(TargetOutOfSight)));
        }

        public override void _Process(double delta)
        {
            if (_dead) return;
            if (_target != null && !_target.Dead)
            {
                if (!(_isWarmingUp || _isStriking) && (Position - _target.Position).LengthSquared() <= Mathf.Pow(_attackDistance, 2))
                {
                    Attack().ConfigureAwait(false);
                }

                if(!_isStriking)
                {
                    Direction = (_target.Position - Position).Normalized();
                    Velocity = Direction * _speed;
                    MoveAndSlide();
                    _animator.Play("walking");
                }
            }
            
            Flip();
        }

        public void Hit()
        {
            if (_dead) return;
            _bloodSpillParticle.Restart();
            Die().ConfigureAwait(false);
        }

        private async Task Die()
        {
            _dead = true;
            var tweener = CreateTween();
            tweener.TweenProperty(this, "rotation_degrees", 90, 0.5f);
            tweener.Play();
            _audioPlayer.Play();
            while (tweener.IsRunning()) await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
            tweener.Stop();
            tweener.TweenProperty(this, "modulate", new Color(Modulate, 0), 1f);
            tweener.Play();
            while (tweener.IsRunning()) await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
            GetParent().RemoveChild(this);
            QueueFree();
        }

        private async Task Attack()
        {
            _isStriking = true;
            
            _animator.Play("attack");
            if((Position - _target.Position).LengthSquared() > Mathf.Pow(_stabDistance, 2))
            {
                Direction = (_target.Position - Position).Normalized();
                var tweener = CreateTween();
                tweener.TweenProperty(
                    this,
                    "position",
                    Position + new Vector2(Direction.X * 5, Direction.Y * 5),
                    0.1f);
                tweener.Play();
                var elapsed = 0d;
                while (elapsed < 0.1f)
                {
                    elapsed += GetProcessDeltaTime();
                    await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
                }
            }

            var animationElapsed = 0d;
            var stabPlayed = false;
            while (animationElapsed < _animator.CurrentAnimation.Length)
            {
                if (!stabPlayed && animationElapsed >= _animator.CurrentAnimation.Length * 0.25f)
                {
                    _target.GetStab();
                    stabPlayed = true;
                }
                animationElapsed += GetProcessDeltaTime();
            }
            
            while (_animator.IsPlaying()) await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
            _isStriking = false;
        }

        private Color RandomColor()
        {
            var color = new Color(
                _randomGenerator.RandfRange(0, 1),
                _randomGenerator.RandfRange(0, 1),
                _randomGenerator.RandfRange(0, 1)
                );
            return color;
        }

        private void Flip()
        {
            _hatSprite.Scale = new Vector2(Mathf.Abs(_hatSprite.Scale.X) * (Direction.X > 0 ? 1 : -1), _hatSprite.Scale.Y);
            _faceSprite.Scale = new Vector2(Mathf.Abs(_faceSprite.Scale.X) * (Direction.X > 0 ? 1 : -1), _faceSprite.Scale.Y);
            _legsSprite.Scale = new Vector2(Mathf.Abs(_legsSprite.Scale.X) * (Direction.X > 0 ? 1 : -1), _legsSprite.Scale.Y);
            _knifeSprite.Scale = new Vector2(Mathf.Abs(_knifeSprite.Scale.X) * (Direction.X > 0 ? 1 : -1), _knifeSprite.Scale.Y);
            _knifeSprite.Position = new Vector2(Mathf.Abs(_knifeSprite.Position.X) * (Direction.X < 0 ? -1 : 1), _knifeSprite.Position.Y);
            _walkingDustParticle.Position = new Vector2(Mathf.Abs(_walkingDustParticle.Position.X) * (Direction.X < 0 ? 1 : -1), 
                _walkingDustParticle.Position.Y);
        }

        private void TargetInSight(PlayerController player)
        {
            if (player.GetType() != typeof(PlayerController)) return;
            
            _target = player;
        }

        private void TargetOutOfSight(PlayerController player)
        {
            _target = null;
        }

        private async Task WaitAttackCooldown()
        {
            _isWarmingUp = true;
            _attackCooldownSlider.Modulate = new Color(_attackCooldownSlider.Modulate);
            var elapsed = 0d;
            while (elapsed < _attackCooldown)
            {
                elapsed += GetProcessDeltaTime();
                _attackCooldownSlider.Value = elapsed / _attackCooldown;
                await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
            }

            _isWarmingUp = false;
            _attackCooldownSlider.Modulate = new Color(_attackCooldownSlider.Modulate, 0);
            _attackCooldownSlider.Value = 0f;
        }

        #region animation-called-functions
        public void PlayWalkingDust()
        {
            _walkingDustParticle.Restart();
        }

        public void AttackLanded()
        {
            WaitAttackCooldown().ConfigureAwait(false);
        }
        #endregion
    }
}
