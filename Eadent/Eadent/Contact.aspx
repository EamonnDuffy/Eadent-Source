<%@ Page Title="Contact" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="Eadent.Contact" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <%--    <h2><%: Title %>.</h2>
    <h3>Your contact page.</h3>
    <address>
        One Microsoft Way<br />
        Redmond, WA 98052-6399<br />
        <abbr title="Phone">P:</abbr>
        425.555.0100
    </address>

    <address>
        <strong>Support:</strong>   <a href="mailto:Support@example.com">Support@example.com</a><br />
        <strong>Marketing:</strong> <a href="mailto:Marketing@example.com">Marketing@example.com</a>
    </address>--%>

    <div>
        <%--			<p>
            All fields are required: <?php echo $Status; ?>
			</p>
            Name:<br />
            <input id='TextName' name='TextName' type='text' maxlength='100' style='width: 700px;' value='<?php echo $TextName; ?>' /><br /><br />
            E-Mail Address:<br />
            <input id='TextEMailAddress' name='TextEMailAddress' type='text' maxlength='100' style='width: 700px;' value='<?php echo $TextEMailAddress; ?>' /><br /><br />
            Message (Max. 5000 characters):<br />
            <textarea id='TextMessage' name='TextMessage' rows='7' cols='85'><?php echo $TextMessage; ?></textarea><br /><br />
            <?php
            if ($UseAccessCode)
            {
            ?>
                Access Code:<br />
                <?php echo $DisplayedAccessCode; ?><br /><br />
                Enter Access Code (without spaces):<br />
                <input id='TextAccessCode' name='TextAccessCode' type='text' maxlength='4' style='width: 80px;' value='<?php echo $TextAccessCode; ?>' /><br /><br />
            <?php
            }
            ?>
            <input id="TextPageState" name="TextPageState" type="hidden" value="<?php echo $TextPageState; ?>" />
            (NOTE: A message may take an hour or more to arrive.)<br /><br />
            <input id='ButtonSubmit' name='ButtonSubmit' type='submit' value='Submit Message'
            <?php
            if ($MessageSubmitted)
                echo "disabled='disabled' ";
            ?>/><br />--%>

        <br />
        All fields are required:
        <asp:Label ID="LabelSubmitResult" runat="server" /><br />
        <br />
        Name:<br />
        <asp:TextBox ID="TextName" runat="server" MaxLength="100" Width="700px"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RFV1" runat="server"
            ErrorMessage="Name field is required." ControlToValidate="TextName">Required</asp:RequiredFieldValidator>
        <br />
        <br />
        E-Mail Address:<br />
        <asp:TextBox ID="TextEMailAddress" runat="server" MaxLength="100" Width="700px"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RFV2" runat="server"
            ErrorMessage="E-Mail Address field is required." ControlToValidate="TextEMailAddress">Required</asp:RequiredFieldValidator>
        <br />
        <br />
        Message (Max. 5000 characters):
    <asp:RequiredFieldValidator ID="RFV3" runat="server"
        ErrorMessage="Message field is required." ControlToValidate="TextMessage">Required</asp:RequiredFieldValidator><asp:RegularExpressionValidator ID="RFV4" runat="server" ErrorMessage="Message has a limit of 5000 characters (including newlines)." ControlToValidate="TextMessage" ValidationExpression="(.|[\r\n]){0,5000}"></asp:RegularExpressionValidator>
        <br />
        <asp:TextBox ID="TextMessage" runat="server" Columns="85" MaxLength="5000"
            Rows="10" TextMode="MultiLine"></asp:TextBox>
        <br />
        <br />
        Access Code:<br />
        <asp:Label ID="LabelAccessCode" runat="server"></asp:Label>
        <br />
        <br />
        Enter Access Code (without spaces):<br />
        <asp:TextBox ID="TextAccessCode" runat="server" MaxLength="4" Width="80px"></asp:TextBox>
        <asp:RequiredFieldValidator ID="RFV5" runat="server"
            ErrorMessage="Access Code field is required." ControlToValidate="TextAccessCode">Required</asp:RequiredFieldValidator>
        <br />
        <br />
        (NOTE: A message may take an hour or more to arrive.)
    <br />
        <br />
        <asp:Button ID="ButtonSubmit" runat="server" Text="Submit Message"
            OnClick="ButtonSubmit_Click" />
        <br /><br />

    </div>

</asp:Content>
