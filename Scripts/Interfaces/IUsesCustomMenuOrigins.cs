using UnityEngine;
using System;

public interface IUsesCustomMenuOrigins
{
	Func<Transform, Transform> customMenuOrigin { set; }
	
	Func<Transform, Transform> customAlternateMenuOrigin { set; }
}
