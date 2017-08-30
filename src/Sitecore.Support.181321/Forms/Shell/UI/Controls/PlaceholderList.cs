using HtmlAgilityPack;
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
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.XmlControls;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace Sitecore.Support.Forms.Shell.UI.Controls
{
    public class PlaceholderList : Sitecore.Web.UI.XmlControls.XmlControl
    {
        protected GenericControl ActiveUrl;

        protected DataContext DeviceDataContext;

        protected TreePicker DevicePicker;

        protected Border DeviceScope;

        protected Literal DevicesLiteral;

        protected Border PlaceholderHidden;

        protected Literal PlaceholdersLiteral;

        protected Border __LIST;

        public string AllowedRendering
        {
            get
            {
                return ((string)this.ViewState["allowedrendering"]) ?? string.Empty;
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
                string text = value;
                if (string.IsNullOrEmpty(this.DeviceID))
                {
                    text = "{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}";
                }
                Item item = StaticSettings.MasterDatabase.GetItem(text);
                if (item != null && item.TemplateID.ToString() == "{B6F7EEB4-E8D7-476F-8936-5ACE6A76F20B}")
                {
                    text = item.ID.ToString();
                }
                else
                {
                    text = "{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}";
                }
                this.DevicePicker.Value = text;
            }
        }

        public Item Item
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
                return ((string)this.ViewState["item"]) ?? string.Empty;
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
                string text = Sitecore.Context.ClientPage.ClientRequest.Form["__LISTACTIVE"];
                if (!string.IsNullOrEmpty(text) && !text.StartsWith("emptyValue"))
                {
                    return Sitecore.Context.ClientPage.ClientRequest.Form["__LISTVALUE"];
                }
                return null;
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

        protected virtual void OnChangedDevice(object sender, EventArgs e)
        {
            Item item = this.Item;
            this.__LIST.Controls.Clear();
            this.__LIST.Controls.Add(new Literal("<span class='scFbProgressScope'><img class='scFbProgressImg' src='/sitecore/shell/themes/standard/images/loading15x15.gif' align='absmiddle'>" + DependenciesManager.ResourceManager.Localize("LOADING") + "</span>"));
            string placeholdersUrl = PlaceholderManager.GetPlaceholdersUrl(item, this.DeviceID, this.AllowedRendering, this.SelectedPlaceholder);
            if (this.ActiveUrl != null)
            {
                this.ActiveUrl.Attributes["value"] = placeholdersUrl;
                Sitecore.Context.ClientPage.ClientResponse.SetOuterHtml(this.ActiveUrl.ID, this.ActiveUrl);
            }
            else
            {
                Sitecore.Context.ClientPage.ClientResponse.SetAttribute("ActiveUrl", "value", placeholdersUrl);
            }
            Sitecore.Context.ClientPage.ClientResponse.SetOuterHtml("__LIST", this.__LIST);
            Sitecore.Context.ClientPage.ClientResponse.Eval("Sitecore.Wfm.PlaceholderManager.load(this, event)");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                this.DevicePicker.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(this.ID);
                this.DeviceDataContext.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(this.ID);
                this.DevicePicker.DataContext = this.DeviceDataContext.ID;
                this.__LIST.ID = "__LIST";
                this.ActiveUrl = new GenericControl("input");
                this.ActiveUrl.ID = "ActiveUrl";
                this.ActiveUrl.Attributes["type"] = "hidden";
                this.PlaceholderHidden.Controls.Add(this.ActiveUrl);
                this.PlaceholderHidden.Controls.Add(new Literal("<input ID=\"__LISTVALUE\" Type=\"hidden\" />"));
                this.PlaceholderHidden.Controls.Add(new Literal("<input ID=\"__LISTACTIVE\" Type=\"hidden\" />"));
                this.PlaceholderHidden.Controls.Add(new Literal("<input ID=\"__NO_PLACEHOLDERS_MESSAGE\" Type=\"hidden\" value=\"" + DependenciesManager.ResourceManager.Localize("THERE_ARE_NO_PLACEHOLDERS_TO_INSERT") + "\" />"));
                this.OnChangedDevice(this, null);
                this.PlaceholdersLiteral.Text = DependenciesManager.ResourceManager.Localize("PLACEHOLDERS");
                this.DevicesLiteral.Text = DependenciesManager.ResourceManager.Localize("DEVICES");
            }
            this.DevicePicker.OnChanged += new EventHandler(this.OnChangedDevice);
        }

        private static void RenderPlaceholder(System.Web.UI.HtmlTextWriter output, KeyValuePair<string, string> placeholder, string selectedPlaceholderKey)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(placeholder, "placeholder");
            Assert.ArgumentNotNull(selectedPlaceholderKey, "selectedPlaceholderKey");
            string value = placeholder.Value;
            string value2 = Regex.Replace(placeholder.Value, "\\W", "_");
            string value3 = value;
            string value4 = string.Empty;
            int num = value.LastIndexOf('/');
            if (num >= 0)
            {
                value4 = value;
                value3 = StringUtil.Mid(value, num + 1);
            }
            int num2 = 0;
            string value5 = " style=\"margin-left:" + num2 * 16 + "px\"";
            string value6 = string.Empty;
            Item placeholderItem = Client.Page.GetPlaceholderItem(placeholder.Key, Sitecore.Context.Database);
            if (placeholderItem != null)
            {
                value6 = placeholderItem["Description"];
            }
            string value7 = (value == selectedPlaceholderKey) ? "scPalettePlaceholderSelected" : "scPalettePlaceholder";
            output.Write("<a id=\"ph_");
            output.Write(value2);
            output.Write("\" href=\"#\" class=\"");
            output.Write(value7);
            output.Write("\"");
            output.Write(value5);
            output.Write(" onclick=\"Sitecore.Wfm.PlaceholderManager.onPlaceholderClick(this,event,'");
            output.Write(value);
            output.Write("')\" onmouseover=\"javascript:return Sitecore.Wfm.PlaceholderManager.highlightPlaceholder(this,event,'");
            output.Write(value2);
            output.Write("')\" onmouseout=\"javascript:return Sitecore.Wfm.PlaceholderManager.highlightPlaceholder(this,event,'");
            output.Write(value2);
            output.Write("')\"");
            if (string.IsNullOrEmpty(value6))
            {
                output.Write(" title=\"");
                output.Write(value4);
                output.Write("\"");
            }
            output.Write(">");
            output.Write("<div class=\"scPalettePlaceholderIcon\"><span class=\"scPalettePlaceholderIconBorder\">");
            ImageBuilder imageBuilder = new ImageBuilder
            {
                Src = ((placeholderItem != null) ? placeholderItem.Appearance.Icon : "Imaging/24x24/layer_blend.png"),
                Width = 24,
                Height = 24,
                Align = "absmiddle"
            };
            string value8 = imageBuilder.ToString().Replace("'", "&quot;");
            output.Write(value8);
            output.Write("</span></div>");
            output.Write("<div class=\"scPalettePlaceholderTitle\">");
            output.Write(value3);
            output.Write("</div>");
            output.Write("<div class=\"scPalettePlaceholderTooltip\" style=\"display:none\">");
            output.Write(value6);
            output.Write("</div>");
            output.Write("</a>");
        }

        internal static void RenderPlaceholderPalette(System.Web.UI.HtmlTextWriter output, string allowedPlaceholder, string selectedPlaceholderKey)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(selectedPlaceholderKey, "selectedPlaceholderKey");
            foreach (KeyValuePair<string, string> current in PlaceholderList.GetAvailablePlaceholders(allowedPlaceholder))
            {
                PlaceholderList.RenderPlaceholder(output, current, selectedPlaceholderKey);
            }
        }

        protected static List<string> GetPagePlaceholders()
        {
            try
            {
                PreviewManager.RestoreUser();
                UrlString webSiteUrl = SiteContext.GetWebSiteUrl(StaticSettings.ContextDatabase);
                webSiteUrl.Add("sc_mode", "edit");
                webSiteUrl.Add("sc_itemid", Sitecore.Context.Item.ID.ToString());
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(WebUtil.GetFullUrl(webSiteUrl.ToString()));
                httpWebRequest.Method = "Get";
                System.Web.HttpCookie httpCookie = System.Web.HttpContext.Current.Request.Cookies["sitecore_userticket"];
                if (httpCookie != null)
                {
                    httpWebRequest.CookieContainer = new CookieContainer();
                    httpWebRequest.CookieContainer.Add(new Cookie
                    {
                        Domain = httpWebRequest.RequestUri.Host,
                        Name = httpCookie.Name,
                        Path = httpCookie.Path,
                        Secure = httpCookie.Secure,
                        Value = httpCookie.Value
                    });
                }

                //start of the modified part
                HttpContext httpContext = HttpContext.Current;
                string authHeader = httpContext.Request.Headers["Authorization"];

                if (authHeader != null && authHeader.StartsWith("Basic"))
                {
                    //code is used when the Basic authentication is enabled (via IIS)
                    string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();

                    Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                    string usernamePassword = encoding.GetString(System.Convert.FromBase64String(encodedUsernamePassword));

                    int separatorIndex = usernamePassword.IndexOf(':');

                    var username = usernamePassword.Substring(0, separatorIndex);
                    var password = usernamePassword.Substring(separatorIndex + 1);

                    httpWebRequest.Credentials = new NetworkCredential(username, password);
                }
                else
                {
                    //code is used to overcome the issue when the Windows Authentication is enabled
                    //for now, it is used along with anonymous authentication enabled.
                    httpWebRequest.Credentials = CredentialCache.DefaultCredentials;
                }
                // end of the modified part

                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Stream responseStream = httpWebResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream);
                HtmlDocument htmlDocument = new HtmlDocument();
                using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
                {
                    htmlDocument.LoadHtml(streamReader.ReadToEnd());
                }
                return (from x in htmlDocument.DocumentNode.SelectNodes("//*")
                        where x.Name == "code" && x.Attributes.Contains("chromeType") && x.Attributes["chromeType"].Value == "placeholder" && x.Attributes["key"] != null
                        select x.Attributes["key"].Value).ToList<string>();
            }
            catch (Exception)
            {
            }
            return new List<string>();
        }

        internal static Dictionary<string, string> GetAvailablePlaceholders(string allowedPlaceholder)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            Database database = Factory.GetDatabase(WebUtil.GetQueryString("sc_database", "master"), false);
            if (database != null)
            {
                List<string> first;
                if (Sitecore.Context.Page.Placeholders.Count != 0)
                {
                    first = (from x in Sitecore.Context.Page.Placeholders
                                select x.Key).ToList<string>();
                }
                else
                {
                    first = (from x in Sitecore.Context.Page.Renderings
                                select x.Placeholder).ToList<string>();
                }
                List<string> list = first;
                list = list.Concat(PlaceholderList.GetPagePlaceholders()).ToList<string>();
                Regex regex = new Regex("[^/]+$");
                foreach (string current in list)
                {
                    string text = current.Contains("/") ? regex.Match(current).Value : current;
                    if (!dictionary.ContainsKey(text))
                    {
                        Item placeholderItem = Client.Page.GetPlaceholderItem(text, database);
                        if (placeholderItem != null && PlaceholderList.CanDesign(placeholderItem) && placeholderItem["Allowed Controls"].Contains(allowedPlaceholder))
                        {
                            string text2 = text;
                            int num = text2.LastIndexOf('/');
                            if (num >= 0)
                            {
                                text2 = StringUtil.Mid(text2, num + 1);
                            }
                            dictionary.Add(text2, current);
                        }
                    }
                }
                dictionary = (from x in dictionary
                              orderby x.Key
                              select x).ToDictionary((KeyValuePair<string, string> x) => x.Key, (KeyValuePair<string, string> x) => x.Value);
            }
            return dictionary;
        }

        private static bool CanDesign(Item placeholderItem)
        {
            if (placeholderItem == null)
            {
                return true;
            }
            if (!placeholderItem.Access.CanRead())
            {
                return false;
            }
            if (Sitecore.Context.PageMode.IsExperienceEditorEditing)
            {
                return true;
            }
            string text = placeholderItem["Allowed Controls"];
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }
            return new ListString(text).Select(new Func<string, Item>(placeholderItem.Database.GetItem)).Any((Item item) => item != null);
        }
    }
}
