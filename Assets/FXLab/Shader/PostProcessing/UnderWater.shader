Shader "FXLab/PostProcessing/Under Water" {
    Properties {
		_FXScreenTexture ("Screen Texture (FXScreenBufferTexture)", 2D) = "" {}
		_FXDepthTexture ("Depth Texture (FXDepthTexture)", 2D) = "" {}
		_Color ("Tint Color", Color) = (1,1,1,1)
		_DistortionMap ("Distortion Map", 2D) = "bump" {}
		_DistortionStrength ("Strength", Float) = 10
		_BlurRange ("Blur Range", Float) = 10
		_FocusDistance ("Focus Distance", Float) = 10
		_FocalFalloff ("Focus Falloff", Float) = 10
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
			#pragma multi_compile FXPOSTPROCESS_GRABTOTEXTURE_ON FXPOSTPROCESS_GRABTOTEXTURE_OFF
			#pragma vertex vert
			#pragma fragment frag 
			#pragma target 3.0
			#include "UnityCG.cginc"
			
			#define SCREENGRAB_BLUR_STEPS 8
			#include "../FXLab.cginc"
			
			fixed3 _Color;
			float _BlurRange;
			float _FocusDistance;
			float _FocalFalloff;
			
			sampler2D _DistortionMap;
			float4 _DistortionMap_ST;
			float _DistortionStrength;
			
			float3 _CameraRotation;

			struct appdata {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 screenPos : TEXCOORD2;
			};
									
			v2f vert (appdata v) {
				v2f o;
				o.pos = mul( UNITY_MATRIX_MVP, v.vertex );
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _DistortionMap);
				o.screenPos = v.texcoord.xy;
				return o;
			}
			
			float4 sampleBlurredScreenDepth(half2 screenUv, half2 offset, half radius)
			{
				float4 result = 0;
				for (int i = 0; i < SCREENGRAB_BLUR_STEPS; ++i)
				{
					half angle = 3.141 * 2.0 * ((i+1.0) / SCREENGRAB_BLUR_STEPS);
					
					float depth = sampleDepth(screenUv + offset + half2(sin(angle), cos(angle)) * radius);

					float strength = saturate(max(0, depth - _FocusDistance) / _FocalFalloff);
					result.rgb += sampleScreen(screenUv + offset + half2(sin(angle), cos(angle)) * radius * strength);
					result.w += strength;
				}
				return result / SCREENGRAB_BLUR_STEPS;
			}
					
			fixed4 frag( v2f o ) : COLOR
			{
				
				float2 uv0 = float2(_CameraRotation.y * 3.141 / 360, _CameraRotation.x * 3.141 / 360);
				float2 uv1 = - uv0 + _Time.x;
  
				half3 distortion0 = UnpackNormal(tex2D(_DistortionMap, uv0 + o.uv)).rgb;
				half3 distortion1 = UnpackNormal(tex2D(_DistortionMap, uv1 + o.uv)).rgb;
				half3 distortion = lerp(distortion0, distortion1, 0.5);				
				
				float4 color = sampleBlurredScreenDepth(o.screenPos, distortion.rg * _DistortionStrength / 100, _BlurRange / 1000);
				color += sampleBlurredScreenDepth(o.screenPos, distortion.rg * _DistortionStrength / 100, _BlurRange / 2000);
				color *= 0.5;				
				
				return fixed4(color.rgb * _Color, 1);
			}
			ENDCG
		}
	}
    Fallback off
	CustomEditor "FXMaterialEditor"
}
