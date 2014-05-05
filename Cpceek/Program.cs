using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml.Serialization;

namespace Cpceek
{
    [Serializable]
    [XmlType(TypeName = "Game")]
    public class GameInfo
    {
        public string ResourcePath { get; set; }
        public string Title { get; set; }
        public string AlsoKnownAs { get; set; }
        public string Year { get; set; }
        public string Company { get; set; }
        public string Publisher { get; set; }
        public string Publication { get; set; }
        public string Cracker { get; set; }
        public string Developer { get; set; }
        public string Author { get; set; }
        public string Designer { get; set; }
        public string Artist { get; set; }
        public string Language { get; set; }
        public string MemoryRequired { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string TitleScreen { get; set; }
        public string CheatMode { get; set; }
        public string Protected { get; set; }
        public string Problems { get; set; }
        public string RunCmd { get; set; }
        public string Uploaded { get; set; }
        public string Comments { get; set; }
    }

    [Serializable]
    [XmlType(TypeName = "game")]
    public class HyperList
    {
        [XmlAttribute("name")]
        public string Title { get; set; }
        [XmlElement(ElementName = "description")]
        public string Description { get; set; }
        [XmlElement(ElementName = "cloneof")]
        public string CloneOf { get; set; }
        [XmlElement(ElementName = "manufacturer")]
        public string Manufacturer { get; set; }
        [XmlElement(ElementName = "year")]
        public string Year { get; set; }
        [XmlElement(ElementName = "genre")]
        public string Genre { get; set; }
    }

    class Program
    {
        private static readonly string CpcIndexFile = ConfigurationManager.AppSettings["CpcIndexFile"];
        private static readonly string CpcIndexFileDestPath = Path.Combine(Environment.CurrentDirectory, CpcIndexFile);
        private static readonly string FullCpcServerPath = string.Concat(ConfigurationManager.AppSettings["FtpServerAddress"], ConfigurationManager.AppSettings["CpcPath"]);
        private static readonly string RomsDestPath = ConfigurationManager.AppSettings["RomsPath"];
        private static Dictionary<string, GameInfo> _downloadList;
        private static int _filesRemaingToDownload;
        private static bool IsContinuing
        {
            get
            {
                while (true)
                {
                    var input = Console.ReadKey(true);

                    if (input.Key == ConsoleKey.Y)
                        return true;

                    if (input.Key == ConsoleKey.N)
                        return false;
                }
            }
        }

        static void Main(string[] args)
        {
            if (string.IsNullOrEmpty(RomsDestPath))
            {
                Log("Please set the 'RomsPath' in the Cpceek.exe.config file.\n\r\n\rPress any key to exit");
                Console.ReadKey();
                return;
            }

            Directory.CreateDirectory(RomsDestPath);

            var credits = string.Concat("- Thanks to Nicolas Campbell for making his server publicly available :-D~\n",
                                        "- FTP address: {0}\n",
                                        "- Want to fiddle with the code? : git@github.com:WalkToFreedom/Cpceek.git\n\n");

            Console.Write(credits, FullCpcServerPath);

            // Download the 'what's new' file
            DownloadWhatsNewFile();

            // Download the CPC index file
            DownloadCpcIndex();

            // Generate a list of ROMs to download
            _downloadList = GenerateGameInfoList();
            _filesRemaingToDownload = _downloadList.Count;

            // Download ROMs
            DownloadRoms(_downloadList);

            // Generate a full game info list based on the index text file
            GenerateFullGameInfoList();

            // Create a Hyperspin XML game list for the wheel
            GenerateHyperSpinXml();

            Log("Job complete. Press any key to exit.");
            Console.ReadKey();
        }

        private static void DownloadWhatsNewFile()
        {
            Log("Checking 'what's new'...");

            var whatsNewFile = ConfigurationManager.AppSettings["WhatsNewFile"];
            var whatsNewSource = Path.Combine(FullCpcServerPath, whatsNewFile);
            var whatsNewDest = Path.Combine(Environment.CurrentDirectory, whatsNewFile);

            _filesRemaingToDownload = 1;

            CheckExistsAndDownload(whatsNewSource, whatsNewDest);
        }

