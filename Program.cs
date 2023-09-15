// See https://aka.ms/new-console-template for more information
using net.zmau.GTimeLineReader;
using System.Configuration;

string? directoryName = ConfigurationManager.AppSettings["path"];
if(directoryName == null)
{
    Console.WriteLine("No path found");
    return;
}
bool perMonth = ConfigurationManager.AppSettings["perMonth"].Equals("true");
if (perMonth)
{
    TimelineProcessor reader = new TimelineProcessor();
    foreach (string file in Directory.EnumerateFiles(directoryName, "*.json"))
    {
        FileInfo fileInfo = new FileInfo(file);
        Console.WriteLine($"processing file {fileInfo.Name}");
        reader.readJson(file);
        reader.process();
        reader.Write();
    }
}
else
{
    TimelineProcessor reader = new TimelineProcessor();
    foreach (string file in Directory.EnumerateFiles(directoryName, "*.json"))
    {
        FileInfo fileInfo = new FileInfo(file);
        Console.WriteLine($"processing file {fileInfo.Name}");
        reader.readJson(file);
    }
    reader.process();
    reader.Write();
}
Console.ReadLine();