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

                                    string htmlBody =
                                        "Name: " + TextName.Text + "<br>" +
                                        "E-Mail: " + TextEMailAddress.Text + "<br>" +
                                        "Date (UTC): " + Utility.GetDate() + "<br>" +
                                        "Time (UTC): " + Utility.GetTime() + "<br>" +
                                        "Domain: " + AssemblyInfo.Domain + "<br>" +
                                        "Url: " + Url + "<br>" +
                                        "Guid: " + Guid + "<br>" +
                                        "Message:<br><br>" +
                                        TextMessage.Text.Replace("\n", "<br>");

                                    EMail.Send("Eadent Web Site", "From.Web.Site@Eadent.com", AssemblyInfo.Domain + ": Someone has sent a message from the Eadent Web Site.", htmlBody);

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
