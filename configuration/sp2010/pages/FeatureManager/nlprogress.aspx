<%@ Assembly Name="NextLabs.Deployment, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e03e4c7ee29d89ce" %>
<%@ Page Language="C#" AutoEventWireup="true"  Inherits="NextLabs.Deployment.nlprogress" %>
<style type="text/css">
    body
    {
        font-size: 11px;
        background-color: #f1f1f2;
        margin:0px;
        padding:0px;
    }
    #n
    {
        margin: 10px auto;
        width: 920px;
        border: 1px solid #CCC;
        font-size: 14px;
        line-height: 30px;
    }
    #n a
    {
        padding: 0 4px;
        color: #333;
    }
    .Bar
    {
        position: relative;
        width: 200px;
        border: 1px solid #B1D632;
        padding: 1px;
    }
    .Bar div
    {
        display: block;
        position: relative;
        background-color: Orange;
        color: #333333;
        height: 20px;
        line-height: 20px;
    }
    .Bars div
    {
        background: #090;
    }
    .Bar div span
    {
        position: absolute;
        width: 200px;
        text-align: center;
        font-weight: bold;
    }
    .cent
    {
        margin: 0 auto;
        width: 330px;
        word-break:break-all;
    }
</style>
<div class="cent">
    <div class="Bar" id="Bar" style="">
        <div id="processBar" style="width: 0%;">
            <span id="barNum">0%</span>
        </div>
    </div>
    <div>
        <span id="processStr" style="font-family:Verdana"><label id="labProgressStr">wait for loading</label></span>
    </div>
</div>
<script type="text/javascript">
    function getQueryStringByName(name) {
        var result = location.search.match(new RegExp("[\?\&]" + name + "=([^\&]+)", "i"));
        if (result == null || result.length < 1) {
            return "";
        }
        return result[1];

    }
    function setBarValue(num) {
        if (num == "" || num == null) {
            num = 0;
        }
        document.getElementById("barNum").innerHTML = num + "%";
        document.getElementById("processBar").style.width = num + "%";
    }
    function setBarStr(str) {
        if (str == "" || str == null) {
            str = "";
        }
        document.getElementById("processStr").innerHTML = str;
    }
    function loadAJAX(webAppName) {
        var xmlhttp;
        if (window.XMLHttpRequest) {// code for IE7+, Firefox, Chrome, Opera, Safari
            xmlhttp = new XMLHttpRequest();
        }
        else {// code for IE6, IE5
            xmlhttp = new ActiveXObject("Microsoft.XMLHTTP");
        }
        xmlhttp.onreadystatechange = function () {
            if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
                if (xmlhttp.responseText.split("|").length >= 2) {
                    var num = xmlhttp.responseText.split("|")[0];
                    var str = xmlhttp.responseText.split("|")[1]
                    setBarValue(num);
                    setBarStr(str);
                    if (num == "100" || num == "0") {
                        document.getElementById("Bar").style.visibility = "hidden";
                        document.getElementById("processStr").innerHTML = document.getElementById("processStr").innerHTML.link("FeatureStatus.aspx?AllLogWebAppName=" + webAppName + "&&t=" + Math.random());
                    }
                    else {
                        document.getElementById("Bar").style.visibility = "visible";
                    }
                }
            }
        }
        xmlhttp.open("GET", "FeatureStatus.aspx?webAppName=" + webAppName + "&&t=" + Math.random(), true);
        xmlhttp.send();


    }

    var webAppName = getQueryStringByName("webAppName");
    //loadAJAX(webAppName);
    var num = getQueryStringByName("barValue");
    var str = getQueryStringByName("barStr");
    setBarStr(str);
    setBarValue(num);
    if (num == "100" || num == "0") {
        document.getElementById("Bar").style.visibility = "hidden";
    }
    else {
        document.getElementById("Bar").style.visibility = "visible";

    }
    var t1 = setInterval('loadAJAX(webAppName)', 3000);
    
</script>
