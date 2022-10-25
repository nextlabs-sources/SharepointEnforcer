function OnTreeClick(evt, doubleClick) {
    shiftPressed = false;

    if (event.shiftKey == 1) {
        shiftPressed = true;
    }

    if (doubleClick || shiftPressed) {
        var src = window.event != window.undefined ? window.event.srcElement : evt.target;
        var isChkBoxClick = (src.tagName.toLowerCase() == "input" && src.type == "checkbox");
        if (isChkBoxClick) {
            var parentTable = GetParentByTagName("table", src);
            var nxtSibling = parentTable.nextSibling;
            if (nxtSibling && nxtSibling.nodeType == 1)
            {
                if (nxtSibling.tagName.toLowerCase() == "div")
                {
                    CheckUncheckChildren(parentTable.nextSibling, src.checked);
                }
            }
        }
    }
}

function CheckUncheckChildren(childContainer, check) {
    var childChkBoxes = childContainer.getElementsByTagName("input");
    var childChkBoxCount = childChkBoxes.length;
    for (var i = 0; i < childChkBoxCount; i++) {
        childChkBoxes[i].checked = check;
    }
}

function CheckUncheckParents(srcChild, check) {
    var parentDiv = GetParentByTagName("div", srcChild);
    var parentNodeTable = parentDiv.previousSibling;

    if (parentNodeTable) {
        var checkUncheckSwitch;

        if (check) //checkbox checked
        {
            var isAllSiblingsChecked = AreAllSiblingsChecked(srcChild);
            if (isAllSiblingsChecked)
                checkUncheckSwitch = true;
            else
                return; //do not need to check parent if any child is not checked
        }
        else //checkbox unchecked
        {
            checkUncheckSwitch = false;
        }

        var inpElemsInParentTable = parentNodeTable.getElementsByTagName("input");
        if (inpElemsInParentTable.length > 0) {
            var parentNodeChkBox = inpElemsInParentTable[0];
            parentNodeChkBox.checked = checkUncheckSwitch;
            //do the same recursively
            CheckUncheckParents(parentNodeChkBox, checkUncheckSwitch);
        }
    }
}

function AreAllSiblingsChecked(chkBox) {
    var parentDiv = GetParentByTagName("div", chkBox);
    var childCount = parentDiv.childNodes.length;
    for (var i = 0; i < childCount; i++) {
        if (parentDiv.childNodes[i].nodeType == 1) //check if the child node is an element node
        {
            if (parentDiv.childNodes[i].tagName.toLowerCase() == "table") {
                var prevChkBox = parentDiv.childNodes[i].getElementsByTagName("input")[0];
                //if any of sibling nodes are not checked, return false
                if (!prevChkBox.checked) {
                    return false;
                }
            }
        }
    }
    return true;
}

//utility function to get the container of an element by tagname
function GetParentByTagName(parentTagName, childElementObj) {
    var parent = childElementObj.parentNode;
    while (parent.tagName.toLowerCase() != parentTagName.toLowerCase()) {
        parent = parent.parentNode;
    }
    return parent;
}

var treeExpanded = false;

function TreeviewExpandCollapseAll(treeViewId, expandAll) {
    var displayState = (expandAll == true ? "none" : "block");
    var treeView = document.getElementById(treeViewId);
    if (treeView) {
        var treeLinks = treeView.getElementsByTagName("a");
        var nodeCount = treeLinks.length;
        var flag = true;
        for (i = 0; i < nodeCount; i++) {
            if (treeLinks[i].firstChild.tagName) {
                if (treeLinks[i].firstChild.tagName.toLowerCase() == "img") {
                    var node = treeLinks[i];
                    
                    var tempID = node.id.substr(node.id.indexOf('FeatureTree') + 11);
                    tempID = tempID.substr(1);
                    var indexOfI = tempID.indexOf('i');
                    if (indexOfI != -1) {
                        tempID = tempID.substr(0, tempID.length - 1);
                    }
                    var level = parseInt(tempID, 10);
                    
                    var childContainer = GetParentByTagName("table", node).nextSibling;
                    if (childContainer) {
                        if (flag) {
                            if (childContainer.style.display == displayState) {
                                TreeView_ToggleNode(eval(treeViewId + "_Data"), level, node, 'r', childContainer);
                            }
                            flag = false;
                        }
                        else {
                            if (childContainer.style.display == displayState) {
                                TreeView_ToggleNode(eval(treeViewId + "_Data"), level, node, 'l', childContainer);
                            }
                        }
                    }
                }
            }
        }
    }
}

function ToggleExpandCollapse(treeViewId) {
    if (treeExpanded) TreeviewExpandCollapseAll(treeViewId, false);
    else TreeviewExpandCollapseAll(treeViewId, true);
    treeExpanded = !treeExpanded;
}