#!/usr/bin/perl
#
# DESCRIPTION
# This script contains helper functions to be used to build an installer.

package InstallHelper;

use strict;
use warnings;

use File::Basename;
use File::Path qw(make_path remove_tree);
use File::Copy;
use File::stat;
use Text::Diff;
use Win32;
use Win32API::Resources;


#
# General utilities
#

# -----------------------------------------------------------------------------
# Format date

sub formatDate_YMDHMS
{
	my	($sec, $min, $hour, $mday, $mon, $year, $wday, $yday, $isdst) = gmtime(time);

	return sprintf("%02d%02d%02d%02d%02d%02d", $year + 1900, $mon + 1, $mday, $hour, $min, $sec);
}

# -----------------------------------------------------------------------------
# Remove leading and trailing whitespaces in a string

sub trim
{
	my	$buf = $_[0];

	$buf =~ s/^\s+|\s+$//g;

	return $buf;
}

# -----------------------------------------------------------------------------
# Print array

sub printArray
{
	my	($refArray, $title) = @_;
	my	$count = scalar(@$refArray);

	print "\n$title (total $count):\n";

	foreach (@$refArray)
	{
		print "  $_\n";
	}
}


#
# Prepare assembly structure
#

# -----------------------------------------------------------------------------
# Delete contents in existing assembly directory

sub removeAssemblyDirectoryContent
{
	my	($assemblyDir) = @_;

	print "INFO: Removing existing installer build structure $assemblyDir\n";

	remove_tree($assemblyDir, {keep_root => 1, error => \my $err});

	if (@$err)
	{
		for my $diag (@$err)
		{
			my	($file, $message) = %$diag;

			if ($file eq "")
			{
				print "### ERROR: Failed to remove content of $assemblyDir. Cause: $message.\n";
			}
			else
			{
				print "### ERROR: Failed to remove file or directory $file. Cause: $message.\n";
			}
		}

		die "### ERROR: Failed to remove content of installer assembly directory $assemblyDir.\n";
	}

	print "INFO:   Successfully removed assembly directory $assemblyDir.\n";
}

# -----------------------------------------------------------------------------
# Create assembly directory

sub createAssemblyDirectory
{
	my	($assemblyDir) = @_;

	make_path($assemblyDir, {mode => 0777, error => \my $err});

	if (@$err)
	{
		for my $diag (@$err)
		{
			my	($file, $message) = %$diag;

			if ($file eq "")
			{
				print "### ERROR: Failed to create directory $assemblyDir. Cause: $message.\n";
			}
			else
			{
				print "### ERROR: Failed to create directory $file. Cause: $message.\n";
			}
		}

		die "### ERROR: Failed to create installer assembly directory $assemblyDir.\n";
	}
}


#
# File version information
#

# -----------------------------------------------------------------------------
# Check file version
#
# Notes: if $versionStr == "", skip version checking.

sub fileVersionRequired
{
	my	($file, $versionStr) = @_;

	# Skip file version check
	if ($versionStr eq "")
	{
		print "INFO: Missing file version, not checked.\n";
		return;
	}

	# Get file version #
	my	$fileVer = Win32::GetFileVersion($file);


	if (!$fileVer)
	{
		die "### ERROR: File version not set (expected $versionStr) on $file.\n";
	}

	print "  FILE: Path = $file\n";
	print "  FILE: Version = $fileVer\n";

	# Check file version
	if ($fileVer ne $versionStr)
	{
		print "### WARNING: Wrong file version $fileVer (expected $versionStr) on $file.\n";
		return;

		die "### ERROR: Wrong file version $fileVer (expected $versionStr) on $file.\n";
	}
}

