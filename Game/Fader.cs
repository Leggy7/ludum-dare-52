using System;
using System.Threading.Tasks;
using Godot;

namespace MyMotherToldMe.Game
{
    public partial class Fader : Panel
    {
        [Export] private float _transitionTime;
        private bool _tweening;

        public async Task GetBlackAsync()
        {
            GD.Print($"{nameof(GetBlackAsync)} called.");
            Visible = true;
            await SetTransparency(1);
        }

        public async Task BackTransparentAsync()
        {
            GD.Print($"{nameof(BackTransparentAsync)} called.");
            await SetTransparency(0);
            Visible = false;
        }
        
        private async Task SetTransparency(float alpha)
        {
            var tweener = CreateTween();
            tweener.TweenProperty(this, "modulate:a", alpha, _transitionTime);
            tweener.Play();

            _tweening = true;
            while (tweener.IsRunning())
            {
                var delay = GetProcessDeltaTime();
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }

            Modulate = new Color(Modulate, alpha);
            tweener.Stop();
            tweener.Kill();
            _tweening = false;
        }
    }
}
