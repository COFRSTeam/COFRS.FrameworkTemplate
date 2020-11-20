using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSFrameworkInstaller
{
	public class ProfileClassFile
	{
		public string ClassName { get; set; }
		public string FileName { get; set; }
		public string ClassNamespace { get; set; }
		public string SourceClass { get; set; }
		public string DestinationClass { get; set; }

		public override string ToString()
		{
			return ClassName;
		}
	}
}
