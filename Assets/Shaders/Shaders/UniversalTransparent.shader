//UNIVERSAL SHADER (TRANSPARENT) v3.31 by Semyonchik27cm
Shader "Flexus/Universal Transparent"
{
    Properties
    {
		[Header(UNIVERSAL TRANSPARENT v3.31)]
		[Space(10)]_Color ("Цвет", Color) = (0,0,0,.5)
        [Space(10)][Toggle(USE_TEXTURE)] _UseTexture("Использовать текстуру вместо цвета", Float) = 0
		_MainTex ("Текстура", 2D) = "white" {}
		[Space(10)][Toggle(USE_GLOW)] _UseGlow("Подсветка", Float) = 1																 
        _GlowColor ("Цвет подсветки", Color) = (.4,.4,.4,1)
        _GlowPower ("Размер подсветки", Range(0, 5)) = 2
		[Space(10)][Toggle(USE_SPECULAR)] _UseSpecular("Блик", Float) = 1															   
		_SpecularMultiplier ("Яркость блика", Range(0, 3)) = 1
		_Roughness ("Шероховатость", Range(0.001, 1)) = 0.1
		[Space(10)][KeywordEnum(No, Cubemap, Gradient)] USE_CUBEMAP ("Использовать Cubemap", Int) = 0
		_Cubemap ("Cubemap", CUBE) = "black" {}
		_CubemapColor ("Цвет отражения", Color) = (1,1,1)
		_CubemapGradient ("Градиент", 2D) = "black" {}												
		_CubemapPower ("Металличность", Range(0, 3)) = 1
		[Space(10)][Toggle(USE_FOG)] _UseFog("Туман", Float) = 0
		[Space(10)][Toggle(USE_CC)] _UseCC("Цветокоррекция", Float) = 0
		_Saturation ("Насыщенность", Range(0, 1)) = 0.5
		_Brightness ("Яркость", Range(0, 2)) = 0.375
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		
        Pass
        {
			Tags {"LightMode"="ForwardBase"}
			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma shader_feature USE_FOG
			#pragma shader_feature USE_CUBEMAP_CUBEMAP
			#pragma shader_feature USE_CUBEMAP_GRADIENT
			#pragma shader_feature USE_TEXTURE
			#pragma shader_feature USE_CC
			#pragma shader_feature USE_GLOW
			#pragma shader_feature USE_SPECULAR									  
			#pragma multi_compile_fog

			#include "Flexus.cginc"

			struct appdata
            {
                UNIVERSAL_APPDATA
            };

            struct v2f
            {
                UNIVERSAL_V2F
				#ifdef USE_TEXTURE
				half2 uv : TEXCOORD0;
				#endif
				#ifdef USE_FOG
				UNITY_FOG_COORDS(2)
				#endif
            };

			UNIVERSAL_VARS
			
			#ifdef USE_TEXTURE
			sampler2D _MainTex;
			float4 _MainTex_ST;
			#endif
			
            v2f vert (appdata v)
            {
                v2f o;
				float3 ws_pos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.N = UnityObjectToWorldNormal(v.normal);
				UNIVERSAL_VS
				
				#ifdef USE_TEXTURE
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				#endif
				
				#ifdef USE_FOG
				UNITY_TRANSFER_FOG(o, o.pos);
				#endif
				
				return o;
            }
			
			#ifdef USE_CUBEMAP_CUBEMAP
			samplerCUBE _Cubemap;
			fixed3 _CubemapColor;
			fixed _CubemapPower;
			float4 _Cubemap_HDR;
			#endif

			#ifdef USE_CUBEMAP_GRADIENT
			sampler2D _CubemapGradient;
			fixed _CubemapPower;
			#endif

			#ifdef USE_CC
			fixed _Saturation;
			fixed _Brightness;
			#endif

            fixed4 frag (v2f i) : SV_Target
            {
				UNIVERSAL_CALCULATE_NDOTV(i.N,-i.V)
				UNIVERSAL_CALCULATE_SHADOW
				UNIVERSAL_CALCULATE_LAMBERT
				
				#ifdef USE_TEXTURE
				fixed4 texColor = tex2D(_MainTex, i.uv);
				fixed4 color = texColor * _Color;
				#else
				fixed4 texColor = 1;
				fixed4 color = _Color;
				#endif
				
				fixed3 diffuse = lambert * _LightColor0.rgb + i.ambient;
				color.rgb = color.rgb * diffuse;
				
				#ifdef USE_CUBEMAP_CUBEMAP
				float4 skyData = GetSkyData(_Cubemap, i.V, i.N, _Roughness);
				skyData.rgb = DecodeHDR(skyData, _Cubemap_HDR);
				fixed NdotVpowCP = pow(NdotV, _CubemapPower);
				color.rgb = lerp(color.rgb, skyData.rgb * _CubemapColor * texColor, NdotVpowCP);
				color.a += (skyData.r + skyData.g + skyData.b) * .33 * pow(NdotV,_CubemapPower);
				#endif
				
				#ifdef USE_CUBEMAP_GRADIENT
				fixed3 gradient = tex2Dlod(_CubemapGradient, fixed4(NdotV, 0, 0, pow(_Roughness,.25)*10));
				color.rgb = lerp(color, gradient, pow(NdotV, _CubemapPower));
				color.a += (gradient.r + gradient.g + gradient.b) * .33 * pow(NdotV,_CubemapPower);
				#endif
				
				#ifdef USE_GLOW
				color = UniversalAddGlowTransparent(color, NdotV, _GlowPower, _GlowColor);
				#endif
				
				#ifdef USE_SPECULAR
				color += UniversalAddSpecularTransparent(i.V, i.N, _Roughness, i.specularMultiplier) * shadow;
				#endif

				#ifdef USE_CC
				color.rgb = UniversalColorCorrection(color.rgb, lambert, _Saturation, _Brightness);
				#endif
				
				#ifdef USE_FOG
				UNITY_APPLY_FOG(i.fogCoord, color);
				#endif
				return saturate(color);
            }
            ENDCG
        }
	UsePass "VertexLit/SHADOWCASTER"
    }
}	