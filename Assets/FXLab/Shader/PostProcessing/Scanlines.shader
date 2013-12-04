Shader "FXLab/PostProcessing/Scanlines" {
    Properties {
		_LineMap ("Scanline Texture", 2D) = "white" {}
		_LineStrength ("Scanline Strength", Range(0, 1.0)) = 0
		_ScrollSpeed ("ScanLine Scroll Strength", Range(0, 0.5)) = 0
		_LineIllumination("Scanline Illumination", Range(1, 1.5)) = 1
	}
	SubShader {
		Blend DstColor Zero
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Lighting Off
		Cull back
		Fog { Mode Off }
		ZWrite Off
				
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _LineMap;
			float4 _LineMap_ST;
			float _LineStrength;
			float _ScrollSpeed;
			float _LineIllumination;
				
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
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _LineMap);	
				return o;
			}
				
			float4 frag( v2f o ) : COLOR
			{
				float2 uv_offset = float2(o.uv.x, o.uv.y + _Time.w * _ScrollSpeed);
				fixed3 lineColor = tex2D(_LineMap, o.uv + uv_offset);
				lineColor.rgb = clamp(lineColor.rgb*_LineIllumination, float3(0), float3(1));
				return fixed4(lerp(fixed3(1), lineColor.rgb, _LineStrength), 1);
			}
			ENDCG
        }
    }
    Fallback off
}
