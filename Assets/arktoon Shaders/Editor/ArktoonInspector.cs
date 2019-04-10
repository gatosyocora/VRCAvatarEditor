using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// Arktoon-Shaders
//
// Copyright (c) 2018 synqark
//
// This code and repos（https://github.com/synqark/Arktoon-Shader) is under MIT licence, see LICENSE
//
// 本コードおよびリポジトリ（https://github.com/synqark/Arktoon-Shader) は MIT License を使用して公開しています。
// 詳細はLICENSEか、https://opensource.org/licenses/mit-license.php を参照してください。
namespace ArktoonShaders
{
    public class ArktoonInspector : ShaderGUI
    {
        #region MaterialProperties
        MaterialProperty BaseTexture;
        MaterialProperty BaseColor;
        MaterialProperty Normalmap;
        MaterialProperty BumpScale;
        MaterialProperty EmissionMap;
        MaterialProperty EmissionColor;
        MaterialProperty AlphaMask;
        MaterialProperty BaseTextureSecondary;
        MaterialProperty BaseColorSecondary;
        MaterialProperty NormalmapSecondary;
        MaterialProperty BumpScaleSecondary;
        MaterialProperty EmissionMapSecondary;
        MaterialProperty EmissionColorSecondary;
        MaterialProperty UseEmissionParallax;
        MaterialProperty EmissionParallaxColor;
        MaterialProperty EmissionParallaxTex;
        MaterialProperty EmissionParallaxMask;
        MaterialProperty EmissionParallaxDepth;
        MaterialProperty EmissionParallaxDepthMask;
        MaterialProperty EmissionParallaxDepthMaskInvert;
        MaterialProperty Shadowborder;
        MaterialProperty ShadowborderBlur;
        MaterialProperty ShadowborderBlurMask;
        MaterialProperty ShadowStrength;
        MaterialProperty ShadowStrengthMask;
        MaterialProperty ShadowIndirectIntensity;
        MaterialProperty ShadowUseStep;
        MaterialProperty ShadowSteps;
        MaterialProperty PointAddIntensity;
        MaterialProperty PointShadowStrength;
        MaterialProperty PointShadowborder;
        MaterialProperty PointShadowborderBlur;
        MaterialProperty PointShadowborderBlurMask;
        MaterialProperty PointShadowUseStep;
        MaterialProperty PointShadowSteps;
        MaterialProperty CutoutCutoutAdjust;
        MaterialProperty ShadowPlanBUsePlanB;
        MaterialProperty ShadowPlanBDefaultShadowMix;
        MaterialProperty ShadowPlanBUseCustomShadowTexture;
        MaterialProperty ShadowPlanBHueShiftFromBase;
        MaterialProperty ShadowPlanBSaturationFromBase;
        MaterialProperty ShadowPlanBValueFromBase;
        MaterialProperty ShadowPlanBCustomShadowTexture;
        MaterialProperty ShadowPlanBCustomShadowTextureRGB;
        MaterialProperty CustomShadow2nd;
        MaterialProperty ShadowPlanB2border;
        MaterialProperty ShadowPlanB2borderBlur;
        MaterialProperty ShadowPlanB2HueShiftFromBase;
        MaterialProperty ShadowPlanB2SaturationFromBase;
        MaterialProperty ShadowPlanB2ValueFromBase;
        MaterialProperty ShadowPlanB2UseCustomShadowTexture;
        MaterialProperty ShadowPlanB2CustomShadowTexture;
        MaterialProperty ShadowPlanB2CustomShadowTextureRGB;
        MaterialProperty UseGloss;
        MaterialProperty GlossBlend;
        MaterialProperty GlossBlendMask;
        MaterialProperty GlossPower;
        MaterialProperty GlossColor;
        MaterialProperty UseOutline;
        MaterialProperty OutlineWidth;
        MaterialProperty OutlineMask;
        MaterialProperty OutlineCutoffRange;
        MaterialProperty OutlineColor;
        MaterialProperty OutlineShadeMix;
        MaterialProperty OutlineTextureColorRate;
        MaterialProperty OutlineWidthMask;
        MaterialProperty OutlineUseColorShift;
        MaterialProperty OutlineHueShiftFromBase;
        MaterialProperty OutlineSaturationFromBase;
        MaterialProperty OutlineValueFromBase;
        MaterialProperty MatcapBlendMode;
        MaterialProperty MatcapBlend;
        MaterialProperty MatcapTexture;
        MaterialProperty MatcapColor;
        MaterialProperty MatcapBlendMask;
        MaterialProperty MatcapNormalMix;
        MaterialProperty MatcapShadeMix;
        MaterialProperty UseReflection;
        MaterialProperty UseReflectionProbe;
        MaterialProperty ReflectionReflectionPower;
        MaterialProperty ReflectionReflectionMask;
        MaterialProperty ReflectionNormalMix;
        MaterialProperty ReflectionShadeMix;
        MaterialProperty ReflectionCubemap;
        MaterialProperty ReflectionSuppressBaseColorValue;
        MaterialProperty RefractionFresnelExp;
        MaterialProperty RefractionStrength;
        MaterialProperty UseRim;
        MaterialProperty RimBlend;
        MaterialProperty RimBlendMask;
        MaterialProperty RimShadeMix;
        MaterialProperty RimFresnelPower;
        MaterialProperty RimUpperSideWidth;
        MaterialProperty RimColor;
        MaterialProperty RimTexture;
        MaterialProperty RimUseBaseTexture;
        MaterialProperty ShadowCapBlendMode;
        MaterialProperty ShadowCapBlend;
        MaterialProperty ShadowCapBlendMask;
        MaterialProperty ShadowCapNormalMix;
        MaterialProperty ShadowCapTexture;
        MaterialProperty StencilNumber;
        MaterialProperty StencilCompareAction;
        MaterialProperty StencilNumberSecondary;
        MaterialProperty StencilCompareActionSecondary;
        MaterialProperty StencilMaskTex;
        MaterialProperty StencilMaskAdjust;
        MaterialProperty StencilMaskAlphaDither;
        MaterialProperty UseDoubleSided;
        MaterialProperty DoubleSidedFlipBackfaceNormal;
        MaterialProperty DoubleSidedBackfaceLightIntensity;
        MaterialProperty ShadowCasterCulling;
        MaterialProperty ZWrite;
        MaterialProperty VertexColorBlendDiffuse;
        MaterialProperty VertexColorBlendEmissive;
        MaterialProperty OtherShadowBorderSharpness;
        MaterialProperty OtherShadowAdjust;
        MaterialProperty UseVertexLight;
        MaterialProperty BackfaceColorMultiply;
        MaterialProperty LightSampling;
        MaterialProperty UsePositionRelatedCalc;

