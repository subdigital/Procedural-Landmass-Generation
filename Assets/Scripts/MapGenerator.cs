using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode { NoiseMap, Mesh, FalloffMap }
	public DrawMode drawMode;

	public NoiseData noiseData;
	public TerrainData terrainData;
	public TextureData textureData;
	public Material terrainMaterial;

	public int mapChunkSize {
		get {
			if (terrainData.useFlatShading) {
				return 95; // flat shading uses a lot more vertices, so we need a smaller chunk size
			} else {
				return 239;
			}
		}
	}

	[Range(0, 6)] 
	public int editorPreviewLevelOfDetail;

	public bool autoUpdate;


	float[,] falloffMap;
	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>> ();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>> ();

	void Awake() {
	}

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor ();
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial (terrainMaterial);
	}

	public void RequestMapData (Vector2 center, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread (center, callback);
		};
		new Thread (threadStart).Start();
	}

	public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread(mapData, levelOfDetail, callback);
		};
		new Thread (threadStart).Start ();
	}

	void MapDataThread(Vector2 center, Action<MapData> callback) {
		MapData mapData = GenerateMapData (center);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	void MeshDataThread(MapData mapData, int levelOfDetail, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, levelOfDetail, terrainData.useFlatShading);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}

	void Update() {
		if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData (Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.textureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.Mesh) {
			MeshData terrainMesh = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLevelOfDetail, terrainData.useFlatShading);
			display.DrawMesh (terrainMesh);
		} else if (drawMode == DrawMode.FalloffMap) {
			display.DrawTexture (TextureGenerator.textureFromHeightMap (FalloffGenerator.GenerateFalloffMap (mapChunkSize)));
		}
	}

	MapData GenerateMapData(Vector2 center) {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize + 2, mapChunkSize + 2, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, noiseData.seed, center + noiseData.offset, noiseData.normalizeMode);

		if (terrainData.useFalloff) {
			if (falloffMap == null) {
				falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize + 2);
			}

			for (int y = 0; y < mapChunkSize+2; y++) {
				for (int x = 0; x < mapChunkSize+2; x++) {
					noiseMap [x, y] = Mathf.Clamp01 (noiseMap [x, y] - falloffMap [x, y]);
				}
			}
		}



		return new MapData (noiseMap);
	}

	void OnValidate() {

		if (terrainData != null) {
			terrainData.OnValuesUpdated -= OnValuesUpdated;
			terrainData.OnValuesUpdated += OnValuesUpdated;
		}

		if (noiseData != null) {
			noiseData.OnValuesUpdated -= OnValuesUpdated;
			noiseData.OnValuesUpdated += OnValuesUpdated;
		}

		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}

	}

	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;
		public MapThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}

public struct MapData {
	public readonly float[,] heightMap;

	public MapData (float[,] heightMap)
	{
		this.heightMap = heightMap;
	}
}
