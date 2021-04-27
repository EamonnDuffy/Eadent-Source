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
// Purpose:	Utility methods.
//
//---------------------------------------------------------------------------

using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Eadent.Helpers
{
    internal class Utility
    {
        // Metaclass data.

        // TODO: Look into whether Unicode Encoding might be better for foreign languages, or even accented characters.
        // TODO: Try and understand the difference between Unicode and UTF8 Encoding.

        // NOTE: 9-Oct-2009. For compatibility with other code (JavaScript, Java/Apache's DigestUtils) it is necessary to use a UTF8 and not a Unicode Encoding.
        //                   This may mean that the original UserAuthentication was "non-standard"?
        private static Encoding m_Encoding = Encoding.UTF8;	// TODO: Consider setting programattically or via configuration.

        // Instance data.

        private DateTime m_Utc = DateTime.UtcNow;

        // Instance Properties.

        internal DateTime Utc
        {
            get { return m_Utc; }
        }

        // Instance Helper methods.

        internal static string GetVersion()
        {
            return string.Format("V{0}.{1}.{2}", AssemblyInfo.VersionMajor, AssemblyInfo.VersionMinor, AssemblyInfo.VersionBuild);
        }

        internal string GetDay()
        {
            return m_Utc.ToString("dddd", DateTimeFormatInfo.InvariantInfo);
        }

        internal string GetDate()
        {
            return m_Utc.ToString("d-MMM-yyyy", DateTimeFormatInfo.InvariantInfo);
        }

        internal string GetTime()
        {
            return m_Utc.ToString("h:mm:ss tt", DateTimeFormatInfo.InvariantInfo);
        }

        internal static string GetPersistentDateTime(DateTime Utc)
        {
            return Utc.ToString("yyyy/MM/dd", DateTimeFormatInfo.InvariantInfo) + " " + Utc.ToString("HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);
        }

        internal string GetPersistentDateTime()
        {
            return GetPersistentDateTime(m_Utc);
        }

        internal string FormatCopyright(string Format)
        {
            string Years = AssemblyInfo.CopyrightStartYear.ToString();

            if (m_Utc.Year != AssemblyInfo.CopyrightStartYear)
                Years += "-" + m_Utc.Year;

            return string.Format(Format, Years);
        }

        // Return the string representing the Remote [or Client/Consumer] Address.
        internal static string GetRemoteAddress(HttpRequest Request)
        {
            string RemoteAddress = Request.ServerVariables["REMOTE_ADDR"];

            if (RemoteAddress == null)
                RemoteAddress = "Unknown: REMOTE_ADDR is null.";
            else
            {
                RemoteAddress = RemoteAddress.Trim();

                if (RemoteAddress == string.Empty)
                    RemoteAddress = "Unknown: REMOTE_ADDR is empty.";
            }

            return RemoteAddress;
        }

        // Return the string representing the Local [or Server/Provider] Address.
        internal static string GetLocalAddress(HttpRequest Request)
        {
            string LocalAddress = Request.ServerVariables["LOCAL_ADDR"];

            if (LocalAddress == null)
                LocalAddress = "Unknown: LOCAL_ADDR is null.";
            else
            {
                LocalAddress = LocalAddress.Trim();

                if (LocalAddress == string.Empty)
                    LocalAddress = "Unknown: LOCAL_ADDR is empty.";
            }

            return LocalAddress;
        }

        // Return the string representing the HTTP User Agent.
        internal static string GetUserAgent(HttpRequest Request)
        {
            string UserAgent = Request.ServerVariables["HTTP_USER_AGENT"];

            if (UserAgent == null)
                UserAgent = "Unknown: HTTP_USER_AGENT is null.";
            else
            {
                UserAgent = UserAgent.Trim();

                if (UserAgent == string.Empty)
                    UserAgent = "Unknown: HTTP_USER_AGENT is empty.";
            }

            return UserAgent;
        }

        // Return the number of Files at the specified Root Path, optionally specifying a Search Pattern.
        internal static int GetNumFiles(string RootFilePath, string SearchPattern)
        {
            int NumFiles = 0;

            try
            {
                if (SearchPattern == null)
                    NumFiles = Directory.GetFiles(RootFilePath).Length;
                else
                    NumFiles = Directory.GetFiles(RootFilePath, SearchPattern).Length;
            }
            catch (Exception Exception)
            {
            }

            return NumFiles;
        }

        // TODO: Consider using the Login as part of the hash. One issue would come if the Login was ever changed I guess.

        // Courtesy of http://dotnetrush.blogspot.com/2007/04/c-sha-2-cryptography-sha-256-sha-384.html as of 9-Oct-2009.
        private static string ByteArrayToString(byte[] InputArray)
        {
            StringBuilder Output = new StringBuilder();

            for (int Index = 0; Index < InputArray.Length; Index++)
            {
                Output.Append(InputArray[Index].ToString("X2"));
            }

            return Output.ToString();
        }

        // Delegate to invoke the relevant Hashing algorithm method.
        private delegate string HashMethod(string Text);

        // Return an Upper Case hashed version of the specified Text and Session, using the Session as a guide, and using the specified Hash method.
        private static string GetHash(string Text, string Session, HashMethod HashMethod)
        {
            string Result = null;

            if ((Session != null) && (Session.Length >= 17))
            {
                try
                {
                    char Version = Session[Session.Length - 1];

                    if (Version == '1')
                    {
                        int Offset = int.Parse(Session.Substring(0, 1), NumberStyles.HexNumber);

                        int Algorithm = int.Parse(Session.Substring(Offset, 1), NumberStyles.HexNumber);

                        // TODO: Implement different types of Algorithm.

                        switch (Algorithm & 0x07)
                        {
                            case 0x00:
                            case 0x01:
                            case 0x02:
                            case 0x03:

                                Result = HashMethod(Text + Session);
                                break;

                            case 0x04:

                                Result = HashMethod(Text + "What" + Session);
                                break;

                            case 0x05:

                                Result = HashMethod(Text + "Happens" + Session);
                                break;

                            case 0x06:

                                Result = HashMethod(Text + "Now?" + Session);
                                break;

                            default:

                                Result = HashMethod(Session + Text);
                                break;
                        }
                    }
                }
                catch (Exception Exception)
                {
                    // TODO: Consider implementing Exception Logging.
                }
            }

            return Result;
        }

        // Return an Upper Case SHA-1 hashed version of the specified Text.
        internal static string GetSha1(string Text)
        {
            return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(Text, "SHA1");
        }

        // Return an Upper Case SHA-1 hashed version of the specified Text and Session, using the Session as a guide.
        internal static string GetSha1(string Text, string Session)
        {
#if DotNet_1_1
            return GetHash(Text, Session, new HashMethod(GetSha1));
#else
            return GetHash(Text, Session, GetSha1);
#endif
        }

        // Return an Upper Case SHA-256 hashed version of the specified Text.
        internal static string GetSha256(string Text)
        {
            HashAlgorithm Hasher = HashAlgorithm.Create("SHA-256");

            byte[] TextBytes = m_Encoding.GetBytes(Text);
            byte[] HashedBytes = Hasher.ComputeHash(TextBytes);

            string HashedText = ByteArrayToString(HashedBytes);

            return HashedText;
        }

        // Return an Upper Case SHA-256 hashed version of the specified Text and Session, using the Session as a guide.
        internal static string GetSha256(string Text, string Session)
        {
#if DotNet_1_1
            return GetHash(Text, Session, new HashMethod(GetSha256));
#else
            return GetHash(Text, Session, GetSha256);
#endif
        }

        // Return an Upper Case SHA-512 hashed version of the specified Text.
        internal static string GetSha512(string Text)
        {
            HashAlgorithm Hasher = HashAlgorithm.Create("SHA-512");

            byte[] TextBytes = m_Encoding.GetBytes(Text);
            byte[] HashedBytes = Hasher.ComputeHash(TextBytes);

            string HashedText = ByteArrayToString(HashedBytes);

            return HashedText;
        }

        // Return an Upper Case SHA-512 hashed version of the specified Text and Session, using the Session as a guide.
        internal static string GetSha512(string Text, string Session)
        {
            // TODO: Try and establish why there is a difference between .NET 1.1 and .NET 2.0+ regarding delegate usage.
            // TODO: And perhaps find out which way is "correct"?
#if DotNet_1_1
            return GetHash(Text, Session, new HashMethod(GetSha512));
#else
            return GetHash(Text, Session, GetSha512);
#endif
        }

        // Some .config file AppSettings helpers.

        // [Attempt to] Return a string for the specified AppSettings Key.
        internal static bool GetAppSetting(string Key, ref string Setting)
        {
            bool bSuccess = false;

            string ValueString = GetAppSetting(Key);

            if (ValueString != null)
            {
                Setting = ValueString;
                bSuccess = true;
            }

            return bSuccess;
        }

        // [Attempt to] Return a bool for the specified AppSettings Key.
        internal static bool GetAppSetting(string Key, ref bool bSetting)
        {
            return TryParse(GetAppSetting(Key), out bSetting, bSetting);
        }

        // [Attempt to] Return an int for the specified AppSettings Key.
        internal static bool GetAppSetting(string Key, ref int Setting)
        {
            return TryParse(GetAppSetting(Key), out Setting, Setting);
        }

        // Some .NET 1.1 vs. .NET 2.0+ helpers.

        // [Attempt to] Return an AppSettings Value String for the specified Key.
        internal static string GetAppSetting(string Key)
        {
#if DotNet_1_1
            return ConfigurationSettings.AppSettings[Key];
#else
            // TODO: Investigate WebConfigurationManager.
            return ConfigurationManager.AppSettings[Key];
#endif
        }

        // Attempt to parse the specified Value String into a bool, using the specified value if the parsing fails.
        internal static bool TryParse(string ValueString, out bool bValue, bool bValueIfFails)
        {
            bool bSuccess = false;

            bValue = bValueIfFails;

            if (ValueString != null)
            {
#if DotNet_1_1
                try
                {
                    bValue = bool.Parse(ValueString);
                    bSuccess = true;
                }
                catch
                {
                    bValue = bValueIfFails;
                }
#else
                if (bool.TryParse(ValueString, out bValue))
                    bSuccess = true;
                else
                    bValue = bValueIfFails;
#endif
            }

            return bSuccess;
        }

        // Attempt to parse the specified Value String into an int, using the specified value if the parsing fails.
        internal static bool TryParse(string ValueString, out int Value, int ValueIfFails)
        {
            bool bSuccess = false;

            Value = ValueIfFails;

            if (ValueString != null)
            {
#if DotNet_1_1
                try
                {
                    Value = int.Parse(ValueString);
                    bSuccess = true;
                }
                catch
                {
                    Value = ValueIfFails;
                }
#else
                if (int.TryParse(ValueString, out Value))
                    bSuccess = true;
                else
                    Value = ValueIfFails;
#endif
            }

            return bSuccess;
        }
    }
}

//---------------------------------------------------------------------------
//                    End Of $RCSfile: $
//---------------------------------------------------------------------------
