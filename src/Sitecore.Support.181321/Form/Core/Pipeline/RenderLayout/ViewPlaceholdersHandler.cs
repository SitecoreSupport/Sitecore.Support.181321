using Sitecore.Diagnostics;
using Sitecore.Form.Core.Data;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Layouts;
using Sitecore.Pipelines.RenderLayout;
using Sitecore.Publishing;
using Sitecore.Web;
using Sitecore.Web.UI.WebControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace Sitecore.Support.Form.Core.Pipeline.RenderLayout
{
    public class ViewPlaceholdersHandler : RenderLayoutProcessor
    {
        public override void Process(RenderLayoutArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (WebUtil.GetQueryString(Sitecore.Support.Form.Core.Data.PlaceholderManager.placeholderQueryKey, "0") == "1")
            {
                PreviewManager.RestoreUser();
                PageContext page = Context.Page;
                if (page != null && Sitecore.Support.Form.Core.Data.PlaceholderManager.IsValid(WebUtil.GetRawUrl()))
                {
                    System.Web.HttpResponse response = System.Web.HttpContext.Current.Response;
                    response.ClearContent();
                    response.ClearHeaders();
                    response.ContentType = "text/plain";
                    response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                    System.Web.UI.HtmlTextWriter htmlTextWriter = new System.Web.UI.HtmlTextWriter(new StringWriter());
                    if (WebUtil.GetQueryString(Sitecore.Support.Form.Core.Data.PlaceholderManager.onlyNamesPlaceholdersKey, string.Empty) != "1")
                    {
                        Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.RenderPlaceholderPalette(htmlTextWriter, WebUtil.GetQueryString(Sitecore.Support.Form.Core.Data.PlaceholderManager.allowedRenderingKey), WebUtil.GetQueryString(Sitecore.Support.Form.Core.Data.PlaceholderManager.selectedPlaceholderKey));
                    }
                    else
                    {
                        new Dictionary<string, Placeholder>();
                        Dictionary<string, string> availablePlaceholders = Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.GetAvailablePlaceholders(WebUtil.GetQueryString(Sitecore.Support.Form.Core.Data.PlaceholderManager.allowedRenderingKey));
                        htmlTextWriter.Write(string.Join<KeyValuePair<string, string>>(",", availablePlaceholders.ToArray<KeyValuePair<string, string>>()));
                    }
                    response.Write(htmlTextWriter.InnerWriter.ToString());
                    response.StatusCode = 200;
                    response.Flush();
                    args.AbortPipeline();
                    response.End();
                }
            }
        }
    }
}
