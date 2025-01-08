using System.Configuration;
using System.Diagnostics;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Our.Umbraco.NuCacheExplorer.Models;
using Our.Umbraco.NuCacheExplorer.Serializer;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;

namespace Our.Umbraco.NuCacheExplorer.Controllers
{
    [PluginController("OurUmbracoNuCacheExplorer")]
    public class NuCacheExplorerApiController : UmbracoAuthorizedApiController
    {
        private readonly IOptions<HostingSettings> _hostingSettings;
        private readonly IOptions<NuCacheSettings> _nuCacheSettings;
        
        public NuCacheExplorerApiController(
            IOptions<HostingSettings> hostingSettings,
            IOptions<NuCacheSettings> nuCacheSettings)
        {
            _hostingSettings = hostingSettings ?? throw new ArgumentNullException(nameof(hostingSettings));
            _nuCacheSettings = nuCacheSettings ?? throw new ArgumentNullException(nameof(nuCacheSettings));
        }
        
        public async Task<IActionResult> GetNuCacheFile(string contentType)
        {
            var baseLocation = _hostingSettings.Value.LocalTempStorageLocation.ToString() == "Default" ? "umbraco\\Data\\TEMP" : "local\\TEMP";

            var filePath = Path.Combine("umbraco\\Data\\TEMP", "NuCache\\NuCache." + contentType + ".db");
            var tempFileName = filePath.Replace(".db", ".Explorer.Temp.db");

            try
            {
                //Check for valid filepath
                if (System.IO.File.Exists(filePath) == false)
                {
                    var message = $"No file exists on disk at {filePath}";
                    return BadRequest(message);
                }

                //Check for file extension ends with .db
                //Don't want to attempt to any old file type
                if (Path.GetExtension(filePath) != ".db")
                {
                    var message = $"The file {filePath} is not a .db file";
                    return BadRequest(message);
                }

                //We need to create a temp copy of the nucache DB - to avoid file locks if its in use whilst we try to read it
                //'NuCache.Content.db' will become 'NuCache.Content.Explorer.Temp.db'
                System.IO.File.Copy(filePath, tempFileName, true);

                var keySerializer = new PrimitiveSerializer();
                var valueSerializer = new ContentNodeKitSerializer();
                var options = new BPlusTree<int, ContentNodeKit>.OptionsV2(keySerializer, valueSerializer)
                {
                    CreateFile = CreatePolicy.Never,
                    FileName = tempFileName,

                    // default is 4096, min 2^9 = 512, max 2^16 = 64K
                    FileBlockSize = GetBlockSize(_nuCacheSettings),
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

                return Ok(response);
            }
            catch (Exception e)
            {
                DeleteTempFile(tempFileName);
                return BadRequest(e.Message);
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
                Thread.Sleep(100);
                try
                {
                    attempts++;
                    System.IO.File.Delete(tempFileName);
                    ok = true;
                }
                catch
                {
                    if (attempts == 10)
                        throw;
                }
            }
        }

        private static int GetBlockSize(IOptions<NuCacheSettings> nuCacheSettings)
        {
            var blockSize = 4096;

            var appSetting = nuCacheSettings.Value.BTreeBlockSize;
            if (appSetting == null)
                return blockSize;

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