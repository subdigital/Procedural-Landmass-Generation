﻿Shader "Custom/Terrain" {
	Properties {
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCount = 8;
		const static float epsilon = 1E-4;

		int layerCount;
		float3 baseColors[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseBlends[maxLayerCount];
		float baseColorStrength[maxLayerCount];
		float baseTextureScale[maxLayerCount];

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		float minHeight;
		float maxHeight;

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		sampler2D testTexture;
		float testScale;

		float inverseLerp(float a, float b, float value) {
			return saturate((value-a) / (b-a)); 
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
		
			return xProjection + yProjection + zProjection;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			for (int i = 0; i < layerCount; i++) {
				float a = -baseBlends[i]/2 - epsilon;
				float b = baseBlends[i]/2;
				float value = heightPercent - baseStartHeights[i];
				float drawStrength = inverseLerp(a, b, value);

				float3 baseColor = baseColors[i] * baseColorStrength[i];
				float3 textureColor = triplanar(IN.worldPos, baseTextureScale[i], blendAxes, i) * (1 - baseColorStrength[i]);

				o.Albedo = o.Albedo * (1-drawStrength) + (baseColor + textureColor) * drawStrength;
			}

			//o.Albedo = xProjection + yProjection + zProjection;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
