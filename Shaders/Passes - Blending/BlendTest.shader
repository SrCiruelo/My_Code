Shader "Holistic/BlendTest" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		Pass{
			SetTexture[_MainTex] {combine texture}
		} 

		
	}
}
