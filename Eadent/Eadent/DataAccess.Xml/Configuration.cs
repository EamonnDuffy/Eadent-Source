//---------------------------------------------------------------------------
// Copyright © 2009-2009 Eamonn Duffy. All Rights Reserved.
//---------------------------------------------------------------------------
//
//  $RCSfile: $
//
// $Revision: $
//
// Created:	Eamonn A. Duffy, 11-Oct-2009.
//
// Purpose:	Provide Configuration for the following namespace:
//
//			EamonnDuffy.DataAccess.Xml
//
//---------------------------------------------------------------------------

using Eadent.Helpers;

namespace Eadent.DataAccess.Xml
{
    internal class Configuration    // TODO: Determine if this can clash with .NET 2.0+'s Configuration class.
    {
        // Attributes.
        // And their default values.
        private static int m_OpenLockedFileBackoffSleep = 15;   // In ms.
        private static int m_OpenLockedFileAbandonTimeout = 10 * 1000;   // In ms.
        private static string m_MessageFilePrefix = "Message - ";

        // Properties.
        internal static int OpenLockedFileBackoffSleep
        {
            get { return m_OpenLockedFileBackoffSleep; }
        }

        internal static int OpenLockedFileAbandonTimeout
        {
            get { return m_OpenLockedFileAbandonTimeout; }
        }

        internal static string MessageFilePrefix
        {
            get { return m_MessageFilePrefix; }
        }

        // Lifetime.
        static Configuration()
        {
            GetSettings();
        }

        // Methods.
        private static void GetSettings()
        {
            // TODO: Consider placing the following in a try/catch block.

            Utility.GetAppSetting("EamonnDuffy.DataAccess.Xml.OpenLockedFile.BackoffSleep",      ref m_OpenLockedFileBackoffSleep);
            Utility.GetAppSetting("EamonnDuffy.DataAccess.Xml.OpenLockedFile.AbandonTimeout",    ref m_OpenLockedFileAbandonTimeout);
            Utility.GetAppSetting("EamonnDuffy.DataAccess.Xml.Message.FilePrefix",               ref m_MessageFilePrefix);
        }
    }
}

//---------------------------------------------------------------------------
//                    End Of $RCSfile: $
//---------------------------------------------------------------------------
