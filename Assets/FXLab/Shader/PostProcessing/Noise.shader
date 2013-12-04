Shader "FXLab/PostProcessing/Noise" {
    Properties {
		_NoiseMap ("Noise Texture", 2D) = "white" {}
		_NoiseStrength ("Noise Strength", Range(0, 1.0)) = 0
		_NoiseIllumination("Noise Illumination", Range(1, 1.5)) = 1
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

			sampler2D _NoiseMap;
			float4 _NoiseMap_ST;
			float _NoiseStrength;
			float _NoiseIllumination;
				
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
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _NoiseMap);
				return o;
			}
				
			float4 frag( v2f o ) : COLOR
			{
				fixed3 noiseColor = tex2D(_NoiseMap, float2(o.uv.x + _Time.x * 234.2345, o.uv.y + _Time.x * 234.2345)).rgb;
				noiseColor.rgb = clamp(noiseColor.rgb*_NoiseIllumination, float3(0), float3(1));
				return fixed4(lerp(fixed3(1), noiseColor.rgb, _NoiseStrength), 1);
			}
			ENDCG
        }
    }
    Fallback off
}