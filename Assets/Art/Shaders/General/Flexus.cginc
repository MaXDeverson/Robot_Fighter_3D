//Supports Universal 3.4 & SuckDeform

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

inline fixed CalculateZFog(float z, float4 zVector) {
	fixed fogFactor = max((z - zVector.x)/(zVector.y - zVector.x), 0);
	fogFactor += max((z - zVector.z)/(zVector.w - zVector.z), 0);
	return min(fogFactor, 1);
}

inline float SmoothStep01(float x) {
	float t = saturate(x);
    return t * t * (3 - t - t);
}

inline float2 ProjectPointToLine(float2 pta, float2 ptb, float2 pt_from) {
		float b1 = pt_from.x * (pta.x - ptb.x) + pt_from.y * (pta.y - ptb.y);
		float b2 = pta.x * ptb.y - pta.y * ptb.x;
		
		float2 pt_to;
        pt_to.y = (pta.x - ptb.x) * (pta.x - ptb.x) + (pta.y - ptb.y) * (pta.y - ptb.y);
        float det_k = b1 * (pta.x - ptb.x) - b2 * (pta.y - ptb.y);

        pt_to.x = det_k/pt_to.y;
        det_k = (pta.x - ptb.x) * b2 + (pta.y - ptb.y) * b1;
        pt_to.y = det_k/pt_to.y;
		
		return pt_to;
}

inline half3 unpackNormalFromUV(float2 uv) {
	//4 bit X / 4 bit Z
	half3 normal;
	half Z = floor(uv.x*.0625);
	normal.z = (Z-8)*.125;
	normal.x = -(uv.x-Z*16-8)*.125;
	normal.y = uv.y;
	return normal;
	}

#define TRANSFER_TANGENT(o)\
half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);\
half tangentSign = v.tangent.w * unity_WorldTransformParams.w;\
half3 wBitangent = cross(normal, wTangent) * tangentSign;\
o.tspace0 = half3(wTangent.x, wBitangent.x, normal.x);\
o.tspace1 = half3(wTangent.y, wBitangent.y, normal.y);\
o.tspace2 = half3(wTangent.z, wBitangent.z, normal.z);

inline half3 ApplyNormalMap(half3 tspace0, half3 tspace1, half3 tspace2, fixed3 normalmap) {
	//Normal map calculations
	half3 N;
	N.x = dot(tspace0, normalmap);
	N.y = dot(tspace1, normalmap);
	N.z = dot(tspace2, normalmap);
	return N;
}

//UNIVERSAL SHADER
inline float3 SuckDeform(float3 ws_pos, float3 origin, float scale, float3 suckPoint, half3 suckVector, fixed suck) {
	
	float DO = distance(origin, suckPoint);
	float DV = distance(ws_pos, suckPoint);
	fixed DR = DV / DO;
	
	fixed F = 0;//(DO - DV) / DO * 5  + suck;
	
	return lerp(ws_pos, ws_pos + float3(0,1,0), F);
}


#define UNIVERSAL_APPDATA \
float4 vertex : POSITION;\
half3 normal : NORMAL;\
half2 uv : TEXCOORD0;

#define UNIVERSAL_V2F \
float4 pos : SV_POSITION;\
half3 V : COLOR0;\
half3 N : NORMAL;\
fixed NdotL : COLOR1;\
fixed3 ambient : COLOR2;\
fixed specularMultiplier : COLOR3;\
SHADOW_COORDS(1)

#define UNIVERSAL_VARS \
fixed4 _Color;\
fixed4 _GlowColor;\
fixed _SpecularMultiplier;\
fixed _Roughness;\
fixed _GlowPower;

#define UNIVERSAL_VS \
o.pos = UnityObjectToClipPos(v.vertex);\
o.V = -normalize(UnityWorldSpaceViewDir(ws_pos));\
o.ambient = ShadeSH9(half4(o.N, 1));\
o.NdotL = max(dot(o.N, _WorldSpaceLightPos0.xyz), 0);\
o.specularMultiplier = _SpecularMultiplier * (1 / _Roughness - 1);\
TRANSFER_SHADOW(o)

#define UNIVERSAL_CALCULATE_SHADOW fixed shadow = SHADOW_ATTENUATION(i) > 0.99;
#define UNIVERSAL_CALCULATE_LAMBERT fixed lambert = _LightShadowData.r + i.NdotL * shadow * (1 - _LightShadowData.r);
#define UNIVERSAL_CALCULATE_NDOTV(N,V) fixed NdotV = 1 - saturate(dot(N, V));
#define UNIVERSAL_CALCULATE_DIFFUSE fixed3 diffuse = lambert * _LightColor0.rgb + i.ambient;

inline fixed3 UniversalColorCorrection(fixed3 color, fixed lambert, fixed saturation, fixed brightness) {
	fixed3 dark = color * brightness;
	fixed minc = min(dark.r, min(dark.g, dark.b));
	fixed maxc = max(dark.r, max(dark.g, dark.b));
	fixed avgc = (maxc + minc) * .5;
	
	fixed3 saturated = saturate(avgc + (dark - avgc) * 2);
	return lerp(color, lerp(saturated, color * 1.5, lambert), saturation);
}


inline float4 GetSkyData(samplerCUBE cubemap, half3 V, half3 N, fixed roughness) {
	half3 R = reflect(V, N);
	return texCUBElod(cubemap, float4(R, pow(roughness,.25)*10));
}

/*inline fixed3 UniversalUseCubemap(fixed3 color, float3 skyData, fixed NdotVpowCP) {
	return lerp(color, skyData, NdotVpowCP);
}*/

inline fixed3 UniversalAddGlow(fixed3 color, fixed NdotV, fixed glowPower, fixed3 glowColor) {
	return color + pow(NdotV, glowPower) * glowColor;
}

inline fixed4 UniversalAddGlowTransparent(fixed4 color, fixed NdotV, fixed glowPower, fixed4 glowColor) {
	return color + pow(NdotV, glowPower) * glowColor;
}

/*inline fixed UniversalGetSpecular(half3 V, half3 N, fixed roughness) {
	fixed specular = dot(reflect(V, N), _WorldSpaceLightPos0.xyz);
	return specular * .5 - .5 + roughness;
}*/

inline fixed3 UniversalAddSpecular(half3 V, half3 N, fixed roughness, fixed specularMultiplier) {
	fixed specular = dot(reflect(V, N), _WorldSpaceLightPos0.xyz);
	specular = max((specular * .5 - .5 + roughness) * specularMultiplier, 0);
	return specular * _LightColor0.rgb;
}

inline fixed4 UniversalAddSpecularTransparent(half3 V, half3 N, fixed roughness, fixed specularMultiplier) {
	fixed specular = dot(reflect(V, N), _WorldSpaceLightPos0.xyz);
	specular = max((specular * .5 - .5 + roughness) * specularMultiplier, 0);
	return fixed4(specular * _LightColor0.rgb, specular);
}