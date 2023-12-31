Shader "Unlit/FogShader"
{
	Properties
	{
		_MainTex("Sprite Texture", 2D) = "white" {}
		_TintColor("Color", Color) = (0,0,0,0)
	}
		SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord  : TEXCOORD0;
			};

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				return OUT;
			}

			sampler2D _MainTex;
			float4 _TintColor;

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c4 = tex2D(_MainTex, IN.texcoord);
				fixed4 c = _TintColor;
				c = c * (1 - c4.r);
				return c;
			}
			ENDCG
		}
	}
}
