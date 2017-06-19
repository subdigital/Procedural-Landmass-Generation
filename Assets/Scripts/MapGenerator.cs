using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {
		NoiseMap, ColorMap, DrawMesh
	}
	public DrawMode drawMode;

	const int mapChunkSize = 241;

	[Range(0, 6)] 
	public int levelOfDetail;

	public float noiseScale;
	public int octaves;

	[Range(0, 1)]
	public float persistance;
	public float lacunarity;
	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public TerrainTypes[] regions;

	public bool autoUpdate;


	public void GenerateMap() {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, seed, offset);

		Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight <= regions [i].height) {
						colorMap [y * mapChunkSize + x] = regions [i].color;
						break;
					}
				}
			}
		}

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.textureFromHeightMap (noiseMap));
		} else if (drawMode == DrawMode.ColorMap) {
			display.DrawTexture (TextureGenerator.textureFromColorMap (colorMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.DrawMesh) {
			Texture2D texture = TextureGenerator.textureFromColorMap (colorMap, mapChunkSize, mapChunkSize);
			MeshData terrainMesh = MeshGenerator.GenerateTerrainMesh (noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
			display.DrawMesh(terrainMesh, texture);
		}
	}

	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}

		if (octaves < 0) {
			octaves = 0;
		}


	}
}

[System.Serializable]
public struct TerrainTypes {
	public string name;
	public float height;
	public Color color;
}
