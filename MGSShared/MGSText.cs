using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MGSShared
{
    class MGSText
    {
        private static string[] Table;

        #region "Character Table"
        public static void Initialize()
        {
//unused
        }
        #endregion

        public static string Buffer2Text(byte[] Data, MGSGame Game)
        {
            switch (Game)
            {
                case MGSGame.MGS3: return MGS2Text(Encoding.UTF8.GetString(Data));
                 case MGSGame.MGS4: return MGS2Text(Encoding.UTF8.GetString(Data));
            }

            return null;
        }

        private static string MGS2Text(string Text)
        {
            Text = Text.Replace(((char)0xa).ToString(), "|");
            Text = Text.Replace(((char)0).ToString(), "[end]");

            return Text;
        }

        public static byte[] Text2Buffer(string Text, MGSGame Game)
        {
            switch (Game)
            {
                case MGSGame.MGS3:return Encoding.UTF8.GetBytes(Text2MGS(Text));
                case MGSGame.MGS4: return Encoding.UTF8.GetBytes(Text2MGS(Text));
            }

            return null;
        }

        private static string Text2MGS(string Text)
        {
            Text = Text.Replace("|", ((char)0xa).ToString());
            Text = Text.Replace("[end]", ((char)0).ToString());

            return Text;
        }
    }
}
