using System.Collections.Generic;
using Godot;
using CollectibleController = MyMotherToldMe.Map.Interactables.CollectibleController;

namespace MyMotherToldMe.Map
{
    public partial class GameMapController : Node2D
    {
        [Export] private NodePath _terrainNodePath;
        [Export] private NodePath _collectiblesNodePath;
        [Export] private NodePath _spawningPointPath;
        [Export] private NodePath _deliveryPointPath;

        private TileMap _terrainTileMap;
        private Node2D _spawningPoint;
        private DeliveryPointController _deliveryPointController;
        private Node2D _collectibles;

        public Rect2 Bounds { get; private set; }
        public DeliveryPointController DeliveryPointController => _deliveryPointController;
        public Vector2 SpawningPoint => _spawningPoint.Position;
        
        public List<CollectibleController> Collectibles
        {
            get
            {
                var collection = new List<CollectibleController>();
                for (var i = 0; i < _collectibles.GetChildCount(); i++)
                {
                    collection.Add(_collectibles.GetChild<CollectibleController>(i));
                }

                return collection;
            }
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _terrainTileMap = GetNode<TileMap>(_terrainNodePath);
            _collectibles = GetNode<Node2D>(_collectiblesNodePath);
            _spawningPoint = GetNode<Node2D>(_spawningPointPath);
            _deliveryPointController = GetNode<DeliveryPointController>(_deliveryPointPath);
        
            // Get the bounding box of the used tiles in the TileMap in tiles
            var usedRect = _terrainTileMap.GetUsedRect();
        
            // Get the size of a single tile in pixels
            var tileSize = _terrainTileMap.TileSet.TileSize;
        
            // Calculate the bounding box of the used tiles in pixels
            Bounds = new Rect2(usedRect.Position * tileSize, usedRect.Size * tileSize);
        }
    }
}
