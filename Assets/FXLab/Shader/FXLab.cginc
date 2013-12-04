#ifndef FXLAB_CG_INCLUDED
#define FXLAB_CG_INCLUDED

#ifndef RADIAL_BLUR_STEPS
	#define RADIAL_BLUR_STEPS 16
#endif

#if defined(UNITY_COMPILER_HLSL2GLSL) || defined(FORCE_TEX2DLOD)
	#define fxTex2d(s, t) tex2Dlod(s, float4(t, 0, 0))
#else
	#define fxTex2d(s, t) tex2D(s, (t))
#endif

#if defined(FASTER_RENDERTEXTURE_ACCESS)
	#define fxClamp(uv, area, texelSize) clamp(uv, area.xy, area.zw)
#else
	#define fxClamp(uv, area, texelSize) clamp(uv, area.xy + texelSize*0.5, area.zw - texelSize*0.5)
#endif
#define MAX_FLOAT 10000

////////////////////////////////////////
// Default/Global textures
////////////////////////////////////////

sampler2D _FXScreenTexture;
float4 _FXScreenTexture_Area;
float4 _FXScreenTexture_FastArea;
float2 _FXScreenTexture_TexelSize;

sampler2D _FXDepthTexture;
float4 _FXDepthTexture_Area;
float4 _FXDepthTexture_FastArea;
float2 _FXDepthTexture_TexelSize;

sampler2D _FXHeightTexture;
float4 _FXHeightTexture_Area;
float4 _FXHeightTexture_FastArea;
float2 _FXHeightTexture_TexelSize;

sampler2D _FXReflectionTexture;
float4 _FXReflectionTexture_Area;
float4 _FXReflectionTexture_FastArea;
float2 _FXReflectionTexture_TexelSize;

sampler2D _FXWorldNormalTexture;
float4 _FXWorldNormalTexture_Area;
float4 _FXWorldNormalTexture_FastArea;
float2 _FXWorldNormalTexture_TexelSize;

////////////////////////////////////////
// Sample functions
////////////////////////////////////////
float sampleFloatFunc(sampler2D tex, float2 uv, float4 area, float2 texelSize)
{
	uv = fxClamp(area.xy + uv * (area.zw - area.xy), area, texelSize);
	return DecodeFloatRGBA(fxTex2d(tex, uv));
}

float sampleFloatMaxFunc(sampler2D tex, float2 uv, float4 area, float2 texelSize)
{
	texelSize *= 0.5;
	float2 size = area.zw - area.xy;
	uv = area.xy + uv * size;
	
	float result = 0;
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1, +1), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0, +1), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1, +1), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1,  0), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0,  0), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1,  0), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1, -1), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0, -1), area, texelSize))));
	result = max(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1, -1), area, texelSize))));
	
	return result;
}

float sampleFloatMinFunc(sampler2D tex, float2 uv, float4 area, float2 texelSize)
{
	texelSize *= .5;
	float2 size = area.zw - area.xy;
	uv = area.xy + uv * size;
	
	float result = MAX_FLOAT;
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1, +1), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0, +1), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1, +1), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1,  0), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0,  0), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1,  0), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1, -1), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0, -1), area, texelSize))));
	result = min(result, DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1, -1), area, texelSize))));
	
	return result;
}

float sampleFloatAverageFunc(sampler2D tex, float2 uv, float4 area, float2 texelSize)
{
	texelSize *= 1.5;
	float2 size = area.zw - area.xy;
	uv = area.xy + uv * size;
	
	float result = 0;
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1, +1), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0, +1), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1, +1), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1,  0), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0,  0), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1,  0), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(+1, -1), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2( 0, -1), area, texelSize)));
	result += DecodeFloatRGBA(fxTex2d(tex, fxClamp(uv + texelSize * float2(-1, -1), area, texelSize)));
	
	return result / 9;
}

