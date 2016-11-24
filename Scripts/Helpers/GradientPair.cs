using System;

namespace UnityEngine.VR.Helpers
{
	/// <summary>
	/// Gradient pair container class
	/// </summary>
	[Serializable]
	public struct GradientPair
	{
		public Color a;
		public Color b;

		public GradientPair(Color a, Color b)
		{
			this.a = a;
			this.b = b;
		}

		public static GradientPair Lerp(GradientPair x, GradientPair y, float t)
		{
			GradientPair r;
			r.a = Color.Lerp(x.a, y.a, t);
			r.b = Color.Lerp(x.b, y.b, t);
			return r;
		}
	}
}