        #endregion

        static bool IsShowAdvanced = false;
        static bool IsShowAlphaMask = false;
        GUIStyle style = new GUIStyle();


        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            Material material = materialEditor.target as Material;
            Shader shader = material.shader;

            // shader.nameによって調整可能なプロパティを制御する。
            bool isOpaque = shader.name.Contains("Opaque");
            bool isFade = shader.name.Contains("Fade");
            bool isCutout = shader.name.Contains("Cutout");
            bool isStencilWriter = shader.name.Contains("Stencil/Writer");
            bool isStencilReader = shader.name.Contains("Stencil/Reader");
            bool isStencilReaderDouble = shader.name.Contains("Stencil/Reader/Double");
            bool isStencilWriterMask = shader.name.Contains("Stencil/WriterMask");
            bool isRefracted = shader.name.Contains("Refracted");

            // FindProperties
            BaseTexture = FindProperty("_MainTex", props);
            BaseColor = FindProperty("_Color", props);
            Normalmap = FindProperty("_BumpMap", props);
            BumpScale = FindProperty("_BumpScale", props);
            EmissionMap = FindProperty("_EmissionMap", props);
            EmissionColor = FindProperty("_EmissionColor", props);
            if(isFade) AlphaMask = FindProperty("_AlphaMask", props);
            if(isStencilReaderDouble) BaseTextureSecondary = FindProperty("_MainTexSecondary", props);
            if(isStencilReaderDouble) BaseColorSecondary = FindProperty("_ColorSecondary", props);
            if(isStencilReaderDouble) NormalmapSecondary = FindProperty("_BumpMapSecondary", props);
            if(isStencilReaderDouble) BumpScaleSecondary = FindProperty("_BumpScaleSecondary", props);
            if(isStencilReaderDouble) EmissionMapSecondary = FindProperty("_EmissionMapSecondary", props);
            if(isStencilReaderDouble) EmissionColorSecondary = FindProperty("_EmissionColorSecondary", props);
            UseEmissionParallax = FindProperty("_UseEmissionParallax", props);
            EmissionParallaxColor = FindProperty("_EmissionParallaxColor", props);
            EmissionParallaxTex = FindProperty("_EmissionParallaxTex", props);
            EmissionParallaxMask = FindProperty("_EmissionParallaxMask", props);
            EmissionParallaxDepth = FindProperty("_EmissionParallaxDepth", props);
            EmissionParallaxDepthMask = FindProperty("_EmissionParallaxDepthMask", props);
            EmissionParallaxDepthMaskInvert = FindProperty("_EmissionParallaxDepthMaskInvert", props);
            if(isCutout) CutoutCutoutAdjust = FindProperty("_CutoutCutoutAdjust", props);
            Shadowborder = FindProperty("_Shadowborder", props);
            ShadowborderBlur = FindProperty("_ShadowborderBlur", props);
            ShadowborderBlurMask = FindProperty("_ShadowborderBlurMask", props);
            ShadowStrength = FindProperty("_ShadowStrength", props);
            ShadowStrengthMask = FindProperty("_ShadowStrengthMask", props);
            ShadowIndirectIntensity = FindProperty("_ShadowIndirectIntensity", props);
            ShadowUseStep = FindProperty("_ShadowUseStep", props);
            ShadowSteps = FindProperty("_ShadowSteps", props);
            PointAddIntensity = FindProperty("_PointAddIntensity", props);
            PointShadowStrength = FindProperty("_PointShadowStrength", props);
            PointShadowborder = FindProperty("_PointShadowborder", props);
            PointShadowborderBlur = FindProperty("_PointShadowborderBlur", props);
            PointShadowborderBlurMask= FindProperty("_PointShadowborderBlurMask", props);
            PointShadowUseStep = FindProperty("_PointShadowUseStep", props);
            PointShadowSteps = FindProperty("_PointShadowSteps", props);
            ShadowPlanBUsePlanB = FindProperty("_ShadowPlanBUsePlanB", props);
            ShadowPlanBDefaultShadowMix = FindProperty("_ShadowPlanBDefaultShadowMix", props);
            ShadowPlanBUseCustomShadowTexture = FindProperty("_ShadowPlanBUseCustomShadowTexture", props);
            ShadowPlanBHueShiftFromBase = FindProperty("_ShadowPlanBHueShiftFromBase", props);
            ShadowPlanBSaturationFromBase = FindProperty("_ShadowPlanBSaturationFromBase", props);
            ShadowPlanBValueFromBase = FindProperty("_ShadowPlanBValueFromBase", props);
            ShadowPlanBCustomShadowTexture = FindProperty("_ShadowPlanBCustomShadowTexture", props);
            ShadowPlanBCustomShadowTextureRGB = FindProperty("_ShadowPlanBCustomShadowTextureRGB", props);
            CustomShadow2nd = FindProperty("_CustomShadow2nd", props);
            ShadowPlanB2border = FindProperty("_ShadowPlanB2border", props);
            ShadowPlanB2borderBlur = FindProperty("_ShadowPlanB2borderBlur", props);
            ShadowPlanB2HueShiftFromBase = FindProperty("_ShadowPlanB2HueShiftFromBase", props);
            ShadowPlanB2SaturationFromBase = FindProperty("_ShadowPlanB2SaturationFromBase", props);
            ShadowPlanB2ValueFromBase = FindProperty("_ShadowPlanB2ValueFromBase", props);
            ShadowPlanB2UseCustomShadowTexture = FindProperty("_ShadowPlanB2UseCustomShadowTexture", props);
            ShadowPlanB2CustomShadowTexture = FindProperty("_ShadowPlanB2CustomShadowTexture", props);
            ShadowPlanB2CustomShadowTextureRGB = FindProperty("_ShadowPlanB2CustomShadowTextureRGB", props);
            UseGloss = FindProperty("_UseGloss", props);
            GlossBlend = FindProperty("_GlossBlend", props);
            GlossBlendMask = FindProperty("_GlossBlendMask", props);
            GlossPower = FindProperty("_GlossPower", props);
            GlossColor = FindProperty("_GlossColor", props);
            UseOutline = FindProperty("_UseOutline", props);
            OutlineWidth = FindProperty("_OutlineWidth", props);
            if(!isOpaque) OutlineMask = FindProperty("_OutlineMask", props);
            if(!isOpaque) OutlineCutoffRange = FindProperty("_OutlineCutoffRange", props);
            OutlineColor = FindProperty("_OutlineColor", props);
            OutlineShadeMix = FindProperty("_OutlineShadeMix", props);
            OutlineTextureColorRate = FindProperty("_OutlineTextureColorRate", props);
            OutlineWidthMask = FindProperty("_OutlineWidthMask", props);
            OutlineUseColorShift = FindProperty("_OutlineUseColorShift", props);
            OutlineHueShiftFromBase = FindProperty("_OutlineHueShiftFromBase", props);
            OutlineSaturationFromBase = FindProperty("_OutlineSaturationFromBase", props);
            OutlineValueFromBase = FindProperty("_OutlineValueFromBase", props);
            MatcapBlendMode = FindProperty("_MatcapBlendMode", props);
            MatcapBlend = FindProperty("_MatcapBlend", props);
            MatcapTexture = FindProperty("_MatcapTexture", props);
            MatcapColor = FindProperty("_MatcapColor", props);
            MatcapBlendMask = FindProperty("_MatcapBlendMask", props);
            MatcapNormalMix = FindProperty("_MatcapNormalMix", props);
            MatcapShadeMix = FindProperty("_MatcapShadeMix", props);
            UseReflection = FindProperty("_UseReflection", props);
            UseReflectionProbe = FindProperty("_UseReflectionProbe", props);
            ReflectionReflectionPower = FindProperty("_ReflectionReflectionPower", props);
            ReflectionReflectionMask = FindProperty("_ReflectionReflectionMask", props);
            ReflectionNormalMix = FindProperty("_ReflectionNormalMix", props);
            ReflectionShadeMix = FindProperty("_ReflectionShadeMix", props);
            ReflectionCubemap = FindProperty("_ReflectionCubemap", props);
            ReflectionSuppressBaseColorValue = FindProperty("_ReflectionSuppressBaseColorValue", props);
            if(isRefracted) RefractionFresnelExp = FindProperty("_RefractionFresnelExp", props);
            if(isRefracted) RefractionStrength = FindProperty("_RefractionStrength", props);
            UseRim = FindProperty("_UseRim", props);
            RimBlend = FindProperty("_RimBlend", props);
            RimBlendMask = FindProperty("_RimBlendMask", props);
            RimShadeMix = FindProperty("_RimShadeMix", props);
            RimFresnelPower = FindProperty("_RimFresnelPower", props);
            RimUpperSideWidth = FindProperty("_RimUpperSideWidth", props);
            RimColor = FindProperty("_RimColor", props);
            RimTexture = FindProperty("_RimTexture", props);
            RimUseBaseTexture = FindProperty("_RimUseBaseTexture", props);
            ShadowCapBlendMode = FindProperty("_ShadowCapBlendMode", props);
            ShadowCapBlend = FindProperty("_ShadowCapBlend", props);
            ShadowCapBlendMask = FindProperty("_ShadowCapBlendMask", props);
            ShadowCapNormalMix = FindProperty("_ShadowCapNormalMix", props);
            ShadowCapTexture = FindProperty("_ShadowCapTexture", props);
            if(isStencilWriter || isStencilReader) StencilNumber = FindProperty("_StencilNumber", props);
            if(isStencilWriterMask) StencilMaskTex = FindProperty("_StencilMaskTex", props);
            if(isStencilWriterMask) StencilMaskAdjust = FindProperty("_StencilMaskAdjust", props);
            if(isStencilWriterMask) StencilMaskAlphaDither = FindProperty("_StencilMaskAlphaDither", props);
            if(isStencilReader) StencilCompareAction = FindProperty("_StencilCompareAction", props);
            if(isStencilReaderDouble) StencilNumberSecondary = FindProperty("_StencilNumberSecondary", props);
            if(isStencilReaderDouble) StencilCompareActionSecondary = FindProperty("_StencilCompareActionSecondary", props);
            UseDoubleSided = FindProperty("_UseDoubleSided", props);
            DoubleSidedFlipBackfaceNormal = FindProperty("_DoubleSidedFlipBackfaceNormal", props);
            DoubleSidedBackfaceLightIntensity = FindProperty("_DoubleSidedBackfaceLightIntensity", props);
            ShadowCasterCulling = FindProperty("_ShadowCasterCulling", props);
            VertexColorBlendDiffuse = FindProperty("_VertexColorBlendDiffuse", props);
            VertexColorBlendEmissive = FindProperty("_VertexColorBlendEmissive", props);
            OtherShadowBorderSharpness = FindProperty("_OtherShadowBorderSharpness", props);
            OtherShadowAdjust = FindProperty("_OtherShadowAdjust", props);
            UseVertexLight = FindProperty("_UseVertexLight", props);
            LightSampling = FindProperty("_LightSampling", props);
            UsePositionRelatedCalc = FindProperty("_UsePositionRelatedCalc", props);
            if(isFade) ZWrite = FindProperty("_ZWrite", props);

