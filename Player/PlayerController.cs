using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using CollectibleController = MyMotherToldMe.Map.Interactables.CollectibleController;

namespace MyMotherToldMe.Player
{
    public partial class PlayerController : CharacterBody2D, IHittable
    {
        [Signal]
        public delegate void CollectedGoalEventHandler();
        [Signal]
        public delegate void PlayerDiedEventHandler();
        
        [Export] private float _speed;
        [Export] private float _dashDistance;
        [Export] private float _dashRefillSpeed;
        [Export] private NodePath _spriteNodePath;
        [Export] private NodePath _animatorNodePath;
        [Export] private NodePath _walkingDustParticleNodePath;
        [Export] private NodePath _bloodSpillParticleNodePath;
        [Export] private NodePath _evasion1StChargeNodePath;
        [Export] private NodePath _evasion2NdChargeNodePath;
        [Export] private Color _rechargingDashColor;
        [Export] private Color _readyDashColor;
        [Export] private NodePath _audioPlayerNodePath;

        private Sprite2D _sprite;
        private AnimationPlayer _animator;
        private GpuParticles2D _walkingDustParticle;
        private GpuParticles2D _bloodSpillParticle;
        private HSlider _dash1StCharge;
        private HSlider _dash2NdCharge;
        private AudioStreamPlayer2D _audioPlayer;
        private readonly List<CollectibleController> _availableCollectibles = new();
        private int _remainingDashes;
        private readonly Queue<HSlider> _refillingQueue = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Vector2 Direction { get; set; }
        public bool Dead { get; private set; }
        private bool Dashing { get; set; }

        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>(_spriteNodePath);
            _animator = GetNode<AnimationPlayer>(_animatorNodePath);
            _walkingDustParticle = GetNode<GpuParticles2D>(_walkingDustParticleNodePath);
            _bloodSpillParticle = GetNode<GpuParticles2D>(_bloodSpillParticleNodePath);
            _dash1StCharge = GetNode<HSlider>(_evasion1StChargeNodePath);
            _dash2NdCharge = GetNode<HSlider>(_evasion2NdChargeNodePath);
            _audioPlayer = GetNode<AudioStreamPlayer2D>(_audioPlayerNodePath);
            _dash1StCharge.Value = 1;
            _dash2NdCharge.Value = 1;
            _dash1StCharge.Modulate = new Color(_readyDashColor);
            _dash2NdCharge.Modulate = new Color(_readyDashColor);
            _remainingDashes = 2;
            TreeExiting += OnTreeExiting;
        }

        public override void _Process(double delta)
        {
            if (Dead) return;
            
            var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            
            if (!Dashing && direction.LengthSquared() > 0)
            {
                Direction = direction.Normalized();
                Velocity = Direction * _speed;
                MoveAndSlide();
                _animator.Play("walking");
            }

            if (!Dashing && Input.IsActionJustReleased("dash") && ConsumeDash())
            {
                Dash().ConfigureAwait(false);
            }

            if (Input.IsActionJustReleased("collect") && _availableCollectibles.Count > 0)
            {
                Harvest();
            }
            
            if(Direction.X != 0)
            {
                Flip();
            }
        }

        private async Task Dash()
        {
            Dashing = true;
            _animator.Play("dash");
            SetCollisionMaskValue(1, false);
            var destination = Direction * _dashDistance;
            
            while (_animator.IsPlaying())
            {
                //TODO fix dash
                Position += Direction * (float) (_animator.CurrentAnimationLength / GetProcessDeltaTime()) / _dashDistance;
                await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
            }
            
            SetCollisionMaskValue(1, true);
            Dashing = false;
        }

        public void GetStab()
        {
            if (Dashing) return;
            _bloodSpillParticle.Restart();
            Die().ConfigureAwait(false);
        }

        public void Hit()
        {
            if (Dead) return;
            _bloodSpillParticle.Restart();
            Die().ConfigureAwait(false);
        }

        private async Task Die()
        {
            Dead = true;
            var tweener = CreateTween();
            tweener.TweenProperty(this, "rotation_degrees", 90, 0.5f);
            tweener.Play();
            _audioPlayer.Play();
            while (tweener.IsRunning()) await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()));
            EmitSignal(SignalName.PlayerDied);
        }

        public void AddCloseTarget(CollectibleController collectible)
        {
            _availableCollectibles.Add(collectible);
        }

        public void RemoveCloseTarget(CollectibleController collectible)
        {
            _availableCollectibles.Remove(collectible);
        }

        private void Harvest()
        {
            var collectible = _availableCollectibles.First();
            collectible.Harvest();
            _availableCollectibles.Remove(collectible);
            EmitSignal(SignalName.CollectedGoal);
        }

        private bool ConsumeDash()
        {
            if (_remainingDashes <= 0) return false;
            
            if (_remainingDashes == 1)
            {
                var secondBarLevel = _dash2NdCharge.Value;
                _dash1StCharge.Value = secondBarLevel;
                _dash2NdCharge.Value = 0;
                RefillDashBar(_dash1StCharge).ConfigureAwait(false);
            } else if (_remainingDashes == 2)
            {
                _dash2NdCharge.Value = 0;
                RefillDashBar(_dash2NdCharge).ConfigureAwait(false);
            }

            _remainingDashes--;
            return true;
        }

        private async Task RefillDashBar(HSlider slider)
        {
            _refillingQueue.Enqueue(slider);
            slider.Modulate = new Color(_rechargingDashColor);
            var elapsed = slider.Value;
            while (elapsed < _dashRefillSpeed)
            {
                if(slider == _dash2NdCharge && _refillingQueue.Count >1)
                {
                    slider.Value = 0;
                    elapsed = 0d;
                    await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
                } 
                slider.Value = elapsed / _dashRefillSpeed;
                elapsed +=  GetProcessDeltaTime();
                await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
            }

            if (slider.Value > slider.MaxValue) slider.Value = slider.MaxValue;
            slider.Modulate = new Color(_readyDashColor);
            _refillingQueue.Dequeue();
            _remainingDashes++;
        }

        private void Flip()
        {
            _sprite.Scale = new Vector2(Mathf.Abs(_sprite.Scale.X) * (Direction.X > 0 ? 1 : -1), _sprite.Scale.Y);
            _walkingDustParticle.Position = new Vector2(Mathf.Abs(_walkingDustParticle.Position.X) * (Direction.X > 0 ? -1 : 1), 
                _walkingDustParticle.Position.Y);
        }

        private void OnTreeExiting()
        {
            _cancellationTokenSource.Cancel();
        }

        #region animation-called-functions
        public void PlayWalkingDust()
        {
            _walkingDustParticle.Restart();
        }
        #endregion
    }
}
