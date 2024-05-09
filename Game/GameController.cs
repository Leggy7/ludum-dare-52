using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MyMotherToldMe.Game.Camera;
using MyMotherToldMe.Map;
using MyMotherToldMe.Player;
using MyMotherToldMe.UI.EndSceneMenu;
using MyMotherToldMe.UI.InGame;

namespace MyMotherToldMe.Game
{
    public partial class GameController : Node2D
    {
        [Export] private PackedScene _playerTemplate;
        [Export] private float _previewTime;
        [Export] private PackedScene[] _levels;

        [Export] private GameCamera _camera;
        [Export] private InGameUiController _inGameUiController;
        [Export] private Fader _fader;
        [Export] private MainMenuController _mainMenuController;
        [Export] private EndSceneController _endSceneController;
        [Export] private AudioStreamPlayer _loseAudioPlayer;
        [Export] private AudioStreamPlayer _nextLevelAudioPlayer;
        
        private PlayerController _playerController;
        private GameMapController _currentMapController;

        private int _goalsCollected;
        private int _totalGoals;
        private int _currentLevel;

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            Engine.MaxFps = 60;
            _inGameUiController.MenuOpened += OnMenuOpened;
            _inGameUiController.MenuClosed += OnMenuClosed;
            _mainMenuController.PlayButtonPressed += OnAdventureBegan;
            _mainMenuController.QuitButtonPressed += OnApplicationQuit;
            _endSceneController.BackButtonPressed += OnGoingBackToMainMenu;
        }

        private void OnAdventureBegan()
        {
            GD.Print($"{nameof(OnAdventureBegan)} called.");
            AdventurePreparation().ConfigureAwait(false);
        }

        private async Task AdventurePreparation()
        {
            await _fader.GetBlackAsync();
            _mainMenuController.Visible = false;
            LoadLevel(_currentLevel);
            await _fader.BackTransparentAsync();
            _camera.RunDequeueZooms().ConfigureAwait(false);
        }

        private void OnApplicationQuit()
        {
            GD.Print($"{nameof(OnApplicationQuit)} called.");
            GetTree().Quit();
        }

        private void OnGoingBackToMainMenu()
        {
            LoadMainMenu().ConfigureAwait(false);
        }

        private void OnPlayerDied()
        {
            ElaboratePlayerDeath().ConfigureAwait(false);
        }

        private async Task ElaboratePlayerDeath()
        {
            _loseAudioPlayer.Play();
            while (_loseAudioPlayer.Playing) await Task.Delay(TimeSpan.FromSeconds(GetProcessDeltaTime()));
            
            await _fader.GetBlackAsync();
            DestroyPlayer();
            _currentMapController?.GetParent().RemoveChild(_currentMapController);
            _currentMapController?.QueueFree();
            _mainMenuController.Visible = true;
            await _fader.BackTransparentAsync();
        }

        private async Task LoadMainMenu()
        {
            GD.Print($"{nameof(LoadMainMenu)} is called...");
            await _fader.GetBlackAsync();
            _endSceneController.Visible = false;
            _mainMenuController.Visible = true;
            await _fader.BackTransparentAsync();
        }

        private void LoadLevel(int level)
        {
            GD.Print($"{nameof(LoadLevel)} begin");
            if (_levels == null || level >= _levels.Length) throw new Exception("You missed to initialize level array.");
            
            _currentMapController = _levels[level].Instantiate<GameMapController>();
            AddChild(_currentMapController);

            _currentMapController.DeliveryPointController.DeliveryPointReached += DeliveryCheck;
            _camera.Bounds = _currentMapController.Bounds;
            _totalGoals = _currentMapController.Collectibles.Count;
            
            _playerController = _playerTemplate.Instantiate<PlayerController>();
            _playerController.PlayerDied += OnPlayerDied;
            _playerController.Position = _currentMapController.SpawningPoint;
            _playerController.CollectedGoal += UpdateGoalDisplay;
            AddChild(_playerController);
            MakeCameraFollowTarget(_playerController);
            
            InitializeCollectibles();
            _inGameUiController.InitializeMaxGoal(_totalGoals);
            _goalsCollected = 0;
            GD.Print($"{nameof(LoadLevel)} end");
        }

        private async Task LoadEndScene()
        {
            DestroyPlayer();
            await _fader.GetBlackAsync();
            _currentMapController.Visible = false;
            _endSceneController.Visible = true;
            await _fader.BackTransparentAsync();
        }

        private void MakeCameraFollowTarget(Node target)
        {
            _camera.GetParent().RemoveChild(_camera);
            target.AddChild(_camera);
        }

        private void InitializeCollectibles()
        {
            _camera.Zoom = new Vector2(10, 10);
            // _camera.EnqueueZoomRequest(_playerController, 10f, 0.5f);
        }

        private void UpdateGoalDisplay()
        {
            _goalsCollected++;
            _inGameUiController.UpdateScore(_goalsCollected);
        }

        private void DeliveryCheck()
        {
            if (_goalsCollected >= _totalGoals)
            {
                _nextLevelAudioPlayer.Play();
                if (_currentLevel < _levels.Length - 1) TransitionToNextLevel().ConfigureAwait(false);
                else LoadEndScene().ConfigureAwait(false);
            }
        }

        private async Task TransitionToNextLevel()
        {
            GD.Print($"{nameof(TransitionToNextLevel)} called...");
            await _fader.GetBlackAsync();
            
            _currentMapController?.GetParent().RemoveChild(_currentMapController);
            _currentMapController?.QueueFree();
            DestroyPlayer();
            
            LoadLevel(++_currentLevel);
            await _fader.BackTransparentAsync();
        }

        private void DestroyPlayer()
        {
            if(_camera.GetParent() == _playerController) _playerController.RemoveChild(_camera); 
            _playerController?.GetParent().RemoveChild(_playerController);
            _playerController?.QueueFree();
            AddChild(_camera);
        }

        private void OnMenuOpened()
        {
            GetTree().Paused = true;
        }

        private void OnMenuClosed()
        {
            GetTree().Paused = false;
        }
    }
}
