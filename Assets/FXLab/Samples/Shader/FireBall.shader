Shader "Custom/FireBall" {
    Properties {
		_DistortionMap ("Distortion Map", 2D) = "bump" {}
		_MainTex ("MainTex", 2D) = "white" {}
		_MaskTex ("MaskTex", 2D) = "white" {}
		_Strength ("Strength", Float) = 0.1
	}
	SubShader {
		Blend One One  
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
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

			sampler2D _DistortionMap;
			sampler2D _MainTex;
			sampler2D _MaskTex;
			half _Strength;
			
			float4 _DistortionMap_ST;
				
			struct appdata {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};
						
			v2f vert (appdata v) {
				v2f o;
				o.pos = mul( UNITY_MATRIX_MVP, v.vertex );
				o.uv = v.texcoord;
				o.uv2 = TRANSFORM_TEX(v.texcoord, _DistortionMap);

				return o;
			}
			
			float2 rotate(float2 what, float angle)
			{
				what -= 0.5;

                float s = sin(angle);
                float c = cos(angle);

                float2x2 rotationMatrix = float2x2(c, -s, s, c);

                rotationMatrix *=0.5;
                rotationMatrix +=0.5;
                rotationMatrix = rotationMatrix * 2-1;

                what = mul (what, rotationMatrix);

                what += 0.5; 
				
				return what;
			}
				
			float4 frag( v2f o ) : COLOR
			{

				float2 uv = rotate(o.uv2, _Time.x);
				float3 dudv = UnpackNormal(tex2D(_DistortionMap, uv + float2(_Time.x, _Time.x)));
				dudv += UnpackNormal(tex2D(_DistortionMap, uv + float2(-_Time.x, _Time.x)));
				dudv += UnpackNormal(tex2D(_DistortionMap, uv + float2(_Time.x, -_Time.x)));
				dudv += UnpackNormal(tex2D(_DistortionMap, uv + float2(-_Time.x, -_Time.x)));
				
				fixed4 color = tex2D(_MainTex, o.uv + normalize(dudv).xy * _Strength);
				
				uv = rotate(o.uv2, _Time.x + 3.141);
				dudv = UnpackNormal(tex2D(_DistortionMap, uv + float2(_Time.x, _Time.x)));
				dudv += UnpackNormal(tex2D(_DistortionMap, uv + float2(-_Time.x, _Time.x)));
				dudv += UnpackNormal(tex2D(_DistortionMap, uv + float2(_Time.x, -_Time.x)));
				dudv += UnpackNormal(tex2D(_DistortionMap, uv + float2(-_Time.x, -_Time.x)));
				
				color += tex2D(_MainTex, o.uv + normalize(dudv).xy * _Strength);
				
				return color * tex2D(_MaskTex, o.uv);
			}
			ENDCG
        }
    }
    Fallback off
}