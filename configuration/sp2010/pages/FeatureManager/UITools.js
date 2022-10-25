function ShowProcess() {
    var imgCell = document.getElementById('loaderCell');
    imgCell.style.display = '';
    setTimeout('RotateImg();', 50);
}

function RotateImg() {
    var img = document.getElementById('loader');
    img.src = '/_layouts/images/FeatureManager/ajax-loader.gif';
}

function StyleFeatureList() {
    var dd = document.getElementById('ctl00_PlaceHolderMain_FeatureSection_ctl00_WebFeatureDropDown');
    if (dd != null) {
        for (var i = 0; i < dd.length; i++) {
            var option = dd.options[i];
            var text = option.text;
            var lastPos = text.length - 1;
            var lastCharacter = text.charAt(lastPos);

            if (lastCharacter == '*') {
                option.style.color = '#999999';
                option.text = text.substring(0, text.length - 2);
            }
        }
    }
}

function AreAllTreeViewNodeChecked() {
    var tree = document.getElementById(FeatureTreeClientID);
    var childContainer = tree.firstChild.nextSibling;
    var childChkBoxes = childContainer.getElementsByTagName("input");
    var childChkBoxCount = childChkBoxes.length;
    for (var i = 0; i < childChkBoxCount; i++) {
        if (!childChkBoxes[i].checked) {
            return false;
        }
    }

    return true;

}

var allSelected = false;

function CheckUncheckTree() {
    //init state
    allSelected = AreAllTreeViewNodeChecked();

    var tree = document.getElementById(FeatureTreeClientID);
    if (tree != null) {
        allSelected = !allSelected;
        var firstTable = tree.firstChild.nextSibling;
        CheckUncheckChildren(firstTable, allSelected);
    }
}
