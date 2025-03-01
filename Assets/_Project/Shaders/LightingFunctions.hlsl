

void MainLight_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
#if defined(SHADERGRAPH_PREVIEW)
   Direction = half3(0.5, 0.5, 0);
   Color = 1;
   DistanceAtten = 1;
   ShadowAtten = 1;
#else
#if defined(SHADOWS_SCREEN)
   half4 clipPos = TransformWorldToHClip(WorldPos);
   half4 shadowCoord = ComputeScreenPos(clipPos);
#else
    half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
#endif
    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    ShadowAtten = mainLight.shadowAttenuation;
#endif
}
void DirectSpecular_half(half3 Specular, half Smoothness, half3 Direction, half3 Color, half3 WorldNormal, half3 WorldView, out half3 Out)
{
#if defined(SHADERGRAPH_PREVIEW)
   Out = 0;
#else
    Smoothness = exp2(10 * Smoothness + 1);
    WorldNormal = normalize(WorldNormal);
    WorldView = SafeNormalize(WorldView);
    Out = LightingSpecular(Color, Direction, WorldNormal, WorldView, half4(Specular, 0), Smoothness);
#endif
}
void AdditionalLights_half(half3 SpecColor, half Smoothness, half3 WorldPosition, half3 WorldNormal, half3 WorldView, out half3 Diffuse, out half3 Specular)
{
    half3 diffuseColor = 0;
    half3 specularColor = 0;

#if !defined(SHADERGRAPH_PREVIEW)
    Smoothness = exp2(10 * Smoothness + 1);
    WorldNormal = normalize(WorldNormal);
    WorldView = SafeNormalize(WorldView);
    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, WorldPosition);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
        specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);
    }
#endif

    Diffuse = diffuseColor;
    Specular = specularColor;
}



void CalculateFogReduction_float(float4 _GILightArea_0, float4 _GILightArea_1, float3 worldPosition, out float fogLevel)
{
    
    float3 delta_0 = worldPosition - _GILightArea_0.xyz;
    float distSq_0 = sqrt(dot(delta_0, delta_0));
    float fog_reduction_0 = distSq_0 > 40000 ? 0 : _GILightArea_0.w / distSq_0;
    
    float3 delta_1 = worldPosition - _GILightArea_1.xyz;
    float distSq_1 = sqrt(dot(delta_1, delta_1));
    float fog_reduction_1 = distSq_1 > 40000 ? 0 : _GILightArea_1.w / distSq_1;
    
    fogLevel = saturate(fog_reduction_0 + fog_reduction_1);
    
    
    //float3 delta = worldPosition - _GILightArea_0.xyz;
    //float distSq = dot(delta, delta);
    //fogLevel =  distSq > 40000 ? 0 : saturate(_GILightArea.w / distSq);
    
    return;
}