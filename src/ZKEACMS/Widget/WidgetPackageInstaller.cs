/* http://www.zkea.net/ 
 * Copyright (c) ZKEASOFT. All rights reserved. 
 * http://www.zkea.net/licenses */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZKEACMS.PackageManger;
using Microsoft.AspNetCore.Hosting;
using Easy.Modules.DataDictionary;

namespace ZKEACMS.Widget
{
    public class WidgetPackageInstaller : FilePackageInstaller
    {
        private readonly IWidgetActivator _widgetActivator;
        public const string InstallerName = "WidgetPackageInstaller";
        public WidgetPackageInstaller(IWebHostEnvironment hostingEnvironment, IWidgetActivator widgetActivator) : base(hostingEnvironment)
        {
            _widgetActivator = widgetActivator;
        }

        public override string PackageInstaller
        {
            get
            {
                return InstallerName;
            }
        }
        public override object Install(Package package)
        {
            var widgetPackage = Easy.Serializer.JsonConverter.Deserialize<WidgetPackage>(package.ToString());
            widgetPackage.SetRowData(package.GetRowData());

            if (widgetPackage.Widget == null) return null;

            var widget = Easy.Serializer.JsonConverter.Deserialize(JObject.Parse(package.ToString()).GetValue("Widget").ToString(), widgetPackage.Widget.GetViewModelType()) as WidgetBase;
            widget.PageId = null;
            widget.LayoutId = null;
            widget.ZoneId = null;
            widget.IsSystem = false;
            widget.IsTemplate = true;
            widgetPackage.Widget = widget;
            _widgetActivator.Create(widgetPackage.Widget).InstallWidget(widgetPackage);
            return null;
        }
        public override Package Pack(object obj)
        {
            var widget = obj as WidgetBase;
            var widgetService = _widgetActivator.Create(widget);
            var package = widgetService.PackWidget(widgetService.GetWidget(widget));
            return package;
        }
        public override FilePackage CreatePackage()
        {
            return new WidgetPackage(PackageInstaller);
        }
    }

    public class DataDictionaryPackageInstaller : FilePackageInstaller
    {
        private readonly IDataDictionaryService _dataDictionaryService;
        public DataDictionaryPackageInstaller(IWebHostEnvironment hostingEnvironment, IDataDictionaryService dataDictionaryService) : base(hostingEnvironment)
        {
            _dataDictionaryService = dataDictionaryService;
        }

        public override string PackageInstaller
        {
            get
            {
                return "DataDictionaryPackageInstaller";
            }
        }
        public override object Install(Package package)
        {
            base.Install(package);
            DataDictionaryPackage dicPackage = package as DataDictionaryPackage;
            if (dicPackage != null)
            {
                var exists = _dataDictionaryService.Count(m => m.DicName == dicPackage.DataDictionary.DicName && m.DicValue == dicPackage.DataDictionary.DicValue);
                if (exists == 0)
                {
                    dicPackage.DataDictionary.ID = 0;
                    _dataDictionaryService.Add(dicPackage.DataDictionary);
                }
            }

            return null;
        }
        public override Package Pack(object obj)
        {
            DataDictionaryPackage package = null;
            if (OnPacking != null)
            {
                package = base.Pack(OnPacking()) as DataDictionaryPackage;
            }
            if (package == null)
            {
                package = CreatePackage() as DataDictionaryPackage;
            }
            package.DataDictionary = obj as DataDictionaryEntity;
            return package;
        }
        public override FilePackage CreatePackage()
        {
            return new DataDictionaryPackage(PackageInstaller);
        }
        public Func<IEnumerable<System.IO.FileInfo>> OnPacking { get; set; }
    }
}
