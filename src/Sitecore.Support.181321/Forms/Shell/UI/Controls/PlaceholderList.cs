using HtmlAgilityPack;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Data;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Publishing;
using Sitecore.Resources;
using Sitecore.Security.AccessControl;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XmlControls;
using Sitecore.WFFM.Abstractions;
using Sitecore.WFFM.Abstractions.Shared;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Sitecore.Support.Forms.Shell.UI.Controls
{
    public class PlaceholderList : Sitecore.Web.UI.XmlControls.XmlControl
    {
        protected GenericControl ActiveUrl;

        protected DataContext DeviceDataContext;

        protected TreePicker DevicePicker;

        protected Border DeviceScope;

        protected Sitecore.Web.UI.HtmlControls.Literal DevicesLiteral;

        protected Border PlaceholderHidden;

        protected Sitecore.Web.UI.HtmlControls.Literal PlaceholdersLiteral;

        protected Border __LIST;

        public string AllowedRendering
        {
            get
            {
                return (string)this.ViewState["allowedrendering"] ?? string.Empty;
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                this.ViewState["allowedrendering"] = value;
            }
        }

        public string DeviceID
        {
            get
            {
                return this.DevicePicker.Value;
            }
            set
            {
                string str = value;
                if (string.IsNullOrEmpty(this.DeviceID))
                {
                    str = "{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}";
                }
                Sitecore.Data.Items.Item item = StaticSettings.MasterDatabase.GetItem(str);
                str = (item == null || !(item.TemplateID.ToString() == "{B6F7EEB4-E8D7-476F-8936-5ACE6A76F20B}") ? "{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}" : item.ID.ToString());
                this.DevicePicker.Value = str;
            }
        }

        public Sitecore.Data.Items.Item Item
        {
            get
            {
                if (string.IsNullOrEmpty(this.ItemUri))
                {
                    return null;
                }
                return Sitecore.Data.Database.GetItem(Sitecore.Data.ItemUri.Parse(this.ItemUri));
            }
        }

        public string ItemUri
        {
            get
            {
                return (string)this.ViewState["item"] ?? string.Empty;
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                this.ViewState["item"] = value;
            }
        }

        public string SelectedPlaceholder
        {
            get
            {
                string item = Sitecore.Context.ClientPage.ClientRequest.Form["__LISTACTIVE"];
                if (string.IsNullOrEmpty(item) || item.StartsWith("emptyValue"))
                {
                    return null;
                }
                return Sitecore.Context.ClientPage.ClientRequest.Form["__LISTVALUE"];
            }
        }

        public bool ShowDeviceTree
        {
            get
            {
                return this.DeviceScope.Visible;
            }
            set
            {
                this.DeviceScope.Visible = value;
            }
        }

        public PlaceholderList()
        {
        }

        private static bool CanDesign(Sitecore.Data.Items.Item placeholderItem)
        {
            if (placeholderItem == null)
            {
                return true;
            }
            if (!placeholderItem.Access.CanRead())
            {
                return false;
            }
            if (Sitecore.Context.PageMode.IsPageEditorEditing)
            {
                return true;
            }
            string str = placeholderItem["Allowed Controls"];
            if (string.IsNullOrEmpty(str))
            {
                return true;
            }
            return (new ListString(str)).Select<string, Sitecore.Data.Items.Item>(new Func<string, Sitecore.Data.Items.Item>(placeholderItem.Database.GetItem)).Any<Sitecore.Data.Items.Item>((Sitecore.Data.Items.Item item) => item != null);


        }

        internal static Dictionary<string, string> GetAvailablePlaceholders(string allowedPlaceholder)
        {
            Dictionary<string, string> strs = new Dictionary<string, string>();
            Database database = Factory.GetDatabase(WebUtil.GetQueryString("sc_database", "master"), false);
            if (database != null)
            {
                List<string> list = (Sitecore.Context.Page.Placeholders.Count == 0 ? (
                    from x in Sitecore.Context.Page.Renderings
                    select x.Placeholder).ToList<string>() : (
                    from x in Sitecore.Context.Page.Placeholders
                    select x.Key).ToList<string>());
                list = list.Concat<string>(Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.GetPagePlaceholders()).ToList<string>();
                Regex regex = new Regex("[^/]+$");
                foreach (string str in list)
                {
                    string str1 = (str.Contains("/") ? regex.Match(str).Value : str);
                    if (strs.ContainsKey(str1))
                    {
                        continue;
                    }
                    Sitecore.Data.Items.Item placeholderItem = Client.Page.GetPlaceholderItem(str1, database);
                    if (placeholderItem == null || !Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.CanDesign(placeholderItem) || !placeholderItem["Allowed Controls"].Contains(allowedPlaceholder))
                    {
                        continue;
                    }
                    string str2 = str1;
                    int num = str2.LastIndexOf('/');
                    if (num >= 0)
                    {
                        str2 = StringUtil.Mid(str2, num + 1);
                    }
                    strs.Add(str2, str);
                }
                strs = (
                    from x in strs
                    orderby x.Key
                    select x).ToDictionary<KeyValuePair<string, string>, string, string>((KeyValuePair<string, string> x) => x.Key, (KeyValuePair<string, string> x) => x.Value);
            }
            return strs;
        }

        protected static List<string> GetPagePlaceholders()
        {
            List<string> list;
            try
            {
                PreviewManager.RestoreUser();
                UrlString webSiteUrl = SiteContext.GetWebSiteUrl(StaticSettings.ContextDatabase);
                webSiteUrl.Add("sc_mode", "edit");
                webSiteUrl.Add("sc_itemid", Sitecore.Context.Item.ID.ToString());
                HttpWebRequest cookieContainer = (HttpWebRequest)WebRequest.Create(WebUtil.GetFullUrl(webSiteUrl.ToString()));
                cookieContainer.Method = "Get";
                HttpCookie item = HttpContext.Current.Request.Cookies["sitecore_userticket"];
                if (item != null)
                {
                    cookieContainer.CookieContainer = new CookieContainer();
                    CookieContainer cookieContainer1 = cookieContainer.CookieContainer;
                    Cookie cookie = new Cookie()
                    {
                        Domain = cookieContainer.RequestUri.Host,
                        Name = item.Name,
                        Path = item.Path,
                        Secure = item.Secure,
                        Value = item.Value
                    };
                    cookieContainer1.Add(cookie);
                }

                HttpContext httpContext = HttpContext.Current;
                string authHeader = httpContext.Request.Headers["Authorization"];

                if (authHeader != null && authHeader.StartsWith("Basic"))
                {
                  string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                  Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                  string usernamePassword = encoding.GetString(System.Convert.FromBase64String(encodedUsernamePassword));
                  int separatorIndex = usernamePassword.IndexOf(':');
                  var username = usernamePassword.Substring(0, separatorIndex);
                  var password = usernamePassword.Substring(separatorIndex + 1);
                    cookieContainer.Credentials = new NetworkCredential(username, password);
                }
                else
                {
                    cookieContainer.Credentials = CredentialCache.DefaultCredentials;
                }

                StreamReader streamReader = new StreamReader(((HttpWebResponse)cookieContainer.GetResponse()).GetResponseStream());
                HtmlDocument htmlDocument = new HtmlDocument();
                using (ThreadCultureSwitcher threadCultureSwitcher = new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
                {
                    htmlDocument.LoadHtml(streamReader.ReadToEnd());
                }
                list = htmlDocument.DocumentNode.SelectNodes("//*").Where<HtmlNode>((HtmlNode x) => {
                    if (!(x.Name == "code") || !x.Attributes.Contains("chromeType") || !(x.Attributes["chromeType"].Value == "placeholder"))
                    {
                        return false;
                    }
                    return x.Attributes["key"] != null;
                }).Select<HtmlNode, string>((HtmlNode x) => x.Attributes["key"].Value).ToList<string>();
            }
            catch (Exception exception)
            {
                return new List<string>();
            }
            return list;
        }

        protected virtual void OnChangedDevice(object sender, EventArgs e)
        {
            Sitecore.Data.Items.Item item = this.Item;
            this.__LIST.Controls.Clear();
            this.__LIST.Controls.Add(new Sitecore.Web.UI.HtmlControls.Literal(string.Concat("<span class='scFbProgressScope'><img class='scFbProgressImg' src='/sitecore/shell/themes/standard/images/loading15x15.gif' align='absmiddle'>", DependenciesManager.ResourceManager.Localize("LOADING"), "</span>")));
            string placeholdersUrl = PlaceholderManager.GetPlaceholdersUrl(item, this.DeviceID, this.AllowedRendering, this.SelectedPlaceholder);
            if (this.ActiveUrl == null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetAttribute("ActiveUrl", "value", placeholdersUrl);
            }
            else
            {
                this.ActiveUrl.Attributes["value"] = placeholdersUrl;
                Sitecore.Context.ClientPage.ClientResponse.SetOuterHtml(this.ActiveUrl.ID, this.ActiveUrl);
            }
                Sitecore.Context.ClientPage.ClientResponse.SetOuterHtml("__LIST", this.__LIST);
                Sitecore.Context.ClientPage.ClientResponse.Eval("Sitecore.PlaceholderManager.load(this, event)");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                this.DevicePicker.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(this.ID);
                this.DeviceDataContext.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(this.ID);
                this.DevicePicker.DataContext = (this.DeviceDataContext.ID);
                this.__LIST.ID = "__LIST";
                this.ActiveUrl = new GenericControl("input")
                {
                    ID = "ActiveUrl"
                };
                this.ActiveUrl.Attributes["type"] = "hidden";
                this.PlaceholderHidden.Controls.Add(this.ActiveUrl);
                this.PlaceholderHidden.Controls.Add(new Sitecore.Web.UI.HtmlControls.Literal("<input ID=\"__LISTVALUE\" Type=\"hidden\" />"));
                this.PlaceholderHidden.Controls.Add(new Sitecore.Web.UI.HtmlControls.Literal("<input ID=\"__LISTACTIVE\" Type=\"hidden\" />"));
                this.PlaceholderHidden.Controls.Add(new Sitecore.Web.UI.HtmlControls.Literal(string.Concat("<input ID=\"__NO_PLACEHOLDERS_MESSAGE\" Type=\"hidden\" value=\"", DependenciesManager.ResourceManager.Localize("THERE_ARE_NO_PLACEHOLDERS_TO_INSERT"), "\" />")));
                this.OnChangedDevice(this, null);
                this.PlaceholdersLiteral.Text = (DependenciesManager.ResourceManager.Localize("PLACEHOLDERS"));
                this.DevicesLiteral.Text = (DependenciesManager.ResourceManager.Localize("DEVICES"));
            }
            Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList placeholderList = this;
            this.DevicePicker.OnChanged += (new EventHandler(placeholderList.OnChangedDevice));
        }

        private static void RenderPlaceholder(HtmlTextWriter output, KeyValuePair<string, string> placeholder, string selectedPlaceholderKey)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(placeholder, "placeholder");
            Assert.ArgumentNotNull(selectedPlaceholderKey, "selectedPlaceholderKey");
            string value = placeholder.Value;
            string str = Regex.Replace(placeholder.Value, "\\W", "_");
            string str1 = value;
            string empty = string.Empty;
            int num = value.LastIndexOf('/');
            if (num >= 0)
            {
                empty = value;
                str1 = StringUtil.Mid(value, num + 1);
            }
            int num1 = 0;
            string str2 = string.Concat(" style=\"margin-left:", num1 * 16, "px\"");
            string item = string.Empty;
            Sitecore.Data.Items.Item placeholderItem = Client.Page.GetPlaceholderItem(placeholder.Key, Sitecore.Context.Database);
            if (placeholderItem != null)
            {
                item = placeholderItem["Description"];
            }
            string str3 = (value == selectedPlaceholderKey ? "scPalettePlaceholderSelected" : "scPalettePlaceholder");
            output.Write("<a id=\"ph_");
            output.Write(str);
            output.Write("\" href=\"#\" class=\"");
            output.Write(str3);
            output.Write("\"");
            output.Write(str2);
            output.Write(" onclick=\"Sitecore.PlaceholderManager.onPlaceholderClick(this,event,'");
            output.Write(value);
            output.Write("')\" onmouseover=\"javascript:return Sitecore.PlaceholderManager.highlightPlaceholder(this,event,'");
            output.Write(str);
            output.Write("')\" onmouseout=\"javascript:return Sitecore.PlaceholderManager.highlightPlaceholder(this,event,'");
            output.Write(str);
            output.Write("')\"");
            if (string.IsNullOrEmpty(item))
            {
                output.Write(" title=\"");
                output.Write(empty);
                output.Write("\"");
            }
            output.Write(">");
            output.Write("<div class=\"scPalettePlaceholderIcon\"><span class=\"scPalettePlaceholderIconBorder\">");
            ImageBuilder imageBuilder = new ImageBuilder();
            imageBuilder.Src = ((placeholderItem != null ? placeholderItem.Appearance.Icon : "Imaging/24x24/layer_blend.png"));
            imageBuilder.Width = 24;
            imageBuilder.Height = 24;
            imageBuilder.Align = "absmiddle";
            output.Write(imageBuilder.ToString().Replace("'", "&quot;"));
            output.Write("</span></div>");
            output.Write("<div class=\"scPalettePlaceholderTitle\">");
            output.Write(str1);
            output.Write("</div>");
            output.Write("<div class=\"scPalettePlaceholderTooltip\" style=\"display:none\">");
            output.Write(item);
            output.Write("</div>");
            output.Write("</a>");
        }

        internal static void RenderPlaceholderPalette(HtmlTextWriter output, string allowedPlaceholder, string selectedPlaceholderKey)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(selectedPlaceholderKey, "selectedPlaceholderKey");
            foreach (KeyValuePair<string, string> availablePlaceholder in Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.GetAvailablePlaceholders(allowedPlaceholder))
            {
                Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.RenderPlaceholder(output, availablePlaceholder, selectedPlaceholderKey);
            }
        }
    }
}
