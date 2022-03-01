using System.Collections.Generic;

namespace Discord_Driver_Bot.Gallery.Host.EHentai
{
    static class TagTranslation
    {       
        public static List<DatumElement> TranslationData { get; set; }
        
        public static string FormatNameMarkDown(this string text)
        {
            if (text.Contains(")"))
            {
                int index = text.IndexOf(')') + 1;
                return text.Substring(index, text.Length - index);
            }
            return text;
        }

        public static string GetTranslatedTag(string nameSpace, string sourceTag, bool isFormatMarkDown = false)
        {
            try
            {
                string translatedTag = TranslationData.Find((x) => x.Namespace == nameSpace).Data[sourceTag].Name;
                return isFormatMarkDown ? translatedTag.FormatNameMarkDown() : translatedTag;
            }
            catch (System.Exception) { return sourceTag; }
        }

        public partial class DataBase
        {
            public List<DatumElement> Data { get; set; }
        }

        public partial class DatumElement
        {
            public string Namespace { get; set; }
            public long Count { get; set; }
            public Dictionary<string, DatumValue> Data { get; set; }
        }

        public partial class DatumValue
        {
            public string Name { get; set; }
            public string Intro { get; set; }
            public string Links { get; set; }
        }
    }
}
