
Shader "FXLab/Water/FlowMap_Decal" {
	Properties {
		
		_MainTex ("MainTex", 2D) = "white" {}
		
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Specular ("Specular", Range (0.0, 2)) = 0.078125
		_Shininess ("Shininess", Range (1, 64)) = 64
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_DistortionStrength ("Distortion Strength", Float) = 10
		_FresnelNormalStrength ("Fresnel Normal Strength", Range(0.0, 1)) = 0.09615385
		_Fresnel ("Fresnel", Range (0.0, 1.0)) = 0.05769231
		_FresnelFactor ("Fresnel Factor", Float) = 4
		_FresnelBias ("Fresnel Bias", Float) = 0
		
		_MaskMap ("Mask Map (B)", 2D) = "white" {}
		_Transparency ("Transparency", Range(0, 1)) = 1
		_BumpUpInfluence ("Bump Up influence", Range(0, 1)) = 1
	}
	
	SubShader {
		Tags { "Queue"="Transparent-1" "RenderType" = "Water"}
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
		sampler2D _MainTex;
		fixed _Specular;
		float _Shininess;
		
		half _DistortionStrength;
		
		fixed _FresnelNormalStrength;
		fixed _Fresnel;
		half _FresnelFactor;
		fixed _FresnelBias;
		
		sampler2D _MaskMap;
		fixed _Transparency;
		fixed _BumpUpInfluence;
		fixed _Scale;
		
		half4 LightingWaterSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			half3 h = normalize (lightDir + viewDir);
			
			half diff = max (0, dot (s.Normal, lightDir));
			
			float nh = max (0, dot (s.Normal, h));
			float spec = pow (nh, _Shininess)* _Specular;
			
			half4 c;
			c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * (atten * 2);
			c.a = s.Alpha;
			return c;
		}
		
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_MaskMap;
			float2 uv_FlowMap;
			float2 uv_BumpMap;
			float4 screenPos;
			float3 viewDir;
			float3 worldPosition;
		};
		
		void vert (inout appdata_full v, out Input o) {
			v.vertex.xyz *= _Scale;
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.worldPosition = mul(_Object2World, v.vertex);
		}
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			float2 screenUv = calcScreenUv(IN.screenPos);
			
			fixed3 bumpNormal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			
			float2 screenUVOffset = bumpNormal.xy * _DistortionStrength / 100;
			
			o.Normal.xyz = normalize(bumpNormal);
			
			fixed3 refr = tex2D(_MainTex, IN.uv_MainTex).rgb;
			fixed3 refl = 1;
			
			float over = max(0, Luminance(refr) - Luminance(refl));
			fixed fresnel = saturate(fresnelTerm(normalize(lerp(fixed3(0, 0, 1), o.Normal.xyz, _FresnelNormalStrength)), normalize(IN.viewDir), _Fresnel, _FresnelFactor, _FresnelBias) - over);
			
			o.Albedo = lerp(refr, refl, fresnel);
			o.Alpha = 1;
			o.Alpha *= _Transparency * tex2D(_MaskMap, IN.uv_MaskMap).b * lerp(1, max(0, 1-bumpNormal.z), _BumpUpInfluence);
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
		
		fixed _Specular;
		float _Shininess;
		
		half _DistortionStrength;
		
		fixed _FresnelNormalStrength;
		fixed _Fresnel;
		half _FresnelFactor;
		fixed _FresnelBias;
		
		sampler2D _MaskMap;
		fixed _Transparency;
		fixed _BumpUpInfluence;
		fixed _Scale;
		
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
			float2 uv_MaskMap;
			float2 uv_BumpMap;
			float3 viewDir;
			float4 screenPos;
			float3 worldRefl;
			INTERNAL_DATA
		};
		
		void vert (inout appdata_full v, out Input o) {
			v.vertex.xyz *= _Scale;
			UNITY_INITIALIZE_OUTPUT(Input,o);
		}
		
		void surf (Input IN, inout SurfaceOutput o) {
			fixed3 bumpNormal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			o.Normal.xyz = normalize(bumpNormal);
			
			fixed fresnel = saturate(fresnelTerm(normalize(lerp(fixed3(0, 0, 1), o.Normal.xyz, _FresnelNormalStrength)), normalize(IN.viewDir), _Fresnel, _FresnelFactor, _FresnelBias));
			
			o.Emission  = texCUBE (_ReflectionCube, WorldReflectionVector (IN, lerp(fixed3(0, 0, 1), o.Normal, _DistortionStrength / 100 / 2))).rgb;
			
			o.Alpha = fresnel;
			o.Alpha *= _Transparency * tex2D(_MaskMap, IN.uv_MaskMap).b * lerp(1, max(0, 1-bumpNormal.z), _BumpUpInfluence);
		}
		
		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "CopyWaterMaterialEditor"
}
