Shader "MeshEdit/AOTLine"
{
	Properties
	{
		_MainTex("Albedo Texture", 2D) = "white" {}
		_Alpha("Transparency", Range(0.0,1.0)) = 0.4
		_CutoutThresh("Cutout Threshold", Range(0.0,1.0)) = 0.2
	}

		SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		ZTest Always
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float4 colour : COLOR;
	};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float2 screenPos : TEXCOORD1;
		float4 colour : COLOR;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float _Alpha;
	float _CutoutThresh;

	v2f vert(appdata v)
	{
		v2f o;
		o.colour = v.colour;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);

		o.screenPos = ComputeScreenPos(o.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		// sample the texture
		fixed4 col = tex2D(_MainTex, i.uv);
	col.a = col.a * _Alpha;
	clip(col.a - _CutoutThresh);

	col.rgb = col.rgb * i.colour.rgb;

	return col;
	}

		ENDCG
	}
	}
}