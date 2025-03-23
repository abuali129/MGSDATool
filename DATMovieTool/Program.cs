using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Text.RegularExpressions;
using DATMovieTool.IO;
using DATMovieTool.IO.Packet;

using MGSShared;

namespace DATMovieTool
{
    public class Program
    {
        static void Main(string[] args)
        {
			Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("DATMovieTool by gdkchan / Modded by VHussian");
            Console.WriteLine("MGS movie.dat subtitle extractor/inserter & xml balancer");
            Console.WriteLine("Version 0.1.5.01");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Working only for MGS4");
            Console.ResetColor();
            Console.Write(Environment.NewLine);
			int currentLinePosition = Console.CursorTop;

            if (args.Length != 4 && args.Length != 5) // Allow for new command
            {
                PrintUsage();
                return;
            }
            else
            {
                MGSGame Game;
                switch (args[1])
                {
                    case "-mgsts": Game = MGSGame.MGSTS; break;
                    case "-mgs3": Game = MGSGame.MGS3; break;
                    case "-mgs4": Game = MGSGame.MGS4; break;
                    default: TextOut.PrintError("Invalid game \"" + args[1] + "\" specified!"); return;
                }

                switch (args[0])
                {
                    case "-e": Extract(args[2], args[3], Game, currentLinePosition); break;
                    case "-i": Insert(args[2], args[3], Game, currentLinePosition); break;
					case "-b": Balance(args[2], args[3], args[4]); break; // New command: -b <listfile> <inputfolder> <outputfolder>
                    default: TextOut.PrintError("Invalid command!"); return;
                }
            }

            Console.Write(Environment.NewLine);
            TextOut.PrintSuccess("Finished!");
        }


        private static void Balance(string ListFile, string InputFolder, string OutputFolder)
        {
            // Read the list file and group entries by XML filename
            var sizeMap = File.ReadAllLines(ListFile)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('\t'))
                .GroupBy(parts => parts[0]) // Group by XML filename
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(parts => uint.Parse(parts[2])).ToList() // Extract sizes per subtitle packet
                );
        
            Directory.CreateDirectory(OutputFolder); // Ensure output folder exists
        
