using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using DATSpeechTool.IO.Packet;
using DATSpeechTool.IO;
using System.Linq;
using System.Xml;


public static class BalanceCommand
{
    public static void Process(string sizesListPath, string inputFolder, string outputFolder)
    {
        var sizeMap = ParseSizesList(sizesListPath);
        Directory.CreateDirectory(outputFolder);

        foreach (var xmlFile in Directory.GetFiles(inputFolder, "*.xml"))
        {
            var filename = Path.GetFileName(xmlFile);
            if (!sizeMap.ContainsKey(filename)) continue;

            var targetSizes = sizeMap[filename];
            var subtitle = LoadSubtitle(xmlFile);
            var subtitleIndex = 0;

            foreach (var dialog in subtitle.Dialogs)
            {
                foreach (var sub in dialog.Subtitles)
                {
                    if (subtitleIndex >= targetSizes.Count) break;
                    AdjustSubtitle(sub, targetSizes[subtitleIndex]);
                    subtitleIndex++;
                }
            }

            SaveSubtitle(subtitle, Path.Combine(outputFolder, filename));
        }
    }

    private static Dictionary<string, List<int>> ParseSizesList(string path)
    {
        var map = new Dictionary<string, List<int>>();
        foreach (var line in File.ReadAllLines(path))
        {
            var parts = line.Split('\t');
            var filename = Path.GetFileName(parts[0]);
            var targetSize = int.Parse(parts[2]);

            if (!map.ContainsKey(filename)) map[filename] = new List<int>();
            map[filename].Add(targetSize);
        }
        return map;
    }

    private static Speech.SpeechSubtitle LoadSubtitle(string path)
    {
        var serializer = new XmlSerializer(typeof(Speech.SpeechSubtitle));
		using (var stream = new FileStream(path, FileMode.Open)) // Add parentheses and braces
		{
			return (Speech.SpeechSubtitle)serializer.Deserialize(stream);
		}

    }

    private static void SaveSubtitle(Speech.SpeechSubtitle subtitle, string path)
    {
        var serializer = new XmlSerializer(typeof(Speech.SpeechSubtitle));
        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), Indent = true };
		
		using (var writer = XmlWriter.Create(path, settings))
		{
			serializer.Serialize(writer, subtitle, ns);
		}
    }

    private static void AdjustSubtitle(SubtitlePacket subtitle, int targetTotal)
    {
        var currentTotal = subtitle.Texts.Sum(t => t.TextSize);
        var delta = currentTotal - targetTotal;
        if (delta <= 0) return;

        var nonL1Texts = subtitle.Texts.Where(t => t.LanguageId != 1).ToList();
        foreach (var text in nonL1Texts)
        {
            if (delta <= 0) break;

            var currentSize = text.TextSize;
            var maxReduction = currentSize; // Allow reducing to 0
            var reduction = Math.Min(delta, maxReduction);
            var newSize = currentSize - reduction;

            text.Text = TruncateText(text.Text, newSize);
            text.TextSize = newSize;
            delta -= reduction;
        }
    }

    private static string TruncateText(string text, int targetByteLength)
    {
		if (string.IsNullOrEmpty(text)) // Add this check
			return string.Empty;
			
        var encoding = Encoding.GetEncoding(932); // Shift-JIS for MGS4
        var bytes = encoding.GetBytes(text);
        if (bytes.Length <= targetByteLength) return text;

        var truncated = new byte[targetByteLength];
        Array.Copy(bytes, truncated, targetByteLength);

        // Decode while handling invalid sequences
        try
        {
            return encoding.GetString(truncated);
        }
        catch
        {
            // Fallback: Truncate further until valid
            for (int i = targetByteLength; i > 0; i--)
            {
                try
                {
                    return encoding.GetString(truncated, 0, i);
                }
                catch { }
            }
            return string.Empty;
        }
    }
}