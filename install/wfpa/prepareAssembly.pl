#!/usr/bin/perl
#
# DESCRIPTION
# This script prepare an installer assembly directory for building an installer.
#
# IMPORTANT:
#	1. "Script Files" folder must be in the same directory as *.ism file. Otherwise, you will get
#		an error message like this:
#			ISDEV : error -7132: An error occurred streaming ISSetup.dll support file 
#			c:\nightly\current\D_SiriusR2\install\spe\sharepoint-enforcer-5.5.0.0-10001-dev-20110321063014\
#			sharepoint-enforcer-5.5.0.0-10001-dev-20110321063014\Script Files\Setup.inx
#		And you will not see these messages at the beginning:
#			Compiling...
#			Setup.rul
#			c:\nightly\current\D_SiriusR2\install\spe\sharepoint-enforcer-5.5.0.0-10001-dev-20110321063014\script files\Setup.rul(90) 
#				: warning W7503: 'ProcessEnd' : function defined but never called
#			c:\nightly\current\D_SiriusR2\install\spe\sharepoint-enforcer-5.5.0.0-10001-dev-20110321063014\script files\Setup.rul(90) 
#				: warning W7503: 'ProcessRunning' : function defined but never called
#			Linking...
#			Setup.inx - 0 error(s), 2 warning(s)
#			ISDEV : warning -4371: There were warnings compiling InstallScript


BEGIN { push @INC, "../scripts"}

use strict;
use warnings;

use Getopt::Long;
use File::Copy::Recursive qw(dircopy);
use File::Path qw(make_path remove_tree);
use InstallHelper;


print "NextLabs Installer Assembly Preparation Script\n";


#
# Global variables
#

my	$noCopy = 0;
my	$buildType = "";
my	$buildNum = "";
my	$productName = "";
my	$msiFileBaseName32 = "";
my	$msiFileBaseName64 = "";
my	$ismTemplateFileName = "";
my	$versionStr = "";
my	$majorVer = 0;
my	$minorVer = 0;
my	$maintenanceVer = 0;
my	$patchVer = 0;


#
# Process parameters
#

# -----------------------------------------------------------------------------
# Print usage

sub printUsage
{
	print "usage: prepareAssembly.pl --buildType=<type> --buildNum=<#> --version=<string>\n";
	print "         --product=<name> --msiFileBaseName32=<file> --msiFileBaseName64-<file>\n";
	print "         --template=<file> [--noCopy]\n";
	print "  buildNum          A build number. Can be any numerical or string value.\n";
	print "  buildType         Specify a build type (e.g., release, pcv, nightly or dev)\n";
	print "  noCopy            Skip copying files, only generate .ism file.\n";
	print "  msiFileBaseName32 Output 32-bit installer file name.\n";
	print "  msiFileBaseName64 Output 64-bit installer file name.\n";
	print "  product           Product name to use in installer.\n";
	print "  template          Name of an InstallShield build script (.ism file).\n";
	print "  version           Version string of format major.minor.maintenance.patch (e.g., 5.5.1.0)\n";
	print "\nEnvironment Variables:\n";
	print "  NLBUILDROOT       Source tree root (e.g., c:/nightly/current/D_SiriusR2).\n";
	print "  NLEXTERNALDIR     External libraries root (e.g., c:/nightly/external).\n";
	print "  NLEXTERNALDIR2    Main_External libraries root (e.g., c:/nightly/main_external).\n";
}

# -----------------------------------------------------------------------------
# Parse command line arguments

