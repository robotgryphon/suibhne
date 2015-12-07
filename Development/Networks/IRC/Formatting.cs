using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Services.Irc {

    /// <summary>
    /// Used to handle and make formatting IRC messages and responses easier.
    /// Handles all formatting, including bold, italics, underlining,
    /// as well as colors (both foreground and backgrounds)
    /// </summary>
    public struct Formatting {

        /// <summary>
        /// Makes all text after appear bold.
        /// </summary>
        public static String Bold = "\u0002";

        /// <summary>
        /// If supported, makes all text after appear italic.
        /// </summary>
        public static String Italic = "\u001D";

        /// <summary>
        /// Makes all text after appear underlined.
        /// </summary>
        public static String Underline = "\u001F";

        /// <summary>
        /// Returns text formatting to clear after code.
        /// </summary>
        public static String Normal = "\u000f";

        /// <summary>
        /// Used to color text. You should probably use GetColoredText() for this.
        /// </summary>
        public static String ColorPrefix = "\u0003";

        /// <summary>
        /// Get a specific color code number.
        /// </summary>
        public enum Colors : byte {

            /// <summary>
            /// White color code.
            /// </summary>
            White = 0,

            /// <summary>
            /// Black color code.
            /// </summary>
            Black,

            /// <summary>
            /// Blue color code.
            /// </summary>
            Blue,

            /// <summary>
            /// Green color code.
            /// </summary>
            Green,

            /// <summary>
            /// Red color code.
            /// </summary>
            Red,

            /// <summary>
            /// Brown color code.
            /// </summary>
            Brown,

            /// <summary>
            /// Purple/Violet color code.
            /// </summary>
            Purple,

            /// <summary>
            /// Orange color code.
            /// </summary>
            Orange,

            /// <summary>
            /// Yellow color code.
            /// </summary>
            Yellow,

            /// <summary>
            /// Light Green / Lime color code.
            /// </summary>
            LightGreen,

            /// <summary>
            /// Teal color code.
            /// </summary>
            Teal, 

            /// <summary>
            /// Light Cyan color code.
            /// </summary>
            LightCyan, 

            /// <summary>
            /// Light Blue color code.
            /// </summary>
            LightBlue,

            /// <summary>
            /// Pink color code.
            /// </summary>
            Pink,

            /// <summary>
            /// Grey color code.
            /// </summary>
            Grey,

            /// <summary>
            /// Light Grey color code.
            /// </summary>
            LightGrey,

            /// <summary>
            /// Unknown or used to clear color.
            /// </summary>
            None
        }

        /// <summary>
        /// Used to get a proper color code (appends a zero if needed)
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string GetColorCode(Colors code) {
            byte codeByte = (byte)code;
            return (codeByte < 10) ? "0" + codeByte.ToString() : codeByte.ToString();
        }

        /// <summary>
        /// Returns a string wrapped with a set of color code control
        /// characters. It will end the string with a Normal character
        /// code for easy use.
        /// </summary>
        /// <param name="text">The text to colorize.</param>
        /// <param name="foreground">The font color to use for the text.</param>
        /// <param name="background">The background color to use for the text.</param>
        public static string GetColoredText(String text, Colors foreground, Colors background = Colors.None) {
            return ColorPrefix +
                ((foreground != Colors.None) ? GetColorCode(foreground) : "") +
                ((background != Colors.None) ? GetColorCode(background) : "") +
                text +
                Normal;
        }

        /// <summary>
        /// Used to format a string of text to make it bold, italic, and/or underlined.
        /// </summary>
        /// <param name="text">Text to format.</param>
        /// <param name="bold">Whether or not text should be bold.</param>
        /// <param name="underline">Whether or not text should be underlined.</param>
        /// <param name="italic">Whether or not text should be italicised/</param>
        public static string GetFormattedText(String text, Boolean bold = false, Boolean underline = false, Boolean italic = false) {
            return (bold ? Bold : "") + (italic ? Italic : "") + (underline ? Underline : "") + text + Normal;
        }

    }
}