#if defined(FASTER_RENDERTEXTURE_ACCESS)
	#define sampleFloat(tex, uv) sampleFloatFunc(tex, uv, tex##_FastArea, tex##_TexelSize)
	#define sampleFloatMax(tex, uv) sampleFloatMaxFunc(tex, uv, tex##_FastArea, tex##_TexelSize)
	#define sampleFloatMin(tex, uv) sampleFloatMinFunc(tex, uv, tex##_FastArea, tex##_TexelSize)
	#define sampleFloatAverage(tex, uv) sampleFloatAverageFunc(tex, uv, tex##_FastArea, tex##_TexelSize)
#else
	#define sampleFloat(tex, uv) sampleFloatFunc(tex, uv, tex##_Area, tex##_TexelSize)
	#define sampleFloatMax(tex, uv) sampleFloatMaxFunc(tex, uv, tex##_Area, tex##_TexelSize)
	#define sampleFloatMin(tex, uv) sampleFloatMinFunc(tex, uv, tex##_Area, tex##_TexelSize)
	#define sampleFloatAverage(tex, uv) sampleFloatAverageFunc(tex, uv, tex##_Area, tex##_TexelSize)

#endif

fixed3 sampleColorFunc(sampler2D tex, float2 uv, float4 area, float2 texelSize)
{
	uv = fxClamp(area.xy + uv * (area.zw - area.xy), area, texelSize);
	return fxTex2d(tex, uv).xyz;
}

float3 sampleColorBlurredFunc(sampler2D tex, float2 uv, float4 area, float2 texelSize, float radius)
{
	float2 size = area.zw - area.xy;
	uv = area.xy + uv * size;
	
	size *= radius;
	
	float3 result = 0;
	for (int i = 0; i < RADIAL_BLUR_STEPS; ++i)
	{
		half angle = 3.141 * 2.0 * ((i+1.0) / RADIAL_BLUR_STEPS);
		
		float2 fixedUv = fxClamp(uv + half2(sin(angle), cos(angle)) * size, area, texelSize);
		
		result += fxTex2d(tex, fixedUv).xyz;
	}
	return result / RADIAL_BLUR_STEPS;
}

#if defined(FASTER_RENDERTEXTURE_ACCESS)
	#define sampleColor(tex, uv) sampleColorFunc(tex, uv, tex##_FastArea, tex##_TexelSize)
	#define sampleColorBlurred(tex, uv, radius) sampleColorBlurredFunc(tex, uv, tex##_FastArea, tex##_TexelSize, radius)
#else
	#define sampleColor(tex, uv) sampleColorFunc(tex, uv, tex##_Area, tex##_TexelSize)
	#define sampleColorBlurred(tex, uv, radius) sampleColorBlurredFunc(tex, uv, tex##_Area, tex##_TexelSize, radius)
#endif
////////////////////////////////////////
// Helper
////////////////////////////////////////

#if defined(FASTER_RENDERTEXTURE_ACCESS)
	#define hasTexture(tex) (tex##_FastArea.z > 0)
#else
	#define hasTexture(tex) (tex##_Area.z > 0)
#endif

float4 _ReflectionPlaneEquation;
float4 _HeightPlaneEquation;

#if defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD) || defined(UNITY_PASS_PREPASSBASE) || defined(UNITY_PASS_PREPASSFINAL) || defined(UNITY_PASS_SHADOWCASTER) || defined(UNITY_PASS_SHADOWCOLLECTOR)
	#define IS_SURFACE_SHADER
#endif

#ifdef IS_SURFACE_SHADER
	float2 calcScreenUv(float4 screenPos)
	{
		return screenPos.xy / screenPos.w;
	}
#else
	float2 calcScreenUv(float4 screenPos)
	{
		float2 screenUv = screenPos.xy;
		screenUv.xy /= screenPos.w;
		screenUv.xy = 0.5*(screenUv.xy+1.0);

		return screenUv.xy;
	}
#endif

float fresnelTermR0(float3 normal, float3 eyeVec, float refractionStrength, float fresnelFactor, float fresnelBias, float r0)
{
	float angle = 1.0f - saturate(dot(normal, eyeVec));
	float fresnel = angle * angle;
	fresnel = fresnel * fresnel;
	fresnel = fresnel * angle;
	return saturate(saturate(fresnelFactor * saturate(fresnel * (1.0f - r0) + r0 - refractionStrength)) + fresnelBias);
}