sub parseCommandLine()
{
	#
	# Parse arguments
	#
	
	# GetOptions() key specification:
	#	option			Given as --option of not at all (value set to 0 or 1)
	#	option!			May be given as --option or --nooption (value set to 0 or 1)
	#	option=s		Mandatory string parameter: --option=somestring
	#	option:s		Optional string parameter: --option or --option=somestring	
	#	option=i		Mandatory integer parameter: --option=35
	#	option:i		Optional integer parameter: --option or --option=35	
	#	option=f		Mandatory floating point parameter: --option=3.14
	#	option:f		Optional floating point parameter: --option or --option=3.14	

	my	$help = 0;
		
	if (!GetOptions(
			'buildNum=s' => \$buildNum,						# --buildNum
			'buildType=s' => \$buildType,					# --buildType
			'help' => \$help,								# --help
			'msiFileBaseName32=s' => \$msiFileBaseName32,	# --msiFileBaseName32
			'msiFileBaseName64=s' => \$msiFileBaseName64,	# --msiFileBaseName64
			'noCopy' => \$noCopy,							# --noCopy
			'product=s' => \$productName,					# --product
			'template=s' => \$ismTemplateFileName,			# --template
			'version=s' => \$versionStr						# --version
		))
	{
		exit(1);
	}

	#
	# Help
	#
	
	if ($help == 1)
	{
		&printHelp();
		exit;
	}

	#
	# Check for errors
	#
	
	if ($buildType eq '')
	{
		print "Missing build type\n";
		exit(1);
	}

	if ($buildType ne "release" && $buildType ne "pcv" && $buildType ne "nightly" && $buildType ne "dev")
	{
		print "Invalid build type $buildType (expected release, pcv, nightly or dev)\n";
		exit(1);
	}
	
	if ($buildNum eq '')
	{
		print "Missing build number\n";
		exit(1);
	}

	if ($msiFileBaseName32 eq '')
	{
		print "Missing 32-bit MSI file name\n";
		exit(1);
	}	

	if ($msiFileBaseName64 eq '')
	{
		print "Missing 64-bit MSI file base name\n";
		exit(1);
	}	

	if ($productName eq '')
	{
		print "Missing product name\n";
		exit(1);
	}	

	if ($ismTemplateFileName eq '')
	{
		print "Missing ISM template file name\n";
		exit(1);
	}	
	
	if ($versionStr eq '')
	{
		print "Missing version string\n";
		exit(1);
	}
	
	if ($versionStr !~ /^(\d+)\.(\d+)\.(\d+)\.(\d+)$/)
	{
		print "Invalid verison string (expects format 5.5.0.1)\n";
		exit(1);
	}
	
	$majorVer = $1;
	$minorVer = $2;
	$maintenanceVer = $3;
	$patchVer = $4;
		
	if ($majorVer < 1 || $majorVer > 100)
	{
		print "Invalid major verison # (expects 1-100)\n";
		exit(1);
	}
	
	if ($minorVer < 0 || $minorVer > 100)
	{
		print "Invalid minor verison # (expects 1-100)\n";
		exit(1);
	}
	
	if ($maintenanceVer < 0 || $maintenanceVer > 100)
	{
		print "Invalid maintenance verison # (expects 1-100)\n";
		exit(1);
	}
	
	if ($patchVer < 0 || $patchVer > 1000)
	{
		print "Invalid patch verison # (expects 1-1000)\n";
		exit(1);
	}
}

my	$argCount = scalar(@ARGV);

if ($argCount < 2 || $ARGV[0] eq "-h" || $ARGV[0] eq "--help")
{
	printUsage;
	exit 1;
}

&parseCommandLine();

# Print parameters
print "Parameters:\n";
print "  No Copy                     = $noCopy\n";
print "  Build Type                  = $buildType\n";
print "  Build #                     = $buildNum\n";
print "  Version String              = $versionStr\n";
print "  Major                       = $majorVer\n";
print "  Minor                       = $minorVer\n";
print "  Maintenance                 = $maintenanceVer\n";
print "  Patch                       = $patchVer\n";
print "  Product Name                = $productName\n";
print "  MSI File Base Name (32-bit) = $msiFileBaseName32\n";
print "  MSI File Base Name (64-bit) = $msiFileBaseName64\n";
print "  Template File Name          = $ismTemplateFileName\n";


#
# Environment
#

my	$buildRootDir = $ENV{NLBUILDROOT};
my	$externalDir = $ENV{NLEXTERNALDIR};
my	$mainexternalDir = $ENV{NLEXTERNALDIR2};
my	$buildRootPath = $buildRootDir;

$buildRootPath =~ s/:$/:\//;

if (! defined $buildRootDir || $buildRootDir eq "")
{
	die "### ERROR: Environment variable NLBUILDROOT is missing.\n";
}

if (! defined $externalDir || $externalDir eq "")
{
	die "### ERROR: Environment variable NLEXTERNALDIR is missing.\n";
}

if (! defined $mainexternalDir || $mainexternalDir eq "")
{
	die "### ERROR: Environment variable NLEXTERNALDIR2 is missing.\n";
}

if (! -d $buildRootPath)
{
	die "### ERROR: $buildRootPath (i.e., NLBUILDROOT) does not exist.\n";
}

if (! -d $externalDir)
{
	die "### ERROR: $externalDir (i.e., NLEXTERNALDIR) does not exist.\n";
}

if (! -d $mainexternalDir)
{
	die "### ERROR: $mainexternalDir (i.e., NLEXTERNALDIR2) does not exist.\n";
}

# Print environment
print "Environment Variables:\n";
print "  NLBUILDROOT     = $buildRootDir\n";
print "  NLEXTERNALDIR   = $externalDir\n";
print "  NLEXTERNALDIR2  = $mainexternalDir\n";


#
# Skip copy files
#

my	$installDir = "$buildRootDir/install/wfpa";
my	$assemblyDir = "$installDir/build/data";

if ($noCopy)
{
	goto LBL_END_OF_COPY;
}


