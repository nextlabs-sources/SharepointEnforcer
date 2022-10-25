$ver = $host | select version
if ($ver.Version.Major -gt 1)  {$Host.Runspace.ThreadOptions = "ReuseThread"}
Add-PsSnapin Microsoft.SharePoint.PowerShell
Set-location $home

Uninstall-SPFeature -Identity ddf3439c-65aa-443b-8973-b87b003c0254 -CompatibilityLevel 14 -Confirm:$false