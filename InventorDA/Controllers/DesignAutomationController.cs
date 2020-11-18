///////////////////////////////////////////////////////////////////////
//// Copyright (c) Autodesk, Inc. All rights reserved
//// Written by Peter Van Avondt - TSS EMEA
//// Based on code written by Forge Partner Development
////
//// Permission to use, copy, modify, and distribute this software in
//// object code form for any purpose and without fee is hereby granted,
//// provided that the above copyright notice appears in all copies and
//// that both that copyright notice and the limited warranty and
//// restricted rights notice below appear in all supporting
//// documentation.
////
//// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
//// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
//// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
//// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
//// UNINTERRUPTED OR ERROR FREE.
///////////////////////////////////////////////////////////////////////

using Autodesk.Forge;
using Autodesk.Forge.DesignAutomation;
using Autodesk.Forge.DesignAutomation.Model;
using Autodesk.Forge.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Activity = Autodesk.Forge.DesignAutomation.Model.Activity;
using Alias = Autodesk.Forge.DesignAutomation.Model.Alias;
using AppBundle = Autodesk.Forge.DesignAutomation.Model.AppBundle;
using Parameter = Autodesk.Forge.DesignAutomation.Model.Parameter;
using WorkItem = Autodesk.Forge.DesignAutomation.Model.WorkItem;
using WorkItemStatus = Autodesk.Forge.DesignAutomation.Model.WorkItemStatus;

namespace InventorDA.Controllers
{
    [ApiController]
    public class DesignAutomationController : ControllerBase
    {
        private IWebHostEnvironment _env;
        private IHubContext<DesignAutomationHub> _hubContext;
        DesignAutomationClient _designAutomation;
        static readonly string EngineName = "Autodesk.Inventor+2021";
        string LocalAppPackageZip { get { return Path.Combine(_env.WebRootPath, @"Bundle/UpdateParamsBundle.zip"); } }
        static readonly string APPNAME = "DivaConfigurator";
        static readonly string ACTIVITY_NAME = "DivaActivity";
        static readonly string ALIAS = "dev";
        static readonly string outputCOLFile = "Result.collaboration";
        static readonly string outputSATFile = "Result.sat";
        static readonly string outputDWFFile = "Result.dwfx";
        static readonly string outputPNGFile = "Result.png";
        static readonly string outputPDFFile = "Result.pdf";

        public static string nickName { get { return OAuthController.GetAppSetting("FORGE_CLIENT_ID"); } }
       

        public DesignAutomationController(IWebHostEnvironment env, IHubContext<DesignAutomationHub> hubContext, DesignAutomationClient api)
        {
            _designAutomation = api;
            _env = env;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Route("api/forge/params/designautomation")]
        public async void Post([FromBody] DAInputs value)
        {
            await CreateBucket();

            await CreateActivity();
            await CreateWorkItem(value);
        }

