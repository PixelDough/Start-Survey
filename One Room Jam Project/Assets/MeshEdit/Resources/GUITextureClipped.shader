
Shader "MeshEdit/GUI2"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_SrcBlend("SrcBlend", Int) = 5.0 // SrcAlpha
		_DstBlend("DstBlend", Int) = 10.0 // OneMinusSrcAlpha
		_MainTex("MainTex", 2D) = "white" {}
		_ZWrite("ZWrite", Int) = 1.0 // On
		_ZTest("ZTest", Int) = 4.0 // LEqual
		_Cull("Cull", Int) = 0.0 // Off
		_ZBias("ZBias", Float) = 0.0
		_Alpha("Transparency", Range(0.0,1.0)) = 1.0
		_CutoutThresh("Cutout Threshold", Range(0.0,1.0)) = 0.2
			_ClipLeft("ClipX", Int) = 0
		_ClipTop("ClipY", Int) = 0
		_ClipRight("ClipWidth", Int) = 100
			_ClipBottom("ClipHeight", Int) = 100
	}

		SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Pass
	{
		Blend[_SrcBlend][_DstBlend]
		ZWrite[_ZWrite]
		ZTest[_ZTest]
		Cull[_Cull]
		Offset[_ZBias],[_ZBias]

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
		struct appdata_t {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float4 color : COLOR;
	};

	struct v2f {
		float2 uv : TEXCOORD0;
		float4 fragPos : TEXCOORD1;
		fixed4 color : COLOR;
		float4 vertex : SV_POSITION;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float _Alpha;
	float _CutoutThresh;
	float4 _Color;
	float _ClipRight;
	float _ClipBottom;
	float _ClipLeft;
	float _ClipTop;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.color = v.color * _Color;
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		o.fragPos = mul(unity_ObjectToWorld, v.vertex);
		return o;
	}
	fixed4 frag(v2f i) : SV_Target
	{
			fixed4 col = tex2D(_MainTex, i.uv);
		col.a = col.a * _Alpha;
		clip(col.a - _CutoutThresh);

		col.rgb = col.rgb * i.color.rgb;

		clip(i.fragPos.y - _ClipTop);
		clip(_ClipBottom - i.fragPos.y);
		clip(i.fragPos.x - _ClipLeft);
		clip(_ClipRight - i.fragPos.x);

		return col;
	}
		ENDCG
	}
	}
}