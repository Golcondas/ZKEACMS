/* http://www.zkea.net/ 
 * Copyright (c) ZKEASOFT. All rights reserved. 
 * http://www.zkea.net/licenses */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc;
using Easy.Extend;
using Easy.MetaData;
using System.Text;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Easy.LINQ;
using Easy.ViewPort.Descriptor;
using Easy.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Easy.Mvc.TagHelpers
{
    public class GridTagHelper : TagHelperBase
    {
        private const string DefaultClass = "dataTable table table-striped table-bordered table-hover";
        private const string DefaultSourceAction = "GetList";
        public const string DefaultEditAction = "Edit";
        public const string DefaultDeleteAction = "Delete";
        private const string TableStructure = "<div><table class=\"{0}\" cellspacing=\"0\" width=\"100%\" data-source=\"{1}\"><thead><tr>{2}</tr></thead><tfoot><tr class=\"search\">{3}</tr></tfoot></table></div>";
        private const string TableHeadStructure = "<th data-key=\"{0}\" data-template=\"{1}\" data-order=\"{2}\" data-option=\"{4}\" data-search-operator=\"{5}\" data-data-type=\"{6}\" data-format=\"{7}\">{3}</th>";
        private const string TableSearchStructure = "<th></th>";
        public const string EditLinkTemplate = "<a href=\"{0}\" class=\"glyphicon glyphicon-edit\"></a>";
        public const string DeleteLinkTemplate = "<a href=\"{0}\" class=\"glyphicon glyphicon-remove\"></a>";
        public const string CheckboxTemplate = "<input type=\"checkbox\" class=\"glyphicon {1}\" value=\"{0}\">";
        public string Source { get; set; }
        public string Edit { get; set; }
        public string EditTemplate { get; set; }
        public string Delete { get; set; }
        public string DeleteTemplate { get; set; }
        public string GridClass { get; set; }
        public bool? EditAble { get; set; }
        public bool? DeleteAble { get; set; }
        public bool? DisplayCheckbox { get; set; }
        public string OrderAsc { get; set; }
        public string OrderDesc { get; set; }
        public string ActionLable { get; set; }
        public Type ModelType { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (Source.IsNullOrWhiteSpace())
            {
                Source = Url.Action(DefaultSourceAction);
            }
            if (GridClass.IsNullOrWhiteSpace())
            {
                GridClass = DefaultClass;
            }
            if (ModelType == null)
            {
                ModelType = ViewContext.ViewData.ModelMetadata.ModelType;
                if (OrderAsc.IsNullOrEmpty() && OrderDesc.IsNullOrEmpty() && typeof(EditorEntity).IsAssignableFrom(ModelType))
                {
                    OrderDesc = "LastUpdateDate";
                }
            }
            var viewConfig = ServiceLocator.GetViewConfigure(ModelType);
            var localize = ViewContext.HttpContext.RequestServices.GetService<ILocalize>();
            StringBuilder tableHeaderBuilder = new StringBuilder();
            StringBuilder tableSearchBuilder = new StringBuilder();
            if (viewConfig != null)
            {
                var primaryKey = viewConfig.MetaData.Properties.Select(m => m.Value).FirstOrDefault(m => m.CustomAttributes.Any(attr => attr.AttributeType == typeof(KeyAttribute)));

                if ((EditAble ?? true) && primaryKey != null)
                {
                    string name = primaryKey.Name.FirstCharToLowerCase();
                    if (name.Length == 2)
                    {
                        name = name.ToLower();
                    }

                    if (Edit.IsNullOrWhiteSpace())
                    {
                        Edit = Url.Action(DefaultEditAction) + "/{" + name + "}";
                    }
                    if (Delete.IsNullOrWhiteSpace())
                    {
                        Delete = Url.Action(DefaultDeleteAction) + "/{" + name + "}";
                    }
                    StringBuilder actionPartBuilder = new StringBuilder();
                    StringBuilder managerBuiller = new StringBuilder();
                    if (DisplayCheckbox ?? false)
                    {
                        managerBuiller.AppendLine(CheckboxTemplate.FormatWith("{" + name + "}", "select-item " + name));
                        actionPartBuilder.AppendLine(CheckboxTemplate.FormatWith(0, "select-all"));
                    }
                    managerBuiller.AppendLine((EditTemplate ?? EditLinkTemplate).FormatWith(Edit));
                    if (DeleteAble ?? true)
                    {
                        managerBuiller.AppendLine((DeleteTemplate ?? DeleteLinkTemplate).FormatWith(Delete));
                    }

                    actionPartBuilder.AppendLine(ActionLable ?? localize.Get("Action"));
                    tableHeaderBuilder.AppendFormat(TableHeadStructure,
                        name,
                        WebUtility.HtmlEncode(managerBuiller.ToString()),
                        string.Empty,
                        actionPartBuilder,
                        string.Empty,
                        Query.Operators.None,
                        string.Empty,
                        string.Empty);
                    tableSearchBuilder.Append(TableSearchStructure);
                }

                var columns = viewConfig.MetaData.ViewPortDescriptors.Select(m => m.Value)
                    .Where(m => m.IsShowInGrid)
                    .OrderBy(m => m.OrderIndex)
                    .Each(m =>
                    {
                        var dropDown = m as DropDownListDescriptor;
                        StringBuilder optionBuilder = new StringBuilder();
                        if (dropDown != null)
                        {
                            if (dropDown.OptionItems != null && dropDown.OptionItems.Any())
                            {
                                foreach (var item in dropDown.OptionItems)
                                {
                                    optionBuilder.AppendFormat("{{\"name\":\"{0}\",\"value\":\"{1}\"}},", item.Value, item.Key);
                                }
                            }
                            else if (dropDown.SourceType == Constant.SourceType.ViewData)
                            {
                                var selectList = ViewContext.ViewData[dropDown.SourceKey] as SelectList;
                                if (selectList != null)
                                {
                                    foreach (var item in selectList)
                                    {
                                        optionBuilder.AppendFormat("{{\"name\":\"{0}\",\"value\":\"{1}\"}},", item.Text, item.Value);
                                    }
                                }
                            }
                        }
                        else if (m.DataType == typeof(bool) || m.DataType == typeof(bool?))
                        {
                            optionBuilder.AppendFormat("{{\"name\":\"{0}\",\"value\":\"{1}\"}},", localize.Get("Yes"), "true");
                            optionBuilder.AppendFormat("{{\"name\":\"{0}\",\"value\":\"{1}\"}},", localize.Get("No"), "false");
                        }
                        tableHeaderBuilder.AppendFormat(TableHeadStructure,
                            m.Name.FirstCharToLowerCase(),
                            WebUtility.HtmlEncode(m.GridColumnTemplate),
                            OrderAsc == m.Name ? "asc" : OrderDesc == m.Name ? "desc" : "",
                            m.DisplayName,
                            optionBuilder.Length == 0 ? string.Empty : WebUtility.HtmlEncode($"[{optionBuilder.ToString().Trim(',')}]"),
                            m.SearchOperator,
                            m.DataType.Name,
                            (m as TextBoxDescriptor)?.JavaScriptDateFormat);
                        tableSearchBuilder.Append(TableSearchStructure);
                    });
            }
            else
            {
                foreach (var property in ModelType.GetProperties())
                {
                    tableHeaderBuilder.AppendFormat(TableHeadStructure,
                            property.Name.FirstCharToLowerCase(),
                            string.Empty,
                            OrderAsc == property.Name ? "asc" : OrderDesc == property.Name ? "desc" : "",
                            property.Name,
                            string.Empty,
                            Query.Operators.Equal,
                            (Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType).Name,
                            string.Empty);
                    tableSearchBuilder.Append(TableSearchStructure);
                }
            }
            output.TagName = "div";
            //output.Attributes.Add("class", "container-fluid");
            output.Content.SetHtmlContent(TableStructure.FormatWith(GridClass, Source, tableHeaderBuilder, tableSearchBuilder));
        }
    }
}
