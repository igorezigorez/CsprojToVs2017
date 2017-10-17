using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017
{
	public static class XElementExtensions
	{
		public static XElement CreateNode(string name, IEnumerable<XAttribute> attributes = null, IEnumerable<XElement> childNodes = null, string value = null)
		{
			var node = new XElement(name);

			if (childNodes?.Any() == true)
				node.Add(childNodes);
			else if (!string.IsNullOrEmpty(value))
				node.Value = value;
			if (attributes?.Any() == true)
				node.Add(attributes);

			return node;
		}

		public static IEnumerable<XElement> RenameNode(this IEnumerable<XElement> nodes, string from, string to)
		{
			var elements = nodes as XElement[] ?? nodes.ToArray();
			var newNodes = elements
				.Where(n => n.Name.LocalName == from)
				.Select(n => CreateNode(to, n.Attributes(), n.Elements()));

			return elements.Where(n => n.Name.LocalName != from).Concat(newNodes);
		}

		public static IEnumerable<XElement> RenameAttribute(this IEnumerable<XElement> nodes, string from, string to)
		{
			foreach (var node in nodes)
			{
				var oldAtt = node.Attributes().SingleOrDefault(p => p.Name == from);
				if (oldAtt != null)
				{
					XAttribute newAtt = new XAttribute(to, oldAtt.Value);
					node.Add(newAtt);
					oldAtt.Remove();
				}
				yield return node;
			}
		}
	}
}
