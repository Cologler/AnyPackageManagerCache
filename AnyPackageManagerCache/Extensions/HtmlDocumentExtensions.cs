using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Extensions
{
    public static class HtmlDocumentExtensions
    {
        public static string GetHtmlString(this HtmlDocument doc)
        {
            using (var writer = new StringWriter())
            {
                doc.Save(writer);
                return writer.ToString();
            }
        }
    }
}
