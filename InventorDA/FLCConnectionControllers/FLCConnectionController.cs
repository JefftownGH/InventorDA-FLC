using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace InventorDA.Controllers
{
    [ApiController]
    public class FLCConnectionController : ControllerBase
    {
        public static string Tenant { get { return OAuthController.GetAppSetting("FLC_TENANT"); } }
        public static string FLCUser { get { return OAuthController.GetAppSetting("FLC_USER"); } }
        public static string BaseURL = $"https://{Tenant}.autodeskplm360.net";

        [HttpGet]
        [Route("api/FLC/GetWorkspaceItems/{wsid}")]
        public async Task<dynamic> GetworkspaceItems(string wsid = null)
        {

            string url = $"/api/v3/workspaces/{wsid}/items";
            string AcceptHeader = "application/json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            return response.Content;

        }

        [HttpGet]
        [Route("api/FLC/SearchItems/{wsid}")]
        public async Task<dynamic> SearchItems(string wsid = null, string query = null, int limit = 100, int offset = 0, int revision = 1)
        {
            // Search all items => set Revision = 2
            string url = $"/api/v3/search-results?limit={limit}&offset={offset}&query={query}&revision={revision}&workspace={wsid}";
            string AcceptHeader = "application/vnd.autodesk.plm.items.bulk+json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            return response.Content;
        }

        [HttpGet]
        [Route("api/FLC/GetItemDetails/{wsid}/{dmsid}")]
        public async Task<dynamic> GetItemDetails(string wsid = null, string dmsid = null)
        {

            string url = $"/api/v3/workspaces/{wsid}/items/{dmsid}";
            string AcceptHeader = "application/json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            return response.Content;
        }

        [HttpGet]
        [Route("api/FLC/GetAttachments/{wsid}/{dmsid}")]
        public async Task<dynamic> GetAttachments(string wsid = null, string dmsid = null)
        {

            string url = $"/api/v3/workspaces/{wsid}/items/{dmsid}/attachments?asc=name";
            string AcceptHeader = "application/vnd.autodesk.plm.attachments.bulk+json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            return response.Content;
        }

        [HttpGet]
        [Route("api/FLC/GetTableau/{wsid}/{tableauid}")]
        public async Task<dynamic> GetTableau(string wsid = null, string tableauid = null)
        {

            string url = $"/api/v3/workspaces/{wsid}/tableaus/{tableauid}";
            string AcceptHeader = "application/json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            return response.Content;
        }

        [HttpGet]
        [Route("api/FLC/GetGridRows/{wsid}/{dmsid}")]
        public async Task<dynamic> GetGridRows(string wsid = null, string dmsid = null)
        {

            string url = $"/api/v3/workspaces/{wsid}/items/{dmsid}/views/13/rows";
            string AcceptHeader = "application/json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            return response.Content;
        }

        [HttpGet]
        [Route("api/FLC/GetGridColumns/{wsid}")]
        public async Task<dynamic> GetGridColumns(string wsid = null)
        {

            string url = $"/api/v3/workspaces/{wsid}/views/13/fields";
            string AcceptHeader = "application/json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            return response.Content;
        }

        [HttpGet]
        [Route("api/FLC/GetImagebBlob")]
        public async Task<dynamic> GetImagebBlob(string link = null)
        {

            string AcceptHeader = "image/avif,image/webp,image/apng,image/*,*/*;q=0.8";

            IRestResponse response = await ExecuteFLCRequest(link, Method.GET, AcceptHeader);
            Base64Image image = new Base64Image();
            if (response.StatusCode is System.Net.HttpStatusCode.OK)
            {

                image.Base64String = System.Convert.ToBase64String(response.RawBytes);
                image.Type = response.ContentType;
                ;

            }

            return image;
        }

        public class Base64Image
        {
            public string Base64String { get; set; }
            public string Type { get; set; }
        }

        [HttpPost]
        [Route("api/FLC/DeleteItem/{wsid}/{dmsid}")]
        public async Task<dynamic> DeleteItem(string wsid = null, string dmsid = null)
        {
            string url = $"/api/v3/workspaces/{wsid}/items/{dmsid}?deleted=true";
            string AcceptHeader = "application/json";

            IRestResponse response = await ExecuteFLCRequest(url, Method.PATCH, AcceptHeader);

            return response.ResponseStatus;
        }


        [HttpPost]
        [Route("api/FLC/UploadAttachments/{wsid}/{dmsid}")]
        public async Task<dynamic> UploadAttachments([FromBody] UploadPayload data, int wsid, int dmsid)
        {
            //Payload:  {
            //             "foldername" : null,
            //             "paths" : [ "https://developer.api.autodesk.com/oss/v2/signedresources/ada8040a-78fc-42c1-aae4-89b1c83b7fa9?region=US"]
            //          }

            // TODO: handle folder creation + update of excisting files


            List<UploadFile> Uploadlist = new List<UploadFile>();

            if (data.filepathlist.Count > 0)
            {

                foreach (string filepath in data.filepathlist)
                {
                    UploadFile fileUpload = await GetUploadFileDetails(filepath);

                    fileUpload.wsid = wsid;
                    fileUpload.dmsid = dmsid;
                    fileUpload.foldername = data.folderName;

                    // add to upload list
                    Uploadlist.Add(fileUpload);

                }
            }

            // preform upload for all files in upload list
            if (Uploadlist.Count > 0)
            {
                foreach (UploadFile Upload in Uploadlist)
                {
                    RestResponse uploadresult = await UploadAttachment(Upload);
                }
            }


            return Ok();
        }

        private async Task<UploadFile> GetUploadFileDetails(string filepath)
        {
            UploadFile fileUpload = new UploadFile();


            if (IsUrl(filepath))
            {
                var hrclient = new HttpClient();
                HttpResponseMessage hresponse = await hrclient.GetAsync(filepath);

                var contType = hresponse.Content.Headers.ContentType;

                // handle Octet-Streams
                if (contType.MediaType == "application/octet-stream")
                {

                    fileUpload.filename = JsonConvert.DeserializeObject<string>(hresponse.Content.Headers.ContentDisposition.FileName);
                    string contentType;
                    new FileExtensionContentTypeProvider().TryGetContentType(fileUpload.filename, out contentType);
                    fileUpload.contenttype = contentType;

                }
                else
                // handle direct attachments
                {
                    fileUpload.filename = String.IsNullOrEmpty(filepath.Trim()) || !filepath.Contains(".") ? string.Empty : Path.GetFileName(new Uri(filepath).AbsolutePath); ;
                    fileUpload.contenttype = hresponse.Content.Headers.ContentType.MediaType;
                }

                fileUpload.fileArray = await hresponse.Content.ReadAsByteArrayAsync();
                //fileUpload.fileSize = fileUpload.fileArray.Length;

            }
            else
            {
                // process local files
                fileUpload.filename = System.IO.Path.GetFileName(filepath);
                string contentType;
                new FileExtensionContentTypeProvider().TryGetContentType(fileUpload.filename, out contentType);
                fileUpload.contenttype = contentType;

                fileUpload.fileArray = System.IO.File.ReadAllBytes(filepath);



            }

            fileUpload.fileSize = fileUpload.fileArray.Length;

            return fileUpload;
        }

        private static bool IsUrl(string p)
        {
            Uri uriResult;

            bool result = Uri.TryCreate(p, UriKind.Absolute, out uriResult)
                 && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result;
        }

        private async Task<dynamic> UploadAttachment(UploadFile attachmentUpload)
        {
            //todo: implement folder creation + extraHeaders handling + Update excisting file

            string url = $"/api/v3/workspaces/{attachmentUpload.wsid}/items/{attachmentUpload.dmsid}/attachments";
            string AcceptHeader = "application/json";
            string fileName = attachmentUpload.filename;
            long Size = attachmentUpload.fileSize;

            var metadata = new FLCAttachmentMetadata
            {
                Folder = null,
                Description = fileName,
                Name = fileName,
                ResourceName = fileName,
                Size = Size
            };

            string body = JsonConvert.SerializeObject(
                metadata,
                Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });

            IRestResponse response = await ExecuteFLCRequest(url, Method.POST, AcceptHeader, body);
            if (response.IsSuccessful)
            {
                JObject fileData = JObject.Parse(response.Content);

                response = await ExecutePrepareUpload(fileData);
                if (response.IsSuccessful)
                {

                    response = await ExecuteFileUpload(fileData, attachmentUpload.fileArray, attachmentUpload.contenttype);
                    if (response.IsSuccessful)
                    {
                        int AttachmentId = fileData.Value<int>("id");
                        body = "{" + '"' + "status" + '"' + " : {" + '"' + "name" + '"' + " : " + '"' + "CheckIn" + '"' + "} }";
                        url = $"/api/v3/workspaces/{attachmentUpload.wsid}/items/{attachmentUpload.dmsid}/attachments/{AttachmentId}";
                        return await ExecuteFLCRequest(url, Method.PATCH, AcceptHeader, body);
                    }

                }

            }
            return response;

        }

        [HttpPost]
        [Route("api/FLC/CreateItem/{wsid}")]
        public async Task<dynamic> CreateworkspaceItem(string wsid, [FromBody] JObject data = null)
        {
            // TODO: Check PickList / Linked Items

            // OAuth token
            dynamic oauth = await OAuthController.GetInternalAsync();
            string Authorisation = "Bearer " + oauth.access_token;

            // Request payload
            string boundary = string.Format("----{0:N}", Guid.NewGuid());

            ItemData bodyData = await CreateItemJsonData(wsid, data);

            // create JSon string
            string bodyString = JsonConvert.SerializeObject(
            bodyData.Sections,
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });


            RestClient FLCClient = new RestClient(BaseURL);
            RestRequest request = new RestRequest($"/api/v3/workspaces/{wsid}/items", Method.POST);


            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", Authorisation);
            request.AddHeader("x-user-id", FLCUser);
            request.AddHeader("X-Tenant", Tenant);
            request.RequestFormat = DataFormat.Json;

            if (bodyData.UploadFile.Count == 0)
            {
                request.AddHeader("Content-Type", $"multipart/form-data; boundary={boundary}");
                bodyString = string.Format("--{0}\nContent-Disposition: form-data; name=\"itemDetail\"; filename=\"blob\"\nContent-Type: application/json\n\n{1}\n--{0}--", boundary, bodyString);
            }

            request.AddParameter(new RestSharp.Parameter()
            {
                Name = "itemDetail",
                ContentType = "application/json",
                Type = ParameterType.RequestBody,
                Value = bodyString

            });


            foreach (UploadFile uploadImage in bodyData.UploadFile)
            {
                request.AddFileBytes(uploadImage.fieldID, uploadImage.fileArray, uploadImage.filename, "application/octet-stream");
            }



            IRestResponse response = await FLCClient.ExecuteTaskAsync(request);

            if (response.Headers.Any(t => t.Name == "Location"))
            {
                string location =
                response.Headers.FirstOrDefault(t => t.Name == "Location").Value.ToString();
                return location;
            }


            return response.StatusCode;

        }

        private async Task<dynamic> CreateItemJsonData(string workspaceId, JObject data)
        {
            //data is a Json object in the form of { "FieldID1" : "Value1", "FieldID2" : "Value2"}

            //get sections
            string url = $"/api/v3/workspaces/{workspaceId}/sections";
            string AcceptHeader = "application/vnd.autodesk.plm.sections.bulk+json";
            IRestResponse SectionsResponse = await ExecuteFLCRequest(url, Method.GET, AcceptHeader);

            JArray Sections = JsonConvert.DeserializeObject<JArray>(SectionsResponse.Content);


            // construct the Request Body data
            var bodyData = new ItemData();

            foreach (JToken child in Sections)
            {
                var newSection = new NewSection();
                newSection.Link = child.Value<string>("__self__");

                JToken SectionFields = child.SelectToken("fields");
                foreach (JToken SectionField in SectionFields)
                {
                    bool sectionHasMatrix = false;
                    if (SectionField.Value<string>("type") != "MATRIX")
                    {

                        ItemFieldData itemFieldData = await CreateNewSection(SectionField, data);

                        newSection.Fields.Add(itemFieldData.SectionField);

                        if (itemFieldData.UploadFile != null)
                        {
                            bodyData.UploadFile.Add(itemFieldData.UploadFile);
                        }
                    }
                    else
                    {
                        // A Matrix in the section exists
                        sectionHasMatrix = true;
                    }

                    if (sectionHasMatrix)
                    {
                        // do somthing with Matrices
                        JToken matrices = child.SelectToken("matrices");
                        foreach (JToken matrix in matrices)
                        {
                            JToken matrixFieldContainers = matrix.SelectToken("fields");
                            foreach (JToken matrixFieldContainer in matrixFieldContainers)
                            {
                                foreach (JToken matrixField in matrixFieldContainer)
                                {
                                    //process fields and add to sectionlist
                                    ItemFieldData itemFieldData = await CreateNewSection(matrixField, data);

                                    newSection.Fields.Add(itemFieldData.SectionField);

                                    if (itemFieldData.UploadFile != null)
                                    {
                                        bodyData.UploadFile.Add(itemFieldData.UploadFile);
                                    }
                                }


                            }


                        }

                    }

                }

                bodyData.Sections.SectionList.Add(newSection);


            }

            return bodyData;
        }

        private async Task<ItemFieldData> CreateNewSection(JToken SectionField, JObject data)
        {

            ItemFieldData newItemFieldData = new ItemFieldData();
            var newSectionField = new NewSectionField();
            newSectionField.Self = SectionField.Value<string>("link");

            //  search for value in value list
            if (data != null)
            {
                string FieldIdURN = SectionField.Value<string>("urn");
                int FieldIdPos = FieldIdURN.LastIndexOf(".");
                string FieldId = FieldIdURN.Substring(FieldIdPos + 1);
                var value = data.SelectToken(FieldId);
                if (value != null)
                {
                    if (value.Count() != 0)
                    {
                        if (value.SelectToken("type").ToString() == "imagefile")
                        {
                            // process the image
                            newItemFieldData.UploadFile = await GetUploadFileDetails(value.SelectToken("path").ToString());
                            newItemFieldData.UploadFile.fieldID = FieldId;
                            value = null;

                        }
                        else if (value.SelectToken("type").ToString() == "imagebytes")
                        {
                            var array = Convert.FromBase64String(@value.SelectToken("bytearray").ToString());
                            UploadFile image = new UploadFile()
                            {
                                fileArray = Convert.FromBase64String(@value.SelectToken("bytearray").ToString()),
                                fieldID = FieldId
                            };

                            newItemFieldData.UploadFile = image;

                            value = null;
                        }


                    }
                }

                newSectionField.Value = value;
            }

            newSectionField.Title = SectionField.Value<string>("title");

            newItemFieldData.SectionField = newSectionField;

            return newItemFieldData;
        }

        private async Task<dynamic> ExecuteFLCRequest(string url, Method method, string AcceptHeader, string body = null)
        {

            // Get OAuth token
            dynamic oauth = await OAuthController.GetInternalAsync();
            string Authorisation = "Bearer " + oauth.access_token;

            // Create new rest client
            RestClient FLCClient = new RestClient(BaseURL);
            RestRequest request = new RestRequest(url, method);

            // Set Authenication headers
            request.AddHeader("Accept", AcceptHeader);
            request.AddHeader("Authorization", Authorisation);
            request.AddHeader("x-user-id", FLCUser);
            request.AddHeader("X-Tenant", Tenant);

            // Request Body (if there is one)
            if (!string.IsNullOrEmpty(body))
            {
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("", body, ParameterType.RequestBody);
                request.RequestFormat = DataFormat.Json;
            }

            // Execute request
            IRestResponse response = await FLCClient.ExecuteTaskAsync(request);

            return response;
        }

        private async Task<dynamic> ExecutePrepareUpload(JObject fileData)
        {


            // OAuth token

            dynamic oauth = await OAuthController.GetInternalAsync();
            string Authorisation = "Bearer " + oauth.access_token;

            RestClient FLCClient = new RestClient(BaseURL);
            RestRequest request = new RestRequest(fileData.Value<string>("url"), Method.OPTIONS);

            int AttachmentId = fileData.Value<int>("id");
            var AttachmentHeaders = fileData.Value<object>("extraHeaders");

            request.AddHeader("Accept", "*/*");
            request.AddHeader("Accept-Encoding", "gzip, deflate, br");
            request.AddHeader("Accept-Language", "en-US,en;q=0.9,de;q=0.8,en-GB;q=0.7");
            request.AddHeader("Access-Control-Request-Headers", "content-type,x-amz-meta-filename"); 
            request.AddHeader("Access-Control-Request-Method", "PUT");
            request.AddHeader("Host", "plm360-aws-useast.s3.amazonaws.com");
            request.AddHeader("Authorization", Authorisation);
            request.AddHeader("Origin", BaseURL);
            request.AddHeader("Sec-Fetch-Mode", "cors");
            request.AddHeader("Sec-Fetch-Site", "cross-site");



            IRestResponse response = await FLCClient.ExecuteTaskAsync(request);


            return response;
        }

        private async Task<dynamic> ExecuteFileUpload(JObject fileData, Byte[] fileArray, string contentType)
        {

            RestClient FLCClient = new RestClient(BaseURL);
            RestRequest request = new RestRequest(fileData.Value<string>("url"), Method.PUT);


            request.AddHeader("Accept", "*/*");
            request.AddHeader("Accept-Encoding", "gzip, deflate, br");
            request.AddHeader("Accept-Language", "en-US,en;q=0.9,de;q=0.8,en-GB;q=0.7");
            request.AddHeader("Host", "plm360-aws-useast.s3.amazonaws.com");
            request.AddHeader("Origin", BaseURL);
            request.AddHeader("Sec-Fetch-Mode", "cors");
            request.AddHeader("Sec-Fetch-Site", "cross-site");

            // Add Extra headers
            JToken AttachmentHeaders = fileData.Value<JToken>("extraHeaders");

            foreach (JToken extraHeader in AttachmentHeaders)
            {
                JProperty jProperty = extraHeader.ToObject<JProperty>();
                request.AddHeader(jProperty.Name, jProperty.Value.ToString());
            }


            request.Parameters.Add(new RestSharp.Parameter()
            {
                Name = contentType,
                ContentType = contentType,
                Type = ParameterType.RequestBody,
                Value = fileArray
            });

            //execute request
            IRestResponse response = await FLCClient.ExecuteTaskAsync(request);


            return response;
        }

        private async Task<dynamic> CreateFolder(string wsid = null, string dmsid = null, string folderName = null)
        {
            string url = $"/api/v3/workspaces/{wsid}/items/{dmsid}/folders";
            string AcceptHeader = "application/json";
            string body = "{ " + '"' + "folderName" + '"' + " : " + folderName + " }";

            IRestResponse response = await ExecuteFLCRequest(url, Method.POST, AcceptHeader, body);

            if (response.Headers.Any(t => t.Name == "Location"))
            {
                string location =
                response.Headers.FirstOrDefault(t => t.Name == "Location").Value.ToString();
                return location;
            }

            return response.StatusCode;

        }


        #region FLCobjects
        public class FLCAttachmentMetadata
        {

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("folder")]
            public string Folder { get; set; }

            [JsonProperty("resourceName")]
            public string ResourceName { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }
        }



        public class NewSection
        {
            [JsonProperty("link")]
            public string Link { get; set; }

            [JsonProperty("fields")]
            public List<NewSectionField> Fields { get; set; }

            public NewSection()
            {
                Fields = new List<NewSectionField>();
            }
        }

        public class NewSectionField
        {
            [JsonProperty("__self__")]
            public string Self { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("value")]
            public object Value { get; set; }

        }

        public class Sections
        {
            [JsonProperty("sections")]
            public List<NewSection> SectionList { get; set; }

            public Sections()
            {
                SectionList = new List<NewSection>();
            }
        }


        public class UploadPayload
        {

            [JsonProperty("foldername")]
            public string folderName { get; set; }

            [JsonProperty("paths")]
            public List<string> filepathlist { get; set; }


            public UploadPayload()
            {
                filepathlist = new List<string>();
            }
        }


        public class UploadFile
        {
            public int wsid { get; set; }

            public int dmsid { get; set; }

            public string filename { get; set; }

            public string foldername { get; set; }

            public string contenttype { get; set; }

            public byte[] fileArray { get; set; }

            public long fileSize { get; set; }

            public string fieldID { get; set; }
        }

        public class ItemFieldData
        {
            public NewSectionField SectionField { get; set; }

            public UploadFile UploadFile { get; set; }
        }

        public class ItemData
        {
            public Sections Sections { get; set; }

            public List<UploadFile> UploadFile { get; set; }

            public ItemData()
            {
                UploadFile = new List<UploadFile>();
                Sections = new Sections();
            }
        }
        #endregion
    }

}

