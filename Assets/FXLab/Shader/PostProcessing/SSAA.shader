Shader "FXLab/PostProcessing/SSAA" {

	Properties {
		_FXScreenTexture ("Screen Texture (FXScreenBufferTexture)", 2D) = "" {}
	}

	// very simple & fast AA by Emmanuel Julien

	SubShader {
		Blend One Zero
		Pass {
		
		ZTest Always
		Cull Off
		ZWrite Off
		
		Fog { Mode off }

		CGPROGRAM

		#pragma target 3.0
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest 

		#include "UnityCG.cginc"
		#include "../FXLab.cginc"
		
		#define TEXELSIZE _FXScreenTexture_TexelSize

		struct v2f {
			float4 pos : POSITION;
			float2 uv[5] : TEXCOORD0;
		};

		struct appdata {
			float4 vertex : POSITION;
			float4 texcoord : TEXCOORD0;
		};

		v2f vert( appdata v ) {
			v2f o;
			o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
			
			float2 uv = v.texcoord.xy;
			
			float w = 1.75;
					
			float2 up = float2(0.0, TEXELSIZE.y) * w;
			float2 right = float2(TEXELSIZE.x, 0.0) * w;	
				
			o.uv[0].xy = uv - up;
			o.uv[1].xy = uv - right;
			o.uv[2].xy = uv + right;
			o.uv[3].xy = uv + up;

			o.uv[4].xy = uv;
			
			return o;
		}
		
		half4 frag (v2f i) : COLOR
		{		 	 
			//half3 outColor;
			
			float t = Luminance( sampleScreen( i.uv[0] ).xyz );
			float l = Luminance( sampleScreen( i.uv[1] ).xyz);
			float r = Luminance( sampleScreen( i.uv[2] ).xyz);
			float b = Luminance( sampleScreen( i.uv[3] ).xyz);
		 
			half2 n = half2( -( t - b ), r - l );
			float nl = length( n );
		 
			//if ( nl < (1.0 / 16.0) )
				half3 outColor1 = sampleScreen( i.uv[4] );
			//else
			//{
				n *= TEXELSIZE.xy / nl;
		 
				half3 o = sampleScreen(i.uv[4]);
				half3 t0 = sampleScreen( i.uv[4] + n * 0.5) * 0.9;
				half3 t1 = sampleScreen( i.uv[4] - n * 0.5) * 0.9;
				half3 t2 = sampleScreen( i.uv[4] + n) * 0.75;
				half3 t3 = sampleScreen( i.uv[4] - n) * 0.75;
		 
				half3 outColor2 = (o + t0 + t1 + t2 + t3) / 4.3;
			//}
			
			return half4(nl < (1.0 / 16.0) ? outColor1 : outColor2, 1);
		}
		
		ENDCG
		}
	}

	Fallback off
	CustomEditor "FXMaterialEditor"
}