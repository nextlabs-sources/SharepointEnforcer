#!/usr/bin/perl
#
# DESCRIPTION
# This script updates VERSIONINFO section in an existing Visual Studio project resouce file
# using version information in Makefile.inc. It should be called from Pre-Build Event of
# a VS project.

use strict;
use warnings;

print "NextLabs Update Resource File VersionInfo Script (.csproj Pre-Build Event)\n";


#
# Check for errors
#

# -----------------------------------------------------------------------------
# Dump parameters

sub dumpParameters
{
	print "ERROR: Wrong # of arguments (expect 2 or 3).\n";
	print "  See updateVersionInfo_csproj.pl and Makefile in //depot/dev/D_SiriusR2/build/ for more details.\n";
	print "Argv[0] = $ARGV[0]\n";
	print "Argv[1] = $ARGV[1]\n";
	print "Argv[2] = $ARGV[2]\n";
	print "Argv[3] = $ARGV[3]\n";
}

my	$argCount = scalar(@ARGV);

if ($argCount < 3 || $argCount > 4)
{
	dumpParameters;
	exit 1;
}


#
# Process parameters
#

# -----------------------------------------------------------------------------
# Print usage

sub printUsage
{
	print "usage: updateVersionInfo_csproj.pl <resource file> <makefile> <build code>\n";
	print "         <architecture>\n";
	print "  architecture  - Targeted architecture (may be x86 or x64).\n";
	print "  build code    - Build code. Valid values are number, 'dev' and 'nightly'.\n";
	print "                  Specify a number for release build.\n";
	print "  makefile      - Project Makefile.inc relative to .vsproj file.\n";
	print "  resource file - Resource file containing VERSIONINFO section to be updated.\n";
	print "                  The path is relative to location of .vsproj file.\n";
}

if (($argCount < 3 || $argCount > 4) || $ARGV[0] eq "-h" || $ARGV[0] eq "--help")
{
	printUsage;
	exit 1;
}

# Collect parameters
my	$resourceFile = $ARGV[0];
my	$makefile = $ARGV[1];
my	$buildCode = $ARGV[2];
my	$architecture = $ARGV[3];
my	$showUpdatedFile = 0;

# Print parameters
print "Parameters:\n";
print "  Resource File       = $resourceFile\n";
print "  Makefile            = $makefile\n";
print "  Build Code          = $buildCode\n";
print "  Architecture        = $architecture\n";
print "  Show Updated File   = $showUpdatedFile\n";


#
# Check for errors
#

if ( ! -e $resourceFile)
{
	print "ERROR: $resourceFile does not exist\n";
	exit 1;
}


#
# Get VERSION_* values from Makefile.inc
#

my	$product = "";
my	$majorVer = 0;
my	$minorVer = 0;
my	$maintenanceVer = 0;
my	$patchVer = 0;

open FILE, $makefile || die "Error opening makefile $makefile";

while (! eof(FILE))
{
	my	$line = readline(*FILE);

	if ($line =~ /VERSION_PRODUCT\s*=\s*(.+)\n*/)
	{
		$product = $1;
	}
	elsif ($line =~ /VERSION_MAJOR\s*=\s*(\d+)/)
	{
		$majorVer = $1;
	}
	elsif ($line =~ /VERSION_MINOR\s*=\s*(\d+)/)
	{
		$minorVer = $1;
	}
	elsif ($line =~ /VERSION_MAINTENANCE\s*=\s*(\d+)/)
	{
		$maintenanceVer = $1;
	}
	elsif ($line =~ /VERSION_PATCH\s*=\s*(\d+)/)
	{
		$patchVer = $1;
	}
}

close FILE;

print "Makefile Version Data:\n";
print "  Product Name        = $product\n";
print "  Major Version       = $majorVer\n";
print "  Minor Version       = $minorVer\n";
print "  Maintenance Version = $maintenanceVer\n";
print "  Patch Version       = $patchVer\n";


#
# Construct build number
#

my	$buildNum = $buildCode;

if ($buildCode eq "pcv" || $buildCode eq "nightly" || $buildCode eq "dev")
{
	my	($second, $minute, $hour, $dayOfMonth, $month, $yearOffset, $dayOfWeek, $dayOfYear, $daylightSavings) = localtime();
	my	$year = 1900 + $yearOffset;
	my	$refCode = sprintf("%02d.%03d.%02d%02d", $year % 100, $dayOfYear + 1, $hour, $minute);
	my	$date = sprintf("%04d.%02d.%02d", $year, $month, $dayOfMonth);

	if ($buildCode eq "pcv")
	{
		$refCode .= "PX";
	}
	elsif ($buildCode eq "nightly")
	{
		$refCode .= "NX";
	}
	else
	{
		$refCode .= "DX";
	}

	$buildNum = "$refCode/$ENV{COMPUTERNAME}-$ENV{USERNAME}-$date";
}

