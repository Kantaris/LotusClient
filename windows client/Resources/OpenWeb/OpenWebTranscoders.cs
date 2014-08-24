using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
namespace OpenWeb
{
	public class OpenWebTranscoders : System.IDisposable
	{
		internal System.Collections.Generic.Dictionary<string, TranscoderTuple> m_Importers = new System.Collections.Generic.Dictionary<string, TranscoderTuple>();
		internal System.Collections.Generic.Dictionary<string, TranscoderTuple> m_Exporters = new System.Collections.Generic.Dictionary<string, TranscoderTuple>();
		internal bool hasImporters
		{
			get
			{
				return this.m_Importers != null && this.m_Importers.Count > 0;
			}
		}
		internal bool hasExporters
		{
			get
			{
				return this.m_Exporters != null && this.m_Exporters.Count > 0;
			}
		}
		internal OpenWebTranscoders()
		{
		}
		internal string[] getImportFormats()
		{
			this.EnsureTranscoders();
			string[] result;
			if (!this.hasImporters)
			{
				result = new string[0];
			}
			else
			{
				string[] array = new string[this.m_Importers.Count];
				this.m_Importers.Keys.CopyTo(array, 0);
				result = array;
			}
			return result;
		}
		internal string[] getExportFormats()
		{
			this.EnsureTranscoders();
			string[] result;
			if (!this.hasExporters)
			{
				result = new string[0];
			}
			else
			{
				string[] array = new string[this.m_Exporters.Count];
				this.m_Exporters.Keys.CopyTo(array, 0);
				result = array;
			}
			return result;
		}
		public override string ToString()
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			stringBuilder.AppendLine("IMPORT FORMATS");
			string[] importFormats = this.getImportFormats();
			for (int i = 0; i < importFormats.Length; i++)
			{
				string arg = importFormats[i];
				stringBuilder.AppendFormat("\t{0}\n", arg);
			}
			stringBuilder.AppendLine("\nEXPORT FORMATS");
			string[] exportFormats = this.getExportFormats();
			for (int j = 0; j < exportFormats.Length; j++)
			{
				string arg2 = exportFormats[j];
				stringBuilder.AppendFormat("\t{0}\n", arg2);
			}
			return stringBuilder.ToString();
		}
		public bool ImportTranscoders(string sAssemblyPath)
		{
			bool result;
			try
			{
				if (!System.IO.File.Exists(sAssemblyPath))
				{
					bool flag = false;
					result = flag;
					return result;
				}
				if (!CONFIG.bRunningOnCLRv4)
				{
					throw new System.Exception("Not reachable.");
				}
				System.Reflection.Assembly assembly = System.Reflection.Assembly.UnsafeLoadFrom(sAssemblyPath);
			}
			catch (System.Exception var_2_36)
			{
				bool flag = false;
				result = flag;
				return result;
			}
			result = true;
			return result;
		}
		private void ScanPathForTranscoders(string sPath)
		{
			try
			{
				if (System.IO.Directory.Exists(sPath))
				{
					bool boolPref = OpenWebApplication.Prefs.GetBoolPref("OpenWeb.debug.extensions.verbose", false);
					if (boolPref)
					{
					}
					System.IO.FileInfo[] files = new System.IO.DirectoryInfo(sPath).GetFiles("*.dll");
					System.IO.FileInfo[] array = files;
					for (int i = 0; i < array.Length; i++)
					{
						System.IO.FileInfo fileInfo = array[i];
						if (!Utilities.IsNotExtension(fileInfo.Name))
						{
							if (boolPref)
							{
							}
							try
							{
								if (!CONFIG.bRunningOnCLRv4)
								{
									throw new System.Exception("Not reachable");
								}
								System.Reflection.Assembly assembly = System.Reflection.Assembly.UnsafeLoadFrom(fileInfo.FullName);
							}
							catch (System.Exception eX)
							{
								OpenWebApplication.LogAddonException(eX, "Failed to load " + fileInfo.FullName);
							}
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				OpenWebApplication.DoNotifyUser(string.Format("[OpenWeb] Failure loading Transcoders: {0}", ex.Message), "Transcoders Load Error");
			}
		}
		private void EnsureTranscoders()
		{
		}
		public TranscoderTuple GetExporter(string sExportFormat)
		{
			this.EnsureTranscoders();
			TranscoderTuple result;
			if (this.m_Exporters == null)
			{
				result = null;
			}
			else
			{
				TranscoderTuple transcoderTuple;
				if (!this.m_Exporters.TryGetValue(sExportFormat, out transcoderTuple))
				{
					result = null;
				}
				else
				{
					result = transcoderTuple;
				}
			}
			return result;
		}
		private static bool AddToImportOrExportCollection(System.Collections.Generic.Dictionary<string, TranscoderTuple> oCollection, System.Type t)
		{
			bool result = false;
			ProfferFormatAttribute[] array = (ProfferFormatAttribute[])System.Attribute.GetCustomAttributes(t, typeof(ProfferFormatAttribute));
			if (array != null && array.Length > 0)
			{
				result = true;
				ProfferFormatAttribute[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					ProfferFormatAttribute profferFormatAttribute = array2[i];
					if (!oCollection.ContainsKey(profferFormatAttribute.FormatName))
					{
						oCollection.Add(profferFormatAttribute.FormatName, new TranscoderTuple(profferFormatAttribute.FormatDescription, t));
					}
				}
			}
			return result;
		}
		public void Dispose()
		{
			if (this.m_Exporters != null)
			{
				this.m_Exporters.Clear();
			}
			if (this.m_Importers != null)
			{
				this.m_Importers.Clear();
			}
			this.m_Importers = (this.m_Exporters = null);
		}
	}
}
