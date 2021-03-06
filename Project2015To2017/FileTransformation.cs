﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;
using System.Linq;

namespace Project2015To2017
{
	internal sealed class FileTransformation : ITransformation
	{
		private static readonly IReadOnlyList<string> itemsToProject = new[]
		{
			"None",
			"Content",
			"AdditionalFiles",
			"CodeAnalysisDictionary",
			"ApplicationDefinition",
			"Page",
			"Resource",
			"SplashScreen",
			"DesignData",
			"DesignDataWithDesignTimeCreatableTypes",
			"EntityDeploy",
			"XamlAppDef"
		};

		public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
		{
			XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
			var itemGroups = projectFile
				.Element(nsSys + "Project")
				.Elements(nsSys + "ItemGroup");

			var compileManualIncludes = FindNonWildcardMatchedFiles(projectFolder, itemGroups, "*.cs", nsSys + "Compile");
			var otherIncludes = itemsToProject.SelectMany(x => itemGroups.Elements(nsSys + x));

			var otherNodes = compileManualIncludes
				.Where(i => !i.Elements().Any(IsDependent))
				.RenameAttribute("Include", "Update")
				.ToList();

			var generatedNodes = compileManualIncludes
				.Where(i => i.Elements().SingleOrDefault(IsDependent) != null)
				.GroupBy(i => i.Elements().SingleOrDefault(IsDependent).Value)
				.Select(e => XElementExtensions.CreateNode(
					"Compile",
					new[] { new XAttribute("Update", CreateGeneratedAttrValue(e.Attributes())) },
					new[] { e.Elements().FirstOrDefault(IsDependent) }))
				.ToList();

			definition.ItemsToInclude =
				otherIncludes
					.RenameAttribute("Include", "Update")
					.RenameNode("Content", "None")
				.Concat(otherNodes)
				.Concat(generatedNodes)
				.ToList();

			return Task.CompletedTask;
		}

		private string CreateGeneratedAttrValue(IEnumerable<XAttribute> attributes)
		{
			var attrValue = attributes.FirstOrDefault(a => a.Name.LocalName == "Include")?.Value;
			return attrValue.Remove(attrValue.LastIndexOf('\\') + 1) + "*.generated.cs";
		}

		private bool IsDependent(XElement node)
		{
			return node.Name.LocalName == "DependentUpon" && node.Value.Trim().EndsWith(".tt");
		}

		private static List<XElement> FindNonWildcardMatchedFiles(
			DirectoryInfo projectFolder,
			IEnumerable<XElement> itemGroups,
			string wildcard,
			XName elementName)
		{
			var manualIncludes = new List<XElement>();
			var filesMatchingWildcard = new List<string>();
			foreach (var compiledFile in itemGroups.Elements(elementName))
			{
				var includeAttribute = compiledFile.Attribute("Include");
				if (includeAttribute != null)
				{
					if (!Path.GetFullPath(Path.Combine(projectFolder.FullName, includeAttribute.Value)).StartsWith(projectFolder.FullName))
					{
						Console.WriteLine($"Include cannot be done through wildcard, adding as separate include {compiledFile.ToString()}.");
						manualIncludes.Add(compiledFile);
					}
					else if (compiledFile.Attributes().Count() != 1)
					{
						Console.WriteLine($"Include cannot be done through wildcard, adding as separate include {compiledFile.ToString()}.");
						manualIncludes.Add(compiledFile);
					}
					else if (compiledFile.Elements().Count() != 0)
					{
						var dependentUpon = compiledFile.Element(elementName.Namespace + "DependentUpon");
						if (dependentUpon != null && dependentUpon.Value.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
						{
							// resx generated code file
							manualIncludes.Add(new XElement(
								"Compile",
								new XAttribute("Update", includeAttribute.Value),
								new XElement("DependentUpon", dependentUpon.Value)));

							filesMatchingWildcard.Add(includeAttribute.Value);
						}
						else
						{
							Console.WriteLine($"Include cannot be done through wildcard, adding as separate include {compiledFile.ToString()}.");
							manualIncludes.Add(compiledFile);
						}
					}
					else
					{
						filesMatchingWildcard.Add(includeAttribute.Value);
					}
				}
				else
				{
					Console.WriteLine($"Compile found with no include, full node {compiledFile.ToString()}.");
				}
			}

			var filesInFolder = projectFolder.EnumerateFiles(wildcard, SearchOption.AllDirectories).Select(x => x.FullName).ToArray();
			var knownFullPaths = manualIncludes
				.Select(x => x.Attribute("Include")?.Value)
				.Where(x => x != null)
				.Concat(filesMatchingWildcard)
				.Select(x => Path.GetFullPath(Path.Combine(projectFolder.FullName, x)))
				.ToArray();

			foreach (var nonListedFile in filesInFolder.Except(knownFullPaths))
			{
				if (nonListedFile.StartsWith(Path.Combine(projectFolder.FullName + "\\obj"), StringComparison.OrdinalIgnoreCase))
				{
					// skip the generated files in obj
					continue;
				}

				Console.WriteLine($"File found which was not included, consider removing {nonListedFile}.");
			}

			foreach (var fileNotOnDisk in knownFullPaths.Except(filesInFolder).Where(x => x.StartsWith(projectFolder.FullName, StringComparison.OrdinalIgnoreCase)))
			{
				Console.WriteLine($"File was included but is not on disk: {fileNotOnDisk}.");
			}

			return manualIncludes;
		}
	}
}
