Shader "RAIN/NavMeshGridShader"
{
    SubShader
    {
    	Tags { "RenderType" = "Opaque" }
    	AlphaTest Greater 0.5
		Offset -1, -1
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
						
			float _maxSlopeCos; 
			float _cellSize;
			float3 _centerPoint;
			float4 _ambientLight;
		
			struct vert_out
			{
				float4 position : POSITION;
				float3 normal : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};
			
			vert_out vert(appdata_base v)
			{
				vert_out tOut;
				tOut.position = mul(UNITY_MATRIX_MVP, v.vertex);
				tOut.normal = normalize(v.normal);
				tOut.worldPosition = v.vertex;
				
				return tOut;
			}
	
			float4 frag(vert_out f) : COLOR
			{
				float3 tLight = normalize(float3(10, 10, 10));
				
				float4 tAmbient = _ambientLight;
				float4 tDiffuse = clamp(float4(1, 1, 1, 1) * max(dot(f.normal, tLight), 0), 0, 1);
				
				float4 tColor = float4(0.5, 0.5, 0.5, 1) * (tDiffuse + tAmbient);
				
				float2 tGridLines = frac((f.worldPosition - _centerPoint).xz / _cellSize);
            	if (f.normal.y > (_maxSlopeCos - 0.001))
            	{
            		if (tGridLines.x < 0.2 || tGridLines.x > 0.8 || tGridLines.y < 0.2 || tGridLines.y > 0.8)
						tColor.a = 0;
					else
						tColor.a = 1;
				}
				else
					tColor.a = 0;
				
				return tColor;
			}
			
			ENDCG
		}
    }
    FallBack "Diffuse"
}