        /// <summary>
        /// Creates Activity
        /// </summary>
        private async Task<dynamic> CreateActivity()
        {
            // App Bundle Name
            string appBundleID = string.Format("{0}.{1}+{2}", nickName, APPNAME, ALIAS);

            // Check if App Bundle Exists
            Page<string> appBundles = await _designAutomation.GetAppBundlesAsync();

            if (!appBundles.Data.Contains(appBundleID))
            {
                // Local App Bundle Package
                if (!System.IO.File.Exists(LocalAppPackageZip)) throw new Exception("Appbundle not found at " + LocalAppPackageZip);

                // Create App Bundle
                AppBundle appBundleSpec = new AppBundle()
                {
                    Package = APPNAME,
                    Engine = EngineName,
                    Id = APPNAME,
                    Description = string.Format("Description for {0}", APPNAME),
                };
                AppBundle newApp = await _designAutomation.CreateAppBundleAsync(appBundleSpec);
                if (newApp == null) throw new Exception("Cannot create new app");

                // create alias pointing to v1
                Alias aliasSpec = new Alias() { Id = ALIAS, Version = 1 };
                Alias newAlias = await _designAutomation.CreateAppBundleAliasAsync(APPNAME, aliasSpec);

                // upload the zip with .bundle
                RestClient uploadClient = new RestClient(newApp.UploadParameters.EndpointURL);
                RestRequest request = new RestRequest(string.Empty, Method.POST);
                request.AlwaysMultipartFormData = true;
                foreach (KeyValuePair<string, string> x in newApp.UploadParameters.FormData) request.AddParameter(x.Key, x.Value);
                request.AddFile("file", LocalAppPackageZip);
                request.AddHeader("Cache-Control", "no-cache");
                await uploadClient.ExecuteTaskAsync(request);
            }


            Page<string> activities = await _designAutomation.GetActivitiesAsync();
            string qualifiedActivityId = string.Format("{0}.{1}+{2}", nickName, ACTIVITY_NAME, ALIAS);


            if (!activities.Data.Contains(qualifiedActivityId))
            {
                // define the activity
                string commandLine = $"$(engine.path)\\InventorCoreConsole.exe /al \"$(appbundles[{APPNAME}].path)\"";

                Activity activitySpec = new Activity()
                {
                    Id = ACTIVITY_NAME,
                    Appbundles = new List<string>() { string.Format("{0}.{1}+{2}", nickName, APPNAME, ALIAS) },
                    CommandLine = new List<string>() { commandLine },
                    Engine = EngineName,
                    Parameters = new Dictionary<string, Parameter>()
                        {
                            { "inputJson", new Parameter() { Description = "input json", LocalName = "params.json", Ondemand = false, Required = false, Verb = Verb.Get, Zip = false } },
                            { "ResultCOL", new Parameter() { Description = "output Collaboration file", LocalName = outputCOLFile, Ondemand = false, Required = true, Verb = Verb.Put, Zip = false } },
                            { "ResultPNG", new Parameter() { Description = "output IMAGE file", LocalName = outputPNGFile, Ondemand = false, Required = true, Verb = Verb.Put, Zip = false } },
                            { "ResultSAT", new Parameter() { Description = "output SAT file", LocalName = outputSATFile, Ondemand = false, Required = true, Verb = Verb.Put, Zip = false } },
                            { "ResultDWF", new Parameter() { Description = "output DWF file", LocalName = outputDWFFile, Ondemand = false, Required = true, Verb = Verb.Put, Zip = false } },
                            { "ResultPDF", new Parameter() { Description = "output PDF file", LocalName = outputPDFFile, Ondemand = false, Required = true, Verb = Verb.Put, Zip = false } }
                        }
                };
                Activity newActivity = await _designAutomation.CreateActivityAsync(activitySpec);


                // specify the alias for this Activity
                Alias aliasSpec = new Alias() { Id = ALIAS, Version = 1 };
                Alias newAlias = await _designAutomation.CreateActivityAliasAsync(ACTIVITY_NAME, aliasSpec);

                return Ok(new { Activity = qualifiedActivityId });
            }
            return Ok(new { Activity = "Activity already defined" });
        }


        // <summary>
        // Creates WorkItem
        // </summary>
        private async Task<IActionResult> CreateWorkItem(DAInputs param)
        {
            // OAuth token
            dynamic oauth = await OAuthController.GetInternalAsync();
            string bucketkey = "inventorilogicdapat" + nickName.ToLower();
            string qualifiedActivityId = string.Format("{0}.{1}+{2}", nickName, ACTIVITY_NAME, ALIAS);

            // input json

            dynamic inputParamJson = new JObject();
            foreach (ModelAttribute attribute in param.ModelAttributes)
            {
                inputParamJson[attribute.name] = attribute.value;
            }
            dynamic inputJson = new JObject();
            inputJson.UserParams = inputParamJson;

            inputJson.InputModel = param.inputmodel;
            inputJson.InputDrawing = param.inputdrawing;
            inputJson.ConfigurationModel = param.configurationmodel;



            XrefTreeArgument inputJsonArgument = new XrefTreeArgument()
            {
                Url = "data:application/json," + inputJson.ToString(Formatting.None)
            };


            //  output COL file
            XrefTreeArgument outputCOLFileArgument = new XrefTreeArgument()
            {
                Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketkey, outputCOLFile),
                Verb = Verb.Put,
                Headers = new Dictionary<string, string>()
                       {
                           {"Authorization", "Bearer " + oauth.access_token }
                       }
            };


            //  output IMAGE file
            XrefTreeArgument outputPNGFileArgument = new XrefTreeArgument()
            {
                Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketkey, outputPNGFile),
                Verb = Verb.Put,
                Headers = new Dictionary<string, string>()
                   {
                       {"Authorization", "Bearer " + oauth.access_token }
                   }
            };

