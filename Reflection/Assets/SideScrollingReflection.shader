Shader "Unlit/SideScrollingReflection"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ReflectTex ("ReflectTexture", 2D) = "white" {}
		_backgroundDis("BackgroundDis",Float) = 10
		_oriWidthHeight("OriWidthHeight",Vector) = (0,0,0,0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 reflectUV : TEXCOORD1;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _ReflectTex;
			float4 _MainTex_ST;
			half _backgroundDis;
			half4 _oriWidthHeight;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				half4 worldPos = mul(unity_ObjectToWorld,v.vertex);
				half3 viewDir = worldPos.xyz - _WorldSpaceCameraPos;
				half dis = worldPos.z - _backgroundDis;
				half dVer = dis*viewDir.y/viewDir.z;
				half dHor = dis*viewDir.x/viewDir.z;
				half targetVer = worldPos.y+dVer;
				half targetHor = worldPos.x-dHor;
				o.reflectUV = half2((targetHor-_oriWidthHeight.x)/_oriWidthHeight.z,(targetVer-_oriWidthHeight.y)/_oriWidthHeight.w);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv.x += _Time.x;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv)+tex2D(_ReflectTex, i.reflectUV)*3;
				col/=4;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
