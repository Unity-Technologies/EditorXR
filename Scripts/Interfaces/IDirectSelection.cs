using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.VR.Proxies;

public class DirectSelection
{
	public Node node;
	public GameObject gameObject;
}
public interface IDirectSelection
{
	Func<Dictionary<Transform, DirectSelection>> getDirectSelection { set; }
}