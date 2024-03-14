void Triplaner_float(float3 Position,float3 Normal, float3 Tile,UnityTexture2D _a, UnityTexture2D _b, UnityTexture2D _c, float Blend,UnitySamplerState Sampler,  out float3 Out){
    float3 Node_UV = Position * Tile;
float3 Node_Blend = pow(abs(Normal), Blend);
Node_Blend /= dot(Node_Blend, 1.0);
float3 Node_X = SAMPLE_TEXTURE2D(_a, Sampler, Node_UV.zy).xyz;
float3 Node_Y = SAMPLE_TEXTURE2D(_b, Sampler, Node_UV.xz).xyz;
float3 Node_Z = SAMPLE_TEXTURE2D(_c, Sampler, Node_UV.xy).xyz;
 Out = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
}