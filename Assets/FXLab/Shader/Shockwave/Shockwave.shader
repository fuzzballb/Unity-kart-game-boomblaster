Shader "FXLab/Shockwave/Shockwave" {
    Properties {
		_FXScreenTexture ("Screen Texture for Refraction (FXScreenBufferTexture)", 2D) = "" {}
		_DistortionMap ("Distortion Map", 2D) = "bump" {}
		_DistortionBumpStrength ("Bump Strength", Range(0, 1)) = 30
		_DistortionStrength ("Distortion Strength", Float) = 1
		_EdgeHardness ("Edge Hardness", Float) = 1
		_EdgePower ("Edge Power", Float) = 2
		_Transparency ("Transparency", Range(0, 1)) = 1
	}
	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha  
		Tags { "Queue"="Transparent+1" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Lighting Off
		Cull Back
		Fog { Mode Off }
		ZWrite Off
						
		Pass {
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag 
				#pragma target 3.0
				#include "UnityCG.cginc"
				#include "../FXLab.cginc"

				sampler2D _DistortionMap;
				float _DistortionStrength;
				float _DistortionBumpStrength;
				fixed _Transparency;
				float _EdgeHardness;
				float _EdgePower;
						
				struct appdata {
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 normal : NORMAL;
					float4 tangent  : TANGENT;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 uv2 : TEXCOORD1;
					float3 tan1 : TEXCOORD2;
					float3 tan2 : TEXCOORD3;
					float3 tan3 : TEXCOORD4;
					float3 viewDir : TEXCOORD5;
				};
										
				v2f vert (appdata v) {
					v2f o;
					o.pos = mul( UNITY_MATRIX_MVP, v.vertex );
					o.uv = float4( v.texcoord.xy, 0, 0 );
					o.uv2 = o.pos;
													
					float3 binormal = cross( v.normal, v.tangent.xyz ) * v.tangent.w;
					float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal );
													
					o.tan1 = mul(rotation, ((float3x3)_Object2World)[0].xyz)*unity_Scale.w;
					o.tan2 = mul(rotation, ((float3x3)_Object2World)[1].xyz)*unity_Scale.w;
					o.tan3 = mul(rotation, ((float3x3)_Object2World)[2].xyz)*unity_Scale.w;
													
					o.viewDir = WorldSpaceViewDir(v.vertex);
													
					return o;
				}
						
				float4 frag( v2f o ) : COLOR
				{
					float2 screenUv = calcScreenUv(o.uv2);
													
					float3 normal = UnpackNormal(tex2D(_DistortionMap, o.uv));
					normal = lerp(fixed3(0,0,1), normal, _DistortionBumpStrength);
					normal = mul(UNITY_MATRIX_V, float4(fixed3(dot(normalize(o.tan1), normal), dot(normalize(o.tan2), normal), dot(normalize(o.tan3), normal)), 0)).xyz;
													
					float strength = saturate(saturate(1 - pow(saturate(_EdgeHardness * dot(normal, float3(0,0,1))), _EdgePower)) * 1);
					float2 screenUVOffset = (-normal.xy * _DistortionStrength / 100) * strength;
													
					fixed3 refr = sampleScreen(screenUv + screenUVOffset * saturate((1-_Transparency) * 2));
					
					return fixed4(refr, _Transparency);
				}
				ENDCG
			}
    }
    Fallback off
	CustomEditor "FXMaterialEditor"
}