#
# Prepare assembly directory
#

print "INFO: Preparing assembly directory\n";

if (-d $assemblyDir)
{
	InstallHelper::removeAssemblyDirectoryContent($assemblyDir);
}
else
{
	InstallHelper::createAssemblyDirectory($assemblyDir);
}


#
# Copy build binaries
#
# Notes: The following includes binaries built by 64-bit Compliant Enterprise Policy Control 5.1.

print "INFO: Copying build binaries\n";

my	$srcBinCsDir = "$buildRootDir/bin/release_dotnet";
my	$srcBin32Dir = "$buildRootDir/bin/release_win_x86";
my	$srcBin64Dir = "$buildRootDir/bin/release_win_x64";
my	$xlibBinCsDir = "$buildRootDir/xlib/release_dotnet";
my	$xlibBin32Dir = "$buildRootDir/xlib/release_win_x86";
my	$xlibBin64Dir = "$buildRootDir/xlib/release_win_x64";
my	$destBinCsDir = "$assemblyDir/binCs";
my	$destBin32Dir = "$assemblyDir/bin32";
my	$destBin64Dir = "$assemblyDir/bin64";
my	$releaseOnly = 0;
my	$baseVersionStr = "5.6.0.0";

my	$speVersionStr = "5.6.0.0";		# POON: TBF: temp override

if ($buildType eq "release")
{
	$releaseOnly = 1;
}

my	@xlibCsFileList = (
#		["NextLabs.CSCInvoke.dll",				$baseVersionStr]
	);	
my	@xlib32FileList = (
		["nlQuench.exe",						$baseVersionStr, "x86", $releaseOnly],
		["PluginInstallerSDK32.dll",			$baseVersionStr, "x86", $releaseOnly]
	);
my	@xlib64FileList = (
		["nlQuench.exe",						$baseVersionStr, "x64", $releaseOnly],
		["PluginInstallerSDK.dll",				$baseVersionStr, "x64", $releaseOnly]
	);
my	@binCsFileList = (
		["NextLabs.CSCInvoke.dll",				$versionStr],		# POON: TBF: Should get this from xlib
		["WorkflowAdapter.exe",					$versionStr]
	);	
my	@bin32FileList = (
		["wfRetry32.dll",						$versionStr, "x86", $releaseOnly]
	);
my	@bin64FileList = (
		["wfRetry.dll",							$versionStr, "x64", $releaseOnly]
	);

InstallHelper::copyListVersionRequired($xlibBinCsDir, $destBinCsDir, \@xlibCsFileList);
InstallHelper::copyListVersionRequired($srcBinCsDir, $destBinCsDir, \@binCsFileList);
InstallHelper::copyListVersionArchitectureAndReleaseRequired($xlibBin32Dir, $destBin32Dir, \@xlib32FileList);
InstallHelper::copyListVersionArchitectureAndReleaseRequired($xlibBin64Dir, $destBin64Dir, \@xlib64FileList);
InstallHelper::copyListVersionArchitectureAndReleaseRequired($srcBin32Dir, $destBin32Dir, \@bin32FileList);
InstallHelper::copyListVersionArchitectureAndReleaseRequired($srcBin64Dir, $destBin64Dir, \@bin64FileList);


#
# Copy external libraries
#

print "INFO: Copying external libraries\n";

my	$srcMsRedistX86Dir = "$externalDir/microsoft/redist/x86";
my	$srcMsRedistX64Dir = "$externalDir/microsoft/redist/amd64";

# 32-bit binaries
InstallHelper::copyVersionRequired("$srcMsRedistX86Dir/Microsoft.VC90.ATL/atl90.dll",		"$destBin32Dir/atl90.dll",		"9.0.21022.8");
InstallHelper::copyVersionRequired("$srcMsRedistX86Dir/Microsoft.VC90.CRT/msvcm90.dll",		"$destBin32Dir/msvcm90.dll",	"9.0.21022.8");
InstallHelper::copyVersionRequired("$srcMsRedistX86Dir/Microsoft.VC90.CRT/msvcp90.dll",		"$destBin32Dir/msvcp90.dll",	"9.0.21022.8");
InstallHelper::copyVersionRequired("$srcMsRedistX86Dir/Microsoft.VC90.CRT/msvcr90.dll",		"$destBin32Dir/msvcr90.dll",	"9.0.21022.8");

InstallHelper::copyRequired("$srcMsRedistX86Dir/Microsoft.VC90.ATL/Microsoft.VC90.ATL.manifest",		"$destBin32Dir/Microsoft.VC90.ATL.manifest");
InstallHelper::copyRequired("$srcMsRedistX86Dir/Microsoft.VC90.CRT/Microsoft.VC90.CRT.manifest",		"$destBin32Dir/Microsoft.VC90.CRT.manifest");