# -----------------------------------------------------------------------------
# Check file version, architecture and release build #
#
# Notes: if $versionStr == "", skip version checking.
#	If $archStr == "", skip architecture checking.
#	if $releaseOnly == 0, skip release build # checking.
#
# Sample:
# For C/C++ executable:
#	CharacterSet : Unicode
#	CompanyName : NextLabs, Inc.
#	FileDescription : NextLabs Windows Policy Controller (x64)
#	FileFlags : VS_UNKNOWN
#	FileOS : VOS__WINDOWS32
#	FileType : VFT_APP
#	FileVersion : 5.5.0.0 (11.070.0242/nightly-smdc-20110311-nxt-build11-pfung)
#	FileVersion64 : 5.05.0
#	FileVersionLS : 0
#	FileVersionMS : 327685
#	Language : English (United States)
#	LegalCopyright : Copyright (C) 2011 NextLabs, Inc. All rights reserved.
#	ProductName : NextLabs Windows Policy Controller
#	ProductVersion : 5.5.0.0 (11.070.0242/nightly-smdc-20110311-nxt-build11-pfung)
#	ProductVersion64 : 5.05.0
#	ProductVersionLS : 0
#	ProductVersionMS : 327685
#
# For C# DLL:
#   FILE: Path = c:/pcv/SharePointEnforcer/D_SiriusR2/bin/release_dotnet/NextLabs.CSCInvoke.dll
#   FILE: Version = 5.5.1.0
#   FILE:   CharacterSet = Multilingual
#   FILE:   FileFlags = VS_UNKNOWN
#   FILE:   FileOS = VOS__WINDOWS32
#   FILE:   FileType = VFT_DLL
#   FILE:   FileVersion64 = 5.05.1.0
#   FILE:   FileVersionLS = 65536
#   FILE:   FileVersionMS = 327685
#   FILE:   Language = English (United States)
#   FILE:   ProductVersion64 = 5.05.1.0
#   FILE:   ProductVersionLS = 65536
#   FILE:   ProductVersionMS = 327685
#
# Notes: Cannot use this function to check architecture and releaseOnly on C# DLL because
# the information is not stamped on a C# DLL.

sub fileVersionArchitectureAndReleaseRequired
{
	my	($file, $versionStr, $archStr, $releaseOnly) = @_;

	# Check version
	if ($versionStr ne "")
	{
		# Get file version #
		my	$fileVer = Win32::GetFileVersion($file);

		if (!$fileVer)
		{
			die "### ERROR: File version not set (expected $versionStr) on $file.\n";
		}

		print "  FILE: Path = $file\n";
		print "  FILE: Version = $fileVer\n";

		# Check file version
		if ($fileVer ne $versionStr)
		{
			print "### WARNING: Wrong file version $fileVer (expected $versionStr) on $file.\n";
			return;

			die "### ERROR: Wrong file version $fileVer (expected $versionStr) on $file.\n";
		}
	}

	# Get file info
	my	%verInfo = ();

	if ($archStr ne "" || $releaseOnly)
	{
		%verInfo = Win32API::Resources::GetFileVersion($file);

		print "  FILE:   $_ = $verInfo{$_}\n" foreach sort keys %verInfo;
	}

	# Check architecture
	if ($archStr eq "")
	{
		print "INFO: Missing architecture, not checked.\n";
	}
	else
	{
		my	$buf = $verInfo{FileDescription};

		if ($buf !~ /\($archStr\)$/)
		{
			die "### ERROR: Wrong file architecture $buf (expected $archStr) on $file.\n";
		}
	}

	# Check release
	if ($releaseOnly)
	{
		my	$buf = "";

		if (exists $verInfo{FileVersion})
		{
			$buf = $verInfo{FileVersion};		# For C/C++ binaries
		}
		elsif (exists $verInfo{Version})
		{
			$buf = $verInfo{Version};			# For C# binaries
		}

		print "BUF=$buf\n";

		if (($buf !~ /\d+\.\d+\.\d+\.\d+\s+\(\d+\)$/) && ($buf !~ /\d+\.\d+\.\d+\.\d+$/))
		{
			print "ERROR: Not a release build $buf of $file.\n";
			die "### ERROR: Not a release build $buf of $file.\n";
		}
	}
}


#
# Copy file
#

# -----------------------------------------------------------------------------
# Copy one file

sub copyRequired
{
	my	($fromFile, $toFile) = @_;
	my	$toDir = dirname($toFile);

	print "INFO: Copying file $fromFile to $toFile.\n";

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 2)
	{
		die "### ERROR: Wrong # of arguments (expected 2, got $argCount) to InstallHelper::copyRequired().\n";
	}

	if (! -e $fromFile)
	{
		die "### ERROR: File $fromFile does not exist. Cannot copy file.\n";
	}

	# Delete destination file
	if (-e $toFile)
	{
		print "INFO:   Deleting existing destination file $toFile.\n";

		unlink($toFile) || die "### ERROR: Failed to remove destination file $toFile. Cannot copy file.\n";
	}

	# Create output directory
	if (! -d $toDir)
	{
		make_path($toDir, {mode => 0711, error => \my $err});

		if (@$err)
		{
			for my $diag (@$err)
			{
				my	($file, $message) = %$diag;

				if ($file eq "")
				{
					print "### ERROR: Failed to create directory $toDir. Cause: $message.\n";
				}
				else
				{
					print "### ERROR: Failed to create directory $file. Cause: $message.\n";
				}
			}

			die "### ERROR: Failed to copy file from $fromFile to $toFile.\n";
		}
	}

	# Copy file
	my	$info = stat($fromFile) || die "### ERROR: Failed to get stat on file $fromFile\n";

	copy($fromFile, $toFile) || die "### ERROR: Failed to copy file from $fromFile to $toFile.\n";

	if (utime($info->[8], $info->[9], $toFile) != 1)
	{
		die "### ERROR: Failed to update timestamps of file $toFile.\n";
	}

	print "INFO:   Successfully copied file to $toFile\n";
}

