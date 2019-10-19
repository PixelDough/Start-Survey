Shader "PSXEffects/PS1Shader"
{
	Properties
	{
		[Toggle] _Unlit("Unlit", Float) = 0.0
		[Toggle] _DrawDist("Affected by Polygonal Draw Distance", Float) = 1.0
		_VertexInaccuracy("Vertex Inaccuracy Override", Float) = -1.0
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
		_NormalMap("Normal Map", 2D) = "bump" {}
		[KeywordEnum(Phong, Gouraud)] _SpecModel("Specular Model", Float) = 0.0
		_SpecularMap("Specular Map", 2D) = "white" {}
		_Specular("Specular Amount", Float) = 0.0
		_MetalMap("Metal Map", 2D) = "white" {}
		_Metallic("Metallic Amount", Range(0.0,1.0)) = 0.0
		_Smoothness("Smoothness Amount", Range(0.0,1.0)) = 0.5
		_Emission("Emission Map", 2D) = "white" {}
		_EmissionAmt("Emission Amount", Float) = 0.0
		_Cube("Cubemap", Cube) = "" {}

		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
		[HideInInspector] _Cul("__cul", Float) = 0.0

		[HideInInspector] _RenderMode("__rnd", Float) = 0.0
	}

	SubShader
	{
		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
		LOD 100
		Lighting On
		Offset[_Offset], 1
		Cull[_Cul]
		Blend[_SrcBlend][_DstBlend]

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM

			sampler2D _MainTex;
			sampler2D _Emission;
			sampler2D _NormalMap;
			sampler2D _SpecularMap;
			sampler2D _MetalMap;
			float4 _MainTex_ST;
			float _VertexSnappingDetail;
			float _VertexInaccuracy;
			float _AffineMapping;
			float _DrawDistance;
			float _Specular;
			float4 _Color;
			float _DarkMax;
			float _Unlit;
			float _SkyboxLighting;
			float _WorldSpace;
			float _EmissionAmt;
			float _Metallic;
			float _Smoothness;
			float _Triplanar;
			float _DrawDist;
			float _SpecModel;
			float _RenderMode;
			float _CamPos;
			uniform samplerCUBE _Cube;

			float _Transparent;

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma shader_feature TRANSPARENT
			#pragma shader_feature BFC

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 color : COLOR;
				float3 tangent: TANGENT;
				#ifdef LIGHTMAP_ON
					float2 uv_lightmap : TEXCOORD1;
				#endif
			};

			struct v2f
			{
				float3 uv : TEXCOORD0;
				float4 color : COLOR;
				fixed4 diff : COLOR1;
				fixed3 spec : COLOR2;
				float4 pos : SV_POSITION;
				float3 uv_lightmap : TEXCOORD1;
				float3 uv_affine : TEXCOORD2;
				float4 vertPos : COORDINATE0;
				float3 normal : NORMAL;
				float3 normalDir : TEXCOORD3;
				float3 viewDir : TEXCOORD4;
				float3 lightDir : TEXCOORD5;

				float3 T : TEXCOORD6;
				float3 B : TEXCOORD7;
				float3 N : TEXCOORD8;
				LIGHTING_COORDS(9, 10)
					UNITY_FOG_COORDS(11)
			};

			float4 PixelSnap(float4 pos)
			{
				float2 hpc = _ScreenParams.xy * 0.75f;
				_VertexInaccuracy /= 8;
				float2 pixelPos = round((pos.xy / pos.w) * hpc / _VertexInaccuracy) * _VertexInaccuracy;
				pos.xy = pixelPos / hpc * pos.w;
				return pos;
			}

			v2f vert(appdata v)
			{
				v2f o;

				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
				worldPos.xyz += _WorldSpaceCameraPos.xyz * _CamPos;
				worldPos.xyz += viewDir * 100 * _CamPos;
				if (_VertexInaccuracy < 0) _VertexInaccuracy = _VertexSnappingDetail;
				if (_WorldSpace == 1) {
					_VertexInaccuracy /= 2048;
					worldPos.xyz /= _VertexInaccuracy;
					worldPos.xyz = round(worldPos.xyz);
					worldPos.xyz *= _VertexInaccuracy;
					worldPos.xyz -= _WorldSpaceCameraPos.xyz * _CamPos;
					worldPos.xyz -= viewDir * 100 * _CamPos;
					v.vertex = mul(unity_WorldToObject, worldPos);
					o.pos = UnityObjectToClipPos(v.vertex);
				}
				else {
					o.pos = UnityObjectToClipPos(v.vertex);
					o.pos = PixelSnap(o.pos);
				}


				float wVal = mul(UNITY_MATRIX_P, o.pos).z;
				o.uv = v.texcoord;
				o.uv_affine = float3(v.texcoord.xy * wVal, wVal);

				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0;
				o.diff.rgb += ShadeSH9(half4(worldNormal, 1));
				o.diff.a = 1;

				if (distance(worldPos, _WorldSpaceCameraPos) > _DrawDistance && _DrawDistance > 0) {
					o.diff.a = 0;
				}

				o.color = v.color;
				o.normal = v.normal;
				o.vertPos = v.vertex;

				#ifdef VERTEXLIGHT_ON
				for (int j = 0; j < 4; j++) {
					float4 lightPos = float4(unity_4LightPosX0[j], unity_4LightPosY0[j], unity_4LightPosZ0[j], 1.0);

					float3 vertexToLightSource = lightPos.xyz - worldPos.xyz;
					float3 lightDir = normalize(vertexToLightSource);
					float squaredDist = dot(vertexToLightSource, vertexToLightSource);
					float atten = 1.0 / (1.0 + unity_4LightAtten0[j] * squaredDist);
					o.color.rgb += atten * unity_LightColor[j].rgb * _Color.rgb * max(0.0, dot(o.normal, lightDir));
				}
				#endif

				o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
				o.normalDir = normalize(mul(v.normal, unity_WorldToObject).xyz);

				o.uv_lightmap = float3(1, 1, 1);
				#ifdef LIGHTMAP_ON
					o.uv_lightmap = float3(v.uv_lightmap.xy * unity_LightmapST.xy + unity_LightmapST.zw, 1.0);
				#endif

				float3 lightDir;

				if (_WorldSpaceLightPos0.w == 0.0) {
					lightDir = normalize(_WorldSpaceLightPos0.xyz);
				}
				else {
					float3 vertToLight = _WorldSpaceLightPos0.xyz - mul(unity_ObjectToWorld, v.vertex).xyz;
					float dist = length(vertToLight);
					lightDir = normalize(vertToLight);
				}

				if (dot(o.normalDir, lightDir) < 0.0 || _SpecModel == 0.0) {
					o.spec = float3(0.0, 0.0, 0.0);
				}
				else {
					float3 reflection = reflect(lightDir, worldNormal);
					float3 viewDir = normalize(o.viewDir);
					o.spec = saturate(dot(reflection, -o.viewDir));
					o.spec = pow(o.spec, 20.0f);
				}

				o.lightDir = normalize(worldPos.xyz - _WorldSpaceLightPos0.xyz);


				worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);
				float3 worldTangent = mul((float3x3)unity_ObjectToWorld, v.normal);
				float3 binormal = cross(v.normal, v.tangent.xyz);
				float3 worldBinormal = mul((float3x3)unity_ObjectToWorld, -binormal);

				o.N = normalize(worldNormal);
				o.T = normalize(worldTangent);
				o.B = normalize(worldBinormal);

				UNITY_TRANSFER_FOG(o, o.pos);
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{

				float2 adjUv = lerp((i.uv + _MainTex_ST.zw) * _MainTex_ST.xy, (i.uv_affine / i.uv_affine.z + _MainTex_ST.zw) * _MainTex_ST.xy, _AffineMapping);

				float3 tangentNormal = tex2D(_NormalMap, adjUv).xyz;
				tangentNormal = normalize(tangentNormal * 2 - 1);
				float3x3 TBN = float3x3(normalize(i.T), normalize(i.B) * 8, normalize(i.N));
				TBN = transpose(TBN);
				float3 worldNormal = mul(TBN, tangentNormal);
				float3 fragNormal = UnityObjectToWorldNormal(i.normal);

				float4 metalMap = tex2D(_MetalMap, adjUv);
				UnityIndirect indirectLight;
				indirectLight.diffuse = max(0, ShadeSH9(half4(i.normal, 1)));
				indirectLight.specular = 0;
				_Smoothness = metalMap.a;
				float roughness = 1 - _Smoothness;
				float3 reflectionDir = reflect(-i.viewDir, i.normal);
				float4 envRefl = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectionDir, roughness * 6);
				indirectLight.specular = DecodeHDR(envRefl, unity_SpecCube0_HDR);

				float3 reflectedDir = reflect(i.viewDir, normalize(i.normalDir));
				float3 lightDir = normalize(i.lightDir);
				float4 albedo = tex2D(_MainTex, adjUv);
				float nl = (max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz)) * (1 - _Unlit));
				float4 diffuse = float4((i.diff.rgb * (1 - _Unlit) + _Unlit) * albedo.rgb, albedo.a);

				float3 specular = i.spec;
				if (diffuse.x > 0 && _SpecModel == 0.0) {
					if (_WorldSpaceLightPos0.w == 0.0) {
						lightDir = normalize(_WorldSpaceLightPos0.xyz);
					}
					else {
						float3 vertToLight = _WorldSpaceLightPos0.xyz - mul(unity_ObjectToWorld, i.pos).xyz;
						lightDir = normalize(vertToLight);
					}

					float3 reflection = reflect(lightDir, worldNormal);
					float3 viewDir = normalize(i.viewDir);
					specular = pow(saturate(dot(reflection, -viewDir)), 20.0f);
				}
				float4 specularIntensity = tex2D(_SpecularMap, adjUv) * _Specular;
				specular *= specularIntensity;

				float4 col = diffuse;

				col.rgb *= (indirectLight.diffuse + indirectLight.specular) * _Metallic * metalMap.r;
				col.rgb += diffuse * (1 - _Metallic) + specular + texCUBE(_Cube, reflectedDir) / 2 - 0.25;
				#ifdef LIGHTMAP_ON
					col.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv_lightmap)).rgb * 2;
				#endif

				col *= i.color * _Color;
				col.rgb -= max(0, (1 - i.diff.rgb) * i.color) * _DarkMax;
				col.rgb += tex2D(_Emission, adjUv) * _EmissionAmt;
				col.rgb *= LIGHT_ATTENUATION(i);

				if (i.diff.a == 0 && _DrawDist == 1.0 || (_RenderMode == 2.0 && albedo.a == 0)) {
					clip(-1);
				}

				UNITY_APPLY_FOG(i.fogCoord, col.rgb);

				return col;
			}
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdadd_fullshadows

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 uv : TEXCOORD0;
				float3 uv_affine : TEXCOORD1;
				float3 lightDir : TEXCOORD2;
				float3 normal : TEXCOORD5;
				fixed4 diff : COLOR;
				LIGHTING_COORDS(3,4)
			};

			float _CamPos;
			float _VertexInaccuracy;
			float _WorldSpace;
			float _NrmlOffset;
			float _VertexSnappingDetail;
			float _AffineMapping;
			float _DrawDistance;
			float _DrawDist;
			float _RenderMode;

			float4 PixelSnap(float4 pos)
			{
				float2 hpc = _ScreenParams.xy * 0.75f;
				_VertexInaccuracy /= 8;
				float2 pixelPos = round(((pos.xy) / pos.w) * hpc / _VertexInaccuracy) * _VertexInaccuracy;
				pos.xy = pixelPos / hpc * pos.w;
				return pos;
			}

			v2f vert(appdata_tan v) {
				v2f o;

				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 viewDir = UNITY_MATRIX_IT_MV[2].xyz;
				worldPos.xyz += _WorldSpaceCameraPos.xyz * _CamPos;
				worldPos.xyz += viewDir * 100 * _CamPos;
				if (_VertexInaccuracy < 0) _VertexInaccuracy = _VertexSnappingDetail;
				if (_WorldSpace == 1) {
					_VertexInaccuracy /= 2048;
					worldPos.xyz /= _VertexInaccuracy;
					worldPos.xyz = round(worldPos.xyz);
					worldPos.xyz *= _VertexInaccuracy;
					worldPos.xyz -= _WorldSpaceCameraPos.xyz * _CamPos;
					worldPos.xyz -= viewDir * 100 * _CamPos;
					v.vertex = mul(unity_WorldToObject, worldPos);
					o.pos = UnityObjectToClipPos(v.vertex + v.normal * _NrmlOffset);
				}
				else {
					o.pos = UnityObjectToClipPos(v.vertex);
					o.pos = PixelSnap(o.pos);
				}

				o.diff.rgb = 0;
				o.diff.a = 1;

				if (distance(worldPos, _WorldSpaceCameraPos) > _DrawDistance && _DrawDistance > 0) {
					o.diff.a = 0;
				}

				float wVal = mul(UNITY_MATRIX_P, o.pos).z;
				o.uv = v.texcoord;
				o.uv_affine = float3(v.texcoord.xy * wVal, wVal);

				o.lightDir = ObjSpaceLightDir(v.vertex);

				o.normal = v.normal;
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
 
			fixed4 _LightColor0;
 
			fixed4 frag(v2f i) : COLOR
			{
				float2 adjUv = lerp((i.uv + _MainTex_ST.zw) * _MainTex_ST.xy, (i.uv_affine / i.uv_affine.z + _MainTex_ST.zw) * _MainTex_ST.xy, _AffineMapping);
				fixed4 tex = tex2D(_MainTex, adjUv);
				if (i.diff.a == 0 && _DrawDist == 1.0 || (_RenderMode == 2.0 && tex.a == 0)) {
					clip(-1);
				}

				i.lightDir = normalize(i.lightDir);
                    
				fixed atten = LIGHT_ATTENUATION(i);
				if (0.0 == _WorldSpaceLightPos0.w) {
					atten = 1.0;
					i.lightDir = normalize(_WorldSpaceLightPos0.xyz);
				}
                    
				tex *= _Color;
                   
				fixed diff = saturate(dot(normalize(i.normal), i.lightDir));
                    
				fixed4 c;
				c.rgb = (tex.rgb * _LightColor0.rgb * diff) * (atten * 2);
				c.a = tex.a;
				return c;
			}

			ENDCG
		}
	}
	CustomEditor "PS1ShaderEditor"
	Fallback "VertexLit"
}
