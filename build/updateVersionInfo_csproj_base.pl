#!/usr/bin/perl
#
# DESCRIPTION
# This script updates VERSIONINFO section in an existing Visual Studio project resouce file
# using version information in Makefile.inc. It should be called from Pre-Build Event of
# a VS project.

use strict;
use warnings;

print "NextLabs Update Resource File VersionInfo Script (.csproj Pre-Build Event)\n";

# -----------------------------------------------------------------------------
# Print usage

sub printUsage
{
	print "usage: updateVersionInfo_csproj_base.pl <resource file> <Product name> <majorVer> <minorVer> <maintenanceVer> <patchVer>\n";
	print "  resource file - Resource file containing VERSIONINFO section to be updated.\n";
	print "                  The path is relative to location of .vsproj file.\n";
	print "  product name - current build product name, eg: NextLabs SharePoint Enforcer.\n";
	print "  version info: <majorVer> <minorVer> <maintenanceVer> <patchVer>, eg: 8 5 1 15\n";
}

#
# Check for parameters
#

my	$argCount = scalar(@ARGV);
print "	argCount	=	$argCount	";

if (($argCount != 7) || $ARGV[0] eq "-h" || $ARGV[0] eq "--help")
{
	printUsage;
	exit 1;
}

# Collect parameters
my	$resourceFile = $ARGV[0];
my	$ProductName = $ARGV[1];
my	$majorVer = $ARGV[2];
my	$minorVer = $ARGV[3];
my	$maintenanceVer = $ARGV[4];
my	$patchVer = $ARGV[5];
my $ignoreUpdateAssemblyVersion = $ARGV[6];
my	$showUpdatedFile = 1;


# Print parameters
print "Parameters:\n";
print " Resource File						= $resourceFile\n";
print " Product Name					= $ProductName\n";
print " VersionInfo						= $majorVer.$minorVer.$maintenanceVer.$patchVer\n";
print " IgnoreUpdateAssemblyVersion	= $ignoreUpdateAssemblyVersion\n";


#
# Check for errors
#

if ( ! -e $resourceFile)
{
	print "ERROR: $resourceFile does not exist\n";
	exit 1;
}

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

# [assembly: AssemblyTitle("Nextlabs.Diagnostic")]
# [assembly: AssemblyDescription("Nextlabs Diagnostic Module(AnyCPU)")]
$buf =~ s/\[assembly:\s+AssemblyCompany\("\s*[^"]*"\)\]/\[assembly: AssemblyCompany(\"NextLabs, Inc.\")\]/g;
$buf =~ s/\[assembly:\s+AssemblyProduct\("\s*[^"]*"\)\]/\[assembly: AssemblyProduct(\"$ProductName\")\]/g;
$buf =~ s/\[assembly:\s+AssemblyCopyright\("\s*[^"]*"\)\]/\[assembly: AssemblyCopyright(\"Copyright (C) 2020 NextLabs, Inc. All rights reserved.\")\]/g;

if ($ignoreUpdateAssemblyVersion != 1)
{
	$buf =~ s/\[assembly:\s+AssemblyVersion\("\s*[^"]*"\)\]/\[assembly: AssemblyVersion(\"$majorVer.$minorVer.$maintenanceVer.$patchVer\")\]/g;
}

$buf =~ s/\[assembly:\s+AssemblyFileVersion\("\s*[^"]*"\)\]/\[assembly: AssemblyFileVersion(\"$majorVer.$minorVer.$maintenanceVer.$patchVer\")\]/g;
$buf =~ s/\[assembly:\s+AssemblyFileVersionAttribute\("\s*[^"]*"\)\]/\[assembly: AssemblyFileVersionAttribute(\"$majorVer.$minorVer.$maintenanceVer.$patchVer\")\]/g;


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
