using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Utilities
{
    [CreateAssetMenu()]
    public class ColorScheme : ScriptableObject
    {
        [SerializeField]
        private List<GradientPair> m_GradientPairs = new List<GradientPair>();
        [SerializeField]
        private List<Color> m_Swatches = new List<Color>();

        private static List<GradientPair> s_BrandGradients = new List<GradientPair>();
        
        #region Unity Swatches 2016

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

        #endregion

        public static List<GradientPair> brandGradients { get { return s_BrandGradients; } }

        public List<GradientPair> gradientPairs { get { return m_GradientPairs; } }
        public List<Color> swatches { get { return m_Swatches; } }

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

        static ColorScheme()
        {
            SetupUnityBrandColors();
        }
        
        private static void SetupUnityBrandColors()
        {
            Red = HexToColor("F44336");
            RedLight = HexToColor("FFEBEE");
            RedDark = HexToColor("B71C1C");

            Magenta = HexToColor("E91E63");
            MagentaLight = HexToColor("FCE4EC");
            MagentaDark = HexToColor("880E4F");

            Purple = HexToColor("9C27B0");
            PurpleLight = HexToColor("F3E5F5");
            PurpleDark = HexToColor("4A148C");

            Blue = HexToColor("03A9F4");
            BlueLight = HexToColor("E1F5FE");
            BlueDark = HexToColor("01579B");

            Cyan = HexToColor("00BCD4");
            CyanLight = HexToColor("E0F7FA");
            CyanDark = HexToColor("006064");

            Teal = HexToColor("009688");
            TealLight = HexToColor("E0F2F1");
            TealDark = HexToColor("004D40");

            Green = HexToColor("8AC249");
            GreenLight = HexToColor("F1F8E9");
            GreenDark = HexToColor("33691E");

            Lime = HexToColor("CDDC39");
            LimeLight = HexToColor("F9FBE7");
            LimeDark = HexToColor("827717");

            Yellow = HexToColor("FFEB3B");
            YellowLight = HexToColor("FFFDE7");
            YellowDark = HexToColor("F57F17");

            Orange = HexToColor("FF9800");
            OrangeLight = HexToColor("FFF3E0");
            OrangeDark = HexToColor("E65100");

            DarkBlue = HexToColor("222C37");
            DarkBlueLight = HexToColor("E9EBEC");

            Dark = HexToColor("323333");
            Darker = HexToColor("1A1A1A");
            Light = HexToColor("F5F8F9");
        }

        private static Color HexToColor(string hexToColor)
        {
            //hex strings should be formatted "FFFFFF"
            byte a = 255;
            byte r = byte.Parse(hexToColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hexToColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hexToColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            //alpha if hex has expected length
            if (hexToColor.Length == 8)
                a = byte.Parse(hexToColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r, g, b, a);
        }
    }
}
