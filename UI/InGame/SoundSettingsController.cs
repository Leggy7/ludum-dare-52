using System;
using Godot;

namespace MyMotherToldMe.UI.InGame
{
    public partial class SoundSettingsController : Panel
    {
        [Export] private NodePath _masterVolumeSliderNodePath;
        private HSlider _masterVolumeSlider;

        private int _masterBus;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _masterVolumeSlider = GetNode<HSlider>(_masterVolumeSliderNodePath);
            _masterVolumeSlider.Connect("value_changed", new Callable(this, nameof(UpdateVolume)));
            
            _masterBus = AudioServer.GetBusIndex("Master");
        }

        private void UpdateVolume(float value)
        {
            AudioServer.SetBusVolumeDb(_masterBus, LinearToDb(value));
        }

        private float LinearToDb(float linear)
        {
            return (float)(20f * Math.Log10(linear));
        }
    }
}