            //  output PDF file
            XrefTreeArgument outputPDFFileArgument = new XrefTreeArgument()
            {
                Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketkey, outputPDFFile),
                Verb = Verb.Put,
                Headers = new Dictionary<string, string>()
                   {
                       {"Authorization", "Bearer " + oauth.access_token }
                   }
            };


            //  output DWF file
            XrefTreeArgument outputDWFFileArgument = new XrefTreeArgument()
            {
                Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketkey, outputDWFFile),
                Verb = Verb.Put,
                Headers = new Dictionary<string, string>()
                   {
                       {"Authorization", "Bearer " + oauth.access_token }
                   }
            };


            //  output SAT file
            XrefTreeArgument outputSATFileArgument = new XrefTreeArgument()
            {
                Url = string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketkey, outputSATFile),
                Verb = Verb.Put,
                Headers = new Dictionary<string, string>()
                   {
                       {"Authorization", "Bearer " + oauth.access_token }
                   }
            };


            string callbackUrl = string.Format("{0}/api/forge/callback/designautomation?id={1}&configurationid={2}", OAuthController.GetAppSetting("FORGE_WEBHOOK_URL"), param.browserconnectionId, param.configurationId);


            WorkItem workItemSpec = new WorkItem()
            {
                ActivityId = qualifiedActivityId,
                Arguments = new Dictionary<string, IArgument>()
                {
                    { "inputJson",  inputJsonArgument },
                    { "ResultCOL", outputCOLFileArgument },
                    { "ResultPNG", outputPNGFileArgument },
                    { "ResultSAT", outputSATFileArgument },
                    { "ResultPDF", outputPDFFileArgument },
                    { "ResultDWF", outputDWFFileArgument },
                    { "onComplete", new XrefTreeArgument { Verb = Verb.Post, Url = callbackUrl } }
                }
            };
            WorkItemStatus workItemStatus = await _designAutomation.CreateWorkItemAsync(workItemSpec);
            return Ok(new { WorkItemId = workItemStatus.Id });

        }

        /// <summary>
        /// Callback from Design Automation Workitem
        /// </summary>
        [HttpPost]
        [Route("/api/forge/callback/designautomation")]
        public async Task<IActionResult> OnCallback([FromBody] dynamic body, string id, string configurationid)
        {
            try
            {

                //do something wiht the resultbody...
                var resultbody = body;
                if (resultbody.status == "success")
                {


                    dynamic oauth = await OAuthController.GetInternalAsync();
                    string bucketkey = "inventorilogicdapat" + nickName.ToLower();

                    ObjectsApi objectsApi = new ObjectsApi();

                    objectsApi.Configuration.AccessToken = oauth.access_token;
                    dynamic objModel = await objectsApi.GetObjectDetailsAsync(bucketkey, outputCOLFile);

                    // create unique files for the configuration by copy the result files to a new file

                    // collaboration file
                    string NewoutputCOLFile = configurationid + ".collaboration";
                    objModel = await objectsApi.CopyToAsync(bucketkey, outputCOLFile, NewoutputCOLFile);
                    dynamic urnModel = TranslateObject(objModel, NewoutputCOLFile);
                    dynamic signedCOLUrl = objectsApi.CreateSignedResourceAsyncWithHttpInfo(bucketkey, NewoutputCOLFile, new PostBucketsSigned(10), "read");

                    // PNG File
                    string NewoutputPNGFile = configurationid + ".png";
                    await objectsApi.CopyToAsync(bucketkey, outputPNGFile, NewoutputPNGFile);
                    dynamic signedPNGUrl = objectsApi.CreateSignedResourceAsyncWithHttpInfo(bucketkey, NewoutputPNGFile, new PostBucketsSigned(10), "read");

                    // PDF File
                    string NewoutputPDFFile = configurationid + ".pdf";
                    await objectsApi.CopyToAsync(bucketkey, outputPDFFile, NewoutputPDFFile);
                    dynamic signedPDFUrl = objectsApi.CreateSignedResourceAsyncWithHttpInfo(bucketkey, NewoutputPDFFile, new PostBucketsSigned(10), "read");

                    // SAT File
                    string NewoutputSATFile = configurationid + ".sat";
                    await objectsApi.CopyToAsync(bucketkey, outputSATFile, NewoutputSATFile);
                    dynamic signedSATUrl = objectsApi.CreateSignedResourceAsyncWithHttpInfo(bucketkey, NewoutputSATFile, new PostBucketsSigned(10), "read");

                    // DWF File
                    string NewoutputDWFFile = configurationid + ".dwf";
                    await objectsApi.CopyToAsync(bucketkey, outputDWFFile, NewoutputDWFFile);
                    dynamic signedDWFUrl = objectsApi.CreateSignedResourceAsyncWithHttpInfo(bucketkey, NewoutputDWFFile, new PostBucketsSigned(10), "read");

                    await _hubContext.Clients.Client(id).SendAsync("downloadResult", (string)await urnModel, (string)(await signedCOLUrl).Data.signedUrl, (string)(await signedPNGUrl).Data.signedUrl, (string)(await signedPDFUrl).Data.signedUrl, (string)(await signedSATUrl).Data.signedUrl, (string)(await signedDWFUrl).Data.signedUrl);

                    return Ok(resultbody);
                }
                else
                {
                    string status = resultbody.status;
                    string report = resultbody.reportUrl;
                    await _hubContext.Clients.Client(id).SendAsync("OnFailure", status, report);

                    return StatusCode(500, resultbody);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                await _hubContext.Clients.Client(id).SendAsync("OnFailure", e.Message, e.InnerException);
                return StatusCode(500, e.Message);
            }
            // return Ok();
        }

        /// <summary>
        /// Create Bucket
        /// </summary>
        private async Task<IActionResult> CreateBucket()
        {
            // Authenicate using the Forge Authenthication 
            dynamic oauth = await OAuthController.GetInternalAsync();

            // Initiate bucket name
            string bucketkey = "inventorilogicdapat" + nickName.ToLower();

            // Connect to Bucket API
            BucketsApi bucketsApi = new BucketsApi();
            bucketsApi.Configuration.AccessToken = oauth.access_token;

            // Check if Bucket already exists
            dynamic buckets = await bucketsApi.GetBucketsAsync();
            bool bucketExists = buckets.items.ToString().Contains(bucketkey);
            if (!bucketExists)
            {
                // Create new bucket
                PostBucketsPayload postBucket = new PostBucketsPayload(bucketkey, null, PostBucketsPayload.PolicyKeyEnum.Transient);
                dynamic newbucket = await bucketsApi.CreateBucketAsync(postBucket);
            }
            return Ok();
        }

        /// <summary>
        /// Translate object
        /// </summary>
        private async Task<dynamic> TranslateObject(dynamic objModel, string outputFileName)
        {
            dynamic oauth = await OAuthController.GetInternalAsync();
            string objectIdBase64 = ToBase64(objModel.objectId);
            // prepare the payload
            List<JobPayloadItem> postTranslationOutput = new List<JobPayloadItem>()
            {
            new JobPayloadItem(
                JobPayloadItem.TypeEnum.Svf,
                new List<JobPayloadItem.ViewsEnum>()
                {
                JobPayloadItem.ViewsEnum._2d,
                JobPayloadItem.ViewsEnum._3d
                })
            };
            JobPayload job;
            job = new JobPayload(
                new JobPayloadInput(objectIdBase64, false, outputFileName),
                new JobPayloadOutput(postTranslationOutput)
                );

            // start the translation
            DerivativesApi derivative = new DerivativesApi();
            derivative.Configuration.AccessToken = oauth.access_token;
            dynamic jobPosted = await derivative.TranslateAsync(job, true);
            // check if it is complete.
            dynamic manifest = null;
            do
            {
                System.Threading.Thread.Sleep(1000); // wait 1 second
                try
                {
                    manifest = await derivative.GetManifestAsync(objectIdBase64);
                }
                catch (Exception) { }
            } while (manifest.progress != "complete");
            return jobPosted.urn;
        }

        /// <summary>
        /// Convert a string into Base64 (source http://stackoverflow.com/a/11743162).
        /// </summary>  
        private static string ToBase64(string input)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(input);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Class used for Inputs
        /// </summary>
        public class ModelAttributes
        {
            public string rim { get; set; }
            public string color { get; set; }
            public string diameter { get; set; }
            public string width { get; set; }
            public string browserconnectionId { get; set; }
        }

        /// <summary>
        /// Class used for Inputs
        /// </summary>
        public class DAInputs
        {
            public List<ModelAttribute> ModelAttributes { get; set; }
            public string browserconnectionId { get; set; }
            public string inputmodel { get; set; }
            public string inputdrawing { get; set; }
            public string configurationmodel { get; set; }
            public string configurationId { get; set; }
            public DAInputs()
            {
                ModelAttributes = new List<ModelAttribute>();

            }
        }

        /// <summary>
        /// Class used for Model Inputs
        /// </summary>
        public class ModelAttribute
        {
            public string name { get; set; }
            public string value { get; set; }
        }

    }
    /// <summary>
    /// Class used for SignalR
    /// </summary>
    public class DesignAutomationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public string GetConnectionId() { return Context.ConnectionId; }
    }
}
