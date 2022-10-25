$snapin="Microsoft.SharePoint.PowerShell"
if (get-pssnapin $snapin -ea "silentlycontinue") {
  write-host -f Green "PSsnapin $snapin is loaded"
}
elseif (get-pssnapin $snapin -registered -ea "silentlycontinue") {
  write-host -f Green "PSsnapin $snapin is registered"
  Add-PSSnapin $snapin
  write-host -f Green "PSsnapin $snapin is loaded"
}
else {
  write-host -f Red "PSSnapin $snapin not found"
}
# get content web service
$contentService = [Microsoft.SharePoint.Administration.SPWebService]::ContentService
# turn off remote administration security
$contentService.RemoteAdministratorAccessDenied = $false
# update the web service
$contentService.Update()