# 64-bit binaries
InstallHelper::copyVersionRequired("$srcMsRedistX64Dir/Microsoft.VC90.ATL/atl90.dll",		"$destBin64Dir/atl90.dll",		"9.0.21022.8");
InstallHelper::copyVersionRequired("$srcMsRedistX64Dir/Microsoft.VC90.CRT/msvcm90.dll",		"$destBin64Dir/msvcm90.dll",	"9.0.21022.8");
InstallHelper::copyVersionRequired("$srcMsRedistX64Dir/Microsoft.VC90.CRT/msvcp90.dll",		"$destBin64Dir/msvcp90.dll",	"9.0.21022.8");
InstallHelper::copyVersionRequired("$srcMsRedistX64Dir/Microsoft.VC90.CRT/msvcr90.dll",		"$destBin64Dir/msvcr90.dll",	"9.0.21022.8");

InstallHelper::copyRequired("$srcMsRedistX64Dir/Microsoft.VC90.ATL/Microsoft.VC90.ATL.manifest",		"$destBin64Dir/Microsoft.VC90.ATL.manifest");
InstallHelper::copyRequired("$srcMsRedistX64Dir/Microsoft.VC90.CRT/Microsoft.VC90.CRT.manifest",		"$destBin64Dir/Microsoft.VC90.CRT.manifest");


#
#  Prepare support files
#

print "INFO: Copying support files\n";

my	$srcResDir = "$installDir/resource";
my	$srcSpeDir = "$buildRootDir/prod/pep/spe/sharepointPEP";
my	$srcSpeConfigDir = "$buildRootDir/configuration";
my	$srcSpeConfigWfpaDir = "$srcSpeConfigDir/wfpa";
my	$srcSpeConfigWfRetryDir = "$srcSpeConfigDir/wfRetry";
my	$destResDir = "$assemblyDir/resource";
my	$destConfigDir = "$assemblyDir/configuration";

# Common
InstallHelper::copyRequired("$srcResDir/ReadMe.txt",									"$destResDir/ReadMe.txt");
InstallHelper::copyRequired("$srcResDir/NextLabs Clickwrap Agreement v5-07 (2).rtf",	"$destResDir/NextLabs Clickwrap Agreement v5-07 (2).rtf");
InstallHelper::copyRequired("$srcResDir/ce-32.ico",										"$destResDir/ce-32.ico");

# Update readme file
InstallHelper::updateReadMeFile("$destResDir/ReadMe.txt", $majorVer, $minorVer, $maintenanceVer, $patchVer, $buildNum);

# Configuration
make_path($destConfigDir, {mode => 0777, error => \my $err});

if (system("cd \"$srcSpeConfigWfpaDir\"; cp -f * \"$destConfigDir\""))
{
	die "### ERROR: Failed to copy configuration files from $srcSpeConfigWfpaDir to $destConfigDir.\n";
}

if (system("cd \"$srcSpeConfigWfRetryDir\"; cp -f * \"$destConfigDir\""))
{
	die "### ERROR: Failed to copy configuration files from $srcSpeConfigWfRetryDir to $destConfigDir.\n";
}


#
# Prepare packages
#

my	$package32Dir = "$installDir/build/package32";
my	$package64Dir = "$installDir/build/package64";

#InstallHelper::copyRequired("$srcSpeConfigDir/product.xml",				"$package32Dir/product.xml");
#InstallHelper::copyRequired("$srcSpeConfigDir/product.xml",				"$package64Dir/product.xml");


#
# Prepare InstallShield files
#

print "INFO: Copying InstallShield files\n";

my	$srcScriptDir = "$installDir/Script Files";
my	$destScriptDir = "$assemblyDir/Script Files";

InstallHelper::copyRequired("$installDir/CommonInstallScript.obl",		"$assemblyDir/CommonInstallScript.obl");
InstallHelper::copyRequired("$installDir/PluginInstallerSDK.obl",		"$assemblyDir/PluginInstallerSDK.obl");

#dircopy($srcScriptDir, $destScriptDir) || die "### ERROR: Failed to copy resource from $srcScriptDir to $destScriptDir.\n";
if (system("cp -fR \"$srcScriptDir\" \"$destScriptDir\""))
{
	die "### ERROR: Failed to copy resource from $srcScriptDir to $destScriptDir.\n";
}

LBL_END_OF_COPY:


#
# Modify installer build script
#

print "INFO: modify installer script\n";

my	$templateFile = "$installDir/$ismTemplateFileName";
my	$newIsmFile = "$assemblyDir/$ismTemplateFileName";

InstallHelper::constructIsmFile($templateFile, $newIsmFile, $msiFileBaseName32, $msiFileBaseName64, $productName, $versionStr, $buildNum);
