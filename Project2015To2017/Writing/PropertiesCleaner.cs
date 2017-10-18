using System;
using System.IO;

namespace Project2015To2017.Writing
{
    internal sealed class PropertiesCleaner
	{
        public void Clear(string projectpath)
        {
	        var directoryPath = projectpath.Remove(projectpath.LastIndexOf('\\')) + "\\Properties";

	        try
	        {
				File.Delete(directoryPath + "\\AssemblyInfo.cs");
				Directory.Delete(directoryPath);
	        }
	        catch (Exception e)
	        {
		        Console.WriteLine("error deleting Properties directory");
	        }
        }
    }
}