            EditorGUIUtility.labelWidth = 0f;

            EditorGUI.BeginChangeCheck();
            {
                // Common
                UIHelper.ShurikenHeader("Common");
                UIHelper.DrawWithGroup(() => {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Main Texture", "Base Color Texture (RGB)"), BaseTexture, BaseColor);
                    materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map", "Normal Map (RGB)"), Normalmap, BumpScale);
                    materialEditor.TexturePropertySingleLine(new GUIContent("Emission", "Emission (RGB)"), EmissionMap, EmissionColor);

                    // materialEditor.ShaderProperty(Cull, "Cull");
                    UIHelper.DrawWithGroup(() => {
                        materialEditor.ShaderProperty(UseDoubleSided, "Is Double Sided");
                        var doublesided = UseDoubleSided.floatValue;
                        if(doublesided > 0){
                            ShadowCasterCulling.floatValue = 0;
                            EditorGUI.indentLevel ++;
                            materialEditor.ShaderProperty(DoubleSidedFlipBackfaceNormal, "Flip backface normal");
                            materialEditor.ShaderProperty(DoubleSidedBackfaceLightIntensity, "Backface Light Intensity");
                            EditorGUI.indentLevel --;
                        } else {
                            ShadowCasterCulling.floatValue = 2;
                        }
                        if(isFade) materialEditor.ShaderProperty(ZWrite, "ZWrite");
                    });
                });

                // Secondary Common
                if(isStencilReaderDouble) {
                    UIHelper.ShurikenHeader("Secondary Common");
                    UIHelper.DrawWithGroup(() => {
                        materialEditor.TexturePropertySingleLine(new GUIContent("Main Texture", "Base Color Texture (RGB)"), BaseTextureSecondary, BaseColorSecondary);
                        materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map", "Normal Map (RGB)"), NormalmapSecondary, BumpScaleSecondary);
                        materialEditor.TexturePropertySingleLine(new GUIContent("Emission", "Emission (RGB)"), EmissionMapSecondary, EmissionColorSecondary);
                    });
                }

                // AlphaMask
                if(isFade){
                    IsShowAlphaMask = UIHelper.ShurikenFoldout("AlphaMask", IsShowAlphaMask);
                    if (IsShowAlphaMask) {
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.ShaderProperty(AlphaMask, "Alpha Mask");
                        });
                    }
                }

