using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Layouts;
using Sitecore.Text;
using Sitecore.Web;
using System;
using System.Runtime.CompilerServices;

namespace Sitecore.Support.Form.Core.Data
{
  public class PlaceholderManager
  {
    internal readonly static string allowedRenderingKey;

    internal readonly static string onlyNamesPlaceholdersKey;

    internal readonly static string placeholderQueryKey;

    internal readonly static string selectedPlaceholderKey;

    private readonly static string placeholderRequestKey;

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
        return string.Concat(WebUtil.GetServerUrl(), PlaceholderManager.Sign(string.Concat(urlString, (object)this.GetDeviceQueryString())));
      }
    }

    public string SelectedPlaceholder
    {
      get;
      private set;
    }

    static PlaceholderManager()
    {
      PlaceholderManager.allowedRenderingKey = "sc_allowrend";
      PlaceholderManager.onlyNamesPlaceholdersKey = "onlyNames";
      PlaceholderManager.placeholderQueryKey = "sc_placeholder";
      PlaceholderManager.selectedPlaceholderKey = "sc_selectedplaceholder";
      PlaceholderManager.placeholderRequestKey = "&screquestplacholderid=";
    }

    public PlaceholderManager(Item item, string deviceID, string allowedRendering, string selectedPlaceholder)
    {
      this.item = item;
      this.deviceID = deviceID;
      this.AllowedRendering = allowedRendering;
      this.SelectedPlaceholder = selectedPlaceholder;
    }

    public string GetDeviceQueryString()
    {
      Item item = this.item.Database.GetItem(this.deviceID);
      if (item == null || string.IsNullOrEmpty(item["query string"]))
      {
        return string.Empty;
      }
      string empty = string.Empty;
      string[] strArrays = new string[] { "&", item["query string"] };
      return string.Join(empty, strArrays);
    }

    internal static string GetOnlyPlaceholderNamesUrl(string baseUrl)
    {
      if (string.IsNullOrEmpty(baseUrl))
      {
        return string.Empty;
      }
      UrlString urlString = new UrlString(baseUrl);
      urlString.Append(PlaceholderManager.placeholderQueryKey, "1");
      urlString.Append(PlaceholderManager.onlyNamesPlaceholdersKey, "1");
      urlString.Append("sc_database", StaticSettings.ContextDatabase.Name);
      urlString.Append(PlaceholderManager.allowedRenderingKey, string.Concat(IDs.FormInterpreterID, (object)"|", IDs.FormMvcInterpreterID));
      return string.Concat(WebUtil.GetServerUrl(), PlaceholderManager.Sign(urlString.ToString()));
    }

    public static string GetPlaceholdersUrl(Item item, string deviceID, string allowedRendering, string selectedPlaceholder)
    {
      return (new PlaceholderManager(item, deviceID, allowedRendering, selectedPlaceholder)).PlaceholdersUrl;
    }

    internal static bool IsValid(string url)
    {
      Assert.ArgumentNotNullOrEmpty(url, "url");
      int num = url.IndexOf(PlaceholderManager.placeholderRequestKey);
      if (num > -1)
      {
        string str = url.Substring(0, num);
        string str1 = url.Substring(num + PlaceholderManager.placeholderRequestKey.Length);
        if (MainUtil.GetMD5Hash(str).ToShortID().ToString() == str1)
        {
          return true;
        }
      }
      return false;
    }

    public LayoutItem LayoutByID(string id)
    {
      return this.item.Database.GetItem(id);
    }

    internal static string Sign(string url)
    {
      string empty = string.Empty;
      string[] strArrays = new string[] { url, PlaceholderManager.placeholderRequestKey, MainUtil.GetMD5Hash(url).ToShortID().ToString() };
      return string.Join(empty, strArrays);
    }
  }
}
