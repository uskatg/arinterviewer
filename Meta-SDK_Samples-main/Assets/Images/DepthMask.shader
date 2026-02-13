Shader "Custom/DepthMask"
{
    SubShader
    {
        // Render before regular geometry (Geometry-1)
        Tags {"Queue" = "Geometry-1" }
        
        // Do not draw any color (invisible)
        ColorMask 0
        
        // Write to the Depth Buffer (blocking things behind it)
        ZWrite On

        Pass {}
    }
}