                // Refraction
                if(isRefracted){
                    UIHelper.ShurikenHeader("Refraction");
                    UIHelper.DrawWithGroup(() => {
                        materialEditor.ShaderProperty(RefractionFresnelExp, "Fresnel Exp");
                        materialEditor.ShaderProperty(RefractionStrength, "Strength");
                    });
                }

                // Alpha Cutout
                if(isCutout){
                    UIHelper.ShurikenHeader("Alpha Cutout");
                    UIHelper.DrawWithGroup(() => {
                        materialEditor.ShaderProperty(CutoutCutoutAdjust, "Cutoff Adjust");
                    });
                }

                // Shadow
                UIHelper.ShurikenHeader("Shadow");
                UIHelper.DrawWithGroup(() => {
                    materialEditor.ShaderProperty(Shadowborder, "Border");

                    UIHelper.DrawWithGroup(() => {
                        materialEditor.TexturePropertySingleLine(new GUIContent("Strength & Mask", "Strength and Mask Texture"), ShadowStrengthMask, ShadowStrength);
                        materialEditor.TextureScaleOffsetPropertyIndent(ShadowStrengthMask);
                    });

                    UIHelper.DrawWithGroup(() => {
                        materialEditor.TexturePropertySingleLine(new GUIContent("Blur & Mask", "Blur and Mask Texture"), ShadowborderBlurMask, ShadowborderBlur);
                        materialEditor.TextureScaleOffsetPropertyIndent(ShadowborderBlurMask);
                    });

                    UIHelper.DrawWithGroup(() => {
                        materialEditor.ShaderProperty(ShadowUseStep, "Use Steps");
                        var useStep = ShadowUseStep.floatValue;
                        if(useStep > 0)
                        {
                            EditorGUI.indentLevel ++;
                            ShadowSteps.floatValue = EditorGUILayout.IntSlider(
                                new GUIContent("Steps"),
                                (int)ShadowSteps.floatValue,
                                (int)ShadowSteps.rangeLimits.x,
                                (int)ShadowSteps.rangeLimits.y)
                            ;
                            EditorGUI.indentLevel --;
                        }
                    });

                    UIHelper.DrawWithGroup(() => {
                        materialEditor.ShaderProperty(ShadowPlanBUsePlanB, "Use Custom Shade");
                        var usePlanB = ShadowPlanBUsePlanB.floatValue;
                        if(usePlanB > 0)
                        {
                            EditorGUILayout.HelpBox(
                                "[Strength] max is recommended for using custom shade." + Environment.NewLine + "Custom Shadeの使用時は[Strength]を最大値に設定することを推奨", MessageType.Info);
                            materialEditor.ShaderProperty(ShadowPlanBDefaultShadowMix, "Mix Default Shade");
                            UIHelper.DrawWithGroup(() => {
                                EditorGUILayout.LabelField("1st shade", EditorStyles.boldLabel);
                                EditorGUI.indentLevel ++;
                                materialEditor.ShaderProperty(ShadowPlanBUseCustomShadowTexture, "Use Shade Texture");
                                var useShadeTexture = ShadowPlanBUseCustomShadowTexture.floatValue;
                                if(useShadeTexture > 0)
                                {
                                    materialEditor.ShaderProperty(ShadowPlanBCustomShadowTexture, "Shade Texture");
                                    materialEditor.ShaderProperty(ShadowPlanBCustomShadowTextureRGB, "Shade Texture RGB");
                                }
                                else
                                {
                                    materialEditor.ShaderProperty(ShadowPlanBHueShiftFromBase, "Hue Shift");
                                    materialEditor.ShaderProperty(ShadowPlanBSaturationFromBase, "Saturation");
                                    materialEditor.ShaderProperty(ShadowPlanBValueFromBase, "Value");
                                }
                                EditorGUI.indentLevel --;
                            });

                            UIHelper.DrawWithGroup(() => {
                                EditorGUILayout.LabelField("2nd shade", EditorStyles.boldLabel);
                                EditorGUI.indentLevel ++;
                                materialEditor.ShaderProperty(CustomShadow2nd, "Use");
                                var customshadow2nd = CustomShadow2nd.floatValue;
                                if(customshadow2nd > 0)
                                {
                                    materialEditor.ShaderProperty(ShadowPlanB2border, "Border");
                                    materialEditor.ShaderProperty(ShadowPlanB2borderBlur, "Blur");
                                    materialEditor.ShaderProperty(ShadowPlanB2UseCustomShadowTexture, "Use Shade Texture");
                                    var useShadeTexture2 = ShadowPlanB2UseCustomShadowTexture.floatValue;
                                    if(useShadeTexture2 > 0)
                                    {
                                        materialEditor.ShaderProperty(ShadowPlanB2CustomShadowTexture,  "Shade Texture");
                                        materialEditor.ShaderProperty(ShadowPlanB2CustomShadowTextureRGB,  "Shade Texture RGB");
                                    }else{
                                        materialEditor.ShaderProperty(ShadowPlanB2HueShiftFromBase, "Hue Shift");
                                        materialEditor.ShaderProperty(ShadowPlanB2SaturationFromBase, "Saturation");
                                        materialEditor.ShaderProperty(ShadowPlanB2ValueFromBase, "Value");
                                    }
                                }
                                EditorGUI.indentLevel --;
                            });
                        }
                    });
                });

