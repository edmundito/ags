#include <metal_stdlib>
using namespace metal;

// Vertex shader input
struct VertexInput {
    float2 position [[attribute(0)]];
    float2 texCoord [[attribute(1)]];
};

// Vertex shader output / Fragment shader input
struct RasterizerData {
    float4 position [[position]];
    float2 texCoord;
};

// Uniforms for transformation
struct Uniforms {
    float4x4 modelViewProjection;
};

// Vertex shader
vertex RasterizerData vertexShader(const VertexInput vIn [[stage_in]],
                                  constant Uniforms &uniforms [[buffer(1)]]) {
    RasterizerData out;
    out.position = uniforms.modelViewProjection * float4(vIn.position, 0.0, 1.0);
    out.texCoord = vIn.texCoord;
    return out;
}

// Basic texture fragment shader
fragment float4 textureShader(RasterizerData in [[stage_in]],
                            texture2d<float> texture [[texture(0)]],
                            sampler textureSampler [[sampler(0)]]) {
    return texture.sample(textureSampler, in.texCoord);
}

// Transparency fragment shader
fragment float4 transparencyShader(RasterizerData in [[stage_in]],
                                 texture2d<float> texture [[texture(0)]],
                                 sampler textureSampler [[sampler(0)]],
                                 constant float &alpha [[buffer(0)]]) {
    float4 color = texture.sample(textureSampler, in.texCoord);
    return float4(color.rgb, color.a * alpha);
}

// Tint fragment shader
struct TintParams {
    float3 tintHSV;
    float tintAmount;
    float tintLuminance;
    float alpha;
};

// RGB to HSV conversion
float3 rgb2hsv(float3 c) {
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = mix(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = mix(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                 d / (q.x + e),
                 q.x);
}

// HSV to RGB conversion
float3 hsv2rgb(float3 c) {
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

fragment float4 tintShader(RasterizerData in [[stage_in]],
                          texture2d<float> texture [[texture(0)]],
                          sampler textureSampler [[sampler(0)]],
                          constant TintParams &params [[buffer(0)]]) {
    float4 texColor = texture.sample(textureSampler, in.texCoord);
    float3 hsv = rgb2hsv(texColor.rgb);
    
    // Apply tint
    hsv.x = params.tintHSV.x;
    hsv.y = mix(hsv.y, params.tintHSV.y, params.tintAmount);
    hsv.z = mix(hsv.z, params.tintHSV.z * params.tintLuminance, params.tintAmount);
    
    float3 rgb = hsv2rgb(hsv);
    return float4(rgb, texColor.a * params.alpha);
}

// Light shader
struct LightParams {
    float4 lightColor;
    float lightAmount;
};

fragment float4 lightShader(RasterizerData in [[stage_in]],
                           texture2d<float> texture [[texture(0)]],
                           sampler textureSampler [[sampler(0)]],
                           constant LightParams &params [[buffer(0)]]) {
    float4 texColor = texture.sample(textureSampler, in.texCoord);
    return mix(texColor, params.lightColor * texColor.a, params.lightAmount);
} 