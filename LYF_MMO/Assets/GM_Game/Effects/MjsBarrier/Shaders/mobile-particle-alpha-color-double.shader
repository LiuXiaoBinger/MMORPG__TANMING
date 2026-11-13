// Simplified Alpha Blended Particle shader. Differences from regular Alpha Blended Particle one:
// - no Smooth particle support
// - no AlphaTest
// - no ColorMask

Shader "Mobile/Particles/Alpha Blended Color(DOUBLE)" {
Properties {
	_TintColor ("Tint Color", Color) = (1.0,1.0,1.0,1.0)
	_MainTex ("Particle Texture", 2D) = "white" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }

	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}

	// ---- Dual texture cards
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				constantColor [_TintColor]
				combine constant * primary DOUBLE
			}
			SetTexture [_MainTex] {
				combine texture * previous
			}
		}
	}

	// ---- Single texture cards (does not do color tint)
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * primary DOUBLE
			}
		}
	}
}
}
