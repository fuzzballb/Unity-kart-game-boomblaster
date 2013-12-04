Shader "FXLab/Display Texture" {
    Properties {
		_FXScreenTexture ("Main Texture (FXScreenBufferTexture)", 2D) = "" {}
	}
	SubShader {
		Blend One Zero  
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Opaque"}
						
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 
			#pragma target 3.0
			#pragma glsl
			#include "UnityCG.cginc"
			#include "FXLab.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR0;
			};
									
			v2f vert (appdata v) {
				v2f o;
				o.pos = mul( UNITY_MATRIX_MVP, v.vertex );
				o.uv = v.texcoord.xy;
				o.color = v.color;
				return o;
			}
					
			float4 frag( v2f o ) : COLOR
			{
				fixed3 color = sampleScreen(o.uv);              
				return fixed4(color, 1);
			}
			ENDCG
		}
	}
    Fallback off
	CustomEditor "FXMaterialEditor"
}