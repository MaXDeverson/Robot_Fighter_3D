// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "HPMaskTint"
{
	Properties
	{
		_Albedo("Albedo", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_EmissionTex("EmissionTex", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_NormalPower("NormalPower", Range( 0 , 6)) = 2
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_Specular("Specular", Color) = (0,0,0,0)
		_Color01("Color01", Color) = (0,0,0,0)
		_Color02("Color02", Color) = (0,0,0,0)
		_Color03("Color03", Color) = (0,0,0,0)
		_EmissionColor("EmissionColor", Color) = (0,0,0,0)
		_EmissionPower("EmissionPower", Range( 0 , 6)) = 2
		_Color01Power("Color01Power", Range( 0 , 6)) = 2
		_Color02Power("Color02Power", Range( 0 , 6)) = 2
		_Color03Power("Color03Power", Range( 0 , 6)) = 2
		_OverallPower("OverallPower", Range( 0 , 6)) = 2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Normal;
		uniform float4 _Normal_ST;
		uniform float _NormalPower;
		uniform sampler2D _Albedo;
		uniform float4 _Albedo_ST;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform float4 _Color01;
		uniform float _Color01Power;
		uniform float4 _Color02;
		uniform float _Color02Power;
		uniform float4 _Color03;
		uniform float _Color03Power;
		uniform float _OverallPower;
		uniform sampler2D _EmissionTex;
		uniform float4 _EmissionTex_ST;
		uniform float4 _EmissionColor;
		uniform float _EmissionPower;
		uniform float4 _Specular;
		uniform float _Smoothness;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
			o.Normal = ( UnpackNormal( tex2D( _Normal, uv_Normal ) ) * _NormalPower );
			float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			float4 tex2DNode16 = tex2D( _Albedo, uv_Albedo );
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float4 tex2DNode13 = tex2D( _Mask, uv_Mask );
			float4 temp_cast_0 = (tex2DNode13.r).xxxx;
			float4 temp_cast_1 = (tex2DNode13.g).xxxx;
			float4 temp_cast_2 = (tex2DNode13.b).xxxx;
			float4 blendOpSrc22 = tex2DNode16;
			float4 blendOpDest22 = ( ( min( temp_cast_0 , _Color01 ) * _Color01Power ) + ( min( temp_cast_1 , _Color02 ) * _Color02Power ) + ( min( temp_cast_2 , _Color03 ) * _Color03Power ) );
			float4 lerpResult4 = lerp( tex2DNode16 , ( ( saturate( ( blendOpSrc22 * blendOpDest22 ) )) * _OverallPower ) , ( tex2DNode13.r + tex2DNode13.g + tex2DNode13.b ));
			o.Albedo = lerpResult4.rgb;
			float2 uv_EmissionTex = i.uv_texcoord * _EmissionTex_ST.xy + _EmissionTex_ST.zw;
			float4 blendOpSrc43 = tex2D( _EmissionTex, uv_EmissionTex );
			float4 blendOpDest43 = _EmissionColor;
			o.Emission = ( ( saturate( ( blendOpSrc43 * blendOpDest43 ) )) * _EmissionPower ).rgb;
			o.Specular = _Specular.rgb;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16100
7;29;1906;1004;2385.034;721.4092;1.77228;True;True
Node;AmplifyShaderEditor.ColorNode;11;-1812.366,776.9087;Float;False;Property;_Color03;Color03;9;0;Create;True;0;0;False;0;0,0,0,0;1,0.6431373,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;9;-1792.213,50.7542;Float;False;Property;_Color01;Color01;7;0;Create;True;0;0;False;0;0,0,0,0;0.07552986,0.4010895,0.9338235,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;10;-1783.358,419.1845;Float;False;Property;_Color02;Color02;8;0;Create;True;0;0;False;0;0,0,0,0;0.2509804,0,0.7803922,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;13;-1881.112,-245.2657;Float;True;Property;_Mask;Mask;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;36;-1654.024,1090.497;Float;False;Property;_Color03Power;Color03Power;14;0;Create;True;0;0;False;0;2;2;0;6;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;34;-1654.025,654.1485;Float;False;Property;_Color02Power;Color02Power;13;0;Create;True;0;0;False;0;2;2;0;6;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-1643.157,251.962;Float;False;Property;_Color01Power;Color01Power;12;0;Create;True;0;0;False;0;2;2;0;6;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;15;-1362.328,-46.61261;Float;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMinOpNode;18;-1410.764,803.3873;Float;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMinOpNode;17;-1390.022,376.991;Float;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-1116.563,131.2737;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-1127.431,533.4603;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;-1127.43,969.8091;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;16;-1231.733,-531.3997;Float;True;Property;_Albedo;Albedo;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;19;-839.7979,101.7272;Float;True;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-612.5484,214.7102;Float;False;Property;_OverallPower;OverallPower;15;0;Create;True;0;0;False;0;2;2;0;6;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;44;-795.2985,620.2067;Float;False;Property;_EmissionColor;EmissionColor;10;0;Create;True;0;0;False;0;0,0,0,0;0.07552986,0.4010895,0.9338235,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;42;-883.9122,403.9885;Float;True;Property;_EmissionTex;EmissionTex;2;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendOpsNode;22;-662.2755,-53.6011;Float;False;Multiply;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendOpsNode;43;-557.8126,441.2065;Float;False;Multiply;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;39;-328.1207,497.4461;Float;True;Property;_Normal;Normal;3;0;Create;True;0;0;False;0;None;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;40;-212.5428,792.9299;Float;False;Property;_NormalPower;NormalPower;4;0;Create;True;0;0;False;0;2;2;0;6;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;20;-1002.543,-224.9695;Float;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-427.5818,3.95664;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-668.9052,870.9102;Float;False;Property;_EmissionPower;EmissionPower;11;0;Create;True;0;0;False;0;2;2;0;6;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;4;-230.8923,-366.3165;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;2;-360.0237,317.1769;Float;False;Property;_Smoothness;Smoothness;5;0;Create;True;0;0;False;0;0;0.2;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-379.3742,718.642;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;38;-198.6917,26.43461;Float;False;Property;_Specular;Specular;6;0;Create;True;0;0;False;0;0,0,0,0;0.07552986,0.4010895,0.9338235,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;23.81996,571.5429;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;283.5895,-212.0172;Float;False;True;2;Float;ASEMaterialInspector;0;0;StandardSpecular;HPMaskTint;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;15;0;13;1
WireConnection;15;1;9;0
WireConnection;18;0;13;3
WireConnection;18;1;11;0
WireConnection;17;0;13;2
WireConnection;17;1;10;0
WireConnection;33;0;15;0
WireConnection;33;1;32;0
WireConnection;35;0;17;0
WireConnection;35;1;34;0
WireConnection;37;0;18;0
WireConnection;37;1;36;0
WireConnection;19;0;33;0
WireConnection;19;1;35;0
WireConnection;19;2;37;0
WireConnection;22;0;16;0
WireConnection;22;1;19;0
WireConnection;43;0;42;0
WireConnection;43;1;44;0
WireConnection;20;0;13;1
WireConnection;20;1;13;2
WireConnection;20;2;13;3
WireConnection;23;0;22;0
WireConnection;23;1;24;0
WireConnection;4;0;16;0
WireConnection;4;1;23;0
WireConnection;4;2;20;0
WireConnection;46;0;43;0
WireConnection;46;1;45;0
WireConnection;41;0;39;0
WireConnection;41;1;40;0
WireConnection;0;0;4;0
WireConnection;0;1;41;0
WireConnection;0;2;46;0
WireConnection;0;3;38;0
WireConnection;0;4;2;0
ASEEND*/
//CHKSM=B7A4EE6CA90B14662AF956E3E336B7CA8F78D81D