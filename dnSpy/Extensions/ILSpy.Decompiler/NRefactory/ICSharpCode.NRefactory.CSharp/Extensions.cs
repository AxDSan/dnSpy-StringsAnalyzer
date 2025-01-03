
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	static class Extensions
	{
		public static T WithAnnotation<T>(this T node, object annotation) where T : AstNode
		{
			if (annotation != null)
				node.AddAnnotation(annotation);
			return node;
		}

		public static IEnumerable<(int, T)> WithIndex<T>(this ICollection<T> source)
		{
			int index = 0;
			foreach (var item in source)
			{
				yield return (index, item);
				index++;
			}
		}
	}
}
