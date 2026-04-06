using Godot;

namespace CardChessDemo.Battle.Rooms;

public static class BattleRoomTileSetFactory
{
	public const int FloorSourceId = 0;
	public const int SceneSourceId = 1;
	public static readonly Vector2I FloorAtlasCoords = Vector2I.Zero;

	public static TileSet CreateTileSet(
		Texture2D floorTexture,
		PackedScene playerScene,
		PackedScene enemyScene,
		PackedScene obstacleScene,
		PackedScene escapeScene,
		PackedScene facingLeftScene,
		PackedScene facingUpScene,
		PackedScene facingRightScene,
		PackedScene facingDownScene,
		int cellSizePixels)
	{
		TileSet tileSet = new TileSet
		{
			TileSize = new Vector2I(cellSizePixels, cellSizePixels),
		};

		TileSetAtlasSource atlasSource = new TileSetAtlasSource
		{
			Texture = floorTexture,
			TextureRegionSize = new Vector2I(cellSizePixels, cellSizePixels),
		};
		atlasSource.CreateTile(FloorAtlasCoords);
		tileSet.AddSource(atlasSource, FloorSourceId);

		TileSetScenesCollectionSource sceneSource = new();
		sceneSource.CreateSceneTile(playerScene);
		sceneSource.CreateSceneTile(enemyScene);
		sceneSource.CreateSceneTile(obstacleScene);
		sceneSource.CreateSceneTile(escapeScene, 6);
		sceneSource.CreateSceneTile(facingLeftScene, 7);
		sceneSource.CreateSceneTile(facingUpScene, 8);
		sceneSource.CreateSceneTile(facingRightScene, 9);
		sceneSource.CreateSceneTile(facingDownScene, 10);
		tileSet.AddSource(sceneSource, SceneSourceId);

		return tileSet;
	}
}