        private static void GenerateHyperSpinXml()
        {
            Log("Generate Hyperspin.xml? (Y/N)");

            if (!IsContinuing) return;

            var alreadyDownloadedList = GenerateAlreadyDownloadedList();

            using (var fs = new FileStream(Path.Combine(Environment.CurrentDirectory, "Amstrad CPC.xml"), FileMode.Create))
            {
                var list = alreadyDownloadedList.Select(x => new HyperList
                    {
                        Title = Path.GetFileNameWithoutExtension(x.ResourcePath),
                        Description = x.Title,
                        CloneOf = string.Empty,
                        Manufacturer = x.Publisher ?? x.Company ?? x.Publication,
                        Year = x.Year,
                        Genre = x.Type ?? x.SubType
                    }).ToList();

                new XmlSerializer(typeof(List<HyperList>), new XmlRootAttribute("menu")).Serialize(fs, list);
            }
        }

        private static void GenerateFullGameInfoList()
        {
            Log("Generate full game info list (XML)? (Y/N)");

            if (!IsContinuing) return;

            var fullGameInfoList = GenerateGameInfoList(false).Select(x => x.Value).ToList();

            using (var fs = new FileStream(Path.Combine(Environment.CurrentDirectory, "GameInfo.xml"), FileMode.Create))
                new XmlSerializer(typeof(List<GameInfo>), new XmlRootAttribute("menu")).Serialize(fs, fullGameInfoList);
        }

        private static void Log(string message)
        {
            var logPath = Path.Combine(Environment.CurrentDirectory, "Cpceek.log");
            const string format = "{0} -- {1}";
            message = string.Format(format, DateTime.Now, message);

            using (var sw = File.AppendText(logPath))
                sw.WriteLine(message);

            Console.WriteLine(message);
        }

        private static void DownloadRoms(Dictionary<string, GameInfo> downloadList)
        {
            if (downloadList.Count == 0)
            {
                Log("The download list is empty.");
                return;
            }

            Log(string.Format("You are missing {0} ROMs. Would you like to downloaded them now? (Y/N)", downloadList.Count));

            if (!IsContinuing) return;

            foreach (var gameInfo in downloadList)
            {
                var destPath = Path.Combine(RomsDestPath, gameInfo.Key);
                CheckExistsAndDownload(gameInfo.Value.ResourcePath, destPath);
            }
        }

        private static void DownloadCpcIndex()
        {
            Log("Checking CPC index...");

            _filesRemaingToDownload = 1;

            var cpcIndexSourcePath = string.Concat(FullCpcServerPath, CpcIndexFile);
            CheckExistsAndDownload(cpcIndexSourcePath, CpcIndexFileDestPath);
        }

        private static bool IsFileSizeSame(string remoteFile, string localFile)
        {
            try
            {
                if (!File.Exists(localFile))
                    return false;

                var request = (FtpWebRequest)WebRequest.Create(remoteFile);
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.Credentials = new NetworkCredential("anonymous", "anonymous");

                long fileLength = 0;

                using (var response = (FtpWebResponse)request.GetResponse())
                    fileLength = response.ContentLength;

                return fileLength == new FileInfo(localFile).Length;
            }
            catch (Exception e)
            {
                Log(string.Format("There was an error requesting the file: {0}. {1}", remoteFile, e.Message));
                return false;
            }
        }

        private static void CheckExistsAndDownload(string sourcePath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
                Log(string.Format("{0} does not exist downloading...", destinationPath));
            else
            {
                if (IsFileSizeSame(sourcePath, destinationPath))
                {
                    Log("File exists and is same size, ignoring.");
                    return;
                }

                Log("File exists but size is different.");
            }

            if (Download(sourcePath, destinationPath))
                Log(string.Format("Complete! Remaining: {0}", --_filesRemaingToDownload));
        }

        public static bool Download(string sourcePath, string destinationPath)
        {
            Log(string.Format("Downloading... {0}", sourcePath));

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(sourcePath);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential("anonymous", "anonymous");

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    using (var output = File.Create(destinationPath))
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream != null) responseStream.CopyTo(output);
                        }
                    }

