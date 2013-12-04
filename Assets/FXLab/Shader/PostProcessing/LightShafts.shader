Shader "FXLab/PostProcessing/Light Shafts" {
    Properties {
		_FXScreenTexture ("Screen Texture (FXScreenBufferTexture)", 2D) = "" {}
		_Contrast ("Contrast", Float) = 1
		_Brightness ("Brightness", Float) = 1
		_Bias ("Bias", Float) = 0
		_StepSize ("Step Size", Float) = 0.1
		_Steps ("Steps", Float) = 100
	}
	SubShader {
		Blend One One
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Opaque"}
		Lighting Off
		Cull Off
		Fog { Mode Off }
		ZWrite Off
						
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 
			#pragma glsl
			#pragma target 3.0
			
			#include "UnityCG.cginc"
			
			#define FORCE_TEX2DLOD
			#include "../FXLab.cginc"
			
			float _Contrast;
			float _Brightness;
			float _Bias;
			float _StepSize;
			float _Steps;

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
					
			fixed4 frag( v2f o ) : COLOR
			{				
				fixed3 color = 0;
				
				float2 sampledPosition = o.uv;
				float2 spUv = o.uv * 2 - 1;
				int i;
				for (i = 0; i < _Steps; ++i)
				{
					float2 uv = (spUv / (1 + i*_StepSize)) * 0.5 + 0.5;
					float3 tmp = sampleScreen(uv);
					tmp *= (1 - saturate(i / _Steps));
					color += tmp;
				}
				color /= _Steps;
				
				color = saturate(color - _Bias);
				color = saturate(color * _Brightness);
				color = saturate(((color * 0.5 - 0.5) * _Contrast) * 2 + 1);
				
				return fixed4(color, 1);
			}
			ENDCG
		}
	}
    Fallback off
	CustomEditor "FXMaterialEditor"
}
