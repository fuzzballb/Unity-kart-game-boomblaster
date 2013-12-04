Shader "Hidden/FXLab/DisplayFloatTexture"
{
	Properties
	{
		 _MainTex ("Texture", any) = "" {} 
		 _MaxFloat ("Max Float", Float) = 1
	} 

	SubShader {

		Tags { "ForceSupported" = "True" "RenderType"="Overlay" } 
		
		Lighting Off 
		Cull Off 
		ZWrite Off 
		Fog { Mode Off } 
		ZTest Always 
		
		Pass {	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "UnityCG.cginc"
			#include "../../Shader/FXLab.cginc" 

			sampler2D _MainTex;
			float4 _MainTex_Area;
			float2 _MainTex_TexelSize;

			float _MaxFloat;
			
			struct appdata_t {
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color;
				o.texcoord = v.texcoord;
				return o;
			}

			fixed4 frag (v2f i) : COLOR
			{
				
				return fixed4(fixed3(sampleFloat(_MainTex, i.texcoord) * _MaxFloat), 1);
			}
			ENDCG 
		}
	} 	
	}