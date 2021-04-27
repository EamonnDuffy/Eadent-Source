using Eadent.Helpers;
using System;
using System.Web.UI;

namespace Eadent
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Utility Utility = new Utility();

            LabelVersion.Text = Utility.GetVersion();
            LabelCopyright.Text = Utility.FormatCopyright(LabelCopyright.Text);
        }
    }
}
