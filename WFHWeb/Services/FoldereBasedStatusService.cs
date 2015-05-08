using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WFHWeb.DataModels;

namespace WFHWeb.Services
{
    public class FoldereBasedStatusService : IStatusService
    {
        public void SetStatus(string dataDir, WorkingStatusData workingStatus)
        {
            var path = GetOrCreateUserDirectory(dataDir, workingStatus.Email);
            var fileName = String.Format("{0}.json", Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(path, fileName);

            File.WriteAllText(filePath, JsonConvert.SerializeObject(workingStatus, Formatting.Indented));
        }

        public IList<WorkingStatusInfo> GetAllStatuses(string dataDir)
        {
            var defaults = ReadDefaults(dataDir);
            var dailies = ReadDailies(dataDir);
            return GetLatest(defaults, dailies);
        }

        public void SetDefault(string dataDir, WorkingStatusData workingStatus)
        {
            var path = Path.Combine(dataDir, "defaults", workingStatus.Email);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var fileName = String.Format("{0}.json", Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(path, fileName);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(workingStatus, Formatting.Indented));
        }

        private IList<WorkingStatusInfo> GetLatest(IEnumerable<DefaultFile> defaults, IList<DailyFile> dailies)
        {
            var data = new List<WorkingStatusInfo>();
            foreach (var defaultFile in defaults)
            {
                var daily = dailies.FirstOrDefault(x => x.UserName == defaultFile.UserName);
                var shouldTakeDefault = daily == null ||
                                        defaultFile.FileInfo.CreationTime > daily.FileInfo.CreationTime;
                var path = shouldTakeDefault ? defaultFile.FileInfo.FullName : daily.FileInfo.FullName;
                data.Add(new WorkingStatusInfo(
                    JsonConvert.DeserializeObject<WorkingStatusData>(File.ReadAllText(path)), shouldTakeDefault));
            }

            return data;
        }

        private IList<DailyFile> ReadDailies(string dataDir)
        {
            var dailyPath = GetOrCreateDayDirectory(dataDir);
            var userFolders = Directory.GetDirectories(dailyPath);
            return userFolders.Select(x => new DailyFile(ReadLatestFile(x), GetUserNameForFileInfo(x))).ToList();
        }

        private IEnumerable<DefaultFile> ReadDefaults(string dataDir)
        {
            var defaultsDirectoryPath = Path.Combine(dataDir, "defaults");
            var userFolders = Directory.GetDirectories(defaultsDirectoryPath);
            return userFolders.Select(x => new DefaultFile(ReadLatestFile(x), GetUserNameForFileInfo(x)));
        }

        private static string GetUserNameForFileInfo(string s)
        {
            var components = s.Split(new[] {Path.DirectorySeparatorChar});
            return components.First(x => x.Contains("@"));
        }

        private static FileInfo ReadLatestFile(string path)
        {
            return
                Directory.EnumerateFileSystemEntries(path)
                    .Select(p => new FileInfo(p))
                    .OrderByDescending(fi => fi.CreationTime)
                    .First();
        }


        private static string GetOrCreateDayDirectory(string dataDir)
        {
            var dayPath = Path.Combine(dataDir, DateTime.UtcNow.ToString("yyyyMMdd"));

            if (!Directory.Exists(dayPath))
            {
                Directory.CreateDirectory(dayPath);
            }

            return dayPath;
        }

        private static string GetOrCreateUserDirectory(string dataDir, string user)
        {
            var dayPath = GetOrCreateDayDirectory(dataDir);

            var userPath = Path.Combine(dayPath, user);

            if (!Directory.Exists(userPath))
            {
                Directory.CreateDirectory(userPath);
            }

            return userPath;
        }
    }

    internal class DefaultFile
    {
        public readonly FileInfo FileInfo;
        public readonly string UserName;

        public DefaultFile(FileInfo fileInfo, string userName)
        {
            FileInfo = fileInfo;
            UserName = userName;
        }
    }

    internal class DailyFile
    {
        public readonly FileInfo FileInfo;
        public readonly string UserName;

        public DailyFile(FileInfo fileInfo, string userName)
        {
            FileInfo = fileInfo;
            UserName = userName;
        }
    }
}