# -----------------------------------------------------------------------------
# Copy one file and check version
#
# Notes: if $versionStr == "", skip version checking.

sub copyVersionRequired
{
	my	($fromFile, $toFile, $versionStr) = @_;
	my	$toDir = dirname($toFile);

	print "INFO: Copying file $fromFile ($versionStr) to $toFile.\n";

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 3)
	{
		die "### ERROR: Wrong # of arguments (expected 3, got $argCount) to InstallHelper::copyVersionRequired().\n";
	}

	if (! -e $fromFile)
	{
		die "### ERROR: File $fromFile does not exist. Cannot copy file.\n";
	}

	# Delete destination file
	if (-e $toFile)
	{
		print "INFO:   Deleting existing destination file $toFile.\n";

		unlink($toFile) || die "### ERROR: Failed to remove destination file $toFile. Cannot copy file.\n";
	}

	# Check file version
	fileVersionRequired($fromFile, $versionStr);

	# Create output directory
	if (! -d $toDir)
	{
		make_path($toDir, {mode => 0711, error => \my $err});

		if (@$err)
		{
			for my $diag (@$err)
			{
				my	($file, $message) = %$diag;

				if ($file eq "")
				{
					print "### ERROR: Failed to create directory $toDir. Cause: $message.\n";
				}
				else
				{
					print "### ERROR: Failed to create directory $file. Cause: $message.\n";
				}
			}

			die "### ERROR: Failed to copy file from $fromFile to $toFile.\n";
		}
	}

	# Copy file
	my	$info = stat($fromFile) || die "### ERROR: Failed to get stat on file $fromFile\n";

	copy($fromFile, $toFile) || die "### ERROR: Failed to copy file from $fromFile to $toFile.\n";

	if (utime($info->[8], $info->[9], $toFile) != 1)
	{
		die "### ERROR: Failed to update timestamps of file $toFile.\n";
	}

	print "INFO:   Successfully copied file to $toFile\n";
}

# -----------------------------------------------------------------------------
# Copy one file and check version, architecture and release build #
#
# Notes: if $versionStr == "", skip version checking.
#	If $archStr == "", skip architecture checking.
#	if $releaseOnly == 0, skip release build # checking.

sub copyVersionArchitectureAndReleaseRequired
{
	my	($fromFile, $toFile, $versionStr, $archStr, $releaseOnly) = @_;
	my	$toDir = dirname($toFile);

	print "INFO: Copying file $fromFile ($versionStr, $archStr, $releaseOnly) to $toFile.\n";

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 5)
	{
		die "### ERROR: Wrong # of arguments (expected 5, got $argCount) to InstallHelper::copyVersionArchitectureAndReleaseRequired().\n";
	}

	if (! -e $fromFile)
	{
		die "### ERROR: File $fromFile does not exist. Cannot copy file.\n";
	}

	# Delete destination file
	if (-e $toFile)
	{
		print "INFO:   Deleting existing destination file $toFile.\n";

		unlink($toFile) || die "### ERROR: Failed to remove destination file $toFile. Cannot copy file.\n";
	}

	# Check file version
	fileVersionArchitectureAndReleaseRequired($fromFile, $versionStr, $archStr, $releaseOnly);

	# Create output directory
	if (! -d $toDir)
	{
		make_path($toDir, {mode => 0711, error => \my $err});

		if (@$err)
		{
			for my $diag (@$err)
			{
				my	($file, $message) = %$diag;

				if ($file eq "")
				{
					print "### ERROR: Failed to create directory $toDir. Cause: $message.\n";
				}
				else
				{
					print "### ERROR: Failed to create directory $file. Cause: $message.\n";
				}
			}

			die "### ERROR: Failed to copy file from $fromFile to $toFile.\n";
		}
	}

	# Copy file
	my	$info = stat($fromFile) || die "### ERROR: Failed to get stat on file $fromFile\n";

	copy($fromFile, $toFile) || die "### ERROR: Failed to copy file from $fromFile to $toFile.\n";

	if (utime($info->[8], $info->[9], $toFile) != 1)
	{
		die "### ERROR: Failed to update timestamps of file $toFile.\n";
	}

	print "INFO:   Successfully copied file to $toFile\n";
}

