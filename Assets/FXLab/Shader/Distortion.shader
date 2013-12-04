Shader "FXLab/Distortion" {
    Properties {
		_FXScreenTexture ("Screen Texture for Refraction (FXScreenBufferTexture)", 2D) = "" {}
		_DistortionMap ("Distortion Map", 2D) = "bump" {}
		_MainTex ("MainTex", 2D) = "white" {}
		_DistortionStrength ("Strength", Float) = 1
		_Transparency ("Transparency", Range(0, 1)) = 1
	}
	SubShader {
		Blend SrcAlpha OneMinusSrcAlpha  
		Tags { "Queue"="Transparent+155" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Lighting Off
		Cull Off
		Fog { Mode Off }
		ZWrite Off
						
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 
			#pragma target 3.0
			#pragma glsl
			#include "UnityCG.cginc"
			#include "FXLab.cginc"

			sampler2D _DistortionMap;
			sampler2D _MainTex;
			
			float4 _DistortionMap_ST;
			float4 _MainTex_ST;
			
			float _DistortionStrength;
			float _Transparency;
					
			struct appdata {
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
				float4 color : COLOR0;
			};
									
			v2f vert (appdata v) {
				v2f o;
				o.pos = mul( UNITY_MATRIX_MVP, v.vertex );
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _DistortionMap);
				o.uv2 = TRANSFORM_TEX(v.texcoord.xy, _MainTex);				
				o.screenPos = o.pos;
				o.color = v.color;
				return o;
			}
					
			float4 frag( v2f o ) : COLOR
			{
				float2 screenUv = calcScreenUv(o.screenPos);
				half3 distortion = UnpackNormal(tex2D(_DistortionMap, o.uv)).rgb;
				fixed3 refr = sampleScreen(screenUv + distortion.rg * _DistortionStrength / 100);              
				
				return fixed4(refr, distortion.b * _Transparency) * tex2D(_MainTex, o.uv2) * o.color;
			}
			ENDCG
		}
	}
    Fallback off
	CustomEditor "FXMaterialEditor"
}