float4 frag(VertexOutput i) : COLOR {

    i.normalDir = normalize(i.normalDir);

    float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir * lerp(1, i.faceSign, _DoubleSidedFlipBackfaceNormal));
    float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
    float3 _BumpMap_var = UnpackScaleNormal(tex2D(REF_BUMPMAP,TRANSFORM_TEX(i.uv0, REF_BUMPMAP)), REF_BUMPSCALE);
    float3 normalLocal = _BumpMap_var.rgb;
    float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals

    float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
    float3 lightColor = _LightColor0.rgb;
    float3 halfDirection = normalize(viewDirection+lightDirection);

    UNITY_LIGHT_ATTENUATION(attenuation,i, i.posWorld.xyz);
    float4 _MainTex_var = UNITY_SAMPLE_TEX2D(REF_MAINTEX, TRANSFORM_TEX(i.uv0, REF_MAINTEX));
    float3 Diffuse = (_MainTex_var.rgb*REF_COLOR.rgb);
    Diffuse = lerp(Diffuse, Diffuse * i.color,_VertexColorBlendDiffuse); // 頂点カラーを合成

    // アウトラインであればDiffuseとColorを混ぜる
    Diffuse = lerp(Diffuse, (Diffuse * _OutlineTextureColorRate + i.col * (1 - _OutlineTextureColorRate)), i.isOutline);

    #ifdef ARKTOON_CUTOUT
        clip((_MainTex_var.a * REF_COLOR.a) - _CutoutCutoutAdjust);
    #endif

    #if defined(ARKTOON_CUTOUT) || defined(ARKTOON_FADE)
        if (i.isOutline) {
            float _OutlineMask_var = UNITY_SAMPLE_TEX2D_SAMPLER(_OutlineMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _OutlineMask)).r;
            clip(_OutlineMask_var.r - _OutlineCutoffRange);
        }
    #endif

    fixed _PointShadowborderBlur_var = UNITY_SAMPLE_TEX2D_SAMPLER(_PointShadowborderBlurMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _PointShadowborderBlurMask)).r * _PointShadowborderBlur;
    float ShadowborderMin = max(0, _PointShadowborder - _PointShadowborderBlur_var/2);
    float ShadowborderMax = min(1, _PointShadowborder + _PointShadowborderBlur_var/2);

    float lightContribution = dot(lightDirection, normalDirection)*attenuation;
    float directContribution = 1.0 - ((1.0 - saturate(( (saturate(lightContribution) - ShadowborderMin)) / (ShadowborderMax - ShadowborderMin))));
    // #ifdef USE_POINT_SHADOW_STEPS
        directContribution = lerp(directContribution, min(1,floor(directContribution * _PointShadowSteps) / (_PointShadowSteps - 1)), _PointShadowUseStep);
    // #endif

    // 光の受光に関する更なる補正
    // ・LightIntensityIfBackface(裏面を描画中に変動する受光倍率)
    // ・ShadowCapのModeがLightShutterの時にかかるマスク乗算
    float additionalContributionMultiplier = 1;
    additionalContributionMultiplier *= i.lightIntensityIfBackface;

    #ifdef _SHADOWCAPBLENDMODE_LIGHT_SHUTTER
        float3 normalDirectionShadowCap = normalize(mul( float3(normalLocal.r*_ShadowCapNormalMix,normalLocal.g*_ShadowCapNormalMix,normalLocal.b), tangentTransform )); // Perturbed normals
        #ifdef USE_POSITION_RELATED_CALC
            float3 transformShadowCapViewDir = mul( UNITY_MATRIX_V, float4(viewDirection,0) ).xyz * float3(-1,-1,1) + float3(0,0,1);
            float3 transformShadowCapNormal = mul( UNITY_MATRIX_V, float4(normalDirectionShadowCap,0) ).xyz * float3(-1,-1,1);
            float3 transformShadowCapCombined = transformShadowCapViewDir * dot(transformShadowCapViewDir, transformShadowCapNormal) / transformShadowCapViewDir.z - transformShadowCapNormal;
            float2 transformShadowCap = ((transformShadowCapCombined.rg*0.5)+0.5);
        #else
            float2 transformShadowCap = (mul( UNITY_MATRIX_V, float4(normalDirectionShadowCap,0) ).xyz.rg*0.5+0.5);
        #endif
        float4 _ShadowCapTexture_var = tex2D(_ShadowCapTexture,TRANSFORM_TEX(transformShadowCap, _ShadowCapTexture));
        float4 _ShadowCapBlendMask_var = UNITY_SAMPLE_TEX2D_SAMPLER(_ShadowCapBlendMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _ShadowCapBlendMask));
        additionalContributionMultiplier *= (1.0 - ((1.0 - (_ShadowCapTexture_var.rgb))*_ShadowCapBlendMask_var.rgb)*_ShadowCapBlend);
    #endif

    directContribution *= additionalContributionMultiplier;
    float _ShadowStrengthMask_var = tex2D(_ShadowStrengthMask, TRANSFORM_TEX(i.uv0, _ShadowStrengthMask));
    float3 finalLight = saturate(directContribution + ((1 - (_PointShadowStrength * _ShadowStrengthMask_var)) * attenuation));
    float3 coloredLight = saturate(lightColor*finalLight*_PointAddIntensity);
    float3 ToonedMap = Diffuse * coloredLight;

    float3 specular = float3(0,0,0);
    float3 shadowcap = float3(1000,1000,1000);
    float3 matcap = float3(0,0,0);
    float3 RimLight = float3(0,0,0);

    #if defined(USE_OUTLINE) && !defined(ARKTOON_REFRACTED)
    if (!i.isOutline) {
    #endif
        // オプション：Gloss
        #ifdef USE_GLOSS
            float _GlossBlendMask_var = UNITY_SAMPLE_TEX2D_SAMPLER(_GlossBlendMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _GlossBlendMask));

            float gloss = _GlossBlend * _GlossBlendMask_var;
            float perceptualRoughness = 1.0 - gloss;
            float roughness = perceptualRoughness * perceptualRoughness;
            float specPow = exp2( gloss * 10.0+1.0);

            float NdotL = saturate(dot( normalDirection, lightDirection ));
            float LdotH = saturate(dot(lightDirection, halfDirection));
            float3 specularColor = _GlossPower;
            float specularMonochrome;
            float3 diffuseColor = Diffuse;
            diffuseColor = DiffuseAndSpecularFromMetallic( diffuseColor, specularColor, specularColor, specularMonochrome );
            specularMonochrome = 1.0-specularMonochrome;
            float NdotV = abs(dot( normalDirection, viewDirection ));
            float NdotH = saturate(dot( normalDirection, halfDirection ));
            float VdotH = saturate(dot( viewDirection, halfDirection ));
            float visTerm = SmithJointGGXVisibilityTerm( NdotL, NdotV, roughness );
            float normTerm = GGXTerm(NdotH, roughness);
            float specularPBL = (visTerm*normTerm) * UNITY_PI;
            #ifdef UNITY_COLORSPACE_GAMMA
                specularPBL = sqrt(max(1e-4h, specularPBL));
            #endif
            specularPBL = max(0, specularPBL * NdotL);
            #if defined(_SPECULARHIGHLIGHTS_OFF)
                specularPBL = 0.0;
            #endif
            specularPBL *= any(specularColor) ? 1.0 : 0.0;
            float3 attenColor = attenuation * _LightColor0.xyz;
            float3 directSpecular = attenColor*specularPBL*FresnelTerm(specularColor, LdotH);
            half grazingTerm = saturate( gloss + specularMonochrome );
            specular = attenuation * directSpecular * _GlossColor.rgb;
        #endif

        // オプション:ShadeCap
        #if defined(_SHADOWCAPBLENDMODE_DARKEN) || defined(_SHADOWCAPBLENDMODE_MULTIPLY)
            float3 normalDirectionShadowCap = normalize(mul( float3(normalLocal.r*_ShadowCapNormalMix,normalLocal.g*_ShadowCapNormalMix,normalLocal.b), tangentTransform )); // Perturbed normals
            #ifdef USE_POSITION_RELATED_CALC
                float3 transformShadowCapViewDir = mul( UNITY_MATRIX_V, float4(viewDirection,0) ).xyz * float3(-1,-1,1) + float3(0,0,1);
                float3 transformShadowCapNormal = mul( UNITY_MATRIX_V, float4(normalDirectionShadowCap,0) ).xyz;
                float2 transformShadowCap_old = transformShadowCapNormal.rg*0.5+0.5;
                transformShadowCapNormal  *= float3(-1,-1,1);
                float3 transformShadowCapCombined = transformShadowCapViewDir * dot(transformShadowCapViewDir, transformShadowCapNormal) / transformShadowCapViewDir.z - transformShadowCapNormal;
                float2 transformShadowCap = lerp(((transformShadowCapCombined.rg*0.5)+0.5), transformShadowCap_old, max(0,transformShadowCapNormal.z));
            #else
                float2 transformShadowCap = (mul( UNITY_MATRIX_V, float4(normalDirectionShadowCap,0) ).xyz.rg*0.5+0.5);
            #endif
            float4 _ShadowCapTexture_var = tex2D(_ShadowCapTexture,TRANSFORM_TEX(transformShadowCap, _ShadowCapTexture));
            float4 _ShadowCapBlendMask_var = UNITY_SAMPLE_TEX2D_SAMPLER(_ShadowCapBlendMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _ShadowCapBlendMask));
            shadowcap = (1.0 - ((1.0 - (_ShadowCapTexture_var.rgb))*_ShadowCapBlendMask_var.rgb)*_ShadowCapBlend);
        #endif

        // オプション：MatCap
        #if defined(_MATCAPBLENDMODE_LIGHTEN) || defined(_MATCAPBLENDMODE_ADD) || defined(_MATCAPBLENDMODE_SCREEN)
            float3 normalDirectionMatcap = normalize(mul( float3(normalLocal.r*_MatcapNormalMix,normalLocal.g*_MatcapNormalMix,normalLocal.b), tangentTransform )); // Perturbed normals
            #ifdef USE_POSITION_RELATED_CALC
                float3 transformMatcapViewDir = mul( UNITY_MATRIX_V, float4(viewDirection,0) ).xyz * float3(-1,-1,1) + float3(0,0,1);
                float3 transformMatcapNormal = mul( UNITY_MATRIX_V, float4(normalDirectionMatcap,0) ).xyz;
                float2 transformMatcap_old = transformMatcapNormal.rg*0.5+0.5;
                transformMatcapNormal *= float3(-1,-1,1);
                float3 transformMatcapCombined = transformMatcapViewDir * dot(transformMatcapViewDir, transformMatcapNormal) / transformMatcapViewDir.z - transformMatcapNormal;
                float2 transformMatcap = lerp(((transformMatcapCombined.rg*0.5)+0.5), transformMatcap_old, max(0,transformMatcapNormal.z));
            #else
                float2 transformMatcap = (mul( UNITY_MATRIX_V, float4(normalDirectionMatcap,0) ).xyz.rg*0.5+0.5);
            #endif
            float4 _MatcapTexture_var = tex2D(_MatcapTexture,TRANSFORM_TEX(transformMatcap, _MatcapTexture));
            float4 _MatcapBlendMask_var = UNITY_SAMPLE_TEX2D_SAMPLER(_MatcapBlendMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _MatcapBlendMask));
            float3 matcapResult = ((_MatcapColor.rgb*_MatcapTexture_var.rgb)*_MatcapBlendMask_var.rgb*_MatcapBlend);
            matcap = min(matcapResult, matcapResult * (coloredLight * _MatcapShadeMix));
        #endif

        // オプション：Rim
        #ifdef USE_RIM
            float _RimBlendMask_var = UNITY_SAMPLE_TEX2D_SAMPLER(_RimBlendMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _RimBlendMask));
            float4 _RimTexture_var = tex2D(_RimTexture,TRANSFORM_TEX(i.uv0, _RimTexture));
            RimLight = (
                lerp( _RimTexture_var.rgb, Diffuse, _RimUseBaseTexture )
                * pow(
                    min(1.0, 1.0 - max(0, dot(normalDirection, viewDirection)) + _RimUpperSideWidth)
                    , _RimFresnelPower
                )
                * _RimBlend
                * _RimColor.rgb
                * _RimBlendMask_var
            );
            RimLight = min(RimLight, RimLight * (coloredLight * _RimShadeMix));
        #endif
    #if defined(USE_OUTLINE) && !defined(ARKTOON_REFRACTED)
    }
    #endif

    float3 finalColor = max(ToonedMap, RimLight) + specular;

    // ShadeCapのブレンドモード
    #ifdef _SHADOWCAPBLENDMODE_DARKEN
        finalColor = min(finalColor, shadowcap);
    #elif _SHADOWCAPBLENDMODE_MULTIPLY
        finalColor = finalColor * shadowcap;
    #endif

    // MatCapのブレンドモード
    #ifdef _MATCAPBLENDMODE_LIGHTEN
        finalColor = max(finalColor, matcap);
    #elif _MATCAPBLENDMODE_ADD
        finalColor = finalColor + matcap;
    #elif _MATCAPBLENDMODE_SCREEN
        finalColor = 1-(1-finalColor) * (1-matcap);
    #endif

    #ifdef ARKTOON_FADE
        fixed _AlphaMask_var = UNITY_SAMPLE_TEX2D_SAMPLER(_AlphaMask, REF_MAINTEX, TRANSFORM_TEX(i.uv0, _AlphaMask)).r;
        fixed4 finalRGBA = fixed4(finalColor * (_MainTex_var.a * REF_COLOR.a * _AlphaMask_var),0);
    #else
        fixed4 finalRGBA = fixed4(finalColor * 1,0);
    #endif
    UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
    return finalRGBA;
}