# -----------------------------------------------------------------------------
# Copy files in a list
#

sub copyListRequired
{
	my	($fromDir, $toDir, $refFileList) = @_;

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 3)
	{
		die "### ERROR: Wrong # of arguments (expected 3, got $argCount) to InstallHelper::copyListRequired().\n";
	}

	if (! -d $fromDir)
	{
		die "### ERROR: From directory $fromDir does not exists.";
	}

	# Copy files
	foreach my $fileName (@$refFileList)
	{
		copyRequired("$fromDir/$fileName", "$toDir/$fileName");
	}
}

# -----------------------------------------------------------------------------
# Copy files in a list, check version
#
# Notes: if $versionStr == "", skip version checking.

sub copyListVersionRequired
{
	my	($fromDir, $toDir, $refFileList) = @_;

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 3)
	{
		die "### ERROR: Wrong # of arguments (expected 3, got $argCount) to InstallHelper::copyListVersionRequired().\n";
	}

	if (! -d $fromDir)
	{
		die "### ERROR: From directory $fromDir does not exists.";
	}

	# Copy files
	foreach my $refFileInfo (@$refFileList)
	{
		my	$fileName = $refFileInfo->[0];
		my	$versionStr = $refFileInfo->[1];

		copyVersionRequired("$fromDir/$fileName", "$toDir/$fileName", $versionStr);
	}
}

# -----------------------------------------------------------------------------
# Copy files in a list, check version, architecture and release build #
#
# Notes: if $versionStr == "", skip version checking.
#	If $archStr == "", skip architecture checking.
#	if $releaseOnly == 0, skip release build # checking.

sub copyListVersionArchitectureAndReleaseRequired
{
	my	($fromDir, $toDir, $refFileList) = @_;

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 3)
	{
		die "### ERROR: Wrong # of arguments (expected 3, got $argCount) to InstallHelper::copyListVersionArchitectureAndReleaseRequired().\n";
	}

	if (! -d $fromDir)
	{
		die "### ERROR: From directory $fromDir does not exists.";
	}

	# Copy files
	foreach my $refFileInfo (@$refFileList)
	{
		my	$fileName = $refFileInfo->[0];
		my	$versionStr = $refFileInfo->[1];
		my	$archStr = $refFileInfo->[2];
		my	$releaseOnly = $refFileInfo->[3];

		copyVersionArchitectureAndReleaseRequired("$fromDir/$fileName", "$toDir/$fileName", $versionStr, $archStr, $releaseOnly);
	}
}


#
# Create or update files
#

# -----------------------------------------------------------------------------
# Create a new ISM file based on an existing one

