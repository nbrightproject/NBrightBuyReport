using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using NBrightCore;
using NBrightCore.common;
using NBrightCore.images;
using NBrightCore.render;
using NBrightDNN;
using DataProvider = DotNetNuke.Data.DataProvider;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Exceptions;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightBuyReport

{
    public class XmlConnector : IHttpHandler

    {
        private String _lang = "";
        private String _itemid = "";


        public void ProcessRequest(HttpContext context)
        {
            var strOut = "";
            try
            {
                var paramCmd = Utils.RequestQueryStringParam(context, "cmd");
                _itemid = Utils.RequestQueryStringParam(context, "itemid");
                NBrightBuyUtils.SetContextLangauge(context);
                _lang = System.Threading.Thread.CurrentThread.CurrentCulture.ToString();

                #region "Do processing of command"

                strOut = "ERROR!! - No Security rights for current user!";

                if (CheckRights())
                {
                    switch (paramCmd)
                    {
                        case "test":
                            strOut = "<root>" + UserController.Instance.GetCurrentUserInfo().Username + "</root>";
                            break;

                        case "addnew":
                            strOut = GetData(context, true);
                            break;

                        case "deleterecord":
                            strOut = DeleteData(context);
                            break;

                        case "runreport":
                            strOut = RunReport(context);
                            break;

                        case "savedata":
                            strOut = SaveData(context);
                            break;

                        case "selectlang":
                            strOut = SelectLang(context);
                            break;

                        case "addreport":
                            strOut = AddReport(context);
                            break;

                        case "savereport":
                            strOut = SaveData(context);
                            break;

                        case "deletereport":
                            strOut = DeleteReport(context);
                            break;

                        case "getreportlist":
                            strOut = GetData(context);
                            break;

                        case "editreport":
                            strOut = GetData(context);
                            break;

                        case "rundisplay":
                            strOut = GetData(context);
                            break;

                        case "reportselection":
                            strOut = ReportSelection(context);
                            break;

                        case "checkadminrights":
                            strOut = CheckAdminRights().ToString();
                            break;
                    }
                }

                #endregion
            }

            catch (Exception ex)

            {
                strOut = ex.ToString();
                Exceptions.LogException(ex);
            }

            #region "return results"

            //send back xml as plain text
            context.Response.Clear();
            context.Response.ContentType = "text/plain";
            context.Response.Write(strOut);
            context.Response.End();

            #endregion

        }

        private string RunReport(HttpContext context)
        {
            var strOut = "Error!! - Invalid ItemId on Run Report";
            var strSql = "";
            try
            {
                var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
                var itemid = ajaxInfo.GetXmlPropertyInt("genxml/hidden/itemid");
                var razortemplate = ajaxInfo.GetXmlProperty("genxml/hidden/razortemplate");
                var portalid = PortalSettings.Current.PortalId.ToString("");


                if (itemid > 0)
                {

                    var objCtrl = new NBrightBuyController();
                    var obj = objCtrl.Get(itemid);
                    if (obj != null)
                    {
                        var inline = obj.GetXmlPropertyBool("genxml/checkbox/inline");
                        strSql = obj.GetXmlProperty("genxml/textbox/sql");
                        // replace any settings tokens (This is used to place the form data into the SQL)
                        strSql = Utils.ReplaceSettingTokens(strSql, ajaxInfo.ToDictionary());
                        strSql = Utils.ReplaceUrlTokens((strSql));

                        strSql =
                            GenXmlFunctions.StripSqlCommands(strSql); // don't allow anything to update through here.

                        // add FOR XML if not there, this function will only output XML results.
                        if (!strSql.ToLower().Contains("for xml")) strSql += " FOR XML PATH ('item'), ROOT ('root')";

                        var strXmlResults = objCtrl.GetSqlxml(strSql);
                        var xdoc = XDocument.Parse(strXmlResults);

                        {
                            var xmlList = new List<NBrightInfo>();
                            foreach (XElement xmlitem in xdoc.XPathSelectElements("root/item"))
                            {
                                var nbi = new NBrightInfo(false);
                                nbi.XMLData = xmlitem.ToString();
                                xmlList.Add(nbi);
                            }

                            var templateControl = "/DesktopModules/NBright/NBrightBuyReport";

                            if (obj.GetXmlProperty("genxml/radiobuttonlist/reportformat") == "html")
                            {
                                if (inline)
                                {
                                    razortemplate = "reporthtmlinline.cshtml";
                                }
                                else
                                {
                                    razortemplate = "reporthtml.cshtml";
                                }
                            }
                            if (obj.GetXmlProperty("genxml/radiobuttonlist/reportformat") == "csv")
                            {
                                inline = false;
                                razortemplate = "reportcsv.cshtml";
                            }
                            //project
                            if (obj.GetXmlProperty("genxml/radiobuttonlist/reportformat") == "barchart")
                            {
                                inline = true;
                                razortemplate = "reportbar.cshtml";
                            }

                            strOut = NBrightBuyUtils.RazorTemplRenderList(razortemplate, -1, "", xmlList,templateControl, "config", _lang, StoreSettings.Current.Settings());

                            if (!inline)
                            {
                                var outfile = StoreSettings.Current.FolderTempMapPath + "\\" + itemid + "." + obj.GetXmlProperty("genxml/radiobuttonlist/reportformat");
                                Utils.SaveFile(outfile, strOut);
                                strOut = "Completed: <a target='_blank' href='" + StoreSettings.Current.FolderTemp + "/" + itemid + "." 
                                + obj.GetXmlProperty("genxml/radiobuttonlist/reportformat") + "' >View</a>";
                            }

                        }
                        return strOut;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.ToString() + " <hr/> " + strSql;
            }
            return strOut;
        }

        public bool IsReusable

        {
            get

            {
                return false;

            }
        }

        #region "Methods"

        private String GetData(HttpContext context, bool clearCache = false)

        {

            var objCtrl = new NBrightBuyController();
            var strOut = "";
            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            var typeCode = ajaxInfo.GetXmlProperty("genxml/hidden/typecode");
            var newitem = ajaxInfo.GetXmlProperty("genxml/hidden/newitem");
            var selecteditemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
            var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
            var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
            var selectlang = ajaxInfo.GetXmlProperty("genxml/hidden/selectlang");
            var rundisplay = ajaxInfo.GetXmlPropertyBool("genxml/hidden/rundisplay");

            if (selectlang != "") editlang = selectlang;

            if (itemid == "") itemid = selecteditemid;

            if (editlang == "") editlang = _lang;

            if (!Utils.IsNumeric(moduleid)) moduleid = "-2"; // use moduleid -2 for razor

            if (clearCache) NBrightBuyUtils.RemoveModCache(Convert.ToInt32(moduleid));

            if (newitem == "new") itemid = AddNew(moduleid, typeCode);

            var templateControl = "/DesktopModules/NBright/NBrightBuyReport";

            if (Utils.IsNumeric(itemid))
            {
                // do edit field data if a itemid has been selected
                var obj = objCtrl.Get(Convert.ToInt32(itemid), "", editlang);
                if (rundisplay)
                {
                    strOut = NBrightBuyUtils.RazorTemplRender(typeCode.ToLower() + "run.cshtml", Convert.ToInt32(moduleid), _lang + itemid + editlang, obj, templateControl, "config", _lang, StoreSettings.Current.Settings());
                }
                else
                {
                    strOut = NBrightBuyUtils.RazorTemplRender(typeCode.ToLower() + "fields.cshtml", Convert.ToInt32(moduleid), _lang + itemid + editlang, obj, templateControl, "config", _lang, StoreSettings.Current.Settings());
                }
            }
            else
            {
                // Return list of items
                var l = objCtrl.GetDataList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), typeCode, typeCode + "LANG", Utils.GetCurrentCulture(), "", " order by ModifiedDate desc", false, "", 100, 0, 0, 0);
                strOut = NBrightBuyUtils.RazorTemplRenderList(typeCode.ToLower() + "list.cshtml", Convert.ToInt32(moduleid), _lang + editlang, l, templateControl, "config", _lang, StoreSettings.Current.Settings());
            }
            return strOut;
        }

        private String AddNew(String moduleid, String typeCode)

        {

            if (!Utils.IsNumeric(moduleid)) moduleid = "-2"; // -2 for razor

            var objCtrl = new NBrightBuyController();
            var nbi = new NBrightInfo(true);
            nbi.PortalId = PortalSettings.Current.PortalId;
            nbi.TypeCode = typeCode;
            nbi.ModuleId = Convert.ToInt32(moduleid);
            nbi.ItemID = -1;
            var itemId = objCtrl.Update(nbi);
            nbi.ItemID = itemId;

            foreach (var lang in DnnUtils.GetCultureCodeList(PortalSettings.Current.PortalId))
            {
                var nbi2 = new NBrightInfo(true);
                nbi2.PortalId = PortalSettings.Current.PortalId;
                nbi2.TypeCode = typeCode + "LANG";
                nbi2.ModuleId = Convert.ToInt32(moduleid);
                nbi2.ItemID = -1;
                nbi2.Lang = lang;
                nbi2.ParentItemId = itemId;
                nbi2.GUIDKey = "";
                nbi2.ItemID = objCtrl.Update(nbi2);
            }
            NBrightBuyUtils.RemoveModCache(nbi.ModuleId);

            return nbi.ItemID.ToString("");
        }

        private String SaveData(HttpContext context)
        {
            var objCtrl = new NBrightBuyController();

            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
            if (lang == "") lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            if (lang == "") lang = _lang;
            if (Utils.IsNumeric(itemid))
            {
                // get DB record
                var nbi = objCtrl.Get(Convert.ToInt32(itemid));
                if (nbi != null)
                {
                    var typecode = nbi.TypeCode;
                    // get data passed back by ajax
                    var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                    // update record with ajax data
                    nbi.UpdateAjax(strIn);
                    if (nbi.GUIDKey == "") nbi.GUIDKey = Utils.GetUniqueKey();
                    objCtrl.Update(nbi);

                    // do language record
                    var nbi2 = objCtrl.GetDataLang(Convert.ToInt32(itemid), lang);
                    nbi2.UpdateAjax(strIn);
                    objCtrl.Update(nbi2);

                    DataCache.ClearCache(); // clear ALL cache.
                }

            }
            return "";
        }

        private String SelectLang(HttpContext context)
        {
            SaveData(context);
            return GetData(context);
        }


        private String DeleteData(HttpContext context)

        {
            var objCtrl = new NBrightBuyController();
            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/itemid");
            if (Utils.IsNumeric(itemid))

            {
                // delete DB record
                objCtrl.Delete(Convert.ToInt32(itemid));
                NBrightBuyUtils.RemoveModCache(-2);
            }
            return "";
        }

        #endregion

        #region "Report Methods"

        private string AddReport(HttpContext context)
        {
            var objCtrl = new NBrightBuyController();
            var obj = new NBrightInfo(true);
            obj.TypeCode = "NBSREPORT";
            obj.SetXmlProperty("genxml/lang/genxml/textbox/name", "New Report");
            obj.PortalId = PortalSettings.Current.PortalId;
            obj.ModuleId = -1;
            obj.ItemID = -1;
            objCtrl.Update(obj);

            var cachekey = "GetReportListData*" + PortalSettings.Current.PortalId.ToString("");
            Utils.RemoveCache(cachekey);

            return "";
        }

        private String DeleteReport(HttpContext context)

        {
            var settings = GetAjaxFields(context);

            if (settings.ContainsKey("itemid") && Utils.IsNumeric(settings["itemid"]))
            {
                var objCtrl = new NBrightBuyController();
                objCtrl.Delete(Convert.ToInt32(settings["itemid"]));

                var cachekey = "GetReportListData*" + PortalSettings.Current.PortalId.ToString("");
                Utils.RemoveCache(cachekey);

                return GetData(context);
            }

            return "Error!! - Invalid ItemId on SaveReport";
        }



        private String ReportSelection(HttpContext context)

        {
            try
            {
                var settings = GetAjaxFields(context);
                var strOut = "Error!! - Invalid ItemId on Selection Report";
                if (settings.ContainsKey("itemid") && Utils.IsNumeric(settings["itemid"]))
                {
                    if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings
                    var objCtrl = new NBrightBuyController();
                    var obj = objCtrl.Get(Convert.ToInt32(settings["itemid"]));
                    if (obj != null)
                    {
                        var bodyTempl = NBrightBuyUtils.GetTemplateData("Admin.cshtml", "", "config", StoreSettings.Current.Settings());
                        var strTempl = obj.GetXmlProperty("genxml/textbox/selectiondetails");
                        bodyTempl = bodyTempl.Replace("[Template:selectiondetails]", strTempl);
                        bodyTempl = Utils.ReplaceSettingTokens(bodyTempl, settings);
                        bodyTempl = Utils.ReplaceUrlTokens(bodyTempl);

                        strOut = GenXmlFunctions.RenderRepeater(obj, bodyTempl);
                    }
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region "functions"

        private Dictionary<String, String> GetAjaxFields(HttpContext context)
        {
            var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            var xmlData = GenXmlFunctions.GetGenXmlByAjax(strIn, "");
            var objInfo = new NBrightInfo();
            objInfo.ItemID = -1;
            objInfo.TypeCode = "AJAXDATA";
            objInfo.XMLData = xmlData;
            var dic = objInfo.ToDictionary();
            return dic;
        }

        private Boolean CheckRights()
        {
            if (UserController.Instance.GetCurrentUserInfo().IsInRole(StoreSettings.ManagerRole) || UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators") || UserController.Instance.GetCurrentUserInfo().IsInRole("Editor"))

            {
                return true;
            }
            return false;
        }

        private Boolean CheckAdminRights()
        {
            if (UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators"))
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
