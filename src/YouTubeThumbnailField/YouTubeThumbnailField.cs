using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;

namespace YouTubeThumbnailField
{

    public class YouTubeThumbnailField : LinkBase
    {
        #region Properties

        private string HTMLTemplatePath
        {
            get
            {
                return Settings.GetSetting("YouTubeThumbnailField.HTMLTemplatePath");
            }
        }
        private string YouTubeUrlPattern
        {
            get
            {
                return Settings.GetSetting("YouTubeThumbnailField.YouTubeUrlPattern");
            }
        }

        private string InputId
        {
            get { return GetID("input"); }
        }

        protected XmlValue XmlValue
        {
            get
            {
                XmlValue viewStateProperty = base.GetViewStateProperty("XmlValue", null) as XmlValue;
                if (viewStateProperty == null)
                {
                    viewStateProperty = new XmlValue(string.Empty, "link");
                    this.XmlValue = viewStateProperty;
                }
                return viewStateProperty;
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.SetViewStateProperty("XmlValue", value, null);
            }
        }

        public string ItemID { get; set; }

        private static ID YoutubeTemplateId

        {
            get
            {
                Sitecore.Data.ID id = null;
                var templateId = Settings.GetSetting("YouTubeThumbnailField.TemplateId");
                if (!string.IsNullOrEmpty(templateId))
                {
                    Sitecore.Data.ID.TryParse(templateId, out id);
                    if (!Sitecore.Data.ID.IsNullOrEmpty(id))
                    {
                        return id;
                    }
                }
                return id;
            }
        }

        private ID MediaFolderId
        {
            get
            {
                Sitecore.Data.ID id = null;
                var templateId = Settings.GetSetting("YouTubeThumbnailField.MediaFolderId");
                if (!string.IsNullOrEmpty(templateId))
                {
                    Sitecore.Data.ID.TryParse(templateId, out id);
                    if (!Sitecore.Data.ID.IsNullOrEmpty(id))
                    {
                        return id;
                    }
                }
                return id;
            }
        }

        #endregion

        #region Events
        protected override void SetModified()
        {
            base.SetModified();
            if (base.TrackModified)
            {
                Sitecore.Context.ClientPage.Modified = true;
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            var literalHTML = new System.Web.UI.WebControls.Literal();
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                BuildYoutubeThumbnailFieldControl(literalHTML);
                Controls.Add(literalHTML);
            }
            else
            {
                var eventType = Sitecore.Context.ClientPage.ClientRequest.Parameters;
                if (eventType.Equals("contenteditor:save") || eventType.Contains("item:save"))
                {

                    var value = this.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (isValid(value))
                        {
                            this.XmlValue.SetAttribute("url", value);

                            var videoId = GetVideoId(value);

                            var task = Task.Run(() => SaveImage(GetImageUrl(videoId), videoId));

                            task.Wait(1);

                            var mediaId = task.Result;

                            this.XmlValue.SetAttribute("media", mediaId.ToString());


                        }
                        else
                        {
                            SheerResponse.Alert("Invalid Youtube Url.");
                            return;
                        }
                    }
                    Sitecore.Context.ClientPage.Modified = (Value != value);
                    if (value != null && value != Value)
                    {
                        Value = GetValue();
                    }
                }
            }
            base.OnLoad(e);
        }
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            string name = message.Name;
            base.HandleMessage(message);
            if (message["id"] == this.ID)
            {

                if (name != null)
                {
                    if (name == "contentyoutube:clear")
                    {
                        this.ClearLink();
                    }
                }
            }

        }

        #endregion

        #region Private Methods

        void BuildYoutubeThumbnailFieldControl(System.Web.UI.WebControls.Literal literalHTML)
        {
            try
            {
                var appDomain = AppDomain.CurrentDomain;
                var basePath = appDomain.BaseDirectory;
                var path = Path.Combine(basePath, HTMLTemplatePath);
                var html = System.IO.File.ReadAllText(path);

                this.Value = this.XmlValue.GetAttribute("url");

                var mediaId = this.XmlValue.GetAttribute("media");

                html = Regex.Replace(html, "<img([^a]|a[^l]|al[^t]|alt[^=])*?/>", @"<img id='" + this.ID + "_image' src='" + GetMediaUrl(mediaId) + @"'/>");

                literalHTML.Text = html;
            }
            catch (Exception ex)
            {
                Log.Error("Youtube Thubmnail Field - BuildYoutubeThumbnailFieldControl method caused an unhandled exception", ex, this);
            }
        }
        string GetMediaUrl(string mediaId)
        {
            var mediaItem = Client.ContentDatabase.GetItem(mediaId);
            var theURL = Sitecore.Resources.Media.MediaManager.GetMediaUrl(mediaItem);
            var mediaUrl = Sitecore.Resources.Media.HashingUtils.ProtectAssetUrl(theURL);
            return mediaUrl;
        }
        bool isValid(string url)
        {
            bool valid = false;

            if (!String.IsNullOrEmpty(url))
            {
                valid = Regex.IsMatch(url, YouTubeUrlPattern);
            }
            return valid;
        }
        string GetVideoId(string url)
        {
            var vidoeId = Regex.Match(url, YouTubeUrlPattern).Groups[1].Value;
            return vidoeId;
        }
        private void ClearLink()
        {
            if (this.Value.Length > 0)
            {
                this.SetModified();
            }
            this.XmlValue = new XmlValue(string.Empty, "link");
            this.Value = string.Empty;
            Sitecore.Context.ClientPage.ClientResponse.SetAttribute(this.ID, "value", string.Empty);

            // Clear HTML
            SheerResponse.SetAttribute(string.Concat(this.ID, "_image"), "src", string.Empty);
        }
        public override string GetValue()
        {
            return this.XmlValue.ToString();
        }
        public override void SetValue(string value)
        {
            Assert.ArgumentNotNull(value, "value");
            this.XmlValue = new XmlValue(value, "link");
            this.Value = this.XmlValue.GetAttribute("url");
        }
        private ID SaveImage(string imageUrl, string VideoId)
        {

            Sitecore.Data.ID itemId = null;
            using (WebClient webClient = new WebClient())
            {
                string filename = VideoId;
                string extension = ".jpg";

                byte[] data = webClient.DownloadData(imageUrl);
                Stream memoryStream = new MemoryStream(data);
                var options = new Sitecore.Resources.Media.MediaCreatorOptions
                {
                    FileBased = false,
                    OverwriteExisting = true,
                    Versioned = true,
                    IncludeExtensionInItemName = true,
                    Destination = Factory.GetDatabase("master").GetItem("{70DB0B88-8C39-4AD9-87BB-2F4818479439}").Paths.Path + "/" + filename,
                    Database = Factory.GetDatabase("master")
                };


                using (new SecurityDisabler())
                {
                    var creator = new Sitecore.Resources.Media.MediaCreator();
                    var mediaItem = creator.CreateFromStream(memoryStream, filename + extension, options);
                    itemId = mediaItem.ID;
                }
            }
            return itemId;
        }
        string GetImageUrl(string videoId)
        {
            return string.Format("http://i.ytimg.com/vi/{0}/mqdefault.jpg", videoId);
        }

        #endregion
    }
}
