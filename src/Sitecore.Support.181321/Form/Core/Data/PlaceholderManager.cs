using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Layouts;
using Sitecore.Text;
using Sitecore.Web;
using System;

namespace Sitecore.Support.Form.Core.Data
{
    public class PlaceholderManager
    {
        internal static readonly string allowedRenderingKey = "sc_allowrend";

        internal static readonly string onlyNamesPlaceholdersKey = "onlyNames";

        internal static readonly string placeholderQueryKey = "sc_placeholder";

        internal static readonly string selectedPlaceholderKey = "sc_selectedplaceholder";

        private static readonly string placeholderRequestKey = "&screquestplacholderid=";

        private readonly string deviceID;

        private readonly Item item;

        public string AllowedRendering
        {
            get;
            private set;
        }

        public DeviceDefinition Device
        {
            get
            {
                return this.Layouts.GetDevice(this.deviceID);
            }
        }

        public LayoutDefinition Layouts
        {
            get
            {
                return LayoutDefinition.Parse(this.item.Fields["__renderings"].Value);
            }
        }

        public string PlaceholdersUrl
        {
            get
            {
                UrlString urlString = new UrlString("/default.aspx");
                urlString.Append("sc_database", StaticSettings.ContextDatabase.Name);
                urlString.Append("sc_itemid", this.item.ID.ToString());
                urlString.Append("sc_duration", "temporary");
                urlString.Append("sc_mode", "preview");
                urlString.Append(PlaceholderManager.allowedRenderingKey, this.AllowedRendering ?? string.Empty);
                urlString.Append(PlaceholderManager.selectedPlaceholderKey, this.SelectedPlaceholder ?? string.Empty);
                urlString.Append(PlaceholderManager.placeholderQueryKey, "1");
                urlString.Append("sc_device", this.deviceID);
                return WebUtil.GetServerUrl() + PlaceholderManager.Sign(urlString + this.GetDeviceQueryString());
            }
        }

        public string SelectedPlaceholder
        {
            get;
            private set;
        }

        public PlaceholderManager(Item item, string deviceID, string allowedRendering, string selectedPlaceholder)
        {
            this.item = item;
            this.deviceID = deviceID;
            this.AllowedRendering = allowedRendering;
            this.SelectedPlaceholder = selectedPlaceholder;
        }

        public static string GetPlaceholdersUrl(Item item, string deviceID, string allowedRendering, string selectedPlaceholder)
        {
            PlaceholderManager placeholderManager = new PlaceholderManager(item, deviceID, allowedRendering, selectedPlaceholder);
            return placeholderManager.PlaceholdersUrl;
        }

        public string GetDeviceQueryString()
        {
            Item item = this.item.Database.GetItem(this.deviceID);
            if (item != null && !string.IsNullOrEmpty(item["query string"]))
            {
                return string.Join(string.Empty, new string[]
                {
                    "&",
                    item["query string"]
                });
            }
            return string.Empty;
        }

        public LayoutItem LayoutByID(string id)
        {
            return this.item.Database.GetItem(id);
        }

        internal static string GetOnlyPlaceholderNamesUrl(string baseUrl)
        {
            if (!string.IsNullOrEmpty(baseUrl))
            {
                UrlString urlString = new UrlString(baseUrl);
                urlString.Append(PlaceholderManager.placeholderQueryKey, "1");
                urlString.Append(PlaceholderManager.onlyNamesPlaceholdersKey, "1");
                urlString.Append("sc_database", StaticSettings.ContextDatabase.Name);
                urlString.Append(PlaceholderManager.allowedRenderingKey, IDs.FormInterpreterID + "|" + IDs.FormMvcInterpreterID);
                return WebUtil.GetServerUrl() + PlaceholderManager.Sign(urlString.ToString());
            }
            return string.Empty;
        }

        internal static bool IsValid(string url)
        {
            Assert.ArgumentNotNullOrEmpty(url, "url");
            int num = url.IndexOf(PlaceholderManager.placeholderRequestKey);
            if (num > -1)
            {
                string text = url.Substring(0, num);
                string b = url.Substring(num + PlaceholderManager.placeholderRequestKey.Length);
                if (MainUtil.GetMD5Hash(text).ToShortID().ToString() == b)
                {
                    return true;
                }
            }
            return false;
        }

        internal static string Sign(string url)
        {
            return string.Join(string.Empty, new string[]
            {
                url,
                PlaceholderManager.placeholderRequestKey,
                MainUtil.GetMD5Hash(url).ToShortID().ToString()
            });
        }
    }
}
