﻿using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swagger.WebApiProxy.Core.Models;

namespace Swagger.WebApiProxy.Core
{
    public class SwaggerParser
    {
        readonly CSharpCodeProvider _csharpCodeProvider = new CSharpCodeProvider();

        public ProxyDefinition ParseSwaggerDoc(string document)
        {
            var jObject = JObject.Parse(document);

            var proxyDefinition = new ProxyDefinition();
            foreach (var definitionToken in jObject["definitions"].Where(i=>i.Type == JTokenType.Property).Cast<JProperty>())
            {
                var classDefinition = new ClassDefinition(definitionToken.Name);
                var properties = definitionToken.Value["properties"];
                foreach (var prop in properties)
                {
                    var typeName = GetTypeName(prop.First);
                    var name = _csharpCodeProvider.CreateValidIdentifier(((JProperty) prop).Name);
                    classDefinition.Properties.Add(new Property(typeName, name));
                }

                proxyDefinition.ClassDefinitions.Add(classDefinition);
            }

            return proxyDefinition;
        }

        internal string GetTypeName(JToken token)
        {
            var refType  =token["$ref"] as JValue;
            if (refType != null)
            {
                return _csharpCodeProvider.CreateValidIdentifier(refType.Value.ToString());
            }

            var type = (JValue) token["type"];
            if (type.Value.Equals("boolean"))
            {
                return "bool";
            }
            if (type.Value.Equals("string"))
            {
                var format = token["format"] as JValue;
                if (format == null)
                    return "string";

                if (format.Value.Equals("date") || format.Value.Equals("date-time"))
                    return "DateTime";

                if (format.Value.Equals("byte"))
                    return "byte";

                return "string";
            }

            if (type.Value.Equals("integer"))
            {
                var format = token["format"] as JValue;
                if (format != null)
                {
                    if (format.Value.Equals("int32"))
                        return "int";

                    if (format.Value.Equals("int64"))
                        return "long";
                }

                return "int";
            }

            if (type.Value.Equals("number"))
            {
                var format = token["format"] as JValue;
                if (format != null)
                {
                    if (format.Value.Equals("float"))
                        return "float";

                    if (format.Value.Equals("double"))
                        return "long";
                }
            }

            if (type.Value.Equals("array"))
            {
                var jToken = token["items"];
                return string.Format("List<{0}>", GetTypeName(jToken));
            }

            return "";
        }
    }
}
