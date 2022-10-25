var gRMSLibraryItemAlert = "Are you sure you want to protect the file?";
var gRMSListItemAlert = "Are you sure you want to protect the item attachment(s)?";

function createRMSdialogOptions(nrpUrl){
	var dialogOptions = {
		title: 'NextLabs Rights Protection',
	    url: nrpUrl,
	    allowMaximize: false,
	    showClose: true,
        autoSize: true,
	    dialogReturnValueCallback: dialogCallback
	};	
	return dialogOptions;
}
function dialogCallback(dialogResult, returnValue){
          //  SP.UI.Notify.addNotification(returnValue);
            SP.UI.ModalDialog.RefreshPage(SP.UI.DialogResult.OK);
          }

function getItemIds()
           {
             var itemIds = '';
             var items = SP.ListOperation.Selection.getSelectedItems();
             var item;
             for(var i in items)
             {
               item = items[i];
               if(itemIds != '')
               {
                 itemIds = itemIds + ',';
               }
               itemIds = itemIds + item.id;
             }
             return itemIds;
           }

function checkIsEnabled(){
          var selectedItems = SP.ListOperation.Selection.getSelectedItems();
          var count = selectedItems.length;
          return (count === 1);
        }
function getSelectedListId(){
	var id = "";
	var selectedListId = SP.ListOperation.Selection.getSelectedList();
	var pageListid = _spPageContextInfo.pageListId;
    if(selectedListId != null){
		id = selectedListId;
	}else if(pageListid!=null){
		id = pageListid;
	}
    return id;
}
