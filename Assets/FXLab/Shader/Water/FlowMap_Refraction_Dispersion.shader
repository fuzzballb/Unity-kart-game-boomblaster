
Shader "FXLab/Water/FlowMap_Refraction_Dispersion" {
	Properties {
		
		_FlowMap ("Flow Map (RG = Direction B = Speed A = Foam Intensity)", 2D) = "gray" {}
		_WaveSpeed ("Wave Flow Factor", Vector) = (2, 4, 8, 16)
		_WaveScale ("Wave Scale", Vector) = (1, 2, 4, 8)
		_WaveInfluenceFactor ("Wave Influence Factor", Vector) = (1, 1, 1, 1)
		_FXScreenTexture ("Screen Texture for Refraction (FXScreenBufferTexture)", 2D) = "" {}
		
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Specular ("Specular", Range (0.0, 2)) = 0.078125
		_Shininess ("Shininess", Range (1, 64)) = 64
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_DistortionStrength ("Distortion Strength", Float) = 10
		_FresnelNormalStrength ("Fresnel Normal Strength", Range(0.0, 1)) = 0.09615385
		_Fresnel ("Fresnel", Range (0.0, 1.0)) = 0.05769231
		_FresnelFactor ("Fresnel Factor", Float) = 4
		_FresnelBias ("Fresnel Bias", Float) = 0
		
		_DispersionFactor ("Red Dispersion Factor", Float) = 1
	}
	
	SubShader {
		Tags { "Queue"="Transparent-2" "RenderType" = "Water"}
		LOD 400
		Cull Off
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma surface surf WaterSpecular alpha noambient noforwardadd vertex:vert
		#pragma target 3.0
		
		#include "UnityCG.cginc"
		
		#define FASTER_RENDERTEXTURE_ACCESS
		#include "Water.cginc"
		
		sampler2D _BumpMap;
		
		sampler2D _FlowMap;
		
		half4 _WaveSpeed;
		half4 _WaveScale;
		half4 _WaveInfluenceFactor;
		fixed _Specular;
		float _Shininess;
		
		half _DistortionStrength;
		
		half _DispersionFactor;
		
		fixed _FresnelNormalStrength;
		fixed _Fresnel;
		half _FresnelFactor;
		fixed _FresnelBias;
		
		half4 LightingWaterSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			half3 h = normalize (lightDir + viewDir);
			
			half diff = max (0, dot (s.Normal, lightDir));
			
			float nh = max (0, dot (s.Normal, h));
			float spec = pow (nh, _Shininess)* _Specular;
			
			half4 c;
			c.rgb = s.Albedo + (_LightColor0.rgb * diff * _SpecColor.rgb * spec) * (atten * 2);
			c.a = s.Alpha;
			return c;
		}
		
		struct Input
		{
			float2 uv_FlowMap;
			float2 uv_BumpMap;
			float4 screenPos;
			float3 viewDir;
			float3 worldPosition;
		};
		
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.worldPosition = mul(_Object2World, v.vertex);
		}
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			float2 screenUv = calcScreenUv(IN.screenPos);
			
			FLOW_MAP_TEXTURE();
			
			half3 normal1 = _WaveInfluenceFactor.x * UnpackNormal(tex2D(_BumpMap, _WaveScale.x * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.x));
			half3 normal2 = _WaveInfluenceFactor.y * UnpackNormal(tex2D(_BumpMap, _WaveScale.y * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.y));
			half3 normal3 = _WaveInfluenceFactor.z * UnpackNormal(tex2D(_BumpMap, _WaveScale.z * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.z));
			half3 normal4 = _WaveInfluenceFactor.w * UnpackNormal(tex2D(_BumpMap, _WaveScale.w * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.w));
			
			fixed3 bumpNormal = normalize(normal1 + normal2 + normal3 + normal4);
			
			float2 screenUVOffset = bumpNormal.xy * _DistortionStrength / 100;
			
			o.Normal.xyz = normalize(bumpNormal);
			
			half3 dispersionFactor = half3(1, 1 + _DispersionFactor * 0.1, 1 + _DispersionFactor * 0.2);
			
			fixed3 refr = sampleScreenDispersion(screenUv, screenUVOffset, dispersionFactor);
			fixed3 refl = 1;
			
			float over = max(0, Luminance(refr) - Luminance(refl));
			fixed fresnel = saturate(fresnelTerm(normalize(lerp(fixed3(0, 0, 1), o.Normal.xyz, _FresnelNormalStrength)), normalize(IN.viewDir), _Fresnel, _FresnelFactor, _FresnelBias) - over);
			
			o.Albedo = lerp(refr, refl, fresnel);
			o.Alpha = 1;
		}
		ENDCG
		Lighting On
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma surface surf WaterSpecular alpha noambient noforwardadd vertex:vert
		#pragma target 3.0
		
		#include "UnityCG.cginc"
		#include "Water.cginc"
		
		sampler2D _BumpMap;
		
		samplerCUBE _ReflectionCube;
		
		sampler2D _FlowMap;
		
		half4 _WaveSpeed;
		half4 _WaveScale;
		half4 _WaveInfluenceFactor;
		
		fixed _Specular;
		float _Shininess;
		
		half _DistortionStrength;
		
		fixed _FresnelNormalStrength;
		fixed _Fresnel;
		half _FresnelFactor;
		fixed _FresnelBias;
		
		half4 LightingWaterSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			half3 h = normalize (lightDir + viewDir);
			
			half diff = max (0, dot (s.Normal, lightDir));
			
			float nh = max (0, dot (s.Normal, h));
			float spec = pow (nh, _Shininess);
			
			half4 c;
			c.rgb = s.Albedo + (_LightColor0.rgb * diff * _SpecColor.rgb * spec) * (atten * 2) * _Specular;
			c.a = s.Alpha;
			return c;
		}
		
		struct Input {
			float2 uv_FlowMap;
			float2 uv_BumpMap;
			float3 viewDir;
			float4 screenPos;
			float3 worldRefl;
			INTERNAL_DATA
		};
		
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
		}
		
		void surf (Input IN, inout SurfaceOutput o) {
			FLOW_MAP_TEXTURE();
			
			half3 normal1 = _WaveInfluenceFactor.x * UnpackNormal(tex2D(_BumpMap, _WaveScale.x * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.x));
			half3 normal2 = _WaveInfluenceFactor.y * UnpackNormal(tex2D(_BumpMap, _WaveScale.y * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.y));
			half3 normal3 = _WaveInfluenceFactor.z * UnpackNormal(tex2D(_BumpMap, _WaveScale.z * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.z));
			half3 normal4 = _WaveInfluenceFactor.w * UnpackNormal(tex2D(_BumpMap, _WaveScale.w * IN.uv_BumpMap + flowMap.rg * _Time.x * _WaveSpeed.w));
			
			fixed3 bumpNormal = normalize(normal1 + normal2 + normal3 + normal4);
			o.Normal.xyz = normalize(bumpNormal);
			
			fixed fresnel = saturate(fresnelTerm(normalize(lerp(fixed3(0, 0, 1), o.Normal.xyz, _FresnelNormalStrength)), normalize(IN.viewDir), _Fresnel, _FresnelFactor, _FresnelBias));
			
			o.Emission  = texCUBE (_ReflectionCube, WorldReflectionVector (IN, lerp(fixed3(0, 0, 1), o.Normal, _DistortionStrength / 100 / 2))).rgb;
			
			o.Alpha = fresnel;
		}
		
		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "WaterMaterialEditor"
}
