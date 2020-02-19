using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using Our.Umbraco.NuCacheExplorer.Models;
using Our.Umbraco.NuCacheExplorer.Serializer;
using Umbraco.Core.Configuration;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Our.Umbraco.NuCacheExplorer.Controllers
{
    [PluginController("OurUmbracoNuCacheExplorer")]
    public class NuCacheExplorerApiController : UmbracoAuthorizedApiController
    {
        private readonly IGlobalSettings globalSettings;

        public NuCacheExplorerApiController(IGlobalSettings globalSettings)
        {
            this.globalSettings = globalSettings ?? throw new ArgumentNullException(nameof(globalSettings));
        }
        public HttpResponseMessage GetNuCacheFile(string contentType)
        {
            var filePath = Path.Combine(globalSettings.LocalTempPath, "NuCache\\NuCache." + contentType + ".db");
            var tempFileName = filePath.Replace(".db", ".Explorer.Temp.db");

            try
            {
                //Check for valid filepath
                if (File.Exists(filePath) == false)
                {
                    var message = $"No file exists on disk at {filePath}";
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, message);
                }

                //Check for file extension ends with .db
                //Don't want to attempt to any old file type
                if (Path.GetExtension(filePath) != ".db")
                {
                    var message = $"The file {filePath} is not a .db file";
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
                }

                //We need to create a temp copy of the nucache DB - to avoid file locks if its in use whilst we try to read it
                //'NuCache.Content.db' will become 'NuCache.Content.Explorer.Temp.db'
                File.Copy(filePath, tempFileName, true);

                var keySerializer = new PrimitiveSerializer();
                var valueSerializer = new ContentNodeKitSerializer();
                var options = new BPlusTree<int, ContentNodeKit>.OptionsV2(keySerializer, valueSerializer)
                {
                    CreateFile = CreatePolicy.Never,
                    FileName = tempFileName,

                    // default is 4096, min 2^9 = 512, max 2^16 = 64K
                    FileBlockSize = GetBlockSize(),
                };

                //Read the file into a BPlusTreeObject & select the kits
                var tree = new BPlusTree<int, ContentNodeKit>(options);
                var sw = Stopwatch.StartNew();
                var kits = tree.Select(x => x.Value).ToArray();
                sw.Stop();
                tree.Dispose();

                DeleteTempFile(tempFileName);

                //Add to our JSON object the stopwatch clock to read the DB/dictionary file
                var response = new ApiResponse
                {
                    Items = kits,
                    TotalItems = kits.Length,
                    StopClock = new StopClock
                    {
                        Hours = sw.Elapsed.Hours,
                        Minutes = sw.Elapsed.Minutes,
                        Seconds = sw.Elapsed.Seconds,
                        Milliseconds = sw.Elapsed.Milliseconds,
                        Ticks = sw.Elapsed.Ticks
                    }
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                DeleteTempFile(tempFileName);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        public class Theme
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

        private void DeleteTempFile(string tempFileName)
        {
            //Delete the file (seems like could be a lock, so we wait 100ms between each attempt upto 10 times)
            var ok = false;
            var attempts = 0;
            while (!ok)
            {
                System.Threading.Thread.Sleep(100);
                try
                {
                    attempts++;
                    File.Delete(tempFileName);
                    ok = true;
                }
                catch
                {
                    if (attempts == 10)
                        throw;
                }
            }
        }

        private static int GetBlockSize()
        {
            var blockSize = 4096;

            var appSetting = ConfigurationManager.AppSettings["Umbraco.Web.PublishedCache.NuCache.BTree.BlockSize"];
            if (appSetting == null)
                return blockSize;

            if (!int.TryParse(appSetting, out blockSize))
                throw new ConfigurationErrorsException($"Invalid block size value \"{appSetting}\": not a number.");

            var bit = 0;
            for (var i = blockSize; i != 1; i >>= 1)
                bit++;
            if (1 << bit != blockSize)
                throw new ConfigurationErrorsException($"Invalid block size value \"{blockSize}\": must be a power of two.");
            if (blockSize < 512 || blockSize > 65536)
                throw new ConfigurationErrorsException($"Invalid block size value \"{blockSize}\": must be >= 512 and <= 65536.");

            return blockSize;
        }
    }
}