                // Gloss
                UIHelper.ShurikenHeader("Gloss");
                materialEditor.DrawShaderPropertySameLIne(UseGloss);
                var useGloss = UseGloss.floatValue;
                if(useGloss > 0)
                {
                    UIHelper.DrawWithGroup(() => {
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Smoothness & Mask", "Smoothness and Mask Texture"), GlossBlendMask, GlossBlend);
                            materialEditor.TextureScaleOffsetPropertyIndent(GlossBlendMask);
                        });
                        materialEditor.ShaderProperty(GlossPower, "Metallic");
                        materialEditor.ShaderProperty(GlossColor, "Color");
                    });
                }

                // Outline
                if(!isRefracted) {
                    UIHelper.ShurikenHeader("Outline");
                    materialEditor.DrawShaderPropertySameLIne(UseOutline);
                    var useOutline = UseOutline.floatValue;
                    if(useOutline > 0)
                    {
                        UIHelper.DrawWithGroup(() => {
                            UIHelper.DrawWithGroup(() => {
                                materialEditor.TexturePropertySingleLine(new GUIContent("Width & Mask", "Width and Mask Texture"), OutlineWidthMask, OutlineWidth);
                                materialEditor.TextureScaleOffsetPropertyIndent(OutlineWidthMask);
                            });
                            UIHelper.DrawWithGroup(() => {
                                if(!isOpaque) {
                                        materialEditor.TexturePropertySingleLine(new GUIContent("Cutoff Mask & Range", "Cutoff Mask Texture & Range"), OutlineMask, OutlineCutoffRange);
                                        materialEditor.TextureScaleOffsetPropertyIndent(OutlineMask);
                                }else{
                                    EditorGUILayout.LabelField("Cutoff Mask & Range","Unavailable in Opaque", EditorStyles.centeredGreyMiniLabel);
                                }
                            });
                            UIHelper.DrawWithGroup(() => {
                                materialEditor.ShaderProperty(OutlineColor,"Color");
                                materialEditor.ShaderProperty(OutlineTextureColorRate,"Base Color Mix");
                                materialEditor.ShaderProperty(OutlineUseColorShift, "Use Color Shift");
                                var useOutlineColorShift = OutlineUseColorShift.floatValue;
                                if(useOutlineColorShift > 0) {
                                    EditorGUI.indentLevel ++;
                                    materialEditor.ShaderProperty(OutlineHueShiftFromBase, "Hue Shift");
                                    materialEditor.ShaderProperty(OutlineSaturationFromBase, "Saturation");
                                    materialEditor.ShaderProperty(OutlineValueFromBase, "Value");
                                    EditorGUI.indentLevel --;
                                }
                            });
                            materialEditor.ShaderProperty(OutlineShadeMix,"Shadow mix");
                        });
                    }
                }

                // MatCap
                UIHelper.ShurikenHeader("MatCap");
                materialEditor.DrawShaderPropertySameLIne(MatcapBlendMode);
                var useMatcap = MatcapBlendMode.floatValue;
                if(useMatcap != 3) // Not 'Unused'
                {
                    UIHelper.DrawWithGroup(() => {
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Blend & Mask", "Blend and Mask Texture"), MatcapBlendMask, MatcapBlend);
                            materialEditor.TextureScaleOffsetPropertyIndent(MatcapBlendMask);
                        });
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Texture & Color", "Color and Texture"), MatcapTexture, MatcapColor);
                            materialEditor.TextureScaleOffsetPropertyIndent(MatcapTexture);
                        });
                        materialEditor.ShaderProperty(MatcapNormalMix, "Normal Map mix");
                        materialEditor.ShaderProperty(MatcapShadeMix,"Shadow mix");
                    });
                }

                // Reflection
                UIHelper.ShurikenHeader("Reflection");
                materialEditor.DrawShaderPropertySameLIne(UseReflection);
                var useReflection = UseReflection.floatValue;
                if(useReflection > 0)
                {
                    UIHelper.DrawWithGroup(() => {
                        materialEditor.ShaderProperty(UseReflectionProbe,"Use Reflection Probe");
                        var useProbe = UseReflectionProbe.floatValue;
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Smoothness & Mask", "Smoothness and Mask Texture"), ReflectionReflectionMask, ReflectionReflectionPower);
                            materialEditor.TextureScaleOffsetPropertyIndent(ReflectionReflectionMask);
                        });
                        UIHelper.DrawWithGroup(() => {
                            var cubemapLabel = "Cubemap";
                            if(useProbe > 0) {
                                cubemapLabel += "(fallback)";
                            }
                            materialEditor.TexturePropertySingleLine(new GUIContent(cubemapLabel, cubemapLabel), ReflectionCubemap);
                            materialEditor.TextureScaleOffsetPropertyIndent(ReflectionCubemap);
                        });
                        materialEditor.ShaderProperty(ReflectionNormalMix,"Normal Map mix");
                        materialEditor.ShaderProperty(ReflectionShadeMix, "Shadow mix");
                        materialEditor.ShaderProperty(ReflectionSuppressBaseColorValue,"Suppress Base Color");
                    });
                }

                // Rim Light
                UIHelper.ShurikenHeader("Rim");
                materialEditor.DrawShaderPropertySameLIne(UseRim);
                var useRim = UseRim.floatValue;
                if(useRim > 0)
                {
                    UIHelper.DrawWithGroup(() => {
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Blend & Mask", "Blend and Mask Texture"), RimBlendMask, RimBlend);
                            materialEditor.TextureScaleOffsetPropertyIndent(RimBlendMask);
                        });
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Texture & Color", "Texture and Color"), RimTexture, RimColor);
                            materialEditor.TextureScaleOffsetPropertyIndent(RimTexture);
                            materialEditor.ShaderProperty(RimUseBaseTexture,"Use Base Color");
                        });
                        materialEditor.ShaderProperty(RimShadeMix,"Shadow mix");
                        materialEditor.ShaderProperty(RimFresnelPower,"Fresnel Power");
                        materialEditor.ShaderProperty(RimUpperSideWidth,"Upper side width");
                    });
                }

                // Shade Cap
                UIHelper.ShurikenHeader("Shade Cap");
                materialEditor.DrawShaderPropertySameLIne(ShadowCapBlendMode);
                var useShadowCap = ShadowCapBlendMode.floatValue;
                if(useShadowCap != 3) // Not 'Unused'
                {
                    UIHelper.DrawWithGroup(() => {
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Blend & Mask", "Blend and Mask Texture"), ShadowCapBlendMask, ShadowCapBlend);
                            materialEditor.TextureScaleOffsetPropertyIndent(ShadowCapBlendMask);
                        });
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Texture", "Texture"), ShadowCapTexture);
                            materialEditor.TextureScaleOffsetPropertyIndent(ShadowCapTexture);
                        });
                        materialEditor.ShaderProperty(ShadowCapNormalMix,"Normal Map mix");
                    });
                }

                // Stencil Writer
                if(isStencilWriter)
                {
                    UIHelper.ShurikenHeader("Stencil Writer");
                    UIHelper.DrawWithGroup(() => {
                        materialEditor.ShaderProperty(StencilNumber,"Stencil Number");
                        if(isStencilWriterMask) {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Stencil Mask & Range", "Stencil Mask and Range"), StencilMaskTex, StencilMaskAdjust);
                            materialEditor.TextureScaleOffsetPropertyIndent(StencilMaskTex);
                        }
                        if(isStencilWriterMask) materialEditor.ShaderProperty(StencilMaskAlphaDither, "Alpha(Dither)");
                    });
                }

                // Stencil Reader
                if(isStencilReader)
                {
                    UIHelper.ShurikenHeader("Stencil Reader");
                    if(isStencilReaderDouble) {
                        UIHelper.DrawWithGroup(() => {
                            UIHelper.DrawWithGroup(() => {
                                EditorGUILayout.LabelField("Primary", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;
                                materialEditor.ShaderProperty(StencilNumber,"Number");
                                materialEditor.ShaderProperty(StencilCompareAction,"Compare Action");
                                EditorGUI.indentLevel--;
                            });
                            UIHelper.DrawWithGroup(() => {
                                EditorGUILayout.LabelField("Secondary", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;
                                materialEditor.ShaderProperty(StencilNumberSecondary,"Number");
                                materialEditor.ShaderProperty(StencilCompareActionSecondary,"Compare Action");
                                EditorGUI.indentLevel--;
                            });
                        });
                    } else {
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.ShaderProperty(StencilNumber,"Number");
                            materialEditor.ShaderProperty(StencilCompareAction,"Compare Action");
                        });
                    }
                }

                // Parallax Emission
                UIHelper.ShurikenHeader("Parallaxed Emission");
                materialEditor.DrawShaderPropertySameLIne(UseEmissionParallax);
                var useEmissionPara = UseEmissionParallax.floatValue;
                if(useEmissionPara > 0){
                    UIHelper.DrawWithGroup(() => {
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Texture & Color", "Texture and Color"), EmissionParallaxTex, EmissionParallaxColor);
                            materialEditor.TextureScaleOffsetPropertyIndent(EmissionParallaxTex);
                        });
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("TexCol Mask", "Texture and Color Mask"), EmissionParallaxMask);
                            materialEditor.TextureScaleOffsetPropertyIndent(EmissionParallaxMask);
                        });
                        UIHelper.DrawWithGroup(() => {
                            materialEditor.TexturePropertySingleLine(new GUIContent("Depth & Mask", "Depth and Mask Texture"), EmissionParallaxDepthMask, EmissionParallaxDepth);
                            materialEditor.TextureScaleOffsetPropertyIndent(EmissionParallaxDepthMask);
                            materialEditor.ShaderProperty(EmissionParallaxDepthMaskInvert, "Invert Depth Mask");
                        });
                    });
                }

                // Advanced / Experimental
                IsShowAdvanced = UIHelper.ShurikenFoldout("Advanced / Experimental (Click to Open)", IsShowAdvanced);
                if (IsShowAdvanced) {
                    UIHelper.DrawWithGroup(() => {
                        EditorGUILayout.HelpBox("These are some shade tweaking. no need to change usually." + Environment.NewLine + "ほとんどのケースで触る必要のないやつら。",MessageType.Info);
                        if (GUILayout.Button("Revert advanced params.")){
                            PointAddIntensity.floatValue = 1f;
                            PointShadowStrength.floatValue = 0.5f;
                            PointShadowborder.floatValue = 0.5f;
                            PointShadowborderBlur.floatValue = 0.01f;
                            PointShadowborderBlurMask.textureValue = null;
                            OtherShadowAdjust.floatValue = -0.1f;
                            OtherShadowBorderSharpness.floatValue = 3;
                            PointShadowUseStep.floatValue = 0;
                            PointShadowSteps.floatValue = 2;
                            ShadowIndirectIntensity.floatValue = 0.25f;
                            VertexColorBlendDiffuse.floatValue = 0f;
                            VertexColorBlendEmissive.floatValue = 0f;
                            UseVertexLight.floatValue = 1f;
                            material.EnableKeyword("USE_VERTEX_LIGHT");
                            LightSampling.floatValue = 0f;
                            material.EnableKeyword("_LIGHTSAMPLING_ARKTOON");
                            material.DisableKeyword("_LIGHTSAMPLING_CUBED");
                            UsePositionRelatedCalc.floatValue = 0f;
                            material.DisableKeyword("USE_POSITION_RELATED_CALC");
                        }
                        UIHelper.DrawWithGroup(() => {
                            EditorGUILayout.LabelField("Lights", EditorStyles.boldLabel);
                            EditorGUI.indentLevel ++;
                            materialEditor.ShaderProperty(LightSampling, "Sampling Style (def:arktoon)");
                            EditorGUI.indentLevel --;
                        });
                        UIHelper.DrawWithGroup(() => {
                            EditorGUILayout.LabelField("Directional Shadow", EditorStyles.boldLabel);
                            EditorGUI.indentLevel ++;
                            materialEditor.ShaderProperty(ShadowIndirectIntensity, "Indirect face Intensity (0.25)");
                            EditorGUI.indentLevel --;
                        });
                        UIHelper.DrawWithGroup(() => {
                            EditorGUILayout.LabelField("Vertex Colors", EditorStyles.boldLabel);
                            EditorGUI.indentLevel ++;
                            materialEditor.ShaderProperty(VertexColorBlendDiffuse, "Color blend to diffuse (def:0) ");
                            materialEditor.ShaderProperty(VertexColorBlendEmissive, "Color blend to emissive (def:0) ");
                            EditorGUI.indentLevel --;
                        });
                        UIHelper.DrawWithGroup(() => {
                            EditorGUILayout.LabelField("PointLights", EditorStyles.boldLabel);
                            EditorGUI.indentLevel ++;
                            materialEditor.ShaderProperty(PointAddIntensity, "Intensity (def:1)");
                            materialEditor.ShaderProperty(PointShadowStrength, "Shadow Strength (def:0.5)");
                            materialEditor.ShaderProperty(PointShadowborder, "Shadow Border (def:0.5)");
                            materialEditor.ShaderProperty(PointShadowborderBlur, "Shadow Border blur (def:0.01)");
                            materialEditor.ShaderProperty(PointShadowborderBlurMask, "Shadow Border blur Mask(def:none)");
                            materialEditor.ShaderProperty(PointShadowUseStep, "Use Shadow Steps");
                            var usePointStep = PointShadowUseStep.floatValue;
                            if(usePointStep > 0)
                            {
                                materialEditor.ShaderProperty(PointShadowSteps, " ");
                            }
                            materialEditor.ShaderProperty(UseVertexLight, "Use Per-vertex Light");
                            EditorGUI.indentLevel --;
                        });
                        UIHelper.DrawWithGroup(() => {
                            EditorGUILayout.LabelField("Shade from other meshes", EditorStyles.boldLabel);
                            EditorGUI.indentLevel ++;
                            materialEditor.ShaderProperty(OtherShadowAdjust, "Adjust (def:-0.1)");
                            materialEditor.ShaderProperty(OtherShadowBorderSharpness, "Sharpness(def:3)");
                            EditorGUI.indentLevel --;
                        });
                        UIHelper.DrawWithGroup(() => {
                            EditorGUILayout.LabelField("MatCap / ShadeCap", EditorStyles.boldLabel);
                            EditorGUI.indentLevel ++;
                            materialEditor.ShaderProperty(UsePositionRelatedCalc, "Use Position Related Calc(def: no)");
                            EditorGUI.indentLevel --;
                        });
                    });
                }

                // Arktoon version info
                string localVersion =  EditorUserSettings.GetConfigValue ("arktoon_version_local");
                string remoteVersion = EditorUserSettings.GetConfigValue ("arktoon_version_remote");

                UIHelper.ShurikenHeader("Arktoon-Shaders");
                style.alignment = TextAnchor.MiddleRight;
                style.normal.textColor = Color.black;
                EditorGUILayout.LabelField("Your Version : " + localVersion, style);

                if (!string.IsNullOrEmpty(remoteVersion))
                {
                    Version local_v = new Version(localVersion);
                    Version remote_v = new Version(remoteVersion);

                    if(remote_v > local_v)  {
                        style.normal.textColor = Color.blue;
                        EditorGUILayout.LabelField("Remote Version : " + remoteVersion, style);
                        EditorGUILayout.BeginHorizontal( GUI.skin.box );
                        {
                            style.alignment = TextAnchor.MiddleLeft;
                            EditorGUILayout.LabelField("New version available : ", style);
                            if(GUILayout.Button("Open download page."))
                            {
                                System.Diagnostics.Process.Start("https://github.com/synqark/Arktoon-Shaders/releases");
                            }
                        }
                        GUILayout.EndHorizontal();
                    } else {
                        EditorGUILayout.LabelField("Remote Version : " + remoteVersion, style);
                    }
                }

                // Docs
                UIHelper.DrawWithGroupHorizontal(() => {
                    if(GUILayout.Button("How to use (Japanese)"))
                    {
                        System.Diagnostics.Process.Start("https://synqark.github.io/Arktoon-Shaders-Doc/");
                    }
                    if(GUILayout.Button("README.md (English)"))
                    {
                        System.Diagnostics.Process.Start("https://github.com/synqark/Arktoon-Shaders/blob/master/README.md");
                    }
                });
            }
            EditorGUI.EndChangeCheck();
        }
    }

    static class UIHelper
    {
        static int HEADER_HEIGHT = 22;

        public static void DrawShaderPropertySameLIne(this MaterialEditor editor, MaterialProperty property) {
            Rect r = EditorGUILayout.GetControlRect(true,0,EditorStyles.layerMaskField);
            r.y -= HEADER_HEIGHT;
            r.height = MaterialEditor.GetDefaultPropertyHeight(property);
            editor.ShaderProperty(r, property, " ");
        }

        private static Rect DrawShuriken(string title, Vector2 contentOffset) {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.margin = new RectOffset(0, 0, 8, 0);
            style.font = new GUIStyle(EditorStyles.boldLabel).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = HEADER_HEIGHT;
            style.contentOffset = contentOffset;
            var rect = GUILayoutUtility.GetRect(16f, HEADER_HEIGHT, style);
            GUI.Box(rect, title, style);
            return rect;
        }
        public static void ShurikenHeader(string title)
        {
            DrawShuriken(title,new Vector2(6f, -2f));
        }
        public static bool ShurikenFoldout(string title, bool display)
        {
            var rect = DrawShuriken(title,new Vector2(20f, -2f));
            var e = Event.current;
            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint) {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) {
                display = !display;
                e.Use();
            }
            return display;
        }
        public static void Vector2Property(MaterialProperty property, string name)
        {
            EditorGUI.BeginChangeCheck();
            Vector2 vector2 = EditorGUILayout.Vector2Field(name,new Vector2(property.vectorValue.x, property.vectorValue.y),null);
            if (EditorGUI.EndChangeCheck())
                property.vectorValue = new Vector4(vector2.x, vector2.y);
        }
        public static void Vector4Property(MaterialProperty property, string name)
        {
            EditorGUI.BeginChangeCheck();
            Vector4 vector4 = EditorGUILayout.Vector2Field(name,property.vectorValue,null);
            if (EditorGUI.EndChangeCheck())
                property.vectorValue = vector4;
        }
        public static void Vector2PropertyZW(MaterialProperty property, string name)
        {
            EditorGUI.BeginChangeCheck();
            Vector2 vector2 = EditorGUILayout.Vector2Field(name,new Vector2(property.vectorValue.x, property.vectorValue.y),null);
            if (EditorGUI.EndChangeCheck())
                property.vectorValue = new Vector4(vector2.x, vector2.y);
        }
        public static void TextureScaleOffsetPropertyIndent(this MaterialEditor editor, MaterialProperty property)
        {
            EditorGUI.indentLevel ++;
            editor.TextureScaleOffsetProperty(property);
            EditorGUI.indentLevel --;
        }
        public static void DrawWithGroup(Action action)
        {
            EditorGUILayout.BeginVertical( GUI.skin.box );
            action();
            EditorGUILayout.EndVertical();
        }
        public static void DrawWithGroupHorizontal(Action action)
        {
            EditorGUILayout.BeginHorizontal( GUI.skin.box );
            action();
            EditorGUILayout.EndHorizontal();
        }
    }
}