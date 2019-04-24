﻿using System;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Recipes;

namespace MLS.Agent.Controllers
{
    public class EmbeddableController : Controller
    {

        [HttpGet]
        [Route("/ide")]
        [Route("/editor")]
        [Route("/v2/ide")]
        [Route("/v2/editor")]
        public IActionResult Html()
        {
            return Content($@"<!DOCTYPE html>
<html lang=""en"">
    <head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <meta name=""robots"" content=""noindex"" />
    <meta http-equiv=""Content-Type"" content=""text/html;charset=utf-8"">
        <link rel=""styleSheet"" href=""/client/bundle.css?v={VersionSensor.Version().AssemblyVersion}"" type=""text/css""/>
    </head>

    <body>
        <div id=""root""></div>

        <script id=""bundlejs""
            data-client-parameters=""{GetClientParameters()}""
            src=""/client/bundle.js?v={VersionSensor.Version().AssemblyVersion}""></script>
    </body>
</html>
", "text/html");
        }

        private string GetClientParameters()
        {
            var referrer = HttpContext.Request.Headers["referer"].ToString();

            if (!string.IsNullOrWhiteSpace(referrer) && Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
            {
                var parameters = new ClientParameters
                                 {
                                     referrer = uri
                                 };

                return HttpUtility.HtmlAttributeEncode(parameters.ToJson());
            }

            return new object().ToJson();
        }

        public class ClientParameters
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string workspaceType { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string scaffold { get; set; }


            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? enableBlazor { get; set; }

            public Uri referrer { get; set; }
        }
    }
}