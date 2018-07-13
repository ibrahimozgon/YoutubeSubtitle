using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using HtmlAgilityPack;
using YoutubeSubtitleResolver.Models;
using Microsoft.AspNetCore.Mvc;

namespace YoutubeSubtitleResolver.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index(string url)
        {
            if (string.IsNullOrEmpty(url))
                return View();

            var model = await GetModel(url);
            return View(model);
        }

        private async Task<ResponseModel> GetModel(string url)
        {
            var model = new ResponseModel
            {
                Url = url.Replace("watch?v=", "embed/"),
                PageStr = await GetPageHtml(url)
            };
            if (string.IsNullOrEmpty(model.PageStr))
                return model;

            model.EnglishPath = GetEnglishSubtitlePath(model.PageStr);
            if (string.IsNullOrEmpty(model.EnglishPath))
                return model;

            model.SubtitleXml = await GetSubstitleHtml(model.EnglishPath);

            model.Words = GetDifferentWords(model.SubtitleXml);

            return model;
        }

        private IList<WordModel> GetDifferentWords(string modelSubtitleXml)
        {
            // parse xml stream
            var xmlDoc = new XmlDocument();
            modelSubtitleXml = HttpUtility.HtmlDecode(modelSubtitleXml);
            xmlDoc.LoadXml(modelSubtitleXml);

            var textList = xmlDoc.DocumentElement?.SelectNodes("//text");
            var words = new List<string>();
            if (textList == null)
                return null;

            foreach (XmlNode textItem in textList)
            {
                var textInnerText = textItem.InnerText;
                words.AddRange(PrepareWord(textInnerText));
            }
            return words
                .GroupBy(s => s)
                .OrderBy(s => s.Key)
                .Select(s => new WordModel
                {
                    Value = s.Key,
                    Count = s.Count(),
                    TranslateUrl = PrepareTranslateUrl(s.Key),

                }).ToList();
        }

        private static string PrepareTranslateUrl(string key)
        {
            return $"http://tureng.com/en/turkish-english/{HttpUtility.HtmlEncode(key)}";
        }

        private static IEnumerable<string> PrepareWord(string text)
        {
            return HttpUtility.HtmlDecode(text)?
                .Replace("\n", " ")
                .Replace(",", "")
                .Replace(".", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("/", "")
                .Replace(Environment.NewLine, " ")
                .Split(" ")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Where(s => s.Length > 1)
                .Where(s => !int.TryParse(s, out _));
        }

        private static async Task<string> GetSubstitleHtml(string url)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }

        }

        private static async Task<string> GetPageHtml(string url)
        {
            using (var client = new HttpClient())
            {
                var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("url", url)
                };
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("http://www.lilsubs.com", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private static string GetEnglishSubtitlePath(string pageHtml)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageHtml);

            var inputs = htmlDoc.DocumentNode.Descendants("button");
            var submits = inputs.Where(s => s.Attributes["type"].Value == "submit");
            var english = submits.Where(s => s.InnerText.Contains("English")).OrderBy(s => s.Line).FirstOrDefault();
            return english?.Attributes["value"].Value;
        }
    }
}
