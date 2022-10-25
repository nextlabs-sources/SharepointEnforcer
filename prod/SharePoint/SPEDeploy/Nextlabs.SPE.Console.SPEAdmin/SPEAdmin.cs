using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Reflection;

namespace Nextlabs.SPE.Console
{

    public static class Console
    {
        static StreamWriter logOut = null;
        public static void SetLogFile(string logfile)
        {
            try
            {
                if (logfile != null)
                    logOut = new StreamWriter(logfile, true);
                logOut.WriteLine("Start to output information in " + DateTime.Now.ToString());
            }
            catch (IOException ioExp)
            {
                Console.WriteLine("Redirect stdout to log file failed. All output will be printed at console.\n Exception:" + ioExp.Message);
            }
        }
        public static void WriteLine(string value)
        {
            if (logOut != null)
            {
                logOut.WriteLine(value);
            }
            System.Console.WriteLine(value);
        }

        public static void Close()
        {
            if (logOut != null)
            {
                logOut.WriteLine("End to output information in " + DateTime.Now.ToString());
                logOut.WriteLine("");
                logOut.WriteLine("");
                logOut.Flush();
                logOut.Close();
            }
        }
    }    
    class SPEAdmin
    {
        static StringDictionary CommandRegisters = new StringDictionary();
        static string FeatureName = "";
        static StringDictionary Properties = new StringDictionary();
        static string CommandLine = "";
        static int Main(string[] args)
        {
            RegisterAllCommands();

            try
            {
                ParseArguments(args);

                ProcessCommand();
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                Console.WriteLine(exp.StackTrace);
                System.Environment.ExitCode = 2;
                PrintHelp();
                return -1;
            }
            return 0;
        }

        static void RegisterAllCommands()
        {
            // command name - Command Class Pair
            CommandRegisters.Add("securitytrimming", "Nextlabs.SPE.Console.SPSecurityTrimmingCommand");
            CommandRegisters.Add("searchresulttrimming", "Nextlabs.SPE.Console.SearchResultTrimmingCommand");
            CommandRegisters.Add("spe", "Nextlabs.SPE.Console.SPECommand");
            CommandRegisters.Add("contentanalysis", "Nextlabs.SPE.Console.ContentAnalysisCommand");
            CommandRegisters.Add("setproperty", "Nextlabs.SPE.Console.PROPBAGCommand");
            CommandRegisters.Add("getproperty", "Nextlabs.SPE.Console.GETPROPCommand");
			#region add by roy
            CommandRegisters.Add("ple", "Nextlabs.SPE.Console.PLECommand");
            #endregion

        }

        static void PrintHelp()
        {
            string help = "";
            if (!String.IsNullOrEmpty(FeatureName) && CommandRegisters.ContainsKey(FeatureName))
            {
                ISpeAdminCommand command = GetObjectByClassName(CommandRegisters[FeatureName]);
                help = command.GetHelpString(FeatureName);
            }
            else
            {
                help = "\nUsage:\n";
                help += "\tCE_SPAdmin.exe -o <featurename> [<parameters>]\n";
                help += "\tCE_SPAdmin.exe -help [<featurename>]\n";

                help += "\nFeatures:\n";

                foreach (string feature in CommandRegisters.Keys)
                {
                    help += "\t";
                    help += feature;
                    help += "\n";
                }
            }

            Console.WriteLine(help);
        }

        static void ParseArguments(string[] args)
        {
            CommandLine = "CE_SPAdmin.exe ";
            for (int i = 0; i < args.Length; )
            {
                string key = "";
                string value = "";
                if (args[i][0] == '-')
                    key = args[i];
                else
                    throw new InvalidOperationException("Unknown args " + args[i]);

                i++;
                if (i < args.Length && args[i][0] != '-')
                {
                    value = args[i];
                    i++;
                }
                CommandLine += key + " ";
                CommandLine += value + " ";
                Properties.Add(key, value);
            }
        }

        static void ProcessCommand()
        {
            if (Properties.ContainsKey("-help"))
            {
                FeatureName = Properties["-help"];
                PrintHelp();
            }
            else if (Properties.ContainsKey("-o"))
            {
                FeatureName = Properties["-o"];
                if (CommandRegisters.ContainsKey(FeatureName))
                {
                    try
                    {
                        if (Properties.ContainsKey("-logfile"))
                            Console.SetLogFile(Properties["-logfile"]);
                    }
                    catch (IOException ioExp)
                    {
                        Console.WriteLine("Redirect stdout to log file failed. All output will be printed at console.\n Exception:" + ioExp.Message);
                    }
                  
                    ISpeAdminCommand command = GetObjectByClassName(CommandRegisters[FeatureName]);
                    Properties.Remove("-o");
                    command.Run(FeatureName, Properties);
                    Console.Close();
                }
                else
                    throw new InvalidOperationException("Missing operation name or the operation name is invalid.");
            }
            else
            {
                throw new InvalidOperationException("The -o parameter is required.");
            }
        }

        static ISpeAdminCommand GetObjectByClassName(string className)
        {
            Assembly assm = Assembly.GetExecutingAssembly();
            Type type = assm.GetType(className);
            object command = Activator.CreateInstance(type);

            return command as ISpeAdminCommand;
        }
    }
}