float fresnelTerm(float3 normal, float3 eyeVec, float refractionStrength, float fresnelFactor, float fresnelBias)
{
	return fresnelTermR0(normal, eyeVec, refractionStrength, fresnelFactor, fresnelBias, 0.02);
}

float DistanceRayReflectionPlane(float3 point, float3 dir, float4 planeEquation)
{
	float b = dot(dir, planeEquation.xyz);
	if (b <= 0)
		return MAX_FLOAT;
		
	float t = (dot(point, planeEquation.xyz) + planeEquation.w) / b;
	return t;
}

////////////////////////////////////////
// Shortcuts
////////////////////////////////////////
#define sampleWorldNormal(uv) normalize(sampleColor(_FXWorldNormalTexture, uv) * 2 - 1)

#define sampleDepth01(uv) sampleFloat(_FXDepthTexture, uv)
#define sampleDepth(uv) (sampleFloat(_FXDepthTexture, uv) * _ProjectionParams.z)
#define sampleDepthMax(uv) (sampleFloatMax(_FXDepthTexture, uv) * _ProjectionParams.z)
#define sampleDepthMin(uv) (sampleFloatMin(_FXDepthTexture, uv) * _ProjectionParams.z)
#define sampleDepthAverage(uv) (sampleFloatAverage(_FXDepthTexture, uv) * _ProjectionParams.z)
#define hasDepthTexture hasTexture(_FXDepthTexture)

#define sampleHeight(uv) (sampleFloat(_FXHeightTexture, uv) * MAX_FLOAT)
#define sampleHeightMax(uv) (sampleFloatMax(_FXHeightTexture, uv) * MAX_FLOAT)
#define sampleHeightMin(uv) (sampleFloatMin(_FXHeightTexture, uv) * MAX_FLOAT)
#define sampleHeightAverage(uv) (sampleFloatAverage(_FXHeightTexture, uv) * MAX_FLOAT)
#define hasHeightTexture hasTexture(_FXHeightTexture)

#define sampleScreen(uv) sampleColor(_FXScreenTexture, uv)
#define sampleScreenBlurred(uv, radius) sampleColorBlurred(_FXScreenTexture, uv, radius)
#define sampleScreenDispersion(uv, offset, dispersion) fixed3(sampleColor(_FXScreenTexture, uv + offset * dispersion.r).r, \
															  sampleColor(_FXScreenTexture, uv + offset * dispersion.g).g, \
															  sampleColor(_FXScreenTexture, uv + offset * dispersion.b).b)
#define sampleScreenDispersionBlurred(uv, offset, dispersion, radius) fixed3(sampleColorBlurred(_FXScreenTexture, uv + offset * dispersion.r, radius).r, \
																			 sampleColorBlurred(_FXScreenTexture, uv + offset * dispersion.g, radius).g, \
																			 sampleColorBlurred(_FXScreenTexture, uv + offset * dispersion.b, radius).b)
#define hasScreenTexture hasTexture(_FXScreenTexture)

#define sampleReflection(uv) sampleColor(_FXReflectionTexture, uv)
#define sampleReflectionBlurred(uv, radius) sampleColorBlurred(_FXReflectionTexture, uv, radius)
#define sampleReflectionDispersion(uv, offset, dispersion) fixed3(sampleColor(_FXReflectionTexture, uv + offset * dispersion.r).r, \
																  sampleColor(_FXReflectionTexture, uv + offset * dispersion.g).g, \
																  sampleColor(_FXReflectionTexture, uv + offset * dispersion.b).b)
#define sampleReflectionDispersionBlurred(uv, offset, dispersion, radius) fixed3(sampleColorBlurred(_FXReflectionTexture, uv + offset * dispersion.r, radius).r, \
																				 sampleColorBlurred(_FXReflectionTexture, uv + offset * dispersion.g, radius).g, \
																				 sampleColorBlurred(_FXReflectionTexture, uv + offset * dispersion.b, radius).b)
#define hasReflectionTexture hasTexture(_FXReflectionTexture)

#endif