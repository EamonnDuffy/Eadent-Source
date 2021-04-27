//---------------------------------------------------------------------------
// Copyright © 2009-2009 Eamonn Duffy. All Rights Reserved.
//---------------------------------------------------------------------------
//
//  $RCSfile: $
//
// $Revision: $
//
// Created:	Eamonn A. Duffy, 3-Oct-2009.
//
// Purpose:	Message XML Data Access routines.
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
    internal class Message
    {
        internal const string StatusOk                          = "00. Ok.";
        internal const string StatusInvalidGuid                 = "01. Invalid Guid.";
        internal const string StatusServerErrorDirectory        = "02. Server error (Could not access Directory).";
        internal const string StatusServerErrorOpenFile         = "03. Server error (Could not open File).";
        internal const string StatusServerErrorWriteFile        = "04. Server error (Could not write File).";
        internal const string StatusServerLockedFile            = "05. Server error (File probably locked).";
        internal const string StatusServerUnhandledException    = "06. Server error (Unhandled Exception).";
        internal const string StatusCreated                     = "07. Created.";
        internal const string StatusUnRead                      = "08. Un-Read.";
        internal const string StatusRead                        = "09. Read.";
#if false
        internal const string StatusServerCorruptFile = "05. Server error (File corrupt).";
        internal const string StatusDoesNotExist = "09. Entry does not exist.";
        internal const string StatusAlreadyExists = "10. Entry already exists.";
        internal const string StatusNotOurs = "11. Entry not ours.";
        internal const string StatusSuspiciousAccess = "12. Suspicious access.";
        internal const string StatusUpdateOk = "13. Update was Ok.";
        internal const string StatusUpdateSuspicious = "14. Update was suspicious (File issue).";
        internal const string StatusSent = "15. Sent.";
        internal const string StatusSentSuspicious = "16. Sent (File update issue).";
        internal const string StatusReadyToSend = "17. Ready to send.";
        internal const string StatusFailedToSend = "18. Failed to send.";
#endif

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

        private static DataTable CreateDetailTable()
        {
            DataTable Table = new DataTable("Detail");
            Table.Columns.Add("Guid");
            Table.Columns.Add("LastCreatedUrl");
            Table.Columns.Add("LastCreatedDateUtc");
            Table.Columns.Add("LastCreatedRemoteAddress");
            Table.Columns.Add("LastCreatedExceptionMessage");
            Table.Columns.Add("Status");
            Table.Columns.Add("Name");
            Table.Columns.Add("EMail");
            Table.Columns.Add("Message");
            Table.Columns.Add("UpdateCount", typeof(int));

            return Table;
        }

        private static DataSet CreateFileSet()
        {
            DataSet FileSet = new DataSet("MessageFile");
            FileSet.Tables.Add(CreateHeaderTable());
            FileSet.Tables.Add(CreateDetailTable());

            // TODO: Understand the convention on things like <ead:DateCreatedUtc> vs <DateCreatedUtc>.
            // TODO: Also find out why the following does not work as I thought it might.
            //FileSet.Prefix = "ead";

            // Ok. Try the following. Seems to work.
            // TODO: Establish if I should use the same on every Table.
            // TODO: Understand the conventions on XML namespaces.
            // TODO: Also establish/consider if there is likely to be any clash with EAD (Employment Authorisation Document? Encoded Archival Description?).
            FileSet.Namespace = "https://www.Eadent.com/DataAccess.Xml/MessageFile.xsd";
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

        private static string MakeFilePath(string RootFilePath, string Guid)
        {
            return RootFilePath + Configuration.MessageFilePrefix + Guid + ".xml";
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

        // Return the number of Message Files at the specified Root Path.
        internal static int GetNumFiles(string RootFilePath)
        {
            return Utility.GetNumFiles(RootFilePath, Configuration.MessageFilePrefix + "*.xml");
        }

        // Create an entry in the Message file.
        internal static string Create(string RootFilePath, string Guid, string RemoteAddress, string ExceptionMessage,
                                    string Name, string EMail, string Url, string Message)
        {
            string ReturnStatus = StatusOk;

            string FilePath = null;

            Stream FileStream = null;

            DataSet FileSet = null;
            DataTable DetailTable = null;

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
                    FilePath = MakeFilePath(RootFilePath, Guid);

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

                    DetailTable = FileSet.Tables["Detail"];

                    DataRow DetailRow = null;

                    bool bNewRow = true;

                    if (DetailTable.Rows.Count > 0)  // At least one entry exists.
                    {
                        DetailRow = DetailTable.Rows[0];  // CONVENTION: Use the first entry.
                        bNewRow = false;
                    }
                    else    // Create a new entry.
                        DetailRow = DetailTable.NewRow();

                    DetailRow["Guid"] = Guid;
                    DetailRow["LastCreatedUrl"] = Url;
                    DetailRow["LastCreatedDateUtc"] = GetUtc();
                    DetailRow["LastCreatedRemoteAddress"] = RemoteAddress;
                    if (ExceptionMessage != null)
                        DetailRow["LastCreatedExceptionMessage"] = ExceptionMessage;
                    DetailRow["Status"] = StatusUnRead;
                    DetailRow["Name"] = Name;
                    DetailRow["EMail"] = EMail;
                    DetailRow["Message"] = Message;

                    if (bNewRow)
                    {
                        DetailRow["UpdateCount"] = 1;
                        DetailTable.Rows.Add(DetailRow);
                    }
                    else
                        DetailRow["UpdateCount"] = (int)DetailRow["UpdateCount"] + 1;

                    ReturnStatus = WriteFile(FileStream, FileSet);

                    if (ReturnStatus == StatusOk)
                        ReturnStatus = StatusCreated;
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

#if false
        // Create an entry in the Message file (using the strongly typed MessageFile DataSet).
        private static string CreateNew(string RootFilePath, string Guid, string RemoteAddress, string ExceptionMessage,
                                    string Name, string EMail, string Url, string Message)
        {
            string ReturnStatus = StatusOk;

            string FilePath = null;

            Stream FileStream = null;

            MessageFile FileSet = null;

            MessageFile.DetailDataTable DetailTable = null;

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
                    FilePath = MakeFilePath(RootFilePath, Guid);

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
                    FileSet = new MessageFile();

                    ReadFile(FileStream, FileSet);

                    DetailTable = FileSet.Detail;

                    MessageFile.DetailRow DetailRow = null;

                    bool bNewRow = true;

                    if (DetailTable.Count > 0)  // At least one entry exists.
                    {
                        DetailRow = DetailTable[0];  // CONVENTION: Use the first entry.
                        bNewRow = false;
                    }
                    else    // Create a new entry.
                        DetailRow = DetailTable.AddDetailRow(Guid, null, null, null, null, null, null, null, null, 0);

                    //DetailRow.Guid = Guid;    // Read Only Exception if set as a Read Only field.
                    DetailRow.LastCreatedUrl = Url;
                    DetailRow.LastCreatedDateUtc = GetUtc();
                    DetailRow.LastCreatedRemoteAddress = RemoteAddress;
                    if (ExceptionMessage != null)
                        DetailRow.LastCreatedExceptionMessage = ExceptionMessage;
                    DetailRow.Status = StatusUnRead;
                    DetailRow.Name = Name;
                    DetailRow.EMail = EMail;
                    DetailRow.Message = Message;

                    DetailRow.UpdateCount++;

                    ReturnStatus = WriteFile(FileStream, FileSet);

                    if (ReturnStatus == StatusOk)
                        ReturnStatus = StatusCreated;
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
#endif

        // [Attempt to] Read an entry from a Message file.
        internal static DataSet Read(string FilePath, string Guid, out string Status)
        {
            DataSet FileSet = null;

            Status = StatusOk;

            Stream FileStream = null;

            try
            {
                // TODO: Add more Exception Handling.

                bool bContinue = true;

                if (!VerifyGuid(Guid))
                {
                    Status = StatusInvalidGuid;
                    bContinue = false;
                }

                if (bContinue)
                {
                    string GuidPath = MakeFilePath(FilePath, Guid);
                    string OpenStatus = null;

                    FileStream = OpenFileIfExists(GuidPath, out OpenStatus);

                    if (FileStream == null)
                    {
                        Status = OpenStatus;
                        bContinue = false;
                    }
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

#if false
        // Test Method to see how typed DataSets work.
        internal static void TestMethod(string FilePath)
        {
            string Guid = null;

            string GuidPath = null;

            string OpenStatus = null;

            Stream FileStream = null;

#if true
            // Create Old, Read New.
            try
            {
                Guid = System.Guid.NewGuid().ToString("N");
                Create(FilePath, Guid, "Create Old", null, "Create Old", "Create Old", "Create Old", "Create Old.");

                GuidPath = MakeFilePath(FilePath, Guid);
                FileStream = OpenFileAlways(GuidPath, out OpenStatus);

                MessageFile FileSet = new MessageFile();

                ReadFile(FileStream, FileSet);

                MessageFile.HeaderRow HeaderRow = FileSet.Header[0];

                string Version = HeaderRow.Version;
                int UpdateCount = HeaderRow.UpdateCount;
                HeaderRow.UpdateCount++;

                MessageFile.DetailRow MessageRow = FileSet.Detail[0];

                string ReadGuid = MessageRow.Guid;
                string ReadName = MessageRow.Name;
                MessageRow.UpdateCount++;
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }
#endif

#if true
            // Create New, Read Old.
            try
            {
                Guid = System.Guid.NewGuid().ToString("N");
                CreateNew(FilePath, Guid, "Create New", null, "Create New", "Create New", "Create New", "Create New.");

                GuidPath = MakeFilePath(FilePath, Guid);
                FileStream = OpenFileAlways(GuidPath, out OpenStatus);

                DataSet FileSet = new DataSet();

                FileSet = CreateFileSet();

                ReadFile(FileStream, FileSet);

                DataRow HeaderRow = FileSet.Tables["Header"].Rows[0];

                string Version = (string)HeaderRow["Version"];
                int UpdateCount = (int)HeaderRow["UpdateCount"];

                DataRow MessageRow = FileSet.Tables["Detail"].Rows[0];

                string ReadGuid = (string)MessageRow["Guid"];
                string ReadName = (string)MessageRow["Name"];
                UpdateCount = (int)MessageRow["UpdateCount"];
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }
#endif

#if true
            // Create New, Read New.
            try
            {
                Guid = System.Guid.NewGuid().ToString("N");
                CreateNew(FilePath, Guid, "Create New", "Example Exception Message.", "Create New", "Create New", "Create New", "Create New.");

                GuidPath = MakeFilePath(FilePath, Guid);
                FileStream = OpenFileAlways(GuidPath, out OpenStatus);

                MessageFile FileSet = new MessageFile();

                ReadFile(FileStream, FileSet);

                MessageFile.HeaderRow HeaderRow = FileSet.Header[0];

                string Version = HeaderRow.Version;
                int UpdateCount = HeaderRow.UpdateCount;
                HeaderRow.UpdateCount++;

                MessageFile.DetailRow MessageRow = FileSet.Detail[0];

                string ReadGuid = MessageRow.Guid;
                string ReadName = MessageRow.Name;
                MessageRow.UpdateCount++;
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }
#endif

#if true
            // Create New, Read Table.
            try
            {
                Guid = System.Guid.NewGuid().ToString("N");
                CreateNew(FilePath, Guid, "Create New", "Example Exception Message.", "Create New", "Create New", "Create New", "Create New.");

                GuidPath = MakeFilePath(FilePath, Guid);
                FileStream = OpenFileAlways(GuidPath, out OpenStatus);

                DataTable HeaderTable = CreateHeaderTable();

                HeaderTable.Namespace = "https://www.Eadent.com/DataAccess.Xml/MessageFile.xsd";

                HeaderTable.ReadXml(FileStream);

                DataRow HeaderRow = HeaderTable.Rows[0];

                string Version = (string)HeaderRow["Version"];
                int UpdateCount = (int)HeaderRow["UpdateCount"];
            }
            finally
            {
                if (FileStream != null)
                    FileStream.Close();
            }
#endif

            // TODO: Find out more about tempuri.org and xsd.

            // TODO: I think I might not be able to read this if I change the Namespace and Prefix manually.
            //FileSet.Namespace = "ead";
            //FileSet.Prefix = "ead";   // TODO: Review the main tag and the namespace attribute when this is used (<ead:Contact xmlns:ead="ead">).
#if false
            FileSet.Header.AddHeaderRow("1", "2", "3", "4");
            FileSet.Detail.AddDetailRow("Blah  Blah Blah Guid");

            FileSet.WriteXml(FilePath);

            MessageFile ReadFileSet = new MessageFile();

            ReadFileSet.ReadXml(FilePath);
            string Version = ReadFileSet.Header[0].Version;
            string Guid = ReadFileSet.Detail[0].Guid;

            Guid = Guid;
#endif
        }
#endif

        internal static void Delme()
        {
            MessageFile MessageFileSet = new MessageFile();

            DataSet FileSet = CreateFileSet();
            CreateDefaultHeader(FileSet.Tables["Header"]);

            MessageFileSet.Merge(FileSet);

            DataTable FileSetHeader = FileSet.Tables["Header"];
            DataTable MessageFileSetHeader = MessageFileSet.Tables["Header"];

            DataRow FRow = FileSetHeader.Rows[0];
            DataRow MRow = MessageFileSetHeader.Rows[0];

            bool bSameObject = false;

            if (object.ReferenceEquals(FRow, MRow))
            {
                bSameObject = true;
            }
            else
            {
                bSameObject = false;
            }
        }
    }
}

//---------------------------------------------------------------------------
//                    End Of $RCSfile: $
//---------------------------------------------------------------------------
