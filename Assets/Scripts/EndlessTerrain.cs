using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	public const float maxViewDistance = 450;
	public Transform viewer;

	public static Vector2 viewerPosition;
	int chunkSize;
	int chunksVisibleInViewDistance;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDistance = Mathf.RoundToInt (maxViewDistance / chunkSize);
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
		UpdateVisibleChunks ();
	}

	void UpdateVisibleChunks() {

		foreach (var terrainChunk in terrainChunksVisibleLastUpdate) {
			terrainChunk.SetVisible (false);
		}
		terrainChunksVisibleLastUpdate.Clear ();

		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					TerrainChunk chunk = terrainChunkDictionary [viewedChunkCoord];
					chunk.UpdateTerrainChunk ();
					if (chunk.IsVisible ()) {
						terrainChunksVisibleLastUpdate.Add (chunk);
					}
				} else {
					TerrainChunk chunk = new TerrainChunk (viewedChunkCoord, chunkSize, transform);
					terrainChunkDictionary.Add (viewedChunkCoord, chunk);
				}
			}
		}
	}

	public class TerrainChunk {

		Vector2 pos;
		GameObject meshObject;
		Bounds bounds;

		public TerrainChunk(Vector2 coord, int size, Transform parent) {
			pos = coord * size;	
			bounds = new Bounds(pos, Vector2.one * size);
			Vector3 pos3 = new Vector3(pos.x, 0, pos.y);

			meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
			meshObject.transform.parent = parent;
			meshObject.transform.position = pos3;
			meshObject.transform.localScale = Vector3.one * size / 10f;
			SetVisible(false);
		}

		public void UpdateTerrainChunk() {
			float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));
			bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
			SetVisible (visible);
		}

		public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}
	}
}
