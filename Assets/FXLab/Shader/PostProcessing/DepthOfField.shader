Shader "FXLab/PostProcessing/Depth Of Field" {
    Properties {
		_FXScreenTexture ("Screen Texture (FXScreenBufferTexture)", 2D) = "" {}
		_FXDepthTexture ("Depth Texture (FXDepthTexture)", 2D) = "" {}
		_FocalDistance ("Focal Distance", Float) = 1
		_FocalRange ("Focal Range", Float) = 10
		_FocalFalloff ("Focal Falloff", Float) = 10
		_BlurRange ("Blur Range", Float) = 20
	}
	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha  
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Opaque"}
		Lighting Off
		Cull Off
		Fog { Mode Off }
		ZWrite Off
						
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 
			#pragma target 3.0
			#include "UnityCG.cginc"
			
			#define SCREENGRAB_BLUR_STEPS 8
			#include "../FXLab.cginc"
			
			float _FocalDistance;
			float _FocalRange;
			float _BlurRange;
			float _FocalFalloff;

			struct appdata {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
									
			v2f vert (appdata v) {
				v2f o;
				o.pos = mul( UNITY_MATRIX_MVP, v.vertex );
				o.uv = v.texcoord.xy;
				return o;
			}
			
			float4 sampleBlurredScreenDepth(half2 screenUv, half radius)
			{
				float4 result = 0;
				for (int i = 0; i < SCREENGRAB_BLUR_STEPS; ++i)
				{
					half angle = 3.141 * 2.0 * ((i+1.0) / SCREENGRAB_BLUR_STEPS);
					
					float depth = sampleDepth(screenUv + half2(sin(angle), cos(angle)) * radius);
					float strength = saturate(max(0, abs(depth - _FocalDistance) - _FocalRange) / _FocalFalloff);
					
					result.xyz += sampleScreen(screenUv + half2(sin(angle), cos(angle)) * radius * strength);
					result.w += strength;
				}
				return result / SCREENGRAB_BLUR_STEPS;
			}
					
			fixed4 frag( v2f o ) : COLOR
			{
				float4 color = sampleBlurredScreenDepth(o.uv, _BlurRange / 1000);
				color += sampleBlurredScreenDepth(o.uv, _BlurRange / 1000 / 2);
				color *= 0.5;				
				
				return color;
			}
			ENDCG
		}
	}
    Fallback off
	CustomEditor "FXMaterialEditor"
}
