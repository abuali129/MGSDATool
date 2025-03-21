using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;
using DATSpeechTool.IO;

namespace DATSpeechTool
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("DATSpeechTool by gdkchan/Modded By VHussain");
            Console.WriteLine("MGS4 speech.dat subtitle extractor/inserter & xml balancer");
            Console.WriteLine("Version 0.2.0.1");
            Console.ResetColor();
            Console.Write(Environment.NewLine);

            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }
            else
            {
                switch (args[0])
                {
                    case "-xdat": Data.Extract(args[1], args[2], args[3]); break;
                    case "-cdat": Data.Create(args[1], args[2], args[3]); break;
                    case "-xspc": Speech.Extract(args[1], args[2]); break;
                    case "-ispc": Speech.Insert(args[1], args[2]); break;
                    case "-xspcall":
                        string[] SPCFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.spc");
                        foreach (string SPCFile in SPCFiles)
                        {
                            string OutFile = SPCFile.Replace(".spc", ".xml");
                            Speech.Extract(SPCFile, OutFile);
                        }
                        break;
                    case "-ispcall":
                        string[] XMLFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.xml");
                        foreach (string XMLFile in XMLFiles)
                        {
                            string OutFile = XMLFile.Replace(".xml", ".spc");
                            Speech.Insert(OutFile, XMLFile);
                        }
                        break; 
					case "-b":
                        if (args.Length < 4)
                        {
                            TextOut.PrintError("Usage: -b sizeslist.txt inputFolder outputFolder");
                            return;
                        }
                        BalanceCommand.Process(args[1], args[2], args[3]);
                        break;
                    default: TextOut.PrintError("Invalid command \"" + args[0] + "\" used!"); return;
                }
            }

            Console.Write(Environment.NewLine);
            TextOut.PrintSuccess("Finished!");
        }

        private static void PrintUsage()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Usage:");
            Console.ResetColor();
            Console.Write(Environment.NewLine);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("DAT Extraction/Creation Commands");
			Console.ResetColor();
            Console.WriteLine("tool -xdat scenerio.gcx speech.dat out_folder  Extracts DAT");
            Console.WriteLine("tool -cdat scenerio.gcx speech.dat in_folder  Creates DAT");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("\n.SPC Extraction/Creation Commands");
			Console.ResetColor();
			Console.WriteLine("*Single File Commands");
            Console.WriteLine("tool -xspc file.spc output.xml  Extracts subtitles from SPC");
            Console.WriteLine("tool -ispc file.spc input.xml  Inserts subtitles into SPC");
			Console.WriteLine("\n*Bulk File Commands");
			Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("tool -xspcall  Extracts subtitles from all SPCs on current dir, Tool & .SPC files should be in same folder");
            Console.WriteLine("tool -ispcall  Inserts subtitles into all SPCs on current dir, Tool & .SPC & .XML files should be in same folder");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("\nXML Balancer Command, go to README for more info");
			Console.ResetColor();
			Console.WriteLine("tool -b FileSizes.txt inputFolder outputFolder");
			
        }
		
    }
}
