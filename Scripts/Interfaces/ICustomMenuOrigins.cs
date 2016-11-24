using UnityEngine;
using System;

public interface ICustomMenuOrigins
{
	Func<Transform, Transform> customMenuOrigin { set; }
	
	Func<Transform, Transform> customAlternateMenuOrigin { set; }
}
