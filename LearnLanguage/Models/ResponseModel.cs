using System.Collections.Generic;
using HtmlAgilityPack;

namespace LearnLanguage.Models
{
    public class ResponseModel
    {
        public string Url { get; set; }
        public string PageStr { get; set; }
        public string EnglishPath{ get; set; }
        public string SubtitleXml { get; set; }
        public IEnumerable<string> Inputs { get; set; }
        public IList<WordModel> Words { get; set; }
    }

    public class WordModel
    {
        public string Value { get; set; }
        public int Count { get; set; }
        public string TranslateUrl { get; set; }
    }
}