using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode { NoiseMap, ColorMap, Mesh, FalloffMap }
	public DrawMode drawMode;

	public const int mapChunkSize = 241;

	[Range(0, 6)] 
	public int editorPreviewLevelOfDetail;

	public Noise.NormalizeMode normalizeMode;
	public float noiseScale;
	public int octaves;

	[Range(0, 1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool useFalloff;
	float[,] falloffMap;

	public bool autoUpdate;

	public TerrainTypes[] regions;


	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>> ();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>> ();

	void Awake() {
		falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize);
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
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
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
		} else if (drawMode == DrawMode.ColorMap) {
			display.DrawTexture (TextureGenerator.textureFromColorMap (mapData.colorMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.Mesh) {
			Texture2D texture = TextureGenerator.textureFromColorMap (mapData.colorMap, mapChunkSize, mapChunkSize);
			MeshData terrainMesh = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLevelOfDetail);
			display.DrawMesh (terrainMesh, texture);
		} else if (drawMode == DrawMode.FalloffMap) {
			display.DrawTexture (TextureGenerator.textureFromHeightMap (FalloffGenerator.GenerateFalloffMap (mapChunkSize)));
		}
	}

	MapData GenerateMapData(Vector2 center) {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, seed, center + offset, normalizeMode);

		Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				if (useFalloff) {
					noiseMap [x, y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap [x, y]);
				}

				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight >= regions [i].height) {
						colorMap [y * mapChunkSize + x] = regions [i].color;
					} else {
						break;
					}
				}
			}
		}

		return new MapData (noiseMap, colorMap);
	}

	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}

		if (octaves < 0) {
			octaves = 0;
		}

		falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize);
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

[System.Serializable]
public struct TerrainTypes {
	public string name;
	public float height;
	public Color color;
}

public struct MapData {
	public readonly float[,] heightMap;
	public readonly Color[] colorMap;

	public MapData (float[,] heightMap, Color[] colorMap)
	{
		this.heightMap = heightMap;
		this.colorMap = colorMap;
	
	}
}
