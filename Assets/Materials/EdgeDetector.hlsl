void NormalEdgeDetection_float(
  // INPUTS
  float3 CenterNormal, // Normal of the center pixel
  float3 Normal1, // Normal of neighbor 1
  float3 Normal2, // Normal of neighbor 2
  float3 Normal3, // Normal of neighbor 3
  float3 Normal4, // Normal of neighbor 4
  float3 Normal5, // Normal of neighbor 5
  float3 Normal6, // Normal of neighbor 6
  float3 Normal7, // Normal of neighbor 7
  float3 Normal8, // Normal of neighbor 8
  float Threshold, // Edge sensitivity (0.1–0.5 typical)
  
  // OUTPUTS
  out float Outline // 1 = edge, 0 = no edge
)
{
  // Unpack center normal (if needed)
    CenterNormal = normalize(CenterNormal);

  // Array of neighbor normals
    float3 neighborNormals[8] =
    {
        normalize(Normal1), normalize(Normal2), normalize(Normal3), normalize(Normal4),
    normalize(Normal5), normalize(Normal6), normalize(Normal7), normalize(Normal8)
    };

    float maxDifference = 0;

  // Compare with all 8 neighbors
    for (int i = 0; i < 8; i++)
    {
    // Calculate angular difference (0 = same direction, 1 = 90° difference)
        float difference = 1.0 - dot(CenterNormal, neighborNormals[i]);
        maxDifference = max(maxDifference, difference);
    }

  // Threshold to create a binary outline
    Outline = step(Threshold, maxDifference);
}