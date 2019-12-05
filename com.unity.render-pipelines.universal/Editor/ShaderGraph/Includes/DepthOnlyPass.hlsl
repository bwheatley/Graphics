﻿#ifndef SG_DEPTH_ONLY_PASS_INCLUDED
#define SG_DEPTH_ONLY_PASS_INCLUDED

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

#if defined(FEATURES_GRAPH_PIXEL)
    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
#endif

    half alpha = 1;
    half clipThreshold = 0.5;

#ifdef OUTPUT_SURFACEDESCRIPTION_ALPHA
    alpha = surfaceDescription.Alpha;
#endif
#ifdef OUTPUT_SURFACEDESCRIPTION_ALPHACLIPTHRESHOLD
    clipThreshold = surfaceDescription.AlphaClipThreshold;
#endif

#if _AlphaClip
    clip(alpha - clipThreshold);
#endif

    return 0;
}

#endif
