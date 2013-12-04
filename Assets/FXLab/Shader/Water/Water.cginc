#ifndef WATER_CG_INCLUDED
#define WATER_CG_INCLUDED

#include "../FXLab.cginc"

#define FLOW_MAP_TEXTURE() half2 flowMap = tex2D( _FlowMap, IN.uv_FlowMap ).rg; \
				 flowMap.rg = flowMap.rg * 2.0f - 1.0f; \

#define FLOW_MAP(scrollX, scrollY) half2 flowMap = half2(scrollX, scrollY);

#define COLOR_EXTINCTION(viewDepthColor, heightColor, maxDepth, maxHeight, color) lerp(lerp(color, viewDepthColor, saturate(depth / maxDepth)), heightColor, saturate(height / maxHeight));

#endif