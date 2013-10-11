Shader "RAIN/NavMeshContourShader"
{
    SubShader
    {
    	Tags { "Queue" = "Geometry" }
		Pass
		{
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
	
			float _height;
			float4 _colorInside;
			float4 _colorOutside;
			float4 _ambientLight;
				
			struct vert_out
			{
				float4 position : POSITION;
				float3 normal : TEXCOORD0;
			};
			
			vert_out vert(appdata_base v)
			{
				vert_out tOut;
				tOut.position = mul(UNITY_MATRIX_MVP, v.vertex + float4(0, _height, 0, 0));
				tOut.normal = normalize(v.normal);
				
				return tOut;
			}
	
			float4 frag(vert_out f) : COLOR
			{
				float3 tLight = normalize(float3(10, 10, 10));
				
				float4 tAmbient = _ambientLight;
				float4 tDiffuse = clamp(float4(1, 1, 1, 1) * max(dot(f.normal, tLight), 0), 0, 1);
				
				return _colorInside * (tDiffuse + tAmbient);
			}
				
			ENDCG
		}
    }
    FallBack "Diffuse"
}
