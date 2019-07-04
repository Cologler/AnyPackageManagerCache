using AnyPackageManagerCache.Extensions;
using HtmlAgilityPack;

namespace AnyPackageManagerCache.Utils
{
    public abstract class HtmlRewriter : ITextRewriter
    {
        public string Rewrite(string raw)
        {
            var document = new HtmlDocument();
            document.LoadHtml(raw);
            this.RewriteCore(document);
            return document.GetHtmlString();
        }

        protected abstract void RewriteCore(HtmlDocument document);
    }
}
