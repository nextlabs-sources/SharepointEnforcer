Set locator = CreateObject("WbemScripting.SWbemLocator") 
Set Service = locator.connectserver(strServer, "root/MicrosoftIISv2") 
Set APCollection = Service.InstancesOf("IISApplicationPool") 
For Each APInstance In APCollection 
    APInstance.Recycle 
Next 