            foreach (var xmlFilePath in Directory.GetFiles(InputFolder, "*.xml"))
            {
                var fileName = Path.GetFileName(xmlFilePath);
                if (!sizeMap.ContainsKey(fileName))
                {
                    TextOut.PrintWarning($"Skipping {fileName}: No size entry found in list.");
                    continue;
                }
        
                var subtitle = GetSubtitle(xmlFilePath);
                var originalSizes = sizeMap[fileName];
        
                // Check if the number of subtitle packets matches the list entries
                if (subtitle.Subtitles.Count != originalSizes.Count)
                {
                    TextOut.PrintError($"Mismatch in {fileName}: {subtitle.Subtitles.Count} packets vs {originalSizes.Count} sizes. Skipping.");
                    continue;
                }
        
                for (int i = 0; i < subtitle.Subtitles.Count; i++)
                {
                    var packet = subtitle.Subtitles[i];
                    var originalTotal = originalSizes[i];
        
                    // Calculate total size of LanguageId=1 texts
                    uint sumL1 = (uint)packet.Texts.Where(t => t.LanguageId == 1).Sum(t => t.TextSize);
                    uint allowedOthers = originalTotal - sumL1;
                    uint currentOthers = (uint)packet.Texts.Where(t => t.LanguageId != 1).Sum(t => t.TextSize);
        
                    if (currentOthers > allowedOthers)
                    {
                        uint excess = currentOthers - allowedOthers;
                        var nonL1Texts = packet.Texts.Where(t => t.LanguageId != 1).ToList();
        
                        foreach (var text in nonL1Texts.OrderByDescending(t => t.TextSize))
                        {
                            if (excess <= 0) break;
        
                            uint reduction = Math.Min(excess, text.TextSize);
                            text.Text = TruncateText(text.Text, text.TextSize - reduction);
                            text.TextSize -= reduction;
                            excess -= reduction;
                        }
                    }
                }
        
                // Serialize to a temporary file with custom settings
                var tempFile = Path.GetTempFileName();
                try
                {
                    var serializer = new XmlSerializer(typeof(MovieSubtitle));
                    
                    // Configure XML writer settings
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8, // Enforce UTF-8 encoding
                        Indent = true, // Optional: pretty formatting
                        OmitXmlDeclaration = false // Ensure <?xml ...?> is included
                    };
        
                    // Suppress default namespaces
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("", "");
        
                    using (var stream = new FileStream(tempFile, FileMode.Create))
                    using (var writer = XmlWriter.Create(stream, settings))
                    {
                        serializer.Serialize(writer, subtitle, namespaces);
                    }
        
                    // Post-process XML to enforce explicit closing tags for <Text> elements
                    string xmlContent = File.ReadAllText(tempFile);
                    xmlContent = Regex.Replace(xmlContent, @"(<Text\b[^>]*)\s*/>", "$1></Text>");
                    File.WriteAllText(Path.Combine(OutputFolder, fileName), xmlContent);
                }
                finally
                {
                    File.Delete(tempFile); // Clean up
                }
            }
        
            TextOut.PrintSuccess("Balancing completed!");
        }
        
        private static string TruncateText(string text, uint newByteLength)
        {
            if (newByteLength == 0)
                return string.Empty; // Explicitly return empty string for TextSize=0
        
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            if (bytes.Length <= newByteLength)
                return text;
        
            // Truncate to valid UTF-8 boundaries
            var truncatedBytes = new byte[newByteLength];
            Array.Copy(bytes, truncatedBytes, newByteLength);
        
            int charCount = Encoding.UTF8.GetCharCount(truncatedBytes, 0, (int)newByteLength);
            int validByteCount = Encoding.UTF8.GetByteCount(
                Encoding.UTF8.GetChars(truncatedBytes, 0, (int)newByteLength)
            );
        
            if (validByteCount < newByteLength)
            {
                truncatedBytes = new byte[validByteCount];
                Array.Copy(bytes, truncatedBytes, validByteCount);
            }
        
            return Encoding.UTF8.GetString(truncatedBytes);
        }
        private static void PrintUsage()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Usage:");
            Console.ResetColor();
            Console.Write(Environment.NewLine);

            Console.WriteLine("tool [command] [game] movie.dat [folder]");
            Console.Write(Environment.NewLine);

            Console.WriteLine("Examples:");
            Console.Write(Environment.NewLine);

            Console.WriteLine("tool -e -mgs4 movie.dat folder  Extracts subtitles from a movie.dat file");
            Console.WriteLine("tool -i -mgs4 movie.dat folder  Creates the movie.dat from a folder");
			Console.WriteLine("tool -b -mgs4 sizeslist.txt input_Folder Output_Folder");
			
        }

        public class MovieSubtitle
        {
            [XmlArrayItem("Subtitle")] // No namespace
            public List<SubtitlePacket> Subtitles;

            public MovieSubtitle()
            {
                Subtitles = new List<SubtitlePacket>();
            }
        }
        
        private static void Extract(string Movie, string Output, MGSGame Game, int currentLinePosition)
        {
            Directory.CreateDirectory(Output);
            MGSText.Initialize();
            using (FileStream Input = new FileStream(Movie, FileMode.Open))
            {
                MovieSubtitle Out = new MovieSubtitle();
                EndianBinaryReader Reader = null;
                switch (Game)
                {
                    case MGSGame.MGSTS:
                        Reader = new EndianBinaryReader(Input, Endian.Big);
                        Game = MGSGame.MGS3;
                        break;
                    case MGSGame.MGS3: Reader = new EndianBinaryReader(Input, Endian.Little); break;
                    case MGSGame.MGS4: Reader = new EndianBinaryReader(Input, Endian.Big); break;
                }
                int Index = 0;
                string currentXmlFile = null; // Track current XML file
        
                while (Input.Position < Input.Length)
                {
                    StreamPacket Packet = StreamPacket.FromStream(Reader, Game);
                    switch (Packet.Type)
                    {
                        case PacketType.Subtitle: 
                            Out.Subtitles.Add((SubtitlePacket)Packet); 
                            break;
                        case PacketType.EndOfStream:
                            string XmlName = string.Format("Subtitle_{0:D5}.xml", Index++);
                            currentXmlFile = XmlName; // Update current file name
                            string FileName = Path.Combine(Output, XmlName);
                            XmlSerializerNamespaces NameSpaces = new XmlSerializerNamespaces();
                            NameSpaces.Add(string.Empty, string.Empty);
                            XmlWriterSettings Settings = new XmlWriterSettings
                            {
                                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                                Indent = true
                            };
                            XmlSerializer Serializer = new XmlSerializer(typeof(MovieSubtitle));
                            using (FileStream OutputStream = new FileStream(FileName, FileMode.Create))
                            {
                                XmlWriter Writer = XmlWriter.Create(OutputStream, Settings);
                                Serializer.Serialize(Writer, Out, NameSpaces);
                            }
                            Out.Subtitles.Clear();
                            break;
                    }
                    // Fixed ReportProgress call with 3 parameters
                    ReportProgress(Input.Position, Input.Length, currentXmlFile, currentLinePosition);
                }
				Console.Write(Environment.NewLine);
				TextOut.PrintSuccess("Subtitles Expotred!");
            }
        }
        
        
		
		private static void Insert(string Movie, string Input, MGSGame Game, int currentLinePosition)
        {
            string[] Files = Directory.GetFiles(Input);
            MGSText.Initialize();
            string NewFile = Path.GetTempFileName();
            FileStream In = new FileStream(Movie, FileMode.Open);
            FileStream Out = new FileStream(NewFile, FileMode.Create);
            Endian Endian = Endian.Default;
            switch (Game)
            {
                case MGSGame.MGSTS:
                    Endian = Endian.Big;
                    Game = MGSGame.MGS3;
                    break;
                case MGSGame.MGS3: Endian = Endian.Little; break;
                case MGSGame.MGS4: Endian = Endian.Big; break;
            }
            EndianBinaryReader Reader = new EndianBinaryReader(In, Endian);
            EndianBinaryWriter Writer = new EndianBinaryWriter(Out, Endian);
            int Index = 0;
            int SubIndex = 0;
            MovieSubtitle Subtitle = GetSubtitle(Files[0]);
            string currentXmlFile = Files[0]; // Track current XML file
        
            while (In.Position < In.Length)
            {
                StreamPacket Packet = StreamPacket.FromStream(Reader, Game);
                switch (Packet.Type)
                {
                    case PacketType.Subtitle: 
                        SubtitlePacket.ToStream(Writer, Subtitle.Subtitles[SubIndex++], Game); 
                        break;
                    case PacketType.EndOfStream:
                        if (++Index < Files.Length) 
                        {
                            Subtitle = GetSubtitle(Files[Index]);
                            currentXmlFile = Files[Index]; // Update current file
                        }
                        SubIndex = 0;
                        break;
                }
                if (Packet.Type != PacketType.Subtitle) 
                    StreamPacket.ToStream(Writer, Packet, Game);
                
                // Fixed ReportProgress call with 3 parameters
                ReportProgress(In.Position, In.Length, currentXmlFile, currentLinePosition);
            }
			Console.Write(Environment.NewLine);
			TextOut.PrintSuccess("Subtitles Impotred!");
			Console.Write(Environment.NewLine);
			TextOut.PrintWarning("Replacing file, please wait...");
            In.Close();
            Out.Close();
            File.Delete(Movie);
            File.Move(NewFile, Movie);
            File.Delete(NewFile);
        }
        

        private static MovieSubtitle GetSubtitle(string FileName)
        {
            XmlSerializer Deserializer = new XmlSerializer(typeof(MovieSubtitle));
            using (FileStream InputStream = new FileStream(FileName, FileMode.Open))
            {
                return (MovieSubtitle)Deserializer.Deserialize(InputStream);
            }
        }

        static bool FirstPercentage = true;
        private static void ReportProgress(
            long Position, 
            long Length, 
            string CurrentFile = null, 
            int currentLinePosition = 0)
        {
            // Calculate progress percentage
            double progressPercentage = (double)Position / Length * 100;
        
            // Clamp currentLinePosition to console buffer height
            int maxLine = Console.BufferHeight - 1;
            int safeLine = Math.Min(currentLinePosition, maxLine);
        
            // Position progress bar
            Console.CursorLeft = 0;
            Console.CursorTop = safeLine;
            Console.Write(new string(' ', Console.WindowWidth)); // Clear the line
            Console.CursorLeft = 0;
        
            // Draw progress bar
            int progressBarWidth = Console.WindowWidth / 3;
            int filledBars = (int)(progressBarWidth * (progressPercentage / 100));
            string progressBar = new string('█', filledBars) + new string(' ', progressBarWidth - filledBars);
            Console.Write($"[{progressBar}] {progressPercentage:F0}%");
        
            // Position file name on the next line (if within buffer)
            if (CurrentFile != null && safeLine + 1 <= maxLine)
            {
                Console.CursorLeft = 0;
                Console.CursorTop = safeLine + 1;
                Console.Write(new string(' ', Console.WindowWidth)); // Clear the line
                Console.CursorLeft = 0;
                Console.Write($"{CurrentFile}");
            }
        }

        public static void ClearLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