sub constructIsmFile
{
	my	($ismTemplateFile, $ismOutFile, $msiFileName32, $msiFileName64, $productName, $versionStr, $buildNum) = @_;
	my	$outDir = dirname($ismOutFile);

	print "INFO: Constructing InstallShield ISM file $ismOutFile based on template $ismTemplateFile.\n";

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 7)
	{
		die "### ERROR: Wrong # of arguments (expected 7, got $argCount) to InstallHelper::constructIsmFile().\n";
	}

	if (! -e $ismTemplateFile)
	{
		die "### ERROR: File $ismTemplateFile does not exist. Cannot construct ISM file.\n";
	}

	if ($msiFileName32 eq "")
	{
		die "### ERROR: Missing 32-bit MSI file name. Cannot construct ISM file.\n";
	}

	if ($msiFileName64 eq "")
	{
		die "### ERROR: Missing 64-bit MSI file name. Cannot construct ISM file.\n";
	}

	if ($versionStr eq "")
	{
		die "### ERROR: Missing file version. Cannot construct ISM file.\n";
	}

	if ($productName eq "")
	{
		die "### ERROR: Missing product name. Cannot construct ISM file.\n";
	}

	# Delete output file
	if (-e $ismOutFile)
	{
		unlink($ismOutFile) || die "### ERROR: Failed to remove destination file $ismOutFile. Cannot construct ISM file.\n";
	}

	# Create output directory
	if (! -d $outDir)
	{
		make_path($outDir, {mode => 0711, error => \my $err});

		if (@$err)
		{
			for my $diag (@$err)
			{
				my	($file, $message) = %$diag;

				if ($file eq "")
				{
					print "### ERROR: Failed to create directory $outDir. Cause: $message.\n";
				}
				else
				{
					print "### ERROR: Failed to create directory $file. Cause: $message.\n";
				}
			}

			die "### ERROR: Failed to construct ISM file $ismOutFile based on $ismTemplateFile.\n";
		}
	}

	# Read ISM file
	my	$data = "";

	open FILE, $ismTemplateFile || die "Error opening ISM file $ismTemplateFile\n";

	while (my $buf = <FILE>)
	{
#		print "ISM: $buf";

		# Replace MSIPackageName in <table name="ISProductConfigurationProperty">
		#	<row><td>32bit</td><td>MSIPackageFileName</td><td>SharePointEnforcer-2019-32-5.6.0.0</td></row>
		#	<row><td>CE</td><td>MSIPackageFileName</td><td>CE-SharePointEnforcerForWindows-SP2019-SingleSDK-x86-setup</td></row>
		#	<row><td>64bit</td><td>MSIPackageFileName</td><td>SharePointEnforcer-2019-64-5.6.0.0</td></row>
		#	<row><td>CE</td><td>MSIPackageFileName</td><td>CE-SharePointEnforcerForWindows-SP2019-SingleSDK-x64-setup</td></row>
		#	<row><td>64bit</td><td>MSIPackageFileName</td><td>SharePointEnforcer-2016-64-5.6.0.0</td></row>
		#	<row><td>CE</td><td>MSIPackageFileName</td><td>CE-SharePointEnforcerForWindows-SP2016-SingleSDK-setup</td></row>
		#	<row><td>32bit</td><td>MSIPackageFileName</td><td>SharePointWorkflowPolicyAssistant-32-5.6.0.0</td></row>
		#	<row><td>64bit</td><td>MSIPackageFileName</td><td>SharePointWorkflowPolicyAssistant-64-5.6.0.0</td></row>

		$buf =~ s#<row><td>32bit</td><td>MSIPackageFileName</td><td>[^<]*</td></row>#<row><td>32bit</td><td>MSIPackageFileName</td><td>$msiFileName32</td></row>#g;
		$buf =~ s#<row><td>64bit</td><td>MSIPackageFileName</td><td>[^<]*</td></row>#<row><td>64bit</td><td>MSIPackageFileName</td><td>$msiFileName64</td></row>#g;
		$buf =~ s#<row><td>CE</td><td>MSIPackageFileName</td><td>[^<]*</td></row>#<row><td>CE</td><td>MSIPackageFileName</td><td>$msiFileName64</td></row>#g;

		# Replace ProudctName in <table name="ISProductConfigurationProperty">
		#	<row><td>32bit</td><td>ProductName</td><td>Entitlement Manager for Microsoft SharePoint</td></row>
		#	<row><td>CE</td><td>ProductName</td><td>Entitlement Manager for Microsoft SharePoint</td></row>
		#	<row><td>64bit</td><td>ProductName</td><td>Compliant Enterprise SharePoint Enforcer for Windows</td></row>
		#	<row><td>CE</td><td>ProductName</td><td>Compliant Enterprise SharePoint Enforcer for Windows</td></row>
		#	<row><td>64bit</td><td>ProductName</td><td>Entitlement Manager for Microsoft SharePoint</td></row>
		#	<row><td>CE</td><td>ProductName</td><td>Entitlement Manager for Microsoft SharePoint</td></row>

		$buf =~ s#<row><td>32bit</td><td>ProductName</td><td>[^<]*</td></row>#<row><td>32bit</td><td>ProductName</td><td>$productName</td></row>#g;
		$buf =~ s#<row><td>64bit</td><td>ProductName</td><td>[^<]*</td></row>#<row><td>64bit</td><td>ProductName</td><td>$productName</td></row>#g;
		$buf =~ s#<row><td>CE</td><td>ProductName</td><td>[^<]*</td></row>#<row><td>CE</td><td>ProductName</td><td>$productName</td></row>#g;

		# Replace ProudctName in <table name="Property">
		#	<row><td>ProductName</td><td>SharePointEnforcer</td><td/></row>
		#	<row><td>ProductName</td><td>Entitlement Manager for Microsoft SharePoint</td><td/></row>
		#	<row><td>ProductName</td><td>Entitlement Manager for Microsoft SharePoint</td><td/></row>
		#	<row><td>ProductName</td><td>Workflow Policy Assistant for Microsoft SharePoint</td><td/></row>

		$buf =~ s#<row><td>ProductName</td><td>[^<]*</td><td/></row>#<row><td>ProductName</td><td>$productName</td><td/></row>#g;

		# Replace ProudctVersion in <table name="Property">
		#	<row><td>ProductVersion</td><td>5.6.0.0</td><td/></row>
		#	<row><td>ProductVersion</td><td>5.6.0.0</td><td/></row>
		#	<row><td>ProductVersion</td><td>5.6.0.0</td><td/></row>
		#	<row><td>ProductVersion</td><td>5.0</td><td/></row>

		$buf =~ s#<row><td>ProductVersion</td><td>[^<]*</td><td/></row>#<row><td>ProductVersion</td><td>$versionStr</td><td/></row>#g;

		$data .= $buf;
	}

	close FILE;

	# Write ISM file
	open FILE, ">$ismOutFile" || die "Error opening output file $ismOutFile\n";
	print FILE $data;
	close FILE;

	# Print differences
	my	$result = diff($ismTemplateFile, $ismOutFile, {STYLE => "OldStyle"});

	print "INFO:   ISM File Differences:\n";
	print $result;

	print "INFO:   Successfully wrote ISM file $ismOutFile.\n";
}

