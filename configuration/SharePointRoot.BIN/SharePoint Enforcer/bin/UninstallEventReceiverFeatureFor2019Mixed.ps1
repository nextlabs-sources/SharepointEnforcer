$ver = $host | select version
if ($ver.Version.Major -gt 1)  {$Host.Runspace.ThreadOptions = "ReuseThread"}
Add-PsSnapin Microsoft.SharePoint.PowerShell
Set-location $home

Uninstall-SPFeature -Identity 4f6fd05e-b392-418b-9dbf-b0fb92f12271 -CompatibilityLevel 14 -Confirm:$false