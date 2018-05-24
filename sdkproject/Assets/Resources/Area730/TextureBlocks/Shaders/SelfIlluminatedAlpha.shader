Shader "Area730/SelfIlluminatedAlpha" {
	Properties{
		_Color("Color Tint", Color) = (1,1,1,1)
		_MainTex("SelfIllum Color (RGB) Alpha (A)", 2D) = "white"
	}
	Category{
		Lighting On
		ZWrite Off
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		Tags{ Queue = Transparent }
		SubShader{
			Material{
				Emission[_Color]
			}
			Pass{
				SetTexture[_MainTex]{
					Combine Texture * Primary, Texture * Primary
				}
			}
		}
	}
}
