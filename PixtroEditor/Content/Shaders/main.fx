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

struct VertexInput {
	float4 Position : POSITION0;
	float2 UVMapping : TEXCOORD0;
};
struct VertexOutput {
	float4 Position : POSITION0;
	float2 UVMapping : TEXCOORD0;
};

float4 getframe(float2 uv){
	
	float4 color = tex2D(spriteSampler, uv).bgra;
	
	return color;
}

float4 PixelShaderF(VertexOutput input) : COLOR0
{
	return getframe(input.UVMapping);
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
