using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Utilities
{
	public static class UnityBrandColorScheme
	{
		private static System.Random colorRandom = new System.Random();

		private static readonly List<Color> s_ColorSwatches = new List<Color>();
		private static int s_ColorSwatchRange;
		private static int s_RandomSwatchColorPosition;

		private static readonly List<GradientPair> s_Gradients = new List<GradientPair>();
		private static int s_RandomGradientPairColorAPosition;
		private static int s_RandomGradientPairColorBPosition;

		// Unity Swatches 2016
		public static Color Red;
		public static Color RedLight;
		public static Color RedDark;

		public static Color Magenta;
		public static Color MagentaLight;
		public static Color MagentaDark;

		public static Color Purple;
		public static Color PurpleLight;
		public static Color PurpleDark;

		public static Color Blue;
		public static Color BlueLight;
		public static Color BlueDark;

		public static Color Cyan;
		public static Color CyanLight;
		public static Color CyanDark;

		public static Color Teal;
		public static Color TealLight;
		public static Color TealDark;

		public static Color Green;
		public static Color GreenLight;
		public static Color GreenDark;

		public static Color Lime;
		public static Color LimeLight;
		public static Color LimeDark;

		public static Color Yellow;
		public static Color YellowLight;
		public static Color YellowDark;

		public static Color Orange;
		public static Color OrangeLight;
		public static Color OrangeDark;

		public static Color DarkBlue;
		public static Color DarkBlueLight;

		public static Color Dark;
		public static Color Darker;
		public static Color Light;

		public static List<GradientPair> gradients { get { return s_Gradients; } }

		[Serializable]
		public class GradientPair
		{
			public Color ColorA;
			public Color ColorB;

			public GradientPair(Color colorA, Color colorB)
			{
				ColorA = colorA;
				ColorB = colorB;
			}
		}

		static UnityBrandColorScheme()
		{
			SetupUnityBrandColors();
		}
		
		private static void SetupUnityBrandColors()
		{
			Red = U.Material.HexToColor("F44336");
			RedLight = U.Material.HexToColor("FFEBEE");
			RedDark = U.Material.HexToColor("B71C1C");

			Magenta = U.Material.HexToColor("E91E63");
			MagentaLight = U.Material.HexToColor("FCE4EC");
			MagentaDark = U.Material.HexToColor("880E4F");

			Purple = U.Material.HexToColor("9C27B0");
			PurpleLight = U.Material.HexToColor("F3E5F5");
			PurpleDark = U.Material.HexToColor("4A148C");

			Blue = U.Material.HexToColor("03A9F4");
			BlueLight = U.Material.HexToColor("E1F5FE");
			BlueDark = U.Material.HexToColor("01579B");

			Cyan = U.Material.HexToColor("00BCD4");
			CyanLight = U.Material.HexToColor("E0F7FA");
			CyanDark = U.Material.HexToColor("006064");

			Teal = U.Material.HexToColor("009688");
			TealLight = U.Material.HexToColor("E0F2F1");
			TealDark = U.Material.HexToColor("004D40");

			Green = U.Material.HexToColor("8AC249");
			GreenLight = U.Material.HexToColor("F1F8E9");
			GreenDark = U.Material.HexToColor("33691E");

			Lime = U.Material.HexToColor("CDDC39");
			LimeLight = U.Material.HexToColor("F9FBE7");
			LimeDark = U.Material.HexToColor("827717");

			Yellow = U.Material.HexToColor("FFEB3B");
			YellowLight = U.Material.HexToColor("FFFDE7");
			YellowDark = U.Material.HexToColor("F57F17");

			Orange = U.Material.HexToColor("FF9800");
			OrangeLight = U.Material.HexToColor("FFF3E0");
			OrangeDark = U.Material.HexToColor("E65100");

			DarkBlue = U.Material.HexToColor("222C37");
			DarkBlueLight = U.Material.HexToColor("E9EBEC");

			Dark = U.Material.HexToColor("323333");
			Darker = U.Material.HexToColor("1A1A1A");
			Light = U.Material.HexToColor("F5F8F9");

			// Set default neutral(luma) swatches
			s_ColorSwatches.Add(Red);
			s_ColorSwatches.Add(Magenta);
			s_ColorSwatches.Add(Purple);
			s_ColorSwatches.Add(Blue);
			s_ColorSwatches.Add(Cyan);
			s_ColorSwatches.Add(Teal);
			s_ColorSwatches.Add(Green);
			s_ColorSwatches.Add(Lime);
			s_ColorSwatches.Add(Yellow);
			s_ColorSwatches.Add(Orange);

			s_ColorSwatchRange = s_ColorSwatches.Count - 1;

			// Define default gradients
			s_Gradients.Add(new GradientPair(Yellow, OrangeDark));
			s_Gradients.Add(new GradientPair(Purple, Green));
			s_Gradients.Add(new GradientPair(Teal, Lime));
			s_Gradients.Add(new GradientPair(Cyan, Red));
			s_Gradients.Add(new GradientPair(Blue, Magenta));
			s_Gradients.Add(new GradientPair(Red, DarkBlue));
			s_Gradients.Add(new GradientPair(Blue, Lime));
			s_Gradients.Add(new GradientPair(Orange, Lime));
		}

		public static Color GetRandomSwatch()
		{
			var randomPosition = colorRandom.Next(s_ColorSwatchRange);
			while (s_RandomSwatchColorPosition == randomPosition)
				randomPosition = colorRandom.Next(s_ColorSwatchRange);

			var color = s_ColorSwatches[randomPosition];

			s_RandomSwatchColorPosition = randomPosition;

			return color;
		}

		public static GradientPair GetRandomGradient()
		{
			var randomPositionA = colorRandom.Next(s_ColorSwatchRange);
			var randomPositionB = colorRandom.Next(s_ColorSwatchRange);

			// Return a new random colorA that is not the same as the previous
			while (s_RandomGradientPairColorAPosition == randomPositionA)
				randomPositionA = colorRandom.Next(s_ColorSwatchRange);

			// Mandate that the second color in the gradient is not the first color
			while (randomPositionA == randomPositionB || s_RandomGradientPairColorBPosition == randomPositionB)
				randomPositionB = colorRandom.Next(s_ColorSwatchRange);

			var colorA = s_ColorSwatches[randomPositionA];
			var colorB = s_ColorSwatches[randomPositionB];

			// Set the first random color position value so it can be compared to the next gradient fetch
			s_RandomGradientPairColorAPosition = randomPositionA;
			s_RandomGradientPairColorBPosition = randomPositionB;

			return new GradientPair(colorA, colorB);
		}
	}
}
