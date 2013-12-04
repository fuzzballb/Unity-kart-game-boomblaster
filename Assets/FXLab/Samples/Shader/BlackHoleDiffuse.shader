Shader "Custom/BlackHoleDiffuse" {
    Properties {
		_MainTex ("MainTex", 2D) = "white" {}
	}
	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha  
		Tags { "Queue"="Transparent+200" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Lighting Off
		Cull back
		Fog { Mode Off }
		ZWrite Off
				
		Pass {
			CGPROGRAM
			#pragma alpha
			#pragma vertex vert
			#pragma fragment frag 
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			
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
				o.uv = v.texcoord;

				return o;
			}
				
			float4 frag( v2f o ) : COLOR
			{
				return fixed4(0, 0, 0, tex2D(_MainTex, o.uv).b);
			}
			ENDCG
        }
    }
    Fallback off
}