                    return response.StatusCode == FtpStatusCode.ClosingData;
                }
            }
            catch (Exception e)
            {
                Log(string.Format("There was an error downloading the file: {0}. {1}", sourcePath, e.Message));
                return false;
            }
        }

        private static IEnumerable<GameInfo> GenerateAlreadyDownloadedList()
        {
            var alreadyDownloadedList = GenerateGameInfoList(false).Where(x => !_downloadList.ContainsKey(x.Key));
            return alreadyDownloadedList.Select(x => x.Value).ToList();
        }

        private static Dictionary<string, GameInfo> GenerateGameInfoList(bool excludeAlreadyDownload = true)
        {
            var gameInfoList = new Dictionary<string, GameInfo>();

            if (!File.Exists(CpcIndexFileDestPath))
            {
                Log(string.Format("Index file does not exist unable to create download list: {0}", CpcIndexFileDestPath));
                return gameInfoList;
            }

            using (var sr = File.OpenText(CpcIndexFileDestPath))
            {
                var line = String.Empty;
                var resourcePath = string.Empty;
                var resourceFileName = string.Empty;
                var readingGameInfo = false;
                GameInfo gameInfo = null;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!readingGameInfo && line.Contains("/cpc/games/"))
                    {
                        resourcePath = line;
                        resourceFileName = Path.GetFileName(resourcePath);
                        var destFile = Path.Combine(RomsDestPath, resourceFileName);

                        if (excludeAlreadyDownload)
                        {
                            if (File.Exists(destFile) && new FileInfo(destFile).Length > 0)
                                continue;
                        }

                        readingGameInfo = true;
                        gameInfo = new GameInfo { ResourcePath = resourcePath };
                        continue;
                    }

                    if (readingGameInfo && line.Contains("----------"))
                    {
                        readingGameInfo = false;
                        gameInfoList.Add(resourceFileName, gameInfo);
                        continue;
                    }

                    if (readingGameInfo)
                        UpdateGameInfo(line, gameInfo);
                }
            }

            return gameInfoList;
        }

        private static void UpdateGameInfo(string line, GameInfo gameInfo)
        {
            var keyValue = line.Split(':');
            var key = keyValue[0].Trim();
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "TITLE":
                    gameInfo.Title = value;
                    break;
                case "ALSO KNOWN AS":
                    gameInfo.AlsoKnownAs = value;
                    break;
                case "YEAR":
                    gameInfo.Year = value;
                    break;
                case "COMPANY":
                    gameInfo.Company = value;
                    break;
                case "PUBLISHER":
                    gameInfo.Publisher = value;
                    break;
                case "PUBLICATION":
                    gameInfo.Publication = value;
                    break;
                case "CRACKER":
                    gameInfo.Cracker = value;
                    break;
                case "DEVELOPER":
                    gameInfo.Developer = value;
                    break;
                case "AUTHOR":
                    gameInfo.Author = value;
                    break;
                case "DESIGNER":
                    gameInfo.Designer = value;
                    break;
                case "ARTIST":
                    gameInfo.Artist = value;
                    break;
                case "LANGUAGE":
                    gameInfo.Language = value;
                    break;
                case "MEMORY REQUIRED":
                    gameInfo.MemoryRequired = value;
                    break;
                case "TYPE":
                    gameInfo.Type = value;
                    break;
                case "SUBTYPE":
                    gameInfo.SubType = value;
                    break;
                case "TITLE SCREEN":
                    gameInfo.TitleScreen = value;
                    break;
                case "CHEAT MODE":
                    gameInfo.CheatMode = value;
                    break;
                case "PROTECTED":
                    gameInfo.Protected = value;
                    break;
                case "PROBLEMS":
                    gameInfo.Problems = value;
                    break;
                case "RUN COMMAND":
                    gameInfo.RunCmd = value;
                    break;
                case "UPLOADED":
                    gameInfo.Uploaded = value;
                    break;
                case "COMMENTS":
                    gameInfo.Comments = value;
                    break;
            }
        }
    }
}
