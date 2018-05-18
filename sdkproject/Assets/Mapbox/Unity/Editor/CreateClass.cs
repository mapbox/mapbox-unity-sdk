using UnityEditor;
using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Text;

public static class CreateClass
{
	/*
	public static string UsingRef(Type type)
	{
		string usingString = string.Empty;
		switch type
		{
			case()
		}
	}
*/
	public static void Create(string name, Type type)
	{
		// remove whitespace and minus
		string copyPath = "Assets/"+name+".cs";
		Debug.Log("Creating Classfile: " + copyPath);
		if( File.Exists(copyPath) == false )
		{ // do not overwrite
			using (StreamWriter outfile = 
			new StreamWriter(copyPath))
			{
				outfile.WriteLine("using UnityEngine;");
				outfile.WriteLine("using System.Collections;");
				outfile.WriteLine("");
				outfile.WriteLine(string.Format("public class {0} : {1}", name, type.ToString()));
				outfile.WriteLine("{");
				outfile.WriteLine("");
				outfile.WriteLine("}");
			}//File written
		}
		AssetDatabase.Refresh();
	}
}
