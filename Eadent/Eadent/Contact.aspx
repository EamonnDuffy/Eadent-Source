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
    </div>

</asp:Content>
