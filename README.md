# Modded DATSpeechTool
If you faced issues with modding Speech.dat using the original tool, have hang issues in-game or some lines not apperaing as they should, use this tool, otherwise you are good to go with the original tool.

---
extract the .SPC from speech.dat with this tool or the original tool
```
DATSpeechTool.exe -xdat scenerio.gcx speech.dat out_folder
```
Note: scenerio.gcx the one from stage>init folder

Use my tool to extract the texts from .SPC to .XML file using this command
```
DATSpeechTool.exe -xspc demo_gencho_hdd.spc demo_gencho_hdd.xml
```
or if you want to extract all texts from all available .SPC in current folder, move DATSpeechTool.exe into that folder and run this command
```
DATSpeechTool.exe -xspcall
```
Generated xml will have each Size in bytes for each SubtitlePacket like this

```
<?xml version="1.0" encoding="utf-8"?>
<SpeechSubtitle>
  <Dialogs>
    <Dialog>
      <Subtitles>
        <Subtitle BaseStartTime="0" TotalTextSize="603">
          <Texts>
            <Text StartTime="69" EndTime="1336" LanguageId="5" TextSize="55">Los Patriots intentan proteger su poder,|sus intereses,</Text>
            <Text StartTime="69" EndTime="1336" LanguageId="4" TextSize="74">I Patriots stanno cercando di proteggere|il loro potere, i loro interessi,</Text>
            <Text StartTime="69" EndTime="1336" LanguageId="3" TextSize="76">Die Patriots versuchen, ihre Macht und|ihre eigenen Interessen zu schützen,</Text>
            <Text StartTime="69" EndTime="1336" LanguageId="2" TextSize="51">En contrôlant les flux d'informations|numériques,</Text>
            <Text StartTime="69" EndTime="1336" LanguageId="1" TextSize="70">The Patriots are trying to protect|their power, their own interests...</Text>
            <Text StartTime="1351" EndTime="2465" LanguageId="5" TextSize="52">controlando la circulación digital de|información.</Text>
            <Text StartTime="1351" EndTime="2465" LanguageId="4" TextSize="51">controllando il flusso digitale delle|informazioni.</Text>
            <Text StartTime="1351" EndTime="2465" LanguageId="3" TextSize="56">indem sie den digitalen Informationsfluss|kontrollieren.</Text>
            <Text StartTime="1351" EndTime="2465" LanguageId="2" TextSize="71">les Patriotes essaient de protéger leur|pouvoir et leurs intérêts...</Text>
            <Text StartTime="1351" EndTime="2465" LanguageId="1" TextSize="47">By controlling the digital flow of information.</Text>
          </Texts>
        </Subtitle>
...etc
```
Replace LanguageId="1" texts with your Subtitles

after editing and saving your custom subtitles

Use the below command to make your texts size equal to original
```
DATSpeechTool.exe -b Speech.file_sizes.txt input_Folder Output_Folder
```
You will have you files ready to import in .SPC from the output folder, copy all the xml files from output folder to the folder that contains the .SPC files 

then run this command
```
DATSpeechTool.exe -ispcall
```

Then run the below command to create scenerio.gcx & speech.dat
```
tool -cdat scenerio.gcx speech.dat in_folder 
```
Note: you have to reinstall the game files in the PS3 or RPCS3 to have update speech.dat working

# Modded DATMovieTool
If you faced issues with original tool with MGS4 demo.dat have hang issues in some cutscenes, use this tool, otherwise you are good to go with the original tool.

Note: this will works only on MGS4
---
extract the subtitles with this tool 
```
DATMovieTool.exe -e -mgs4 demo.dat folder
```
Generated xml will have each Size in bytes for each SubtitlePacket like this

```
<?xml version="1.0" encoding="utf-8"?>
<MovieSubtitle>
  <Subtitles>
    <Subtitle StreamId="4" BaseStartTime="0" TotalTextSize="1322">
      <Texts>
        <Text StartTime="11410" EndTime="12134" LanguageId="5" TextSize="85">Un héroe siempre leal a las llamas de la guerra,|descansa en Outer Heaven. 193X-1999</Text>
        <Text StartTime="11410" EndTime="12134" LanguageId="4" TextSize="86">Un eroe eternamente devoto alle fiamme della guerra,|riposa in Outer Heaven. 193X-1999</Text>
        <Text StartTime="11410" EndTime="12134" LanguageId="3" TextSize="89">Ein Held, stets den Flammen des Krieges treu ergeben,|ruht nun in Outer Heaven. 193X-1999</Text>
        <Text StartTime="11410" EndTime="12134" LanguageId="2" TextSize="85">Un héros à jamais loyal aux flammes de la guerre,|repose en Outer Heaven. 193X-1999</Text>
...etc
```
Replace LanguageId="1" texts with your Subtitles

after editing and saving your custom subtitles

Use the below command to make your texts size equal to original
```
DATMovieTool.exe -b -mgs4 sizeslist.txt input_Folder Output_Folder
```
You will have you files ready to import to demo.dat

However, there is one catch

Generated xml top 2 rows will be like below
```
<?xml version="1.0"?>
<MovieSubtitle xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
```
& Before importing You have to manually change them to
```
<?xml version="1.0" encoding="utf-8"?>
<MovieSubtitle>
```

Original Readme below
----
MGSDATool

Metal Gear Solid 3/4 translation toolkit

This is a toolkit for translating Metal Gear Solid 3: Subsistence and Metal Gear Solid 4: Guns of the Patriots.

Below you can see a list of what each tool does:

DATCodecTool - Extracts texts from the codec.dat file and inserts then back. Works on both MGS3 and MGS4.

DATMovieTool - Extracts subtitles from movie.dat and demo.dat, and can also insert the modified subtitles back. Like the codec tool, works on both MGS3 and MGS4.

DATSpeechTool - Extracts *.spc files from a speech.dat (with a scenerio.gcx from the init folder), and extracts subtitles from each *.spc. It can insert subtitles back into the *.spc, and can re-create the speech.dat aswell. Only works on MGS4, since MGS3 don't have a speech.dat file.

A note about the DATCodecTool and DATMovieTool: It's still unknown where the pointers for the data inside those files (codec.dat, movie.dat and demo.dat) are, therefore if you change the lengths of the texts inside those files, there's a possibility that the game will crash. Due to the fact that those files have paddings (with movie.dat and demo.dat being aligned into 2kb blocks), there's usually a lot of room for expansion. But again, it's recommended that you keep the same lengths, or recalculate the pointers if you know where they are. Keeping the same lengths is the sure-fire to make it work.

Please look carefully the usage instructions of each tool, since each tool is used in a different way.
