using AnyPackageManagerCache.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Utils
{
    public abstract class JsonRewriter : ITextRewriter
    {
        public string Rewrite(string raw)
        {
            var jsonObject = JObject.Parse(raw);
            this.RewriteCore(jsonObject);
            return jsonObject.ToString();
        }

        protected abstract void RewriteCore(JObject document);
    }
}
