Shader "FXLab/PostProcessing/Screen Space Ambient Occlusion" {
    Properties {
		_FXDepthTexture ("Depth Texture (FXDepthTexture)", 2D) = "" {}
		_SampleRange ("Sample Range", Range(0, 10)) = 3
		_Area ("Area", Range(0, 20)) = 8
		_Falloff ("Falloff", Float) = 0.87
		_Strength ("Strength", Range(0, 2)) = 1
		_Bias ("Bias", Range(0, 1)) = 0.0
		_NoiseFactor ("Noise Factor", Range(1, 50)) = 30
		_NoiseTex ("Noise Texture", 2D) = "gray" {}
	}
	SubShader {
		//Blend One Zero
		Blend Zero SrcColor
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Opaque"}
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
			
			#define FORCE_TEX2DLOD
			#include "../FXLab.cginc"
			
			float _SampleRange;
			float _Area;
			float _Falloff;
			float _Strength;
			float _NoiseFactor;
			float _Bias;
			sampler2D _NoiseTex;

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
				o.uv = v.texcoord.xy;
				return o;
			}
			
			float3 calculateNormal(float depth, float2 texcoords)
			{
				depth *= _ProjectionParams.z;

				float2 offset1 = float2(0.0,_FXDepthTexture_TexelSize.y);
				float2 offset2 = float2(_FXDepthTexture_TexelSize.x,0.0);

				float depth1 = sampleFloat(_FXDepthTexture, texcoords + offset1) * _ProjectionParams.z;
				float depth2 = sampleFloat(_FXDepthTexture, texcoords + offset2) * _ProjectionParams.z;

				float3 p1 = float3(offset1, depth1 - depth);
				float3 p2 = float3(offset2, depth2 - depth);

				float3 normal = cross(p1, p2);
				normal.z = -normal.z;

				return normalize(normal);
			}
			
			#define SAMPLES 16
					
			fixed4 frag( v2f o ) : COLOR
			{
				float3 sample_sphere[SAMPLES] = {
					float3( 0.5381, 0.1856,-0.4319), float3( 0.1379, 0.2486, 0.4430),
					float3( 0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
					float3( 0.0689,-0.1598,-0.8547), float3( 0.0560, 0.0069,-0.1843),
					float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924,-0.0344),
					float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
					float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
					float3( 0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
					float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
				};
				
				float3 random = normalize( tex2D(_NoiseTex, o.uv * _NoiseFactor).rgb );
  
				float depth = sampleFloat(_FXDepthTexture, o.uv);

				float3 position = float3(o.uv, depth * _ProjectionParams.z);
				float3 normal = calculateNormal(depth, o.uv);
				float radius_depth = _SampleRange / 100 / 100 / depth;
				float occlusion = 0.0;
				for(int i=0; i < SAMPLES; i++)
				{
					float3 ray = radius_depth * reflect(sample_sphere[i], random);
					float3 hemi_ray = position + sign(dot(ray,normal)) * ray;

					float occ_depth = sampleFloat(_FXDepthTexture, saturate(hemi_ray.xy));
					float difference = (depth - occ_depth);

					occlusion += step(_Falloff / 100 / 100, difference) * (1.0-smoothstep(_Falloff / 100 / 100, _Area / 100 / 100, difference));
				}

				float s = _Strength;
				float ao = 1.0 - s * occlusion * (1.0 / SAMPLES);

				fixed3 color = saturate(ao + _Bias);

				return fixed4(color, 1);				
			}
			ENDCG
		}
	}
    Fallback off
	CustomEditor "FXMaterialEditor"
}