print "Build Number:\n";
print "  Build Number        = $buildNum\n";


#
# Read resource file
#

local $/ = undef;
open FILE, $resourceFile || die "Error opening resource file $resourceFile (read)";
my	$buf = <FILE>;
close FILE;

#print "\nSource Data:\n----------------\n$buf\n\n";


#
# Update version info
#


#$buf =~ s/FILEVERSION\s+\d+,\s*\d+,\s*\d+,\s*\d+/FILEVERSION $majorVer, $minorVer, $maintenanceVer, $patchVer/g;
#$buf =~ s/PRODUCTVERSION\s+\d+,\s*\d+,\s*\d+,\s*\d+/PRODUCTVERSION $majorVer, $minorVer, $maintenanceVer, $patchVer/g;
#$buf =~ s/VALUE\s+"CompanyName"\s*,\s*"[^"]*"/VALUE "CompanyName", "NextLabs, Inc."/g;
#$buf =~ s/VALUE\s+"FileDescription"\s*,\s*"[^"]*"/VALUE "FileDescription", "$product ($architecture)"/g;
#$buf =~ s/VALUE\s+"FileVersion"\s*,\s*"[^"]*"/VALUE "FileVersion", "$majorVer.$minorVer.$maintenanceVer.$patchVer ($build)"/g;
#$buf =~ s/VALUE\s+"LegalCopyright"\s*,\s*"[^"]*"/VALUE "LegalCopyright", "Copyright (C) 2011 NextLabs, Inc. All rights reserved."/g;
#$buf =~ s/VALUE\s+"ProductName"\s*,\s*"[^"]*"/VALUE "ProductName", "$product"/g;
#$buf =~ s/VALUE\s+"ProductVersion"\s*,\s*"[^"]*"/VALUE "ProductVersion", "$majorVer.$minorVer.$maintenanceVer.$patchVer ($build)"/g;
$product=~ s/\s*\n//g;
$product=~ s/\s*\r//g;
$buf =~ s/\[assembly:\s+AssemblyVersion\("\s*[^"]*"\)\]/\[assembly: AssemblyVersion(\"$majorVer.$minorVer.$maintenanceVer.$patchVer\")\]/g;
$buf =~ s/\[assembly:\s+AssemblyCompany\("\s*[^"]*"\)\]/\[assembly: AssemblyCompany(\"NextLabs, Inc.\")\]/g;
$buf =~ s/\[assembly:\s+AssemblyCopyright\("\s*[^"]*"\)\]/\[assembly: AssemblyCopyright(\"Copyright (C) 2011 NextLabs, Inc. All rights reserved.\")\]/g;
$buf =~ s/\[assembly:\s+AssemblyProduct\("\s*[^"]*"\)\]/\[assembly: AssemblyProduct(\"$product\")\]/g;
$buf =~ s/\[assembly:\s+AssemblyFileVersion\("\s*[^"]*"\)\]/\[assembly: AssemblyFileVersion(\"$majorVer.$minorVer.$maintenanceVer.$patchVer ($build)\")\]/g;

#print "\nUpdated Data:\n----------------\n$buf\n\n";


#
# Write resource file
#
# Notes: There is a problem with Cygwin + Perforce combination. If you run chmod from
# Cygwin, you will get an error. If you run "ls -al" you will see no permission and
# group is mkpasswd. If you check "if (-r myfile)", it will always return true. To
# work around this problem, we call Windows ATTRIB command directly.

my	$resourceFileDos = $resourceFile;

$resourceFileDos =~ s#/#\\#;

system("ATTRIB -R \"$resourceFileDos\"");

#if (chmod(0777, $resourceFile) == 0)
#{
#	die "### ERROR: Failed to chmod on file $resourceFile\n";
#}

open FILE, ">$resourceFile" || die "Error opening resource file $resourceFile (write)";
print FILE $buf;
close FILE;


#
# Print updated file
#

if ($showUpdatedFile)
{
	open FILE, $resourceFile || die "Error opening updated file $resourceFile\n";

	while (<FILE>)
	{
		print $_;
	}

	close FILE;
}

exit 0;
