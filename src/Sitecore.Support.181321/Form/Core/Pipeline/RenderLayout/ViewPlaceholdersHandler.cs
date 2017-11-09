using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Support.Form.Core.Data;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Layouts;
using Sitecore.Pipelines;
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
        public ViewPlaceholdersHandler()
        {
        }

        public override void Process(RenderLayoutArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (WebUtil.GetQueryString(PlaceholderManager.placeholderQueryKey, "0") == "1")
            {
                PreviewManager.RestoreUser();
                if (Context.Page != null && PlaceholderManager.IsValid(WebUtil.GetRawUrl()))
                {
                    HttpResponse response = HttpContext.Current.Response;
                    response.ClearContent();
                    response.ClearHeaders();
                    response.ContentType = "text/plain";
                    response.Cache.SetCacheability(HttpCacheability.NoCache);
                    HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                    if (WebUtil.GetQueryString(PlaceholderManager.onlyNamesPlaceholdersKey, string.Empty) == "1")
                    {
                        Dictionary<string, Placeholder> strs = new Dictionary<string, Placeholder>();
                        Dictionary<string, string> availablePlaceholders = Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.GetAvailablePlaceholders(WebUtil.GetQueryString(PlaceholderManager.allowedRenderingKey));
                        htmlTextWriter.Write(string.Join<KeyValuePair<string, string>>(",", availablePlaceholders.ToArray<KeyValuePair<string, string>>()));
                    }
                    else
                    {
                        Sitecore.Support.Forms.Shell.UI.Controls.PlaceholderList.RenderPlaceholderPalette(htmlTextWriter, WebUtil.GetQueryString(PlaceholderManager.allowedRenderingKey), WebUtil.GetQueryString(PlaceholderManager.selectedPlaceholderKey));
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
