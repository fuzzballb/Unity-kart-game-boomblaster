Shader "FXLab/PostProcessing/Pixelize" {

	Properties {
		_FXScreenTexture ("Screen Texture (FXScreenBufferTexture)", 2D) = "" {}
		_Size ("Size", Float) = 10
	}

	SubShader {
		Blend One Zero
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
				float2 uv = float2((int)(i.uv.x * _Size) / _Size, (int)(i.uv.y * _Size) / _Size);
				fixed3 color = sampleScreen(uv);
				return fixed4(color, 1);
			}
			
			ENDCG
		}
	}

	Fallback off
	CustomEditor "FXMaterialEditor"
}