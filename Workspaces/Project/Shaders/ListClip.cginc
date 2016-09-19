#ifndef LIST_CLIP_INCLUDED
#define LIST_CLIP_INCLUDED

float4x4 _ParentMatrix;
float4 _ClipExtents;

float3 listClipLocalPos(float4 vertex)
{
	return mul(_ParentMatrix, mul(UNITY_MATRIX_M, vertex)).xyz;
}

void listClipVert(inout appdata_full v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);
	o.localPos = listClipLocalPos(v.vertex);
}

void listClipCheckExtents(float3 localPos)
{
	// Clip if position is outside of clip bounds
	float3 diff = abs(localPos);
	if (diff.x > _ClipExtents.x || diff.y > _ClipExtents.y || diff.z > _ClipExtents.z)
		discard;
}

#endif // LIST_CLIP_INCLUDED