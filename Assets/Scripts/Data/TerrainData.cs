using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainData : UpdatableData {

	public float uniformScale = 2f;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool useFalloff;
	public bool useFlatShading;
}
