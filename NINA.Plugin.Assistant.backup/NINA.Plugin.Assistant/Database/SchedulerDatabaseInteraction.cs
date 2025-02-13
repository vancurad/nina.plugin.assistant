﻿using Assistant.NINAPlugin.Util;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Assistant.NINAPlugin.Database {

    public class SchedulerDatabaseInteraction {

        private static readonly string DATABASE_BASENAME = "schedulerdb";
        private static readonly string DATABASE_SUFFIX = "sqlite";
        private static readonly string DATABASE_FILENAME = $"{DATABASE_BASENAME}.{DATABASE_SUFFIX}";
        private static readonly int DATABASE_BACKUPS = 3;

        static SchedulerDatabaseInteraction() {
            DllLoader.LoadDll(Path.Combine("SQLite", "SQLite.Interop.dll"));
        }

        private string connectionString;

        public SchedulerDatabaseInteraction()
            : this(string.Format(@"Data Source={0};", Environment.ExpandEnvironmentVariables($@"{AssistantPlugin.PLUGIN_HOME}\{DATABASE_FILENAME}"))) {
        }

        public SchedulerDatabaseInteraction(string connectionString) {
            this.connectionString = connectionString;
        }

        public SchedulerDatabaseContext GetContext() {
            return new SchedulerDatabaseContext(connectionString);
        }

        public static void BackupDatabase() {
            string sourceFile = Path.Combine(AssistantPlugin.PLUGIN_HOME, DATABASE_FILENAME);
            if (!File.Exists(sourceFile)) {
                return;
            }

            try {
                string renamed = $"{DATABASE_BASENAME}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}-backup.{DATABASE_SUFFIX}";
                string newFileName = Path.Combine(AssistantPlugin.PLUGIN_HOME, renamed);
                TSLogger.Debug($"backing up Target Scheduler database to {newFileName}");
                File.Copy(sourceFile, newFileName);

                // Keep only the most recent N backups
                List<FileInfo> dbFiles = new DirectoryInfo(AssistantPlugin.PLUGIN_HOME).GetFiles($"*-backup.{DATABASE_SUFFIX}")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (dbFiles.Count <= DATABASE_BACKUPS) {
                    return;
                }

                for (int i = DATABASE_BACKUPS; i < dbFiles.Count; i++) {
                    string filename = dbFiles[i].FullName;
                    TSLogger.Debug($"removing older backup database file: {filename}");
                    File.Delete(filename);
                }

            }
            catch (Exception e) {
                TSLogger.Error($"failed to backup database: {e.Message}:{Environment.NewLine}{e.StackTrace}");
                Notification.ShowWarning($"Failed to backup Target Scheduler database, see log for errors");
            }
        }
    }

}
