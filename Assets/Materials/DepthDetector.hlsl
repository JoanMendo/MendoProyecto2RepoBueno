void DepthEdgeDetection_float(
  // INPUTS
  float CenterDepth, // Depth of the center pixel (linear depth)
  float Depth1, // Depth of neighbor 1
  float Depth2, // Depth of neighbor 2
  float Depth3, // Depth of neighbor 3
  float Depth4, // Depth of neighbor 4
  float Depth5, // Depth of neighbor 5
  float Depth6, // Depth of neighbor 6
  float Depth7, // Depth of neighbor 7
  float Depth8, // Depth of neighbor 8
  float Threshold, // Edge sensitivity (adjust based on scene scale)
  
  // OUTPUTS
  out float Outline // 1 = edge, 0 = no edge
)
{
  // Array of neighbor depths
    float neighborDepths[8] = { Depth1, Depth2, Depth3, Depth4, Depth5, Depth6, Depth7, Depth8 };

    float maxDifference = 0;

  // Compare with all 8 neighbors
    for (int i = 0; i < 8; i++)
    {
    // Calculate absolute depth difference
        float difference = abs(CenterDepth - neighborDepths[i]);
        maxDifference = max(maxDifference, difference);
    }

  // Threshold to create a binary outline
    Outline = step(Threshold, maxDifference);
}