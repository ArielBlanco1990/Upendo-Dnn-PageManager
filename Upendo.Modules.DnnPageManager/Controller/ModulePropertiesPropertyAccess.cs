﻿using DotNetNuke.Services.Tokens;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.Modules;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Dynamic;
using System.Web;
using Upendo.Modules.DnnPageManager.Common;
using System.IO;
using System.Collections;
using System.Resources;

namespace Upendo.Modules.DnnPageManager.Controller
{
    public class ModulePropertiesPropertyAccess : IPropertyAccess
    {
        private ModuleInstanceContext _moduleContext;

        public ModulePropertiesPropertyAccess(ModuleInstanceContext moduleContext)
        {
            _moduleContext = moduleContext;
        }

        public string GetProperty(string propertyName, string format, CultureInfo formatProvider, UserInfo accessingUser, Scope accessLevel, ref bool propertyNotFound)
        {
            int moduleId = _moduleContext.ModuleId;
            int portalId = _moduleContext.PortalId;
            int tabId = _moduleContext.TabId;
            ModuleInfo module = new ModuleController().GetModule(moduleId, tabId);

            string moduleDirectory = "/" + _moduleContext.Configuration.ModuleControl.ControlSrc; 
            moduleDirectory = moduleDirectory.Substring(0, moduleDirectory.LastIndexOf('/') + 1);

            switch (propertyName.ToLower())
            {
                case "all":
                    dynamic properties = new ExpandoObject();
                    properties.Resources = GetResources(module);
                    properties.Settings = _moduleContext.Settings;
                    properties.IsEditable = _moduleContext.IsEditable;
                    properties.EditMode = _moduleContext.EditMode;
                    properties.IsAdmin = accessingUser.IsInRole(PortalSettings.Current.AdministratorRoleName);
                    properties.ModuleId = _moduleContext.ModuleId;
                    properties.PortalId = _moduleContext.PortalId;
                    properties.UserId = accessingUser.UserID;
                    properties.HomeDirectory = PortalSettings.Current.HomeDirectory.Substring(1);
                    properties.ModuleDirectory = moduleDirectory;
                    properties.RawUrl = HttpContext.Current.Request.RawUrl;
                    properties.PortalLanguages = GetPortalLanguages();
                    properties.CurrentLanguage = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
                    properties.routingWebAPI = Constants.APIPath;
                    properties.TabId = _moduleContext.TabId;
                     
                    return JsonConvert.SerializeObject(properties);

                case "modulepath":
                    return moduleDirectory;
                case "approotangularbegin":
                    return "<app-root-" + moduleId + ">";
                case "approotangularend":
                    return "</app-root-" + moduleId + ">";
                case "ModuleId":
                    return _moduleContext.ModuleId.ToString();
                case "rawurl":
                    return HttpContext.Current.Request.RawUrl;
                case "test":
                    return "test";

            }
            return string.Empty;
        }

        public CacheLevel Cacheability
        {
            get { return CacheLevel.notCacheable; }
        }

        private Dictionary<string, string> GetResources(ModuleInfo module)
        {
            var currentLanguage = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            System.IO.FileInfo fi = new System.IO.FileInfo(HttpContext.Current.Server.MapPath("~/" + _moduleContext.Configuration.ModuleControl.ControlSrc + "." + currentLanguage + ".resx"));
            string physResourceFile = string.Format("{0}/{1}/{2}", fi.DirectoryName,Constants.Resources, fi.Name);
            string relResourceFile =string.Format("/{0}/{1}/{2}/{3}", Constants.DesktopModules, module.DesktopModule.FolderName , Constants.Resources, fi.Name);
            if (File.Exists(physResourceFile))
            {
                using (var rsxr = new ResXResourceReader(physResourceFile))
                {
                    var res = rsxr.OfType<DictionaryEntry>()
                        .ToDictionary(
                            entry => entry.Key.ToString().Replace(".", "_"),
                            entry => Localization.GetString(entry.Key.ToString(), relResourceFile));

                    return res;
                }
            }
            return new Dictionary<string, string>();
        }

        private List<string> GetPortalLanguages()
        {
            List<string> languages = new List<string>();
            LocaleController lc = new LocaleController();
            Dictionary<string, Locale> loc = lc.GetLocales(_moduleContext.PortalId);
            foreach (KeyValuePair<string, Locale> item in loc)
            {
                string cultureCode = item.Value.Culture.Name;
                languages.Add(cultureCode);
            }
            return languages;
        }
    }
}