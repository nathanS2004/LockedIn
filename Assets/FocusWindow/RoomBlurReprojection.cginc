// RGB capture is not native compositor passthrough. Require depth and project
// each eye's surface point into that eye's calibrated, capture-time RGB camera.
UNITY_DECLARE_TEX2DARRAY(_RoomDepth);
sampler2D _RoomLeft, _RoomRight, _RoomAmbient;
float4x4 _RoomDepthProjection[2], _RoomDepthInverse[2];
float4x4 _RoomWorldToLeft, _RoomWorldToRight;
float4 _RoomLeftIntrinsics, _RoomRightIntrinsics;
float _RoomBlurMode, _RoomBlurReady, _RoomBlurMix;

float4 RoomAmbientFill(float3 ray)
{
    // Both eyes share this low-detail spherical color field. It approximates
    // unseen surroundings; it is not a second, incorrectly aligned camera view.
    float3 direction = normalize(mul((float3x3)_RoomWorldToLeft, ray));
    float2 uv = float2(0.5 + atan2(direction.x, direction.z) / (2 * UNITY_PI),
                      0.5 + asin(clamp(direction.y, -1, 1)) / UNITY_PI);
    float3 color = tex2D(_RoomAmbient, uv).rgb;
    return float4(color * _RoomBlurMix, _RoomBlurMix);
}

float4 RoomBlur(float3 eye, float3 ray)
{
    if (_RoomBlurReady < 0.5) return 0;
    float4 ambient = RoomAmbientFill(ray);
    int index = 0;
    #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(UNITY_SINGLE_PASS_STEREO)
        index = unity_StereoEyeIndex;
    #endif
    float3 roomPoint = eye + ray * 2;
    // Iterative eye-to-depth reprojection. Reject holes/disocclusions instead of
    // guessing a flat room plane or stretching camera pixels across the view.
    [unroll] for (int stepIndex = 0; stepIndex < 4; stepIndex++)
    {
        float4 projected = mul(_RoomDepthProjection[index], float4(roomPoint, 1));
        if (projected.w <= 0.00001) return ambient;
        float2 depthUv = (projected.xy / projected.w + 1) * 0.5;
        if (any(depthUv < 0.01) || any(depthUv > 0.99)) return ambient;
        float depth = UNITY_SAMPLE_TEX2DARRAY_LOD(_RoomDepth, float3(depthUv, index), 0).r;
        if (!(depth > 0.00001 && depth < 0.99999)) return ambient;
        float4 world = mul(_RoomDepthInverse[index], float4(depthUv * 2 - 1, depth * 2 - 1, 1));
        if (abs(world.w) < 0.00001) return ambient;
        float3 surface = world.xyz / world.w;
        float distance = dot(surface - eye, ray);
        if (!(distance >= 0.15 && distance <= 15)) return ambient;
        float residual = length(surface - (eye + ray * distance));
        if (stepIndex == 3 && residual > 0.015 + distance * 0.005) return ambient;
        roomPoint = eye + ray * distance;
    }
    float3 cameraPoint;
    float4 intrinsics;
    if (index == 0)
    {
        cameraPoint = mul(_RoomWorldToLeft, float4(roomPoint, 1)).xyz;
        intrinsics = _RoomLeftIntrinsics;
    }
    else
    {
        cameraPoint = mul(_RoomWorldToRight, float4(roomPoint, 1)).xyz;
        intrinsics = _RoomRightIntrinsics;
    }
    if (cameraPoint.z <= 0.01) return ambient;
    float2 uv = cameraPoint.xy / cameraPoint.z * intrinsics.xy + intrinsics.zw;
    // Keep the Gaussian footprint in-bounds and fade into the ambient fill.
    float edge = min(min(uv.x, uv.y), min(1 - uv.x, 1 - uv.y));
    float coverage = smoothstep(0.06, 0.22, edge);
    if (coverage <= 0) return ambient;
    float3 color = index == 0 ? tex2D(_RoomLeft, uv).rgb : tex2D(_RoomRight, uv).rgb;
    return lerp(ambient, float4(color * _RoomBlurMix, _RoomBlurMix), coverage);
}
