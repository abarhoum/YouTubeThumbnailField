using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Mvc.Helpers;
using Sitecore.Resources.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace YouTubeThumbnailField.Extensions
{
    public static class HtmlHelperExtensions
    {
        public static HtmlString YouTubeField(this SitecoreHelper helper, string fieldName, Item item)
        {
            if (item == null)
                return new HtmlString("no item");

            var xml = new XmlDocument();
            xml.LoadXml(item[fieldName]);

            if (xml.DocumentElement == null) return new HtmlString("field is empty.");

            var videoUrl = xml.DocumentElement.HasAttribute("url") ? xml.DocumentElement.GetAttribute("url") : string.Empty;
            var mediaId = xml.DocumentElement.HasAttribute("media") ? xml.DocumentElement.GetAttribute("media") : string.Empty;

            var mediaItem = item.Database.GetItem(mediaId);
            
            var imageSrc = MediaManager.GetMediaUrl(mediaItem);
            
            var builder = new TagBuilder("img");
            builder.Attributes.Add("src", imageSrc);
            builder.Attributes.Add("video", videoUrl);

            return MvcHtmlString.Create(builder.ToString(TagRenderMode.Normal));
        }
    }
}