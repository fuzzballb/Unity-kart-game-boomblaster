Shader "FXLab/PostProcessing/Vignette" {

	Properties {
		_FXScreenTexture ("Screen Texture (FXScreenBufferTexture)", 2D) = "" {}
		_Size ("Size", Float) = 1.5
		_Bias ("Bias", Float) = -0.5
		_Brightness ("Brightness", Range(0, 1)) = 0.5
		_Saturation ("Saturation", Range(0, 1)) = 0
	}

	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {
			
			ZTest Always
			Cull Off
			ZWrite Off
			
			Fog { Mode off }

			CGPROGRAM

			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 

			#include "UnityCG.cginc"
			#include "../FXLab.cginc"
			
			float _Size;
			float _Bias;
			float _Brightness;
			float _Saturation;
			
			struct v2f {
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct appdata {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			v2f vert( appdata v ) {
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord.xy;
				return o;
			}
			
			fixed4 frag (v2f i) : COLOR
			{		 	 
				float2 dir = i.uv * 2 - 1;
				fixed strength = saturate(saturate(length(dir) + _Bias) * _Size);
				
				fixed3 color = sampleScreen(i.uv) * _Brightness;
				fixed luminance = Luminance(color);
				color = lerp(fixed3(luminance), color, _Saturation);
				
				return fixed4(color, strength);
			}
			
			ENDCG
		}
	}

	Fallback off
	CustomEditor "FXMaterialEditor"
}