# -----------------------------------------------------------------------------
# Update ReadMe file

sub updateReadMeFile
{
	my	($file, $majorVer, $minorVer, $maintenanceVer, $patchVer, $buildNum) = @_;

	print "INFO: Updating ReadMe file $file.\n";
	print "  Major        = $majorVer\n";
	print "  Minor        = $minorVer\n";
	print "  Maintenance  = $maintenanceVer\n";
	print "  Patch        = $patchVer\n";
	print "  Build #      = $buildNum\n";

	# Check for errors
	my	$argCount = scalar(@_);

	if ($argCount != 6)
	{
		die "### ERROR: Wrong # of arguments (expected 6, got $argCount) to InstallHelper::updateReadMeFile().\n";
	}

	if (! -e $file)
	{
		die "### ERROR: File $file does not exist. Cannot update ReadMe file.\n";
	}

	if ($majorVer < 0)
	{
		die "### ERROR: Missing major version #. Cannot update ReadMe file.\n";
	}

	if ($minorVer < 0)
	{
		die "### ERROR: Missing minor version #. Cannot update ReadMe file.\n";
	}

	if ($maintenanceVer < 0)
	{
		die "### ERROR: Missing maintenance version #. Cannot update ReadMe file.\n";
	}

	if ($patchVer < 0)
	{
		die "### ERROR: Missing patch version #. Cannot update ReadMe file.\n";
	}

	if ($buildNum eq "")
	{
		die "### ERROR: Missing buildNum #. Cannot update ReadMe file.\n";
	}

	# Read ReadMe file
	my	$data = "";

	open FILE, $file || die "Error opening ReadMe file $file\n";

	while (my $buf = <FILE>)
	{
#		print "LINE: $buf";

		$buf =~ s/{MAJOR.VERSION}/$majorVer/g;
		$buf =~ s/{MINOR.VERSION}/$minorVer/g;
		$buf =~ s/{MAINTENANCE.VERSION}/$maintenanceVer/g;
		$buf =~ s/{PATCH.VERSION}/$patchVer/g;
		$buf =~ s/{BUILD}/$buildNum/g;

		$data .= $buf;
	}

	close FILE;

	# Create temporary file
	my	$tmpFile = "${file}.tmp";

	# Write output file
	open FILE, ">$tmpFile" || die "Error opening output file $tmpFile\n";
	print FILE $data;
	close FILE;

	# Print output
	print "INFO:   New ReadMe File:\n";
	print "[README.TXT] $data";

	# Print differences
	my	$result = diff($file, $tmpFile, {STYLE => "OldStyle"});

	print "INFO:   ReadMe File Differences:\n";
	print $result;

	# Replace file
	unlink($file) || die "### ERROR: Failed to delete existing ReadMe file $file.\n";
	rename($tmpFile, $file) || die "### ERROR: Failed to rename ReadMe file from $tmpFile to $file.\n";

	print "INFO:   Successfully wrote ReadMe file $file.\n";
}


#
# Main program
#

1;
