// Wet version of "TOZ/Object/Splat/VertexColors/Standard" (RaceTracksPack road):
// darker, glossy asphalt + world-space puddles with animated rain ripples. Layer 0 (vertex R) = asphalt, layer 1 (G) = snow.
Shader "RaceUI/Wet Road Splat" {
	Properties {
		_Splat0("Layer 0 (R)", 2D) = "red" {}
		_Splat1("Layer 1 (G)", 2D) = "green" {}
		_Normal0("Normal 0 (R)", 2D) = "bump" {}
		_Normal1("Normal 1 (G)", 2D) = "bump" {}
		[Gamma]_Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
		[Gamma]_Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
		_Glossiness("Dry smoothness", Range(0,1)) = 0.5
		[Toggle] _UseVertexColors("Blend layers by vertex colour (off = all layer 0)", Float) = 1

		[Header(Wet)]
		_Wetness("Wetness", Range(0,1)) = 1
		_WetDarken("Wet albedo multiplier", Range(0,1)) = 0.6
		_WetSmoothness("Wet asphalt smoothness", Range(0,1)) = 0.78
		_SnowWetness("Wetness on snow layer", Range(0,1)) = 0.25

		[Header(Puddles)]
		_PuddleMask("Puddle mask (R, tileable)", 2D) = "black" {}
		_PuddleScale("Puddle world tiling", Float) = 0.025
		_PuddleCoverage("Puddle coverage", Range(0,1)) = 0.45
		_PuddleEdge("Puddle edge softness", Range(0.01,0.3)) = 0.08
		_PuddleColor("Puddle tint", Color) = (0.42, 0.46, 0.5, 1)
		_PuddleSmoothness("Puddle smoothness", Range(0,1)) = 0.96

		[Header(Rain ripples)]
		_RippleStrength("Ripple strength", Range(0,1)) = 0.45
		_RippleScale("Ripple cells per metre", Float) = 0.7
		_RippleSpeed("Ripple speed", Float) = 1.3
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard vertex:vert fullforwardshadows
		#pragma target 3.0

		sampler2D _Splat0, _Splat1, _Normal0, _Normal1, _PuddleMask;
		float4 _Splat0_ST, _Splat1_ST;
		half _Metallic0, _Metallic1, _Glossiness, _UseVertexColors;
		half _Wetness, _WetDarken, _WetSmoothness, _SnowWetness;
		float _PuddleScale; half _PuddleCoverage, _PuddleEdge, _PuddleSmoothness; fixed4 _PuddleColor;
		half _RippleStrength; float _RippleScale, _RippleSpeed;

		struct Input {
			fixed4 color : COLOR0;
			float4 coord0 : TEXCOORD0;
			float3 worldPos;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.color = v.color;
			o.coord0.xy = v.texcoord.xy * _Splat0_ST.xy + _Splat0_ST.zw;
			o.coord0.zw = v.texcoord.xy * _Splat1_ST.xy + _Splat1_ST.zw;
		}

		float hash12(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

		// one expanding ring per cell, random phase/offset -> tangent-space xy
		float2 ripple(float2 p, float t) {
			float2 cell = floor(p);
			float rnd = hash12(cell);
			float2 f = frac(p) - 0.5 - (float2(hash12(cell + 7.1), hash12(cell + 3.3)) - 0.5) * 0.4;
			float phase = frac(t + rnd);
			float d = length(f);
			float r = phase * 0.45;
			float ring = sin((d - r) * 60.0) * saturate(1.0 - abs(d - r) * 14.0) * (1.0 - phase);
			return (f / max(d, 1e-3)) * ring;
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			half2 w = lerp(half2(1, 0), IN.color.rg, _UseVertexColors); // meshes without vertex colours are plain asphalt
			half asphalt = w.x, snow = w.y;
			fixed4 col = asphalt * tex2D(_Splat0, IN.coord0.xy) + snow * tex2D(_Splat1, IN.coord0.zw);
			fixed4 nrm = asphalt * tex2D(_Normal0, IN.coord0.xy) + snow * tex2D(_Normal1, IN.coord0.zw);
			float3 n = UnpackNormal(nrm);

			half wet = _Wetness * saturate(asphalt + snow * _SnowWetness);
			// puddles only on the asphalt layer
			half mask = tex2D(_PuddleMask, IN.worldPos.xz * _PuddleScale).r;
			half puddle = smoothstep(1.0 - _PuddleCoverage, 1.0 - _PuddleCoverage + _PuddleEdge, mask) * asphalt * _Wetness;

			float t = _Time.y * _RippleSpeed;
			float2 rip = ripple(IN.worldPos.xz * _RippleScale, t) + ripple(IN.worldPos.xz * _RippleScale * 1.37 + 17.0, t * 1.21);
			float3 puddleN = normalize(float3(rip * _RippleStrength, 1.0));

			o.Albedo = col.rgb * lerp(1.0, _WetDarken, wet);
			o.Albedo = lerp(o.Albedo, o.Albedo * _PuddleColor.rgb, puddle);
			o.Normal = normalize(lerp(lerp(n, float3(0, 0, 1), wet * 0.4), puddleN, puddle));
			o.Smoothness = lerp(lerp(_Glossiness, _WetSmoothness, wet), _PuddleSmoothness, puddle);
			o.Metallic = dot(w, half2(_Metallic0, _Metallic1)) * (1.0 - puddle);
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
