#ifndef MESH_CHAIN
#define MESH_CHAIN
// UNITY_SHADER_NO_UPGRADE

    half4 _Color;		// What color to tint the line
    half4 _lineSettings;	// Settings for how to shade the line - basically applying a levels filter to the gradient

    half3 _lineRadius;		// x: Element size multiplier, y: min, z: max

    struct appdata_meshChain
    {
        float4 vertex : POSITION;
        float4 texcoord : TEXCOORD0;
        float4 texcoord1 : TEXCOORD1;
        fixed4 color : COLOR;
    };

    struct meshChain_vertex
    {
        float4  pos : SV_POSITION;
        float4  uv : TEXCOORD0;
        fixed4 color : COLOR;
    };

    meshChain_vertex vert(appdata_meshChain v)
    {
        meshChain_vertex o;

        // In 5.4 and later we have stereo instance rendering so we go through the
        // ClipPos function which is aware of the proper projection matrix per-eye
        #if LINE_WORLD_SPACE
            o.pos = mul(UNITY_MATRIX_VP, v.vertex);
        #elif UNITY_VERSION < 540 
            o.pos = UnityObjectToClipPos(v.vertex);
        #else
            o.pos = UnityObjectToClipPos(v.vertex);
        #endif

        // We calculate the aspect ratio since the lines are
        // being transformed into a psuedo-screen space area
        half aspectRatio = _ScreenParams.x / _ScreenParams.y;
        
        // Determine the location of our neighbor
        #if LINE_WORLD_SPACE
            half4 neighborPos = mul(UNITY_MATRIX_VP, v.texcoord1);
        #elif UNITY_VERSION < 540 
            half4 neighborPos = UnityObjectToClipPos(v.texcoord1);
        #else
            half4 neighborPos = UnityObjectToClipPos(v.texcoord1);
        #endif

        // We calculate the distance this billboard or pipe expands at each end
        // We use this for the vertex deformation and proper non-perspective texture mapping
        #if LINE_FIXED_WIDTH
            half expandDistanceSource = clamp(_lineRadius.x * o.pos.w * .1, _lineRadius.y, _lineRadius.z) * v.texcoord.z;
            half expandDistanceDest = clamp(_lineRadius.x * neighborPos.w * .1, _lineRadius.y, _lineRadius.z) * v.texcoord.w;
        #else
            half expandDistanceSource = max(_lineRadius * .1, _lineRadius.y) * v.texcoord.z;
            half expandDistanceDest = max(_lineRadius * .1, _lineRadius.y) * v.texcoord.w;
        #endif

        // If the screen space distance between these two points is under a threshold, we are a billboard
        // Otherwise, we are a pipe
        half2 perpVec = (neighborPos.xy / neighborPos.w) - (o.pos.xy / o.pos.w);
        half pipeFlag = step(.001, length(perpVec));
        perpVec = normalize(perpVec).yx;
        perpVec.y *= -1;
        perpVec.xy *=  (2 * (v.texcoord.x - .5)) * (2 * (v.texcoord.y - .5));

        // Billboard logic
        // We billboard based off the UV's we had stored
        // Since the UV's represent each corner, we convert these into offsets 
        half2 billboardVec = 2 * (v.texcoord.xy - .5);

        // Whether this element is a billboard or a pipe is encoded in the secondary texture coordinates
        // A 0 on the u coordinate specifies using the billboard rendering mode
        // A 1 on the u coordinate specifies using the pipe rendering mode
        o.pos.x += lerp(billboardVec.x, perpVec.x, pipeFlag) * expandDistanceSource;
        o.pos.y += lerp(billboardVec.y, perpVec.y, pipeFlag) * expandDistanceSource*aspectRatio;

        // We store the w coordinate of the worldspace position separately here
        // We need to conditionally undo the perspective correction on these UV coordinates
        // And the w coordinate is needed for that
        float sizeRatio = ((expandDistanceSource + expandDistanceDest) / expandDistanceDest);
        o.uv = float4(v.texcoord.x, v.texcoord.y, pipeFlag, sizeRatio);
        o.uv.y = o.uv.y * (1.0 - pipeFlag) + .5 * pipeFlag;
        o.uv.xy *= sizeRatio;

        o.color = v.color * _Color;
        return o;
    }
            
    //----------------------------------------------
    // Function that takes an input brightness,
    // and applies the levels logic we've described
    //----------------------------------------------
    // curve.x : min range (0 - 1, based on brightness)
    // curve.y : max range (1 - 0, based on brightness)
    // curve.z : bend (.5 is linear)
    fixed applyLevels(fixed original, fixed3 curve)
    {
        // Any value less than 1/256 (the minimum the color variable can represent in the editor)
        // is clamped to 0 to prevent texture bleeding
        fixed inRange = saturate((original - curve.x) / (curve.y - curve.x)) * step(1.0 / 256.0, original);

        // We take this in-range value and apply a power function to it
        // We calculate the power from our bend value with the following logic
        // Any curve value from 0 to .5 goes from 1/32 to 1
        // Any curve value from .5 to 1 goes from 1 to 32
        // This lets us have really strong curve controls
        // Pow is not necessarily cheap, but equivalent perf to lerping
        // between a curve that bows out and one that bows in
        half bendValue = curve.z;
        fixed powValue = (saturate(-(bendValue - .5)) * 5) + (1 / (saturate(bendValue - .5) * 5 + 1));
        return pow(inRange, powValue);
    }

    half4 frag(meshChain_vertex i) : COLOR
    {
        // This used to be a texture lookup, we have now turned it into pure math
        // Undo the perspective correction
        float2 texCoord = i.uv.xy / i.uv.w;

        // Calculate the how close to the center of the billboard or pipe we are, and convert
        // that to a brightness value
        half lineAlpha = 1 - saturate(length(texCoord * 2 - float2(1.0, 1.0)));
        // Apply our curve logic and color tint
        return applyLevels(lineAlpha*i.color.a, _lineSettings.rgb) * i.color;
    }

    half4 fragInvert(meshChain_vertex i) : COLOR
    {
        // This used to be a texture lookup, we have now turned it into pure math
        // Undo the perspective correction
        float2 texCoord = i.uv.xy / i.uv.w;

        // Calculate the how close to the center of the billboard or pipe we are, and convert
        // that to a brightness value
        half lineAlpha = 1 - saturate(length(texCoord * 2 - float2(1.0, 1.0)));
        // Apply our curve logic and color tint
        return lerp(i.color, half4(1,1,1,1), 1 - applyLevels(lineAlpha * i.color.a, _lineSettings.rgb));
    }

    half4 fragAlphaMask(meshChain_vertex i) : COLOR
    {
        // This used to be a texture lookup, we have now turned it into pure math
        // Undo the perspective correction
        float2 texCoord = i.uv.xy / i.uv.w;

        // Calculate the how close to the center of the billboard or pipe we are, and convert
        // that to a brightness value
        half lineAlpha = 1 - saturate(length(texCoord * 2 - float2(1.0, 1.0)));
        lineAlpha = lerp(1, 1 - applyLevels(lineAlpha, _lineSettings.rgb), i.color.a);
        return half4(0, 0, 0, lineAlpha);
    }

    half4 fragColor(meshChain_vertex i) : COLOR
    {
        return half4(i.color.rgb,1);
    }
#endif // MESH_CHAIN
