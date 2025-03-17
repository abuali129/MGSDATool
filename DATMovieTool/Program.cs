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
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("DATMovieTool by gdkchan / Modded by VHussian");
            Console.WriteLine("MGS movie.dat subtitle extractor/inserter & xml balancer");
            Console.WriteLine("Version 0.1.5.01");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Working only for MGS4");
            Console.ResetColor();
            Console.Write(Environment.NewLine);

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
                    case "-e": Extract(args[2], args[3], Game); break;
                    case "-i": Insert(args[2], args[3], Game); break;
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
                .Where(line => !string.IsNullOrWhiteSpace(line)) // Ignore empty lines
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
                var sizes = sizeMap[fileName];
        
                // Check if the number of subtitle packets matches the list entries
                if (subtitle.Subtitles.Count != sizes.Count)
                {
                    TextOut.PrintError($"Mismatch in {fileName}: {subtitle.Subtitles.Count} packets vs {sizes.Count} sizes. Skipping.");
                    continue;
                }
        
                for (int i = 0; i < subtitle.Subtitles.Count; i++)
                {
                    var packet = subtitle.Subtitles[i];
                    var originalTotal = sizes[i];
        
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
        
                // Serialize to a temporary file and post-process XML
                var tempFile = Path.GetTempFileName();
                try
                {
                    var serializer = new XmlSerializer(typeof(MovieSubtitle));
                    using (var stream = new FileStream(tempFile, FileMode.Create))
                    {
                        serializer.Serialize(stream, subtitle);
                    }
        
                    // Replace self-closing tags with explicit closing tags
                    string xmlContent = File.ReadAllText(tempFile);
                    xmlContent = Regex.Replace(xmlContent, @"(<Text\b[^>]*)\s*/>", "$1></Text>");
                    File.WriteAllText(Path.Combine(OutputFolder, fileName), xmlContent);
                }
                finally
                {
                    File.Delete(tempFile); // Clean up temporary file
                }
            }
        
            TextOut.PrintSuccess("Balancing completed!");
        }

        private static string TruncateText(string text, uint newByteLength)
        {
			if (newByteLength == 0)
				return string.Empty; // Explicitly return empty string for TextSize=0
			byte[] bytes = Encoding.UTF8.GetBytes(text);
            if (bytes.Length <= newByteLength) return text;
        
            var truncatedBytes = new byte[newByteLength];
            Array.Copy(bytes, truncatedBytes, (int)newByteLength); // Cast to int
        
            // Handle partial UTF-8 characters
            int charCount = Encoding.UTF8.GetCharCount(truncatedBytes, 0, (int)newByteLength); // Cast to int
            int validByteCount = Encoding.UTF8.GetByteCount(
                Encoding.UTF8.GetChars(truncatedBytes, 0, (int)newByteLength) // Cast to int
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

        private static void Extract(string Movie, string Output, MGSGame Game)
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
                while (Input.Position < Input.Length)
                {
                    StreamPacket Packet = StreamPacket.FromStream(Reader, Game);

                    switch (Packet.Type)
                    {
                        case PacketType.Subtitle: Out.Subtitles.Add((SubtitlePacket)Packet); break;
                        case PacketType.EndOfStream:
                            string XmlName = string.Format("Subtitle_{0:D5}.xml", Index++);
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

                    ReportProgress((float)Input.Position / Input.Length);
                }
            }
        }

        private static void Insert(string Movie, string Input, MGSGame Game)
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
            while (In.Position < In.Length)
            {
                StreamPacket Packet = StreamPacket.FromStream(Reader, Game);

                switch (Packet.Type)
                {
                    case PacketType.Subtitle: SubtitlePacket.ToStream(Writer, Subtitle.Subtitles[SubIndex++], Game); break;
                    case PacketType.EndOfStream:
                        if (++Index < Files.Length) Subtitle = GetSubtitle(Files[Index]);
                        SubIndex = 0;
                        break;
                }

                if (Packet.Type != PacketType.Subtitle) StreamPacket.ToStream(Writer, Packet, Game);

                ReportProgress((float)In.Position / In.Length);
            }

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
        private static void ReportProgress(float Percentage)
        {
            const int BarSize = 40;
            int Progress = (int)(Percentage * BarSize);

            if (FirstPercentage)
            {
                Console.BackgroundColor = ConsoleColor.DarkGray;
                for (int Index = 0; Index < BarSize; Index++) Console.Write(" ");
                Console.ResetColor();
                Console.CursorTop++;
                FirstPercentage = false;
            }

            Console.CursorTop--;

            if (Percentage > 0)
            {
                Console.CursorLeft = (int)(Percentage * (BarSize - 1));
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write(" ");
                Console.ResetColor();
            }

            Console.CursorLeft = BarSize + 1;
            Console.WriteLine((int)(Percentage * 100) + "%");
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
