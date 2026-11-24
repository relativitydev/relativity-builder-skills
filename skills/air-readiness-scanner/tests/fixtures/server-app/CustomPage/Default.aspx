<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Contoso.Cleanup.CustomPage.Default" %>
<!DOCTYPE html>
<html>
<head>
    <title>Cleanup status</title>
    <script src="/Relativity/Scripts/jquery-3.5.1.min.js"></script>
    <script src="../../Javascript/angular.min.js"></script>
    <script src="lib/knockout-3.5.1.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div id="status"></div>
    </form>
    <script>
        var kepler = GetKeplerApplicationPath();
        RelativityAjax(kepler + "/Relativity.REST/api/contoso/status", "GET", null, function (r) {
            document.getElementById("status").innerText = r;
        });
        window.top.location.hash = "#cleanup";
    </script>
</body>
</html>
