Shader "Custom/Gradation" {
	Properties{
		_Color("Color", Color) = (1, 1, 1, 1)
	}
		SubShader{
		Tags{ "RenderType" = "Transparent" }
		Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
		Pass{
		CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

	struct VertexInput {
		float4 vertex : POSITION;
		float4 color: COLOR;
		float4 normal: NORMAL;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		float3 worldPos : TEXCOORD0;
		float4 col : COLOR;
	};

	float4 _Color;

	v2f vert(VertexInput v)
	{
		v2f o;
		float3 n = UnityObjectToWorldNormal(v.normal);
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.col = v.color;
		return o;
	}

	half4 frag(v2f i) : COLOR
	{
		return i.col;
	}
		ENDCG
	}
	}
		FallBack Off
}