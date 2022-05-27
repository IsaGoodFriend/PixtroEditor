// Pixel shader combines the bloom image with the original
// scene, using tweakable intensity levels and saturation.
// This is the final step in applying a bloom postprocess.

texture spriteTexture;
sampler2D spriteSampler = sampler_state {
	Texture = (spriteTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

float time, on;

struct VertexInput {
	float4 Position : POSITION0;
	float2 UVMapping : TEXCOORD0;
};
struct VertexOutput {
	float4 Position : POSITION0;
	float2 UVMapping : TEXCOORD0;
};

float rand(float2 co){
    return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
}
float rand1(float co){
    return frac(sin(dot(float2(1, co), float2(12.9898, 78.233))) * 43758.545);
}

float3 desaturate(float3 color, float saturation)
{
    // The constants 0.3, 0.59, and 0.11 are chosen because the
    // human eye is more sensitive to green light, and less to blue.
    float grey = (color[0] * 0.299) + (color[1] * 0.587) + (color[2] * 0.114);
	grey = pow(grey, 1 / 2.2);

    return lerp(color, grey, saturation);
}

float3 getcolor(float2 uv){
	
	float factorX = abs((uv[0] % 1) - 0.5) * 2;
	float factorY = abs((uv[1] % 1) - 0.5) * 2;
	factorX = pow(factorX, 1.2);
	factorY = pow(factorY, 0.8);
	
	uv[0] = floor(uv[0]);
	
	uv -= float2(0.5, 0.5);
	uv /= float2(240, 160);
	
	float3 color = tex2D(spriteSampler, uv).rgb;
	
	color = pow(color, 1 / 2.2);
	
	color = lerp(color, 0, (lerp(factorX, factorY, 0.04)) * 0.5);
	
	return color;
}

float3 screenColor(float3 c) {
	
	return desaturate(pow(c, 1.7), 0.35) * float3(1, 0.86, 0.62) * 0.7 + float3(0.03, 0.025, 0.02);
}
float3 litColor(float3 c) {
	
	return desaturate(pow(c, 0.95), 0.2) * float3(0.93, 0.98, 1);
}

float4 PixelShaderF(VertexOutput input) : COLOR0
{
	float2 pixel = input.UVMapping;
	
	pixel *= float2(240, 160);
	pixel += float2(0.5, 0.5);
	
	
	float rng = floor(pixel[1]);
	pixel[1] = lerp(pixel[1], rng, 0.08);
	
	float r = getcolor(pixel + float2(-0.35, 0)).r;
	float g = getcolor(pixel + float2(+0.00, 0)).g;
	float b = getcolor(pixel + float2(+0.35, 0)).b;
	
	float3 color = float4(r, g, b, 1);
	
	color = lerp(color, 0, rand(float2(time, rng)) * 0.02);
	
	
	if (on < 0.5)
		color = screenColor(color);
	else
		color = litColor(color);
	
	color = pow(color, 2.2);
	
	return float4(color, 1);
}

technique BloomCombine
{
    pass Pass1
    {
#if SM4
        PixelShader = compile ps_4_0_level_9_1 PixelShaderF();
#else
        PixelShader = compile ps_3_0 PixelShaderF();
#endif
    }
}
