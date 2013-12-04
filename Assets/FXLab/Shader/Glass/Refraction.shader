
Shader "FXLab/Glass/Refraction" {
	Properties {
		_FXScreenTexture ("Screen Texture for Refraction (FXScreenBufferTexture)", 2D) = "" {}
		_Color ("Refraction Color", Color) = (1, 1, 1, 0.2)
		_ReflectionColor ("Reflection Color", Color) = (1, 1, 1, 1)
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Specular ("Specular", Range (0.0, 2)) = 1
		_Shininess ("Shininess", Range (1, 64)) = 64
		_MainTex ("MainTex", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_BumpStrength ("Bump Strength", Range(0.0, 1)) = 0.5
		_DistortionStrength ("Distortion Strength", Float) = 10
		_ReflectionCube ("Reflection Cubemap", Cube) = "white" {}
		_FresnelNormalStrength ("Fresnel Normal Strength", Range(0.0, 1)) = 0.1
		_Fresnel ("Fresnel", Range (-1.0, 1.0)) = 1
		_FresnelFactor ("Fresnel Factor", Float) = 1
		_FresnelBias ("Fresnel Bias", Float) = 0
		_LightInfluence ("Light Influence", Range (0.0, 1.0)) = 1
	}
	
	SubShader {
		Tags { "Queue"="Geometry" "RenderType" = "Opaque"}
		
		LOD 400
		Cull Back
		Lighting On
		
		CGPROGRAM
		#pragma surface surf WrapSpecular noambient noforwardadd vertex:vert
		
		#pragma target 3.0
		#include "UnityCG.cginc"
		
		#define SCREENGRAB_BLUR_STEPS 20
		#include "../FXLab.cginc"
		
		sampler2D _MainTex;
		sampler2D _BumpMap;
		float4 _MainTex_ST;
		
		half _BumpStrength;
		fixed4 _Color;
		fixed4 _ReflectionColor;
		fixed _Specular;
		float _Shininess;
		
		half _DistortionStrength;
		
		fixed _FresnelNormalStrength;
		fixed _Fresnel;
		
		fixed _LightInfluence;
		
		half4 LightingWrapSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			half3 h = normalize (lightDir + viewDir);
			
			half diff = max (0, dot (s.Normal, lightDir));
			
			float nh = max (0, dot (s.Normal, h));
			float spec = pow (nh, _Shininess)* _Specular;
			
			half4 c;
			c.rgb = s.Albedo + (_LightColor0.rgb * spec * _SpecColor) * (atten * 2);
			c.a = s.Alpha;
			return c;
		}
		
		struct Input
		{
			float4 tex;
			float4 screenPos;
			float4 tan1;
			float3 tan2;
			INTERNAL_DATA
		};
		
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			
			TANGENT_SPACE_ROTATION;
			float3 tan1 = mul(rotation, ((float3x3)_Object2World)[0].xyz)*unity_Scale.w;
			float3 tan2 = mul(rotation, ((float3x3)_Object2World)[1].xyz)*unity_Scale.w;
			float3 tan3 = mul(rotation, ((float3x3)_Object2World)[2].xyz)*unity_Scale.w;
			float2 uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			
			o.tex.xy = uv;
			o.tex.zw = tan1.xy;
			o.tan1.x = tan1.z;
			o.tan1.yzw = tan2;
			
			o.tan2 = tan3;
		}
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			float2 screenUv = calcScreenUv(IN.screenPos);
			
			fixed3 baseNormal = UnpackNormal(tex2D(_BumpMap, IN.tex.xy));
			o.Normal.xyz = lerp(fixed3(0,0,1), baseNormal, _BumpStrength);
			
			float3 normal = mul(UNITY_MATRIX_V, float4(fixed3(dot(fixed3(IN.tex.zw, IN.tan1.x), o.Normal), dot(IN.tan1.yzw, o.Normal), dot(IN.tan2,o.Normal)), 0)).xyz;
			
			float2 screenUVOffset = -normal.xy * _DistortionStrength / 100;
			
			fixed3 refr = _Color.rgb * tex2D(_MainTex, IN.tex.xy);
			refr *= sampleScreen(screenUv + screenUVOffset);
			
			o.Albedo = refr;
			o.Alpha = 1;
		}
		ENDCG
		
		Lighting On
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma surface surf WrapSpecular alpha noambient noforwardadd vertex:vert
		#pragma target 3.0
		
		#include "UnityCG.cginc"
		#include "../FXLab.cginc"
		
		sampler2D _BumpMap;
		
		samplerCUBE _ReflectionCube;
		half _BumpStrength;
		fixed4 _ReflectionColor;
		fixed _Specular;
		float _Shininess;
		
		half _DistortionStrength;
		
		fixed _FresnelNormalStrength;
		fixed _Fresnel;
		half _FresnelFactor;
		fixed _FresnelBias;
		fixed _LightInfluence;
		
		half4 LightingWrapSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
			half3 h = normalize (lightDir + viewDir);
			
			half diff = max (0, dot (s.Normal, lightDir));
			
			float nh = max (0, dot (s.Normal, h));
			float spec = pow (nh, _Shininess)* _Specular;
			
			half4 c;
			c.rgb = (lerp(1, diff * _LightColor0.rgb, _LightInfluence) * s.Albedo + _LightColor0.rgb * spec * _SpecColor) * (atten * 2);
			c.a = s.Alpha;
			return c;
		}
		
		struct Input
		{
			float2 uv_BumpMap;
			float3 worldRefl;
			float3 viewDir;
			INTERNAL_DATA
		};
		
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
		}
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed3 baseNormal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			o.Normal.xyz = lerp(fixed3(0,0,1), baseNormal, _BumpStrength);
			//fixed fresnel = fresnelTerm(normalize(lerp(fixed3(0,0,1), baseNormal, _FresnelNormalStrength)), normalize(IN.viewDir), _Fresnel, _FresnelFactor, _FresnelBias);
			
			fixed fresnel = saturate(pow(1.0 - dot(lerp(fixed3(0,0,1), baseNormal, _FresnelNormalStrength), normalize(IN.viewDir)), _Fresnel * 10) * _FresnelFactor - _FresnelBias);
			
			fixed3 refl = texCUBE(_ReflectionCube, WorldReflectionVector(IN, o.Normal)).rgb * _ReflectionColor.rgb;
			
			o.Albedo = refl;
			o.Alpha = fresnel;
		}
		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "GlassMaterialEditor"
}
