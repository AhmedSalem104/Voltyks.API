<%@ Page Language="C#" %>
<%@ Import Namespace="System.Runtime.InteropServices" %>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Write("OS: " + RuntimeInformation.OSDescription);
    }
</script>
