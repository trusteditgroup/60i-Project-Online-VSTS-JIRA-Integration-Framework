#region License

/* 
 * Copyright © 2017 Yaroslav Boychenko.
 * Copyright © 2017 TrustedIt Group. Contacts: mailto:60i@trusteditgroup.com
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by Trusted IT Group Inc.
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this program.
 * If not, see https://www.trusteditgroup.com/60ilicense
 */

#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using NLog;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;
using ProjectOnlineSystemConnector.SignalR;

namespace ProjectOnlineSystemConnector.Common
{
    public static class HelperMethods
    {
        private static List<LogMessage> logMessages;
        private static int actionIndex;

        private static readonly string csvLogPath;

        static HelperMethods()
        {
            csvLogPath = ConfigurationManager.AppSettings["CsvLogPath"];
            logMessages = new List<LogMessage>();
        }

        public static string RemoveNewLinesTabs(this string sourceStr)
        {
            if (!String.IsNullOrEmpty(sourceStr))
            {
                return sourceStr.Replace("\t", " ").Replace(Environment.NewLine, " ").Trim();
            }
            return sourceStr;
        }

        public static string Truncate(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }
            string destination = source;
            if (source.Length > ProjectServerConstants.MaxTaskNameLength)
            {
                destination = source.Substring(0, ProjectServerConstants.MaxTaskNameLength);
            }
            return destination;
        }

        public static void LogAndSendMessage(this Logger logger, StagingDTO staging, string action,
            Guid? projectUid, Guid winServiceIterationUid, string message,
            bool isWarn, Exception exception, string actionSource = null, string actionStartEndMarker = null)
        {
            LogMessage logMessage = GenerateMessage(staging, action, projectUid, winServiceIterationUid,
                message, isWarn, exception, actionSource, actionStartEndMarker);
            if (logMessage.WinServiceIterationUid != Guid.Empty
                && !string.IsNullOrEmpty(logMessage.ActionSource))
            {
                AddToCollection(logMessage);
            }
            if (logMessage.WinServiceIterationUid != Guid.Empty
                && !string.IsNullOrEmpty(logMessage.ActionSource)
                && logMessage.ActionStartEndMarker == CommonConstants.End)
            {
                try
                {
                    ProjectOnlineSystemConnectorHubHelper.SendLogMessage(logMessage);
                }
                catch (InvalidOperationException invalidOperationException)
                {

                }
                catch (Exception exc)
                {
                    logger.Error(exc);
                }
            }

            if (!String.IsNullOrEmpty(message))
            {
                if (isWarn)
                {
                    logger.Warn(message);
                }
                else
                {
                    if (exception != null)
                    {
                        logger.Error(message);
                    }
                    else
                    {
                        logger.Info(message);
                    }
                }
            }
            if (exception != null)
            {
                logger.Error(exception);
            }
        }

        public static void InitCsvLog()
        {
            logMessages = new List<LogMessage>();
            actionIndex = 0;
        }

        public static void WriteToCsvLog()
        {
            if (!File.Exists(csvLogPath))
            {
                string csvHeader = $"{nameof(LogMessage.WinServiceIterationUid)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.TimeStampStart)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.TimeStampEnd)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.EndStartDiff)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.ProjectUid)}{CommonConstants.Delimeter}" +
                                   //$"{nameof(logMessage.StagingSystemId)}{CommonConstants.Delimeter}" +
                                   //$"{nameof(logMessage.StagingIssueKey)}{CommonConstants.Delimeter}" +
                                   //$"{nameof(logMessage.StagingRecordDateCreated)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.ActionIndex)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.Action)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.ActionSource)}{CommonConstants.Delimeter}" +
                                   //$"{nameof(logMessage.ActionStartEndMarker)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.ActionResult)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.Message)}{CommonConstants.Delimeter}" +
                                   $"{nameof(LogMessage.ExceptionMessage)}{CommonConstants.Delimeter}";
                csvHeader += Environment.NewLine;
                File.AppendAllText(csvLogPath, csvHeader);
            }
            string csvString = "";
            foreach (LogMessage logMessage in logMessages)
            {
                List<string> csvList = new List<string>
                {
                    logMessage.WinServiceIterationUid.ToString(),
                    logMessage.TimeStampStart.ToString("MM/dd/yyyy HH:mm:ss.ffff"),
                    logMessage.TimeStampEnd.ToString("MM/dd/yyyy HH:mm:ss.ffff"),
                    logMessage.EndStartDiff.ToString(CultureInfo.InvariantCulture),
                    logMessage.ProjectUid.ToString(),
                    //logMessage.StagingSystemId.ToString(),
                    //logMessage.StagingIssueKey,
                    //logMessage.StagingRecordDateCreated.ToString(CultureInfo.InvariantCulture),
                    logMessage.ActionIndex.ToString(),
                    logMessage.Action,
                    logMessage.ActionSource,
                    //logMessage.ActionStartEndMarker,
                    logMessage.ActionResult,
                    logMessage.Message,
                    logMessage.ExceptionMessage,
                };
                csvString = csvString + csvList.Aggregate("",
                                (current, csvValue) => current + csvValue + CommonConstants.Delimeter);
                csvString += Environment.NewLine;
            }
            File.AppendAllText(csvLogPath, csvString);
        }

        private static void AddToCollection(LogMessage logMessage)
        {
            if (logMessage.ActionStartEndMarker == CommonConstants.Start)
            {
                logMessages.Add(logMessage);
            }
            else
            {
                LogMessage logMessageTmp = logMessages.LastOrDefault(
                    x => x.ActionStartEndMarker == CommonConstants.Start &&
                         x.ProjectUid == logMessage.ProjectUid && x.Action == logMessage.Action &&
                         x.ActionSource == logMessage.ActionSource);
                if (logMessageTmp != null)
                {
                    logMessageTmp.TimeStampEnd = DateTime.Now;
                    logMessageTmp.EndStartDiff = (logMessageTmp.TimeStampEnd - logMessageTmp.TimeStampStart).TotalSeconds;
                }
            }
        }

        private static LogMessage GenerateMessage(StagingDTO staging, string action, Guid? projectUid,
            Guid winServiceIterationGuid, string message, bool isWarn, Exception exception, string actionSource,
            string actionStartEndMarker)
        {
            var logMessage = new LogMessage
            {
                WinServiceIterationUid = winServiceIterationGuid,
                Message = message,
                IsWarn = isWarn,
                ActionIndex = actionIndex,
                Action = action,
                ActionSource = actionSource,
                ActionStartEndMarker = actionStartEndMarker,
                TimeStampStart = DateTime.Now
            };
            if (staging != null)
            {
                logMessage.StagingRecordDateCreated = staging.RecordDateCreated;
                logMessage.StagingIssueKey = staging.IssueKey;
                logMessage.StagingSystemId = staging.SystemId;
            }
            if (exception != null)
            {
                logMessage.ActionResult = CommonConstants.Ko;
                logMessage.ExceptionMessage = exception.Message;
            }
            else
            {
                logMessage.ActionResult = CommonConstants.Ok;
            }
            if (projectUid.HasValue)
            {
                logMessage.IsBroadcastMessage = false;
                logMessage.ProjectUid = projectUid.Value;
            }
            else
            {
                logMessage.IsBroadcastMessage = true;
            }
            actionIndex++;
            return logMessage;
        }
    }
}