// --- Copyright (c) notice NevoWeb ---
//  Copyright (c) 2014 SARL NevoWeb.  www.nevoweb.com. The MIT License (MIT).
// Author: D.C.Lee
// ------------------------------------------------------------------------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ------------------------------------------------------------------------
// This copyright notice may NOT be removed, obscured or modified without written consent from the author.
// --- End copyright notice --- 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;

using Nevoweb.DNN.NBrightBuy.Base;
using Nevoweb.DNN.NBrightBuy.Components;
using DataProvider = DotNetNuke.Data.DataProvider;
using System.Web.UI.HtmlControls;
using System.Xml;

namespace Nevoweb.DNN.NBrightBuy.Providers.NBrightBuyReport
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class Admin : NBrightBuyAdminBase
    {

        #region Event Handlers

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (Page.IsPostBack == false)
                {
                    // do razor code
                    RazorPageLoad();
                }
            }
            catch (Exception exc) //Module failed to load
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                phData.Controls.Add(l);
            }
        }

        private void RazorPageLoad()
        {
            var strOut = NBrightBuyUtils.RazorTemplRender("Admin.cshtml", 0, "", null, ControlPath, "config", Utils.GetCurrentCulture(), StoreSettings.Current.Settings());
            var lit = new Literal();
            lit.Text = strOut;
            phData.Controls.Add(lit);
        }

        #endregion

        #region "Events"

       /* protected void CtrlItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var cArg = e.CommandArgument.ToString();
            var param = new string[3];

            switch (e.CommandName.ToLower())
            {
                case "import":
                    ImportReport();
                    param[0] = "";
                    Response.Redirect(NBrightBuyUtils.AdminUrl(TabId, param), true);
                    break;
                case "export":
                    ExportReport();
                    param[0] = "";
                    Response.Redirect(NBrightBuyUtils.AdminUrl(TabId, param), true);
                    break;
                case "download":
                    DownloadReport();
                    param[0] = "";
                    Response.Redirect(NBrightBuyUtils.AdminUrl(TabId, param), true);
                    break;
                case "cancel":
                    param[0] = "";
                    Response.Redirect(NBrightBuyUtils.AdminUrl(TabId, param), true);
                    break;
            }

        }

        private void ExportReport()
        {
            try
            {
                var itemid = GenXmlFunctions.GetField(rpData, "itemid");

                var strOut = "Error!! - Invalid ItemId on Edit Report";
                if (Utils.IsNumeric(itemid))
                {
                    var objCtrl = new NBrightBuyController();
                    var obj = objCtrl.Get(Convert.ToInt32(itemid));
                    if (obj != null)
                    {
                        var dataout = obj.ToXmlItem();
                        Utils.ForceStringDownload(Response, "report.xml", dataout);
                    }
                }
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void DownloadReport()
        {
            try
            {
                var itemid = GenXmlFunctions.GetField(rpData, "itemid");
                var reportfilepath = GenXmlFunctions.GetField(rpData, "reportfilepath");

                if (Utils.IsNumeric(itemid) && reportfilepath != "")
                {
                    var objCtrl = new NBrightBuyController();
                    var obj = objCtrl.Get(Convert.ToInt32(itemid));
                    if (obj != null)
                    {
                        var repData = Utils.ReadFile(reportfilepath);
                        Utils.ForceStringDownload(Response, "report." + obj.GetXmlProperty("genxml/textbox/extension"), repData);
                    }
                }
            }
            catch (Exception ex)
            {
                //
            }
        }


        private void ImportReport()
        {
            GenXmlFunctions.UploadFile(rpData, "importxml", StoreSettings.Current.FolderTempMapPath);
            var ctrl = ((HtmlGenericControl)rpData.Items[0].FindControl("hidimportxml"));
            if (ctrl != null)
            {
                var fName = ctrl.Attributes["value"];
                var xDoc = new XmlDocument();
                xDoc.Load(StoreSettings.Current.FolderTempMapPath + "\\" + fName);

                var nbi = new NBrightInfo();
                nbi.FromXmlItem(xDoc.OuterXml);
                nbi.ItemID = -1;
                nbi.PortalId = PortalSettings.Current.PortalId;
                if (nbi.TypeCode == "NBSREPORT")
                {
                    var objCtrl = new NBrightBuyController();
                    objCtrl.Update(nbi);
                    var cachekey = "GetReportListData*" + PortalSettings.Current.PortalId.ToString("");
                    Utils.RemoveCache(cachekey);
                }
            }

        }*/

        #endregion

    }

}
