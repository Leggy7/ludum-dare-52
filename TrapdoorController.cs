using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MyMotherToldMe.Player;

namespace MyMotherToldMe
{
    public partial class TrapdoorController : Node2D
    {
        [Export] private float _rechargeTime;
        [Export] private NodePath _animatorPath;
        [Export] private NodePath _perceptionAreaNodePath;
        [Export] private NodePath _effectAreaNodePath;
        [Export] private NodePath _audioPlayerNodePath;

        private AnimationPlayer _animator;
        private Area2D _perceptionArea;
        private Area2D _effectArea;
        private AudioStreamPlayer2D _audioPlayer;

        private readonly List<IHittable> _targets = new();
        private bool _activated;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public override void _Ready()
        {
            _animator = GetNode<AnimationPlayer>(_animatorPath);
            _perceptionArea = GetNode<Area2D>(_perceptionAreaNodePath);
            _effectArea = GetNode<Area2D>(_effectAreaNodePath);
            _audioPlayer = GetNode<AudioStreamPlayer2D>(_audioPlayerNodePath);

            _perceptionArea.Connect("body_entered", new Callable(this, nameof(Trigger)));
            _effectArea.Connect("body_entered", new Callable(this, nameof(TargetAcquired)));
            _effectArea.Connect("body_exited", new Callable(this, nameof(TargetLost)));
            Visible = false;
        }

        public void Trigger(PlayerController player)
        {
            if (!_activated && player.GetType() != typeof(PlayerController)) return;
            _animator.Play("trigger");
            _audioPlayer.Play();
            Visible = true;
        }

        private void TargetAcquired(CharacterBody2D target)
        {
            if (!(target is IHittable hittable)) return;
            _targets.Add(hittable);
            if(_activated) _targets.ForEach(t => t.Hit());
        }

        private void TargetLost(CharacterBody2D target)
        {
            if (!(target is IHittable hittable)) return;
            _targets.Remove(hittable);
        }

        public void ApplyEffect()
        {
            if (_activated) return;
            _activated = true;
            _targets.ForEach(t => t.Hit());
            Recharge().ConfigureAwait(false);
        }

        private async Task Recharge()
        {
            var elapsed = 0d;
            while (elapsed < _rechargeTime)
            {
                elapsed += GetProcessDeltaTime();
                await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);
            }
            
            _animator.PlayBackwards("trigger");
            while(_animator.IsPlaying()) await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()), _cancellationTokenSource.Token);

            _activated = false;
            Visible = false;
        }
    }
}
