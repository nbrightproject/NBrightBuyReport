using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Xml;
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
    /// <summary>
    /// Summary description for XMLconnector
    /// </summary>
    public class XmlConnector : IHttpHandler
    {
        private String _lang = "";
        private String _itemid = "";

        public void ProcessRequest(HttpContext context)
        {
            #region "Initialize"

            var strOut = "";
            try
            {

                var moduleid = Utils.RequestQueryStringParam(context, "mid");
                var paramCmd = Utils.RequestQueryStringParam(context, "cmd");
                var lang = Utils.RequestQueryStringParam(context, "lang");
                var language = Utils.RequestQueryStringParam(context, "language");
                _itemid = Utils.RequestQueryStringParam(context, "itemid");

                #region "setup language"

                // because we are using a webservice the system current thread culture might not be set correctly,
                //  so use the lang/language param to set it.
                if (lang == "") lang = language;
                if (!string.IsNullOrEmpty(lang)) _lang = lang;

                // default to current thread if we have no language.
                if (_lang == "") _lang = System.Threading.Thread.CurrentThread.CurrentCulture.ToString();

                System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture(_lang);

                #endregion

                #endregion

            #region "Do processing of command"

                strOut = "ERROR!! - No Security rights for current user!";
                if (CheckRights())
                {
                    switch (paramCmd)
                    {
                        case "test":
                            strOut = "<root>" + UserController.Instance.GetCurrentUserInfo().Username + "</root>";
                            break;
                        case "getdata":
                            strOut = GetData(context);
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
                            strOut = SaveData(context);
                            break;
                        case "addreport":
                            strOut = AddReport(context);
                            break;
                        case "savereport":
                            strOut = SaveReport(context);
                            break;
                        case "deletereport":
                            strOut = DeleteReport(context);
                            break;
                        case "getreportlist":
                            strOut = GetReportList(context);
                            break;
                        case "editreport":
                            strOut = EditReport(context);
                            break;
                        case "reportselection":
                            strOut = ReportSelection(context);
                            break;
                        case "checkadminrights":
                            strOut = CheckAdminRights(context);
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
            if (editlang == "") editlang = _lang;

            if (!Utils.IsNumeric(moduleid)) moduleid = "-2"; // use moduleid -2 for razor

            if (clearCache) NBrightBuyUtils.RemoveModCache(Convert.ToInt32(moduleid));

            if (newitem == "new") selecteditemid = AddNew(moduleid, typeCode);

            var templateControl = "/DesktopModules/NBright/NBrightBuyReport";

            if (Utils.IsNumeric(selecteditemid))
            {
                // do edit field data if a itemid has been selected
                var obj = objCtrl.Get(Convert.ToInt32(selecteditemid), "", editlang);
                strOut = NBrightBuyUtils.RazorTemplRender(typeCode.ToLower() + "fields.cshtml", Convert.ToInt32(moduleid), _lang + itemid + editlang + selecteditemid, obj, templateControl, "config", _lang, StoreSettings.Current.Settings());
            }
            else
            {
                // Return list of items
                var l = objCtrl.GetList(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), typeCode, "", " order by [XMLData].value('(genxml/textbox/validuntil)[1]','nvarchar(50)'), ModifiedDate desc", 0, 0, 0, 0, editlang);
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

        private void AddReport(HttpContext context)
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

        }

        private String SaveReport(HttpContext context)
        {
            var settings = GetAjaxFields(context);

            if (settings.ContainsKey("itemid") && Utils.IsNumeric(settings["itemid"]))
            {
                var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                var objCtrl = new NBrightBuyController();
                var obj = objCtrl.Get(Convert.ToInt32(settings["itemid"]));
                obj.UpdateAjax(strIn);
                objCtrl.Update(obj);

                var cachekey = "GetReportListData*" + PortalSettings.Current.PortalId.ToString("");
                Utils.RemoveCache(cachekey);

                return GetReportListData(settings);
            }

            return "Error!! - Invalid ItemId on SaveReport";

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

                return GetReportListData(settings);
            }

            return "Error!! - Invalid ItemId on SaveReport";

        }

        private String GetReportList(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                return GetReportListData(settings);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private String EditReport(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);

                var strOut = "Error!! - Invalid ItemId on Edit Report";
                if (settings.ContainsKey("itemid") && Utils.IsNumeric(settings["itemid"]))
                {
                    if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings

                    var objCtrl = new NBrightBuyController();
                    var bodyTempl = NBrightBuyUtils.GetTemplateData("NBSREPORTfields.html", "", "config", StoreSettings.Current.Settings());

                    var obj = objCtrl.Get(Convert.ToInt32(settings["itemid"]));
                    if (obj != null)
                    {
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

        private String RunReport(HttpContext context)
        {
            var strSql = "";
            try
            {
                var settings = GetAjaxFields(context);
                var strOut = "Error!! - Invalid ItemId on Run Report";

                if (settings.ContainsKey("itemid") && Utils.IsNumeric(settings["itemid"]))
                {
                    if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings

                    var objCtrl = new NBrightBuyController();
                    var obj = objCtrl.Get(Convert.ToInt32(settings["itemid"])); 
                    if (obj != null)
                    {
                        var xslTemp = obj.GetXmlProperty("genxml/textbox/templatexsl");
                        strSql = obj.GetXmlProperty("genxml/textbox/sql");
                        var extension = obj.GetXmlProperty("genxml/textbox/extension");
                        // replace any settings tokens (This is used to place the form data into the SQL)
                        strSql = Utils.ReplaceSettingTokens(strSql, settings);
                        strSql = Utils.ReplaceUrlTokens(strSql);

                        strSql = GenXmlFunctions.StripSqlCommands(strSql); // don't allow anything to update through here.

                        // add FOR XML if not there, this function will only output XML results.
                        if (!strSql.ToLower().Contains("for xml")) strSql += " FOR XML PATH('item'), ROOT('root')";

                        var strXmlResults = objCtrl.GetSqlxml(strSql);
                        if (xslTemp != "")
                        {
                            strOut = "";
                            if (!strXmlResults.StartsWith("<root>")) strXmlResults = "<root>" + strXmlResults + "</root>"; // always wrap with root node.
                            var strReportResults = XslUtils.XslTransInMemory(strXmlResults, xslTemp);
                            if (obj.GetXmlPropertyBool("genxml/checkbox/download"))
                            {
                                var k = Utils.GetUniqueKey(4); // use unique key so we don't get caching issues with browsers
                                var filename = StoreSettings.Current.FolderTempMapPath + "\\reportdownload" + k + "." + extension.Trim('.');
                                var relfilename = StoreSettings.Current.FolderTemp + "/reportdownload" + k + "." + extension.Trim('.');
                                if (!settings.ContainsKey("filename")) settings.Add("filename", filename);
                                Utils.SaveFile(filename, strReportResults);
                                strOut = NBrightBuyUtils.GetTemplateData("download.html", "", "config", settings);
                            }
                            strOut += "<br/>";
                            if (obj.GetXmlPropertyBool("genxml/checkbox/inline"))
                            {
                                strOut += strReportResults;
                            }
                            var nbi = new NBrightInfo();
                            nbi.XMLData = strXmlResults;
                            var nods = nbi.XMLDoc.SelectNodes("root/*");
                            if (nods != null)
                            {
                                strOut = strOut.Replace("[itemscount]", nods.Count.ToString(""));
                            }
                            strOut = GenXmlFunctions.RenderRepeater(obj, strOut);
                        }
                        else
                        {
                            strOut = "Error!! - XSL template required ";
                        }
                    }
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString() + " <hr/> " + strSql;
            }
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
                        var bodyTempl = NBrightBuyUtils.GetTemplateData("selection.html", "", "config", StoreSettings.Current.Settings());
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



        private String GetReportListData(Dictionary<String, String> settings, bool paging = true)
        {
            var strOut = "";
            var cachekey = "GetReportListData*" + PortalSettings.Current.PortalId.ToString("");
            var cacheobj = Utils.GetCache(cachekey);
            if (cacheobj != null) return cacheobj.ToString();


            if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings

            var objCtrl = new NBrightBuyController();

           // var headerTempl = NBrightBuyUtils.GetTemplateData("listh.html", "", "config", StoreSettings.Current.Settings());
            var bodyTempl = NBrightBuyUtils.GetTemplateData("NBSREPORTlist.cshtml", "", "config", StoreSettings.Current.Settings());
            //var footerTempl = NBrightBuyUtils.GetTemplateData("listf.html", "", "config", StoreSettings.Current.Settings());

            var obj = new NBrightInfo(true);
            //strOut = GenXmlFunctions.RenderRepeater(obj, headerTempl);

            var objList = objCtrl.GetDataList(PortalSettings.Current.PortalId, -1, "NBSREPORT", "", "", "", "", true);
            strOut += GenXmlFunctions.RenderRepeater(objList, bodyTempl);

            //strOut += GenXmlFunctions.RenderRepeater(obj, footerTempl);

            Utils.SetCache(cachekey, strOut);

            return strOut;
        }

        private Dictionary<String, String> GetAjaxFields(HttpContext context)
        {
            var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            var xmlData = GenXmlFunctions.GetGenXmlByAjax(strIn, "");
            var objInfo = new NBrightInfo();

            objInfo.ItemID = -1;
            objInfo.TypeCode = "AJAXDATA";
            objInfo.XMLData = xmlData;
            var dic = objInfo.ToDictionary();
            // set langauge if we have it passed.
            if (dic.ContainsKey("lang") && dic["lang"] != "") _lang = dic["lang"];

            // set the context  culturecode, so any DNN functions use the correct culture (entryurl tag token)
            if (_lang != "" && _lang != System.Threading.Thread.CurrentThread.CurrentCulture.ToString()) System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(_lang);
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