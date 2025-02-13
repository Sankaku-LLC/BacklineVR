void ToonAdditionalLights_half(half3 SpecColor, half Smoothness, half3 WorldPosition, half3 WorldNormal, half3 WorldView, half3 BaseColor, half3 SecondColor, half Feather, half Step, out half3 Diffuse, out half3 Specular)
{
    half3 diffuseColor = 0;
    half3 specularColor = 0;

#if !defined(SHADERGRAPH_PREVIEW)
    Smoothness = exp2(10 * Smoothness + 1);
    WorldNormal = normalize(WorldNormal);
    WorldView = SafeNormalize(WorldView);
    uint lightsCount = GetAdditionalLightsCount();
#if USE_FORWARD_PLUS
    for (int i = 0; i < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++i)
    {
        Light light = GetAdditionalLight(i, WorldPosition);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        half halfLambert = dot(WorldNormal, light.direction) * 0.5 + 0.5;
        diffuseColor += lerp(BaseColor, SecondColor, saturate(1 - (halfLambert - Step - Feather) / Feather)) * attenuatedLightColor;
        specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);
    }
#endif
    
    InputData inputData = (InputData) 0;
    float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPosition));
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;
    inputData.positionWS = WorldPosition;
    LIGHT_LOOP_BEGIN(lightsCount)

    Light light = GetAdditionalLight(lightIndex, WorldPosition);
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half halfLambert = dot(WorldNormal, light.direction) * 0.5 + 0.5;
    diffuseColor += lerp(BaseColor, SecondColor, saturate(1 - (halfLambert - Step - Feather) / Feather)) * attenuatedLightColor;
    specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);
    LIGHT_LOOP_END
    
    
#endif

    Diffuse = diffuseColor;
    Specular = specularColor;
}