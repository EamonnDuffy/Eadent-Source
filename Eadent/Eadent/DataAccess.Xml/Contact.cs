//---------------------------------------------------------------------------
// Copyright © 2009-2009 Eamonn Duffy. All Rights Reserved.
//---------------------------------------------------------------------------
//
//  $RCSfile: $
//
// $Revision: $
//
// Created:	Eamonn A. Duffy, 29-Sep-2009.
//
// Purpose:	Contact XML Data Access routines.
//
//---------------------------------------------------------------------------

using Eadent.Helpers;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;

// TODO: Consider having some way of telling if a Failed To Send message has been acknowledged (e.g. via a Web Service). Mark as Read/Unread?

// TODO: Data resources should be disposed.

namespace Eadent.DataAccess.Xml
{
    internal class Contact
    {
        private const string StatusOk                           = "00. Ok.";
        internal const string StatusInvalidGuid                 = "01. Invalid Guid.";
        internal const string StatusServerErrorDirectory        = "02. Server error (Could not access Directory).";
        internal const string StatusServerErrorOpenFile         = "03. Server error (Could not open File).";
        internal const string StatusServerErrorWriteFile        = "04. Server error (Could not write File).";
        internal const string StatusServerLockedFile            = "05. Server error (File probably locked).";
        internal const string StatusServerCorruptFile           = "06. Server error (File corrupt).";
        internal const string StatusServerUnhandledException    = "07. Server error (Unhandled Exception).";
        internal const string StatusCreated                     = "08. Created.";
        internal const string StatusDoesNotExist                = "09. Entry does not exist.";
        internal const string StatusAlreadyExists               = "10. Entry already exists.";
        internal const string StatusNotOurs                     = "11. Entry not ours.";
        internal const string StatusSuspiciousAccess            = "12. Suspicious access.";
        internal const string StatusUpdateOk                    = "13. Update was Ok.";
        internal const string StatusUpdateSuspicious            = "14. Update was suspicious (File issue).";
        internal const string StatusSent                        = "15. Sent.";
        internal const string StatusSentSuspicious              = "16. Sent (File update issue).";
        internal const string StatusReadyToSend                 = "17. Ready to send.";
        internal const string StatusFailedToSend                = "18. Failed to send.";

        // TODO: Look into performance and elegance differences between DataSet and XML access classes.
        // TODO: I think maybe I cannot use attributes with the DataSet approach?

        private static string GetUtc()
        {
            string Result;

            Utility Utility = new Utility();
            Result = Utility.GetPersistentDateTime();

            return Result;
        }

        private static DataTable CreateHeaderTable()
        {
            DataTable Table = new DataTable("Header");
            Table.Columns.Add("Version");
            Table.Columns.Add("Domain");
            Table.Columns.Add("CreatedDateUtc");
            Table.Columns.Add("LastUpdateDateUtc");
            Table.Columns.Add("UpdateCount", typeof(int));

            return Table;
        }

        private static DataTable CreateEntryTable()
        {
            DataTable Table = new DataTable("Entry");
            Table.Columns.Add("Guid");
            Table.Columns.Add("CreatedDateUtc");
            Table.Columns.Add("CreatedRemoteAddress");
            Table.Columns.Add("Status");
            Table.Columns.Add("SentDateUtc");
            Table.Columns.Add("SentRemoteAddress");
            Table.Columns.Add("LastSuspiciousDateUtc");
            Table.Columns.Add("LastSuspiciousRemoteAddress");
            Table.Columns.Add("LastAdditionalMessage");
            Table.Columns.Add("UpdateCount", typeof(int));

            return Table;
        }

        private static DataSet CreateFileSet()
        {
            DataSet FileSet = new DataSet("ContactFile");
            FileSet.Tables.Add(CreateHeaderTable());
            FileSet.Tables.Add(CreateEntryTable());

            // TODO: Understand the convention on things like <ead:DateCreatedUtc> vs <DateCreatedUtc>.
            // TODO: Also find out why the following does not work as I thought it might.
            //FileSet.Prefix = "ead";

            // Ok. Try the following. Seems to work.
            // TODO: Establish if I should use the same on every Table.
            // TODO: Understand the conventions on XML namespaces.
            // TODO: Also establish/consider if there is likely to be any clash with EAD (Employment Authorisation Document? Encoded Archival Description?).
            FileSet.Namespace = "https://www.Eadent.com/DataAccess.Xml/ContactFile.xsd";
            //FileSet.Prefix = "ead";   // TODO: Review the main tag and the namespace attribute when this is used (<ead:Contact xmlns:ead="ead">).

            return FileSet;
        }

        private static DataRow CreateDefaultHeader(DataTable HeaderTable)
        {
            HeaderTable.Clear();

            DataRow NewRow = HeaderTable.NewRow();
            NewRow["Version"] = "1";   // TODO: Consider this.
            NewRow["Domain"] = AssemblyInfo.Domain;
            NewRow["CreatedDateUtc"] = GetUtc();
            NewRow["UpdateCount"] = 0;
            HeaderTable.Rows.Add(NewRow);

            return NewRow;
        }

        private static bool VerifyGuid(string Guid)
        {
            bool bValid = false;

            // TODO: Verify the integrity of the Guid.
            bValid = true;

            return bValid;
        }

        // [Attempt to] Always Open a file, creating it if necessary.
        private static Stream OpenFileAlways(string FilePath, out string Status)
        {
            Status = StatusOk;

            Stream FileStream = null;

            Stopwatch Timer = null;

            bool bRetry = false;

            int CreateDirectoryCount = 0;

            do
            {
                try
                {
                    if (FileStream != null) // Should never happen.
                        FileStream.Close();

                    FileStream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                    bRetry = false;

                    if (FileStream == null) // Should never happen.
                        Status = StatusServerErrorOpenFile;
                    else
                        Status = StatusOk;
                }
                catch (DirectoryNotFoundException Exception)
                {
                    if (CreateDirectoryCount >= 1)  // We have already tried to create a Directory. NOTE: Would probably have gotten an Exception from CreateDirectory() below so this case is unlikely.
                    {
                        bRetry = false;
                        Status = StatusServerErrorDirectory;
                    }
                    else    // First time to try creating the Directory.
                    {
                        CreateDirectoryCount++;

                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); // NOTE: Probably won't succeed on a Shared Hosting Server.
                            bRetry = true;
                        }
                        catch (Exception CreateException)
                        {
                            bRetry = false;
                            Status = StatusServerErrorDirectory;
                        }
                    }
                }
                catch (IOException Exception)
                {
                    if (Timer == null)
                    {
                        Timer = new Stopwatch();
                        Timer.Start();
                        bRetry = true;
                    }
                    else if (Timer.ElapsedMilliseconds >= Configuration.OpenLockedFileAbandonTimeout)
                    {
                        bRetry = false;
                        Status = StatusServerLockedFile;
                    }
                    else
                    {
                        Thread.Sleep(Configuration.OpenLockedFileBackoffSleep);
                        bRetry = true;
                    }
                }
                catch (Exception Exception)
                {
                    Status = StatusServerErrorOpenFile;
                }
            } while (bRetry);

            if (Timer != null)
                Timer.Stop();

            return FileStream;
        }

        // Open a file if it already exists.
        private static Stream OpenFileIfExists(string FilePath, out string Status)
        {
            Status = StatusOk;

            Stream FileStream = null;

            Stopwatch Timer = null;

            bool bRetry = false;

            do
            {
                try
                {
                    if (FileStream != null) // Should never happen.
                        FileStream.Close();

                    FileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    bRetry = false;

                    if (FileStream == null) // Should never happen.
                        Status = StatusServerErrorOpenFile;
                    else
                        Status = StatusOk;
                }
                catch (DirectoryNotFoundException Exception)
                {
                    bRetry = false;
                    Status = StatusServerErrorDirectory;
                }
                catch (FileNotFoundException Exception)
                {
                    bRetry = false;
                    Status = StatusServerErrorOpenFile;
                }
                catch (IOException Exception)
                {
                    if (Timer == null)
                    {
                        Timer = new Stopwatch();
                        Timer.Start();
                        bRetry = true;
                    }
                    else if (Timer.ElapsedMilliseconds >= Configuration.OpenLockedFileAbandonTimeout)
                    {
                        bRetry = false;
                        Status = StatusServerLockedFile;
                    }
                    else
                    {
                        Thread.Sleep(Configuration.OpenLockedFileBackoffSleep);
                        bRetry = true;
                    }
                }
                catch (Exception Exception)
                {
                    Status = StatusServerErrorOpenFile;
                }
            } while (bRetry);

            if (Timer != null)
                Timer.Stop();

            return FileStream;
        }

        private static void ReadFile(Stream FileStream, DataSet FileSet)
        {
            try
            {
                FileSet.ReadXml(FileStream);

                // TODO: Check the Version Number and determine what should be done if anything.
                // TODO: Update the Version Number to be the latest if it is not already so?
            }
            catch (Exception Exception)
            {
                // Silently ignore any read issues.
            }
        }

        private static string WriteFile(Stream FileStream, DataSet FileSet)
        {
            string ReturnStatus = StatusOk;

            try
            {
                // Original: <?xml version="1.0" standalone="yes"?>
                // Now: ﻿     <?xml version="1.0" encoding="utf-8" standalone="yes"?>
                FileStream.SetLength(0);
                XmlWriterSettings Settings = new XmlWriterSettings();
                Settings.Indent = true;
                XmlWriter Writer = XmlWriter.Create(FileStream, Settings);
                Writer.WriteStartDocument(true);

                DataTable HeaderTable = FileSet.Tables["Header"];
                DataRow HeaderRow = null;

                if (HeaderTable.Rows.Count < 1) // We need to create a default Header.
                    HeaderRow = CreateDefaultHeader(HeaderTable);
                else
                    HeaderRow = HeaderTable.Rows[0];    // CONVENTION: Choose the first entry.

                HeaderRow["LastUpdateDateUtc"] = GetUtc();
                HeaderRow["UpdateCount"] = (int)HeaderRow["UpdateCount"] + 1;

                FileSet.WriteXml(Writer);
            }
            catch
            {
                ReturnStatus = StatusServerErrorWriteFile;
            }

            return ReturnStatus;
        }

        // Create an entry in the Contact file, ready for when the message is submitted.
        internal static string Create(string FilePath, string Guid, string RemoteAddress)
        {
            string ReturnStatus = StatusOk;

            Stream FileStream = null;

            DataSet FileSet = null;
            DataTable EntryTable = null;

            try
            {
                bool bContinue = true;

                if (!VerifyGuid(Guid))
                {
                    ReturnStatus = StatusInvalidGuid;
                    bContinue = false;
                }

                // TODO: Add more Exception Handling.

                if (bContinue)
                {
                    string OpenStatus = null;

                    FileStream = OpenFileAlways(FilePath, out OpenStatus);

                    //Thread.Sleep(20 * 1000);

                    if (FileStream == null)
                    {
                        ReturnStatus = OpenStatus;
                        bContinue = false;
                    }
                }

                if (bContinue)
                {
                    FileSet = CreateFileSet();
                    ReadFile(FileStream, FileSet);

                    EntryTable = FileSet.Tables["Entry"];

                    DataRow[] GuidRows = EntryTable.Select("Guid='" + Guid + "'");

                    //DataRow[] GuidRows = EntryTable.Select("MAX(SendSequenceNumber)");    // Does not work.
                    //int Max = (int)EntryTable.Compute("MAX(SendSequenceNumber)", null);   // E.g. If ever introduce a Sequence Number.

                    if (GuidRows.Length > 0)    // Entry already exists.
                    {
                        if (GuidRows.Length > 1)    // Corrupt file.
                        {
                            // TODO: Determine what to do here.
                        }

                        DataRow EntryRow = GuidRows[0]; // CONVENTION: Choose the first entry?

                        if ((string)EntryRow["Status"] == StatusNotOurs)   // DECISION: We overwrite once/if we generate the same Guid as a suspicious access.
                        {
                            EntryRow["Guid"] = Guid;
                            EntryRow["CreatedDateUtc"] = GetUtc();
                            EntryRow["CreatedRemoteAddress"] = RemoteAddress;
                            EntryRow["Status"] = StatusCreated;
                            EntryRow["SentDateUtc"] = null;
                            EntryRow["SentRemoteAddress"] = null;
                            EntryRow["LastSuspiciousDateUtc"] = null;
                            EntryRow["LastSuspiciousRemoteAddress"] = null;
                            EntryRow["LastAdditionalMessage"] = null;
                            EntryRow["UpdateCount"] = 1;

                            WriteFile(FileStream, FileSet);

                            ReturnStatus = StatusCreated;
                        }
                        else    // Suspicious access. Should not happen.
                        {
                            EntryRow["Status"] = StatusSuspiciousAccess;
                            EntryRow["LastSuspiciousDateUtc"] = GetUtc();
                            EntryRow["LastSuspiciousRemoteAddress"] = RemoteAddress;

                            EntryRow["UpdateCount"] = (int)EntryRow["UpdateCount"] + 1;

                            WriteFile(FileStream, FileSet);

                            ReturnStatus = StatusAlreadyExists; // REVIEW: Could use: (string)EntryRow["Status"];
                        }
                    }
                    else    // Entry does not exist, so new entry.
                    {
                        // TODO: Consider performance issues as file grows. Could have a separate file for each Guid.

                        DataRow NewRow = EntryTable.NewRow();
                        NewRow["Guid"] = Guid;
                        NewRow["CreatedDateUtc"] = GetUtc();
                        NewRow["CreatedRemoteAddress"] = RemoteAddress;
                        NewRow["Status"] = StatusCreated;
                        NewRow["UpdateCount"] = 1;
                        EntryTable.Rows.Add(NewRow);

                        WriteFile(FileStream, FileSet);

                        ReturnStatus = StatusCreated;
                    }
                }
            }
            catch (Exception Exception)
            {
                ReturnStatus = StatusServerUnhandledException;
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }

            return ReturnStatus;
        }

        // Return the Row for the Contact entry for the specified Guid.
        internal static DataRow GetRow(string FilePath, string Guid)
        {
            DataRow Row = null;

            Stream FileStream = null;

            DataSet FileSet = null;

            try
            {
                // TODO: Add more Exception Handling.

                bool bContinue = true;

                if (!VerifyGuid(Guid))
                {
                    bContinue = false;
                }

                if (bContinue)
                {
                    string OpenStatus = null;

                    FileStream = OpenFileAlways(FilePath, out OpenStatus);

                    if (FileStream == null)
                    {
                        bContinue = false;
                    }
                }

                if (bContinue)
                {
                    FileSet = CreateFileSet();
                    ReadFile(FileStream, FileSet);

                    DataRow[] GuidRows = FileSet.Tables["Entry"].Select("Guid='" + Guid + "'");

                    if (GuidRows.Length > 0)  // The entry exists.
                    {
                        if (GuidRows.Length > 1)    // Corrupt file.
                        {
                            // TODO: Determine what to do here.
                        }

                        // TODO: Put in more Exception handling, e.g. if the file has become corrupt.

                        // TODO: Determine if I need to return a copy of the DataRow rather than just using the one obtained from the DataTable.
                        Row = GuidRows[0]; // CONVENTION: Choose the first entry?
                    }
                }
            }
            catch (Exception Exception)
            {
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }

            return Row;
        }

        // Return the number of Contact entries that are marked as Failed To Send.
        internal static int GetFailedToSendCount(string FilePath)
        {
            int FailedToSendCount = 0;

            Stream FileStream = null;

            DataSet FileSet = null;

            try
            {
                // TODO: Add more Exception Handling.

                bool bContinue = true;

                string OpenStatus = null;

                FileStream = OpenFileIfExists(FilePath, out OpenStatus);

                if (FileStream == null)
                {
                    bContinue = false;
                }

                if (bContinue)
                {
                    FileSet = CreateFileSet();
                    ReadFile(FileStream, FileSet);

                    // TODO: Determine if Compute() can ever return DBNull.Value. c.f. http://msdn.microsoft.com/en-us/library/system.data.datatable.compute.aspx
                    FailedToSendCount = (int)FileSet.Tables["Entry"].Compute("COUNT(Status)", "Status='" + StatusFailedToSend + "'");
                }
            }
            catch (Exception Exception)
            {
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }

            return FailedToSendCount;
        }

        // Return the Status of the Contact entry for the specified Guid.
        internal static string GetStatus(string FilePath, string Guid)
        {
            string ReturnStatus = StatusDoesNotExist;

            DataRow Row = GetRow(FilePath, Guid);

            if (Row != null)
                ReturnStatus = (string)Row["Status"];

            return ReturnStatus;
        }

        // Verify that the Contact entry for the specified Guid is ready to send.
        internal static string VerifyCanSend(string FilePath, string Guid, string RemoteAddress)
        {
            string ReturnStatus = StatusNotOurs;

            Stream FileStream = null;

            DataSet FileSet = null;
            DataTable EntryTable = null;

            try
            {
                bool bContinue = true;

                if (!VerifyGuid(Guid))
                {
                    ReturnStatus = StatusInvalidGuid;
                    bContinue = false;
                }

                // TODO: Add more Exception Handling.

                if (bContinue)
                {
                    string OpenStatus = null;

                    FileStream = OpenFileAlways(FilePath, out OpenStatus);

                    if (FileStream == null)
                    {
                        ReturnStatus = OpenStatus;
                        bContinue = false;
                    }
                }

                if (bContinue)
                {
                    FileSet = CreateFileSet();
                    ReadFile(FileStream, FileSet);

                    EntryTable = FileSet.Tables["Entry"];

                    DataRow[] GuidRows = EntryTable.Select("Guid='" + Guid + "'");

                    if (GuidRows.Length < 1)    // Entry does not exist. This may indicate some form of suspicious access or attack?
                    {
                        string Utc = GetUtc();
                        DataRow NewRow = EntryTable.NewRow();
                        NewRow["Guid"] = Guid;
                        NewRow["CreatedDateUtc"] = Utc;
                        NewRow["CreatedRemoteAddress"] = RemoteAddress;
                        NewRow["Status"] = StatusNotOurs;
                        NewRow["LastSuspiciousDateUtc"] = Utc;
                        NewRow["LastSuspiciousRemoteAddress"] = RemoteAddress;
                        NewRow["UpdateCount"] = 1;
                        EntryTable.Rows.Add(NewRow);

                        WriteFile(FileStream, FileSet);

                        ReturnStatus = (string)NewRow["Status"];
                    }
                    else    // The entry exists.
                    {
                        if (GuidRows.Length > 1)    // Corrupt file.
                        {
                            // TODO: Determine what to do here.
                        }

                        // TODO: Put in more Exception handling, e.g. if the file has become corrupt.

                        DataRow EntryRow = GuidRows[0]; // CONVENTION: Choose the first entry?

                        string EntryStatus = (string)EntryRow["Status"];

                        if ((EntryStatus == StatusCreated) || (EntryStatus == StatusFailedToSend))  // The Entry is ready to be Sent.
                        {
                            ReturnStatus = StatusReadyToSend;
                        }
                        else    // Some unexpected/suspicious access.
                        {
                            if (EntryStatus != StatusNotOurs)
                                EntryRow["Status"] = StatusSuspiciousAccess;
                            EntryRow["LastSuspiciousDateUtc"] = GetUtc();
                            EntryRow["LastSuspiciousRemoteAddress"] = RemoteAddress;
                            EntryRow["UpdateCount"] = (int)EntryRow["UpdateCount"] + 1;

                            WriteFile(FileStream, FileSet);

                            ReturnStatus = (string)EntryRow["Status"];
                        }
                    }
                }
            }
            catch (Exception Exception)
            {
                ReturnStatus = StatusServerUnhandledException;
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }

            return ReturnStatus;
        }
        
        // Update the Contact entry for the specified Guid as "Sent".
        internal static string UpdateAsSent(string FilePath, string Guid, string RemoteAddress)
        {
            string ReturnStatus = StatusDoesNotExist;

            Stream FileStream = null;

            DataSet FileSet = null;
            DataTable EntryTable = null;

            try
            {
                // TODO: Add more Exception Handling.

                bool bContinue = true;

                if (!VerifyGuid(Guid))
                {
                    ReturnStatus = StatusInvalidGuid;
                    bContinue = false;
                }

                if (bContinue)
                {
                    string OpenStatus = null;

                    FileStream = OpenFileAlways(FilePath, out OpenStatus);

                    if (FileStream == null)
                    {
                        ReturnStatus = OpenStatus;
                        bContinue = false;
                    }
                }

                if (bContinue)
                {
                    FileSet = CreateFileSet();
                    ReadFile(FileStream, FileSet);

                    EntryTable = FileSet.Tables["Entry"];

                    DataRow[] GuidRows = EntryTable.Select("Guid='" + Guid + "'");

                    if (GuidRows.Length < 1)    // Entry does not exist. This may indicate some form of coding error or a file corruption/replacement issue?
                    {
                        string Utc = GetUtc();
                        DataRow NewRow = EntryTable.NewRow();
                        NewRow["Guid"] = Guid;
                        NewRow["CreatedDateUtc"] = Utc;
                        NewRow["CreatedRemoteAddress"] = RemoteAddress;
                        NewRow["Status"] = StatusSentSuspicious;
                        NewRow["SentDateUtc"] = Utc;
                        NewRow["SentRemoteAddress"] = RemoteAddress;
                        NewRow["LastSuspiciousDateUtc"] = Utc;
                        NewRow["LastSuspiciousRemoteAddress"] = RemoteAddress;
                        NewRow["LastAdditionalMessage"] = "NOTE: Update As Sent could not find Entry. Could be a coding error or a file corruption/replacement issue?";
                        NewRow["UpdateCount"] = 1;
                        EntryTable.Rows.Add(NewRow);

                        WriteFile(FileStream, FileSet);

                        ReturnStatus = StatusUpdateSuspicious;
                    }
                    else    // The entry exists.
                    {
                        // TODO: Put in more Exception handling, e.g. if the file has become corrupt.

                        DataRow EntryRow = GuidRows[0]; // CONVENTION: Choose the first entry?

                        if (GuidRows.Length > 1)    // Corrupt file.
                        {
                            // TODO: Determine what to do here.
                        }

                        // DECISION: At this stage we overwrite the entry, regardless of what the existing Status is.

                        EntryRow["Status"] = StatusSent;
                        EntryRow["SentDateUtc"] = GetUtc();
                        EntryRow["SentRemoteAddress"] = RemoteAddress;
                        EntryRow["UpdateCount"] = (int)EntryRow["UpdateCount"] + 1;

                        WriteFile(FileStream, FileSet);

                        ReturnStatus = StatusUpdateOk;
                    }
                }
            }
            catch (Exception Exception)
            {
                ReturnStatus = StatusServerUnhandledException;
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }

            return ReturnStatus;
        }

        // Update the Status as Failed To Send and optionally update the LastAdditionalMessage for the Contact entry for the specified Guid.
        // NOTE: If the s pecified LastAdditionalMessage is null, the LastAdditionalMessage field is NOT updated.
        internal static string UpdateAsFailedToSend(string FilePath, string Guid, string RemoteAddress, string LastAdditionalMessage)
        {
            string ReturnStatus = StatusDoesNotExist;

            Stream FileStream = null;

            DataSet FileSet = null;
            DataTable EntryTable = null;

            try
            {
                // TODO: Add more Exception Handling.

                bool bContinue = true;

                if (!VerifyGuid(Guid))
                {
                    ReturnStatus = StatusInvalidGuid;
                    bContinue = false;
                }

                if (bContinue)
                {
                    string OpenStatus = null;

                    FileStream = OpenFileAlways(FilePath, out OpenStatus);

                    if (FileStream == null)
                    {
                        ReturnStatus = OpenStatus;
                        bContinue = false;
                    }
                }

                if (bContinue)
                {
                    FileSet = CreateFileSet();
                    ReadFile(FileStream, FileSet);

#if false
                    // TODO: Review XmlException when reading. CONVENTION: Overwrite a corrupt file.

                    catch (XmlException Exception)
                    {
                        ReturnStatus = StatusServerCorruptFile;
                        bContinue = false;
                    }
#endif

                    EntryTable = FileSet.Tables["Entry"];

                    DataRow[] GuidRows = EntryTable.Select("Guid='" + Guid + "'");

                    if (GuidRows.Length < 1)    // Entry does not exist. This may indicate some form of coding error or a file corruption/replacement issue?
                    {
                        string Utc = GetUtc();
                        DataRow NewRow = EntryTable.NewRow();
                        NewRow["Guid"] = Guid;
                        NewRow["CreatedDateUtc"] = Utc;
                        NewRow["CreatedRemoteAddress"] = RemoteAddress;
                        NewRow["Status"] = StatusFailedToSend;
                        NewRow["LastSuspiciousDateUtc"] = Utc;
                        NewRow["LastSuspiciousRemoteAddress"] = RemoteAddress;
                        string UpdateMessage = "NOTE: Update could not find Entry. Could be a coding error or a file corruption/replacement issue?";
                        if (LastAdditionalMessage == null)
                            NewRow["LastAdditionalMessage"] = UpdateMessage;
                        else
                            NewRow["LastAdditionalMessage"] = LastAdditionalMessage + " [" + UpdateMessage + "]";
                        NewRow["UpdateCount"] = 1;
                        EntryTable.Rows.Add(NewRow);

                        WriteFile(FileStream, FileSet);

                        ReturnStatus = StatusUpdateSuspicious;
                    }
                    else    // The entry exists.
                    {
                        // TODO: Put in more Exception handling, e.g. if the file has become corrupt.

                        DataRow EntryRow = GuidRows[0]; // CONVENTION: Choose the first entry?

                        if (GuidRows.Length > 1)    // Corrupt file.
                        {
                            // TODO: Determine what to do here.
                        }

                        // DECISION: At this stage we overwrite the entry, regardless of what the existing Status is.

                        EntryRow["Status"] = StatusFailedToSend;
                        if (LastAdditionalMessage != null)
                            EntryRow["LastAdditionalMessage"] = LastAdditionalMessage;
                        EntryRow["UpdateCount"] = (int)EntryRow["UpdateCount"] + 1;

                        WriteFile(FileStream, FileSet);

                        ReturnStatus = StatusUpdateOk;
                    }
                }
            }
            catch (Exception Exception)
            {
                ReturnStatus = StatusServerUnhandledException;
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }

            return ReturnStatus;
        }

        // [Attempt to] Read the Contact file.
        internal static DataSet Read(string FilePath, out string Status)
        {
            DataSet FileSet = null;

            Status = StatusOk;

            Stream FileStream = null;

            try
            {
                // TODO: Add more Exception Handling.

                bool bContinue = true;

                string OpenStatus = null;

                FileStream = OpenFileIfExists(FilePath, out OpenStatus);

                if (FileStream == null)
                {
                    Status = OpenStatus;
                    bContinue = false;
                }

                if (bContinue)
                {
                    FileSet = CreateFileSet();
                    ReadFile(FileStream, FileSet);
                    Status = StatusOk;
                }
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }

            return FileSet;
        }
    }
}

//---------------------------------------------------------------------------
//                    End Of $RCSfile: $
//---------------------------------------------------------------------------
