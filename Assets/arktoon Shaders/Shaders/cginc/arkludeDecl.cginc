#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"


#if defined(ARKTOON_SECONDARY)
    // Secondary
    UNITY_DECLARE_TEX2D(_MainTexSecondary); uniform float4 _MainTexSecondary_ST;
    uniform float4 _ColorSecondary;
    uniform sampler2D _BumpMapSecondary; uniform float4 _BumpMapSecondary_ST;
    uniform float _BumpScaleSecondary;
    uniform sampler2D _EmissionMapSecondary; uniform float4 _EmissionMapSecondary_ST;
    uniform float4 _EmissionColorSecondary;
#   define REF_MAINTEX _MainTexSecondary
#   define REF_COLOR _ColorSecondary
#   define REF_BUMPMAP _BumpMapSecondary
#   define REF_BUMPSCALE _BumpScaleSecondary
#   define REF_EMISSIONMAP _EmissionMapSecondary
#   define REF_EMISSIONCOLOR _EmissionColorSecondary
#else
    // Main, Normal, Emission
    UNITY_DECLARE_TEX2D(_MainTex); uniform float4 _MainTex_ST;
    uniform float4 _Color;
    uniform sampler2D _BumpMap; uniform float4 _BumpMap_ST;
    uniform float _BumpScale;
    uniform sampler2D _EmissionMap; uniform float4 _EmissionMap_ST;
    uniform float4 _EmissionColor;
#   define REF_MAINTEX _MainTex
#   define REF_COLOR _Color
#   define REF_BUMPMAP _BumpMap
#   define REF_BUMPSCALE _BumpScale
#   define REF_EMISSIONMAP _EmissionMap
#   define REF_EMISSIONCOLOR _EmissionColor
#endif

// Alpha Mask
UNITY_DECLARE_TEX2D_NOSAMPLER(_AlphaMask); uniform float4 _AlphaMask_ST;

// Emission Parallax
uniform float _UseEmissionParallax;
UNITY_DECLARE_TEX2D_NOSAMPLER(_EmissionParallaxTex); uniform float4 _EmissionParallaxTex_ST;
uniform float4 _EmissionParallaxColor;
UNITY_DECLARE_TEX2D_NOSAMPLER(_EmissionParallaxMask); uniform float4 _EmissionParallaxMask_ST;
uniform float _EmissionParallaxDepth;
UNITY_DECLARE_TEX2D_NOSAMPLER(_EmissionParallaxDepthMask); uniform float4 _EmissionParallaxDepthMask_ST;
uniform float _EmissionParallaxDepthMaskInvert;

// Double Side
uniform float _DoubleSidedBackfaceLightIntensity;
uniform float _DoubleSidedFlipBackfaceNormal;

// Shadow
uniform float _Shadowborder;
uniform float _ShadowborderBlur;
UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadowborderBlurMask); uniform float4 _ShadowborderBlurMask_ST;
uniform float _ShadowStrength;
uniform float _ShadowIndirectIntensity;
uniform float _ShadowUseStep;
uniform int _ShadowSteps;
uniform sampler2D _ShadowStrengthMask; uniform float4 _ShadowStrengthMask_ST;

// Custom shade1
uniform float _ShadowPlanBDefaultShadowMix;
uniform float _ShadowPlanBHueShiftFromBase;
uniform float _ShadowPlanBSaturationFromBase;
uniform float _ShadowPlanBValueFromBase;
UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadowPlanBCustomShadowTexture); uniform float4 _ShadowPlanBCustomShadowTexture_ST;
uniform float4 _ShadowPlanBCustomShadowTextureRGB;

// Cutsom shade2
uniform float _ShadowPlanB2border;
uniform float _ShadowPlanB2borderBlur;
uniform float _ShadowPlanB2HueShiftFromBase;
uniform float _ShadowPlanB2SaturationFromBase;
uniform float _ShadowPlanB2ValueFromBase;
UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadowPlanB2CustomShadowTexture); uniform float4 _ShadowPlanB2CustomShadowTexture_ST;
uniform float4 _ShadowPlanB2CustomShadowTextureRGB;

// Outline
UNITY_DECLARE_TEX2D_NOSAMPLER(_OutlineMask); uniform float4 _OutlineMask_ST;
uniform float _OutlineCutoffRange;
uniform float _OutlineTextureColorRate;
uniform float _OutlineShadeMix;
uniform float _OutlineWidth;
uniform float4 _OutlineColor;
uniform sampler2D _OutlineWidthMask; uniform float4 _OutlineWidthMask_ST; // FIXME:tex2dLodはUNITY_SAMPLE_TEX2D_SAMPLERの代用が判らないためいったん保留
uniform float _OutlineUseColorShift;
uniform float _OutlineHueShiftFromBase;
uniform float _OutlineSaturationFromBase;
uniform float _OutlineValueFromBase;

// Gloss
uniform float _GlossBlend;
UNITY_DECLARE_TEX2D_NOSAMPLER(_GlossBlendMask); uniform float4 _GlossBlendMask_ST;
uniform float _GlossPower;
uniform float4 _GlossColor;
uniform float _CutoutCutoutAdjust;

// Point lights
uniform float _PointAddIntensity;
uniform float _PointShadowStrength;
uniform float _PointShadowborder;
uniform float _PointShadowborderBlur;
UNITY_DECLARE_TEX2D_NOSAMPLER(_PointShadowborderBlurMask); uniform float4 _PointShadowborderBlurMask_ST;
uniform float _PointShadowUseStep;
uniform int _PointShadowSteps;

// MatCap
uniform sampler2D _MatcapTexture; uniform float4 _MatcapTexture_ST;
uniform float _MatcapBlend;
UNITY_DECLARE_TEX2D_NOSAMPLER(_MatcapBlendMask); uniform float4 _MatcapBlendMask_ST;
uniform float4 _MatcapColor;
uniform float _MatcapNormalMix;
uniform float _MatcapShadeMix;

// Reflection
uniform float _ReflectionReflectionPower;
UNITY_DECLARE_TEX2D_NOSAMPLER(_ReflectionReflectionMask); uniform float4 _ReflectionReflectionMask_ST;
uniform float _ReflectionNormalMix;
uniform float _ReflectionShadeMix;
uniform float _ReflectionSuppressBaseColorValue;
uniform samplerCUBE _ReflectionCubemap;
uniform half4  _ReflectionCubemap_HDR;

// Rim
uniform float _RimFresnelPower;
uniform float _RimUpperSideWidth;
uniform float4 _RimColor;
uniform fixed _RimUseBaseTexture;
uniform float _RimBlend;
uniform float _RimShadeMix;
UNITY_DECLARE_TEX2D_NOSAMPLER(_RimBlendMask); uniform float4 _RimBlendMask_ST;
uniform sampler2D _RimTexture; uniform float4 _RimTexture_ST;

// Shade cap (Shadow cap)
uniform sampler2D _ShadowCapTexture; uniform float4 _ShadowCapTexture_ST;
UNITY_DECLARE_TEX2D_NOSAMPLER(_ShadowCapBlendMask); uniform float4 _ShadowCapBlendMask_ST;
uniform float _ShadowCapBlend;
uniform float _ShadowCapNormalMix;

// Vertex Color Blend
uniform float _VertexColorBlendDiffuse;
uniform float _VertexColorBlendEmissive;

// Shade from other objects.
uniform float _OtherShadowAdjust;
uniform float _OtherShadowBorderSharpness;

// Refraction IF refracted
#ifdef ARKTOON_REFRACTED
uniform sampler2D _GrabTexture;
uniform float _RefractionFresnelExp;
uniform float _RefractionStrength;
#endif