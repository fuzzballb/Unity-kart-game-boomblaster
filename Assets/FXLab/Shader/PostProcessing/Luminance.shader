Shader "FXLab/PostProcessing/Luminance" {

	Properties {
		_FXScreenTexture ("Screen Texture (FXScreenBufferTexture)", 2D) = "" {}
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
				fixed lum = Luminance( sampleScreen( i.uv ) );
				return fixed4(fixed3(lum), 1);
			}
			
			ENDCG
		}
	}

	Fallback off
	CustomEditor "FXMaterialEditor"
}