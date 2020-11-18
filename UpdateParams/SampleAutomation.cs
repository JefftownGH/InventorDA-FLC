/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Peter Van Avondt - TSS EMEA
// Based on code written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using Inventor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UpdateParams
{
    [ComVisible(true)]
    public class SampleAutomation
    {
        private readonly InventorServer m_inventorServer;

        public SampleAutomation(InventorServer inventorServer)
        {
            LogTrace("Starting sample plugin.");
            m_inventorServer = inventorServer;
        }

        public void Run(Document doc)
        {
            LogTrace("Running with no Args.");
            try
            {

                string curDir = System.IO.Directory.GetCurrentDirectory();
                LogTrace("Current dir = " + curDir);

                string paramsPath = System.IO.Path.Combine(curDir, "params.json");
                LogTrace("Params path = " + paramsPath);

                string data = System.IO.File.ReadAllText(paramsPath);
                LogTrace("After reading params.json");

                JObject inputJson = JObject.Parse(data);
                string text = inputJson.ToString(Formatting.None);
                LogTrace(text);


                LogTrace("Start changing User params");

                IDictionary<string, JToken> userparams = (JObject)inputJson["UserParams"];
                Dictionary<string, string> parameters = userparams.ToDictionary(pair => pair.Key, pair => (string)pair.Value);
                LogTrace("Userparameters extracted");

                // Get path of add-in dll
                string assemblyPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                LogTrace("Assembly Path = " + assemblyPath);

                // Path of template relative to the dll's path
                string modelPath = inputJson.Value<string>("InputModel");
                string drawingPath = inputJson.Value<string>("InputDrawing");
                // Open template document
                string configModel = inputJson.Value<string>("ConfigurationModel") + "\\";
                LogTrace("configModel= " + configModel);
                string modelfullPath = System.IO.Path.Combine(assemblyPath, configModel, modelPath);
                string drawingfullPath = System.IO.Path.Combine(assemblyPath, configModel, drawingPath);
                LogTrace("Model Path = " + modelfullPath);
                LogTrace("drawing Path = " + drawingfullPath);


                Document modelDoc = m_inventorServer.Documents.Open(modelfullPath, false);


                var theParams = GetParameters(modelDoc);
                foreach (KeyValuePair<string, string> entry in parameters)
                {
                    var parameterName = entry.Key;
                    var value = entry.Value;
                    LogTrace("Parameter to change: {0}:{1}", parameterName, value);
                    try
                    {
                        UserParameter param = theParams[parameterName];
                        try { param.Value = value; }
                        catch { param.Expression = value; }
                    }
                    catch (Exception e)
                    {
                        LogTrace("Cannot update '{0}' parameter. ({1})", parameterName, e.Message);
                    }
                }

                modelDoc.Update();

                LogTrace("Model Doc updated.");
                var currDir = System.IO.Directory.GetCurrentDirectory();
                //string ext = System.IO.Path.GetExtension(modelPath);

                // Save Drawing as PDF
                Document drawingDoc = m_inventorServer.Documents.Open(drawingfullPath, false);
                var pdffilename = System.IO.Path.Combine(currDir, "Result.pdf"); // name must be in sync with OutputPDF localName in Activity
                drawingDoc.SaveAs(pdffilename, true);
                LogTrace("Drawing pdf path:" + pdffilename);

                // Save Thumbnail
                SaveThumbnail(modelDoc);
                LogTrace("Model Thumbnail saved");

                // Save Model to Collaboration file
                SaveAsSVF(modelDoc);
                LogTrace("Collaboration file saved ");

                // Save Model to SAT
                ExportSAT(modelDoc);

                // Save Model to DWF
                ExportDWF(modelDoc);



            }
            catch (Exception ex)
            { LogTrace(ex.Message); }


        }

        public void RunWithArguments(Document doc, NameValueMap map)
        {
            // not implemented
        }

        #region Create Collaboration file
        private void SaveAsSVF(Document Doc)
        {

            LogTrace("** Saving SVF");

            try
            {
                ApplicationAddIn svfAddin = m_inventorServer
                    .ApplicationAddIns
                    .Cast<ApplicationAddIn>()
                    .FirstOrDefault(item => item.ClassIdString == "{C200B99B-B7DD-4114-A5E9-6557AB5ED8EC}");

                var oAddin = (TranslatorAddIn)svfAddin;

                if (oAddin != null)
                {
                    LogTrace("SVF Translator addin is available");
                    TranslationContext oContext = m_inventorServer.TransientObjects.CreateTranslationContext();
                    // Setting context type
                    oContext.Type = IOMechanismEnum.kFileBrowseIOMechanism;

                    NameValueMap oOptions = m_inventorServer.TransientObjects.CreateNameValueMap();
                    // Create data medium;
                    DataMedium oData = m_inventorServer.TransientObjects.CreateDataMedium();

                    LogTrace("SVF save");

                    var sessionDir = Directory.GetCurrentDirectory();
                    oData.FileName = System.IO.Path.Combine(sessionDir, "Result.collaboration");
                    var outputDir = System.IO.Path.Combine(sessionDir, "output");
                    var bubbleFileOriginal = System.IO.Path.Combine(outputDir, "bubble.json");
                    var bubbleFileNew = System.IO.Path.Combine(sessionDir, "bubble.json");

                    // Setup SVF options
                    if (oAddin.get_HasSaveCopyAsOptions(Doc, oContext, oOptions))
                    {
                        oOptions.set_Value("GeometryType", 1);
                        oOptions.set_Value("EnableExpressTranslation", false);
                        oOptions.set_Value("SVFFileOutputDir", sessionDir);
                        oOptions.set_Value("ExportFileProperties", true);
                        oOptions.set_Value("ObfuscateLabels", false);
                    }

                    LogTrace($"SVF files are output to: {oOptions.get_Value("SVFFileOutputDir")}");

                    oAddin.SaveCopyAs(Doc, oContext, oOptions, oData);
                    LogTrace("SVF can be exported.");
                    LogTrace($"** Saved SVF as {oData.FileName}");

                }
            }
            catch (Exception e)
            {
                LogError($"********Export to format SVF failed: {e.Message}");
            }

        }
        #endregion

        #region Create Thumbnail

        public void SaveThumbnail(Document doc)
        {
            try
            {
                LogTrace("Processing " + doc.FullFileName);
                int ThumbnailSize = 640;
                dynamic invDoc = doc;

                // TODO: only IAM and IPT are supported now, but it's not validated
                invDoc.ObjectVisibility.AllWorkFeatures = false;
                invDoc.ObjectVisibility.Sketches = false;
                invDoc.ObjectVisibility.Sketches3D = false;

                if (doc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    invDoc.ObjectVisibility.WeldmentSymbols = false;
                }

                string fileNameLarge = "thumbnail-large.png";
                string filePathLarge = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fileNameLarge);


                m_inventorServer.DisplayOptions.Show3DIndicator = false;
                m_inventorServer.DisplayOptions.DisplaySilhouettes = true;
                m_inventorServer.DisplayOptions.ShowXYZAxisLabels = false;
                m_inventorServer.DisplayOptions.UseRayTracingForRealisticDisplay = true;
                Camera cam = m_inventorServer.TransientObjects.CreateCamera();
                cam.SceneObject = invDoc.ComponentDefinition;

                cam.ViewOrientationType = ViewOrientationTypeEnum.kIsoTopRightViewOrientation;
                cam.Fit();
                cam.ApplyWithoutTransition();

                Inventor.Color backgroundColor = m_inventorServer.TransientObjects.CreateColor(0xEC, 0xEC, 0xEC, 0.0); // hardcoded. Make as a parameter

                // generate image twice as large, and then downsample it (antialiasing)
                cam.SaveAsBitmap(filePathLarge, ThumbnailSize * 2, ThumbnailSize * 2, backgroundColor, backgroundColor);

                // based on https://stackoverflow.com/a/24199315
                using (var image = Image.FromFile(filePathLarge))
                using (var destImage = new Bitmap(ThumbnailSize, ThumbnailSize))
                {
                    destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                    using (var graphics = Graphics.FromImage(destImage))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        using (var wrapMode = new ImageAttributes())
                        {
                            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                            var destRect = new Rectangle(0, 0, ThumbnailSize, ThumbnailSize);
                            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                        }
                    }

                    string fileName = "Result.png";
                    string filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fileName);
                    destImage.Save(filePath);

                    LogTrace($"Saved thumbnail as {filePath}");
                }

                System.IO.File.Delete(filePathLarge);
            }
            catch (Exception e)
            {
                LogError("Processing failed. " + e.ToString());
            }
        }

        #endregion

        #region Create SAT File

        public void ExportSAT(Document doc)
        {
            string currentDirectory = System.IO.Directory.GetCurrentDirectory();

            LogTrace("Export SAT file.");
            TranslatorAddIn SAT_AddIn = (TranslatorAddIn)m_inventorServer.ApplicationAddIns.ItemById["{89162634-02B6-11D5-8E80-0010B541CD80}"];

            if (SAT_AddIn == null)
            {
                LogTrace("Could not access to SAT translator ...");
                return;
            }

            TranslationContext oContext = m_inventorServer.TransientObjects.CreateTranslationContext();
            NameValueMap map = m_inventorServer.TransientObjects.CreateNameValueMap();

            if (SAT_AddIn.get_HasSaveCopyAsOptions(doc, oContext, map))
            {
                LogTrace("SAT: Set context type");
                oContext.Type = IOMechanismEnum.kFileBrowseIOMechanism;

                LogTrace("SAT: create data medium");
                DataMedium oData = m_inventorServer.TransientObjects.CreateDataMedium();

                LogTrace("SAT save to: " + currentDirectory + "\\Result.sat");
                oData.FileName = currentDirectory + "\\Result.sat";

                map.set_Value("GeometryType", 1);

                SAT_AddIn.SaveCopyAs(doc, oContext, map, oData);
                LogTrace("SAT exported.");
            }
        }

        #endregion

        #region Create DWF File


        public void ExportDWF(Document doc)
        {
            string currentDirectory = System.IO.Directory.GetCurrentDirectory();

            LogTrace("Export DWF file.");
            TranslatorAddIn DWF_AddIn = (TranslatorAddIn)m_inventorServer.ApplicationAddIns.ItemById["{0AC6FD95-2F4D-42CE-8BE0-8AEA580399E4}"];

            if (DWF_AddIn == null)
            {
                LogTrace("Could not access the DWF translator ...");
                return;
            }

            TranslationContext oContext = m_inventorServer.TransientObjects.CreateTranslationContext();
            NameValueMap map = m_inventorServer.TransientObjects.CreateNameValueMap();


            if (DWF_AddIn.get_HasSaveCopyAsOptions(doc, oContext, map))
            {
                LogTrace("DWF: Set options");
                map.Value["Launch_Viewer"] = 0;
                map.Value["Publish_All_Component_Props"] = 1;
                map.Value["Publish_All_Physical_Props"] = 1;
                map.Value["BOM_Structured"] = 0;


                oContext.Type = IOMechanismEnum.kFileBrowseIOMechanism;

                LogTrace("DWF: create data medium");
                DataMedium oData = m_inventorServer.TransientObjects.CreateDataMedium();

                LogTrace("DWF save to: " + currentDirectory + "\\Result.dwf");
                oData.FileName = currentDirectory + "\\Result.dwf";

                DWF_AddIn.SaveCopyAs(doc, oContext, map, oData);
                LogTrace("DWF exported.");
            }
        }


        #endregion


        private static UserParameters GetParameters(Document doc)
        {
            var docType = doc.DocumentType;
            switch (docType)
            {
                case DocumentTypeEnum.kAssemblyDocumentObject:
                    var asm = doc as AssemblyDocument;
                    return asm.ComponentDefinition.Parameters.UserParameters;

                case DocumentTypeEnum.kPartDocumentObject:
                    var ipt = doc as PartDocument;
                    return ipt.ComponentDefinition.Parameters.UserParameters;

                default:
                    throw new ApplicationException(string.Format("Unexpected document type ({0})", docType));
            }
        }

        #region Logging utilities

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string message)
        {
            Trace.TraceError(message);
        }

        #endregion

    }
}
