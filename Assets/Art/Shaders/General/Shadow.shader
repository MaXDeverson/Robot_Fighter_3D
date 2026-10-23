Shader "Unlit/Shadow"
{Properties
	{
		_Color("Color", Color) = (1,1,1,.2)
}

SubShader
	{
		//Rndertype : Opague...
		//Queue is important here...
		Tags { "Queue" = "Transparent" "RnderType" = "Transparent" }
		LOD 100
		ZWrite Off
	//first pass sets the mask for the "front geometry"
	Pass
	{
		Cull Back
		ZTest Greater
		ColorMask 0

		Stencil
		{
			Ref 3
			Comp Always
			Pass Replace
		}
	}
	//second pass turn off culling...
	//use the mask from the first pass and draw the color
	Pass
	{
		Cull Off
		ZTest Greater
	//ColorMask rgb
	//ZWrite On
	Blend SrcAlpha OneMinusSrcAlpha

	Stencil
	{
		Ref 3
		Comp NotEqual
	}

	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
	};

	float4 _Color;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return _Color;
	}
	ENDCG
}
}
}
