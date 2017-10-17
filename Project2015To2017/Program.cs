﻿using Project2015To2017.Definition;
using Project2015To2017.Writing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: InternalsVisibleTo("Project2015To2017Tests")]

namespace Project2015To2017
{
	class Program
	{
		private static readonly IReadOnlyList<ITransformation> _transformationsToApply = new ITransformation[]
		{
			new ProjectPropertiesTransformation(),
			new ProjectReferenceTransformation(),
			new PackageReferenceTransformation(),
			new AssemblyReferenceTransformation(),
			new FileTransformation(),
			new AssemblyInfoTransformation(),
			new NugetPackageTransformation()
		};

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine($"Please specify a project file.");

				//args = new[] { @"c:\a\b\c.csproj" };
				return;
			}

			var projWriter = new ProjectWriter();
			var propCleaner = new PropertiesCleaner();

			foreach (var proj in args)
			{
				if (!File.Exists(proj))
				{
					Console.WriteLine($"File {proj} could not be found.");
					return;
				}

				XDocument xmlDocument;
				using (var stream = File.Open(proj, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					xmlDocument = XDocument.Load(stream);
				}

				XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
				if (xmlDocument.Element(nsSys + "Project") == null)
				{
					Console.WriteLine($"This is not a VS2015 project file.");
					return;
				}

				var projectDefinition = new Project();

				var fileInfo = new FileInfo(proj);
				var directory = fileInfo.Directory;
				Task.WaitAll(_transformationsToApply.Select(t => t.TransformAsync(xmlDocument, directory, projectDefinition)).ToArray());

				var backupFileName = fileInfo.FullName + ".old";
				if (File.Exists(backupFileName))
				{
					Console.Write($"Transformation succeeded but cannot create backup file. Please delete {backupFileName}.");
					return;
				}
				File.Copy(proj, fileInfo.FullName + ".old");

				projWriter.Write(projectDefinition, fileInfo);
				propCleaner.Clear(proj);
			}
        }
    }
}
