using System;
using System.Web.UI;
using Eadent.Helpers;

using ContactData = Eadent.DataAccess.Xml.Contact;

namespace Eadent
{
    public partial class Contact : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string FilePath = MapPath("/App_Data/Xml/Contact.xml");  // TODO: Consider obtaining from Configuration.
            string Guid = null;
            string RemoteAddress = Utility.GetRemoteAddress(Request);

            if (IsPostBack) // Post Back. Let the Submit Button handler deal with this case.
            {
            }
            else    // Normal/Initial Page Load.
            {
                bool bRetry = false;
                int NumTries = 10;  // TODO: Consider getting from configuation.

                string Status = null;

                do
                {
                    Guid = System.Guid.NewGuid().ToString("N");

                    Status = ContactData.Create(FilePath, Guid, RemoteAddress);

                    if (Status != ContactData.StatusCreated)
                    {
                        // Some sort of failure. Retry?
                        NumTries--;
                        if (NumTries <= 0)
                            bRetry = false;
                        else
                            bRetry = true;
                    }
                } while (bRetry);

                if (Status != ContactData.StatusCreated)
                {
                    LabelSubmitResult.ForeColor = System.Drawing.Color.Red;
                    LabelSubmitResult.Text = Status + " Try navigating away and back to this page.";
                    ButtonSubmit.Enabled = false;
                }
                else
                {
                    ViewState.Add("FullAccessCode", Guid);  // ASSUMPTION: ViewStateEncryptionMode="Always".

                    LabelAccessCode.Text =
#if false
                                           Guid + ": " + 
#endif
                                           Guid[28] + "\n" + Guid[29] + "\n" +
                                           Guid[30] + "\n" + Guid[31];
                }
            }
        }

        protected void ButtonSubmit_Click(object sender, EventArgs e)
        {
            bool bSuccess = false;

            string SubmitResult = "There are one or more issues.";

            if (IsValid)
            {
                string FilePath = MapPath("/App_Data/Xml/Contact.xml");  // TODO: Consider obtaining from Configuration.
                string Guid = null;
                string RemoteAddress = Utility.GetRemoteAddress(Request);

                try
                {
                    string PageAccessCode = TextAccessCode.Text;

                    Guid = (string)ViewState["FullAccessCode"];

                    // TODO: Consider a high-level Guid verification.

                    if (Guid != null)
                    {
                        if (PageAccessCode == Guid.Substring(28))
                        {
                            SubmitResult = ContactData.VerifyCanSend(FilePath, Guid, RemoteAddress);

                            if (SubmitResult == ContactData.StatusReadyToSend)
                            {
                                string Url = string.Empty;
                                if (Request.Url != null)
                                    Url = Request.Url.ToString();

                                try
                                {
                                    Utility Utility = new Utility();

                                    // TODO: Consider using an e-mail for the domain to see if message will arrive [faster?].

                                    //MailMessage Message = new MailMessage("From.WebPage@ProtectAddress.biz", "From.WebPage@ProtectAddress.biz");
                                    //Message.Subject = AssemblyInfo.Domain + ": Someone has sent a message from the web.";
                                    //Message.Body =
                                    //    "Name: " + TextName.Text + "\n" +
                                    //    "E-Mail: " + TextEMailAddress.Text + "\n" +
                                    //    "Date (UTC): " + Utility.GetDate() + "\n" +
                                    //    "Time (UTC): " + Utility.GetTime() + "\n" +
                                    //    "Domain: " + AssemblyInfo.Domain + "\n" +
                                    //    "Url: " + Url + "\n" +
                                    //    "Guid: " + Guid + "\n" +
                                    //    "Message:\n\n" +
                                    //    TextMessage.Text;
                                    ////Message.IsBodyHtml = true;    // Use with caution. Need to format Message.Body and/or send a multi-part message.

                                    //SmtpClient Smtp = new SmtpClient();

                                    //NetworkCredential Credentials = new NetworkCredential("From.WebPage@ProtectAddress.biz", "WebMail1");

                                    ////Smtp.EnableSsl = true;
                                    ////Smtp.Port = 465;
                                    ////string SmtpHost = "k2smtpout.secureserver.net"; // 3-Oct-2009. Does not seem to work.

                                    //string SmtpHost = "relay-hosting.secureserver.net";
                                    //if (Request.Url != null)
                                    //{
                                    //    if (Request.Url.ToString().Substring(0, 16) == "http://localhost")
                                    //        SmtpHost = "smtpout.secureserver.net";
                                    //}

                                    //// TODO: Remove the next line.
                                    ////throw new Exception("Pretending not to send.");

                                    //Smtp.Host = SmtpHost;
                                    //Smtp.Port = 25;
                                    //Smtp.UseDefaultCredentials = false;
                                    //Smtp.Credentials = Credentials; // 1-Oct-2009. Does not seem to be required for relay-hosting case.
                                    //Smtp.Send(Message);

                                    SubmitResult = ContactData.UpdateAsSent(FilePath, Guid, RemoteAddress);

                                    if (SubmitResult != ContactData.StatusUpdateOk)
                                    {
                                        // TODO: Determine what to do.
                                        // TODO: Consider having a Debug Label somewhere?
                                    }

                                    bSuccess = true;
                                }
                                catch (Exception Exception)     // Unable to send the message.
                                {
                                    SubmitResult = ContactData.UpdateAsFailedToSend(FilePath, Guid, RemoteAddress, Exception.Message);

                                    if (SubmitResult != ContactData.StatusUpdateOk)
                                    {
                                        // TODO: Determine what to do.
                                        // TODO: Consider having a Debug Label somewhere?
                                    }

                                    FilePath = MapPath("/App_Data/Xml/FailedToSendMessages/");  // TODO: Consider obtaining from Configuration.

                                    // Now [attempt to] keep a copy of the message on the server.
                                    SubmitResult = DataAccess.Xml.Message.Create(FilePath, Guid, RemoteAddress, Exception.Message,
                                        TextName.Text, TextEMailAddress.Text, Url, TextMessage.Text);

                                    if (SubmitResult != DataAccess.Xml.Message.StatusCreated)
                                    {
                                        // TODO: Determine what to do.
                                        // TODO: Consider having a Debug Label somewhere?
                                    }

                                    SubmitResult = Exception.Message;
                                }
                            }
                        }
                        else
                        {
                            RFV5.IsValid = false;
                            RFV5.Text = "Invalid Access Code.";
                        }
                    }
                }
                catch (Exception)
                {
                }

                // TODO: Determine if the following should be reset: ViewState.Add("FullAccessCode", Guid);  // ASSUMPTION: ViewStateEncryptionMode="Always".
            }

            if (bSuccess)
            {
                LabelSubmitResult.ForeColor = System.Drawing.Color.White;
                LabelSubmitResult.BackColor = System.Drawing.Color.Green;
                LabelSubmitResult.Text = "&nbsp;The message has been submitted.&nbsp;";
                ButtonSubmit.Enabled = false;
            }
            else
            {
                LabelSubmitResult.ForeColor = System.Drawing.Color.Red;
                LabelSubmitResult.Text = SubmitResult;
            }
        }
    }
}
