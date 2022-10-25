#ifndef SETUP_HEADER 
#define SETUP_HEADER    

    /*
	 *  Validate Administration Password
	 */
	export prototype INT ValidateAdministrationPassword(HWND); 
	export prototype INT BackupFiles(HWND);
	export prototype INT RestoreFiles(HWND);
	export prototype INT CheckUninstall(HWND);
	
	prototype BlankLeadingZeros(BYREF STRING);
	export prototype CheckFirstOrUpgrade(HWND);
	export prototype BakFiles(HWND);
	export prototype BOOL IISReset(HWND);
#endif
