﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Abp.Extensions;
using Abp.Web.Api.Modeling;

namespace Abp.Web.Api.ProxyScripting.Generators
{
    internal static class ProxyScriptingHelper
    {
        private const string ValidJsVariableNameChars = "abcdefghijklmnopqrstuxwvyxABCDEFGHIJKLMNORQRSTUXWVYZ0123456789_";

        private static string NormalizeJsVariableName(string name)
        {
            var sb = new StringBuilder(name);

            sb.Replace('-', '_');

            //Delete invalid chars
            foreach (var c in name)
            {
                if (!ValidJsVariableNameChars.Contains(c))
                {
                    sb.Replace(c.ToString(), "");
                }
            }

            if (sb.Length == 0)
            {
                return NormalizeJsVariableName("_" + Guid.NewGuid().ToString("N").Left(8));
            }

            return sb.ToString();
        }

        public static string GenerateUrlWithParameters(ActionApiDescriptionModel action)
        {
            //TODO: Can be optimized using StringBuilder?
            var url = ReplacePathVariables(action.Url, action.Parameters);
            url = AddQueryStringParameters(url, action.Parameters);
            return url;
        }

        public static string GenerateJsFuncParameterList(ActionApiDescriptionModel action, string ajaxParametersName)
        {
            var paramNames = action.Parameters.Select(prm => NormalizeJsVariableName(prm.Name.ToCamelCase())).ToList();
            paramNames.Add(ajaxParametersName);
            return string.Join(", ", paramNames);
        }

        public static string GenerateHeaders(ActionApiDescriptionModel action, int indent = 0)
        {
            var parameters = action
                .Parameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Header)
                .ToArray();

            if (!parameters.Any())
            {
                return null;
            }

            return CreateJsObjectLiteral(parameters, indent);
        }

        public static string GenerateBody(ActionApiDescriptionModel action)
        {
            var parameters = action
                .Parameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Body)
                .ToArray();

            if (parameters.Length <= 0)
            {
                return null;
            }

            if (parameters.Length > 1)
            {
                throw new AbpException(
                    $"Only one complex type allowed as argument to a controller action that's binding source is 'Body'. But {action.Name} ({action.Url}) contains more than one!"
                    );
            }

            return parameters[0].Name.ToCamelCase();
        }

        private static string CreateJsObjectLiteral(ParameterApiDescriptionModel[] parameters, int indent = 0)
        {
            var sb = new StringBuilder();

            sb.AppendLine("{");

            foreach (var prm in parameters)
            {
                sb.AppendLine($"{new string(' ', indent)}  '{prm.Name}': {NormalizeJsVariableName(prm.Name)}");
            }

            sb.Append(new string(' ', indent) + "}");

            return sb.ToString();
        }

        public static string GenerateFormPostData(ActionApiDescriptionModel action, int indent = 0)
        {
            var parameters = action
                .Parameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Form)
                .ToArray();

            if (!parameters.Any())
            {
                return null;
            }

            return CreateJsObjectLiteral(parameters, indent);
        }

        private static string ReplacePathVariables(string url, IList<ParameterApiDescriptionModel> actionParameters)
        {
            var pathParameters = actionParameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Path)
                .ToArray();

            if (!pathParameters.Any())
            {
                return url;
            }

            foreach (var pathParameter in pathParameters)
            {
                url = url.Replace($"{{{pathParameter.Name}}}", $"' + {pathParameter.Name.ToCamelCase()} + '");
            }

            return url;
        }

        private static string AddQueryStringParameters(string url, IList<ParameterApiDescriptionModel> actionParameters)
        {
            var queryStringParameters = actionParameters
                .Where(p => p.BindingSourceId.IsIn(ParameterBindingSources.ModelBinding, ParameterBindingSources.Query))
                .ToArray();

            if (!queryStringParameters.Any())
            {
                return url;
            }

            if (!url.Contains("?"))
            {
                url += "?";
            }

            for (var i = 0; i < queryStringParameters.Length; i++)
            {
                var parameterInfo = queryStringParameters[i];

                if (i > 0)
                {
                    url += "&";
                }

                url += (parameterInfo.Name.ToCamelCase() + "=' + escape(" + parameterInfo.Name.ToCamelCase() + ") + '");
            }

            return url;
        }
    }
}