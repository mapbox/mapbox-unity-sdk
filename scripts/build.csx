#r "System"

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

public bool RunCommand(string cmd, bool showCmd = false, bool logOnErrorOnly = false) {
	using (Process p = new Process()) {
		p.StartInfo = new ProcessStartInfo("cmd", @"/c """ + cmd + "\"") {
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			//WorkingDirectory = Environment.GetEnvironmentVariable("ROOTDIR"),
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};

		StringBuilder sb = new StringBuilder();
		p.OutputDataReceived += (sender, e) => {
			if (!string.IsNullOrWhiteSpace(e.Data)) {
				if (logOnErrorOnly) {
					sb.AppendLine(e.Data);
				} else {
					Console.WriteLine(e.Data);
				}
			}
		};
		p.ErrorDataReceived += (sender, e) => {
			if (!string.IsNullOrWhiteSpace(e.Data)) {
				if (logOnErrorOnly) {
					sb.AppendLine(e.Data);
				} else {
					Console.Error.WriteLine(e.Data);
				}
			}
		};

		p.Start();
		Console.WriteLine("[{0}] process id: {1}", showCmd ? cmd : "process started", p.Id);
		p.BeginOutputReadLine();
		p.BeginErrorReadLine();
		p.WaitForExit();

		//if process failed and logOnErrorOnly==true dump all messages now
		if (logOnErrorOnly && p.ExitCode != 0) { Console.WriteLine(sb.ToString()); }

		Console.WriteLine("[{0}] exit code: {1}", showCmd ? cmd : "process exited", p.ExitCode);
		return 0 == p.ExitCode;
	}
}

public bool GetEnvVariable(string varName, out string val) {
	val = Environment.GetEnvironmentVariable(varName);
	if (string.IsNullOrWhiteSpace(val)) {
		Console.Error.WriteLine($"%{varName}% not set");
		return false;
	}
	return true;
}

string rootDir;
if (!GetEnvVariable("APPVEYOR_BUILD_FOLDER", out rootDir)) { Environment.Exit(1); }
if (!Directory.Exists(rootDir)) {
	Console.Error.WriteLine($"%APPVEYOR_BUILD_FOLDER%: [{rootDir}] does not exist");
	Environment.Exit(1);
}

//work arround: since 0.17 scripcs changes current directory to script folder instead of folder from where script was called
Environment.CurrentDirectory = rootDir;

string commitMessage;
if (!GetEnvVariable("APPVEYOR_REPO_COMMIT_MESSAGE", out commitMessage)) { Environment.Exit(1); }

string commitMessageEx;
if (GetEnvVariable("APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED", out commitMessageEx)) { commitMessage += commitMessageEx; }

string commitAuthor = Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT_AUTHOR");
if (string.IsNullOrWhiteSpace(commitAuthor)) {
	commitAuthor = "no commit author available";
}

string originalCommit = Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT");
string repoName = Environment.GetEnvironmentVariable("APPVEYOR_REPO_NAME");
if (string.IsNullOrWhiteSpace(originalCommit)) {
	originalCommit = "no SHA available";
} else {
	originalCommit = $"https://github.com/{repoName}/commit/{originalCommit}";
}

string raiseErrorOnFailure;
GetEnvVariable("UNITYPACKAGER_RAISE_ERROR_ON_FAILURE", out raiseErrorOnFailure);

bool publishDocs = commitMessage.IndexOf("[publish docs]", StringComparison.InvariantCultureIgnoreCase) >= 0;

bool triggerCloudBuild = commitMessage.IndexOf("[trigger cloudbuild]", StringComparison.InvariantCultureIgnoreCase) >= 0;

string githubToken;
if (publishDocs || triggerCloudBuild) {
	if (!GetEnvVariable("GITHUB_TOKEN", out githubToken)) {
		publishDocs = false;
		triggerCloudBuild = false;
		Console.Error.WriteLine("no %GITHUB_TOKEN% set, cannot publish docs or trigger Unity Cloud Build");
		Environment.Exit(1);
	}
}

string mapboxAccessToken;
if (triggerCloudBuild) {
	if (!GetEnvVariable("MAPBOX_ACCESS_TOKEN", out mapboxAccessToken)) {
		triggerCloudBuild = false;
		Console.Error.WriteLine("no %MAPBOX_ACCESS_TOKEN% set, cannot trigger Unity Cloud Build");
		Environment.Exit(1);
	}
}


Console.WriteLine(string.Format("GITHUB_TOKEN: {0} set", string.IsNullOrWhiteSpace(githubToken) ? "not": "is"));
Console.WriteLine($"%UNITYPACKAGER_RAISE_ERROR_ON_FAILURE%: {raiseErrorOnFailure}");
Console.WriteLine($"%APPVEYOR_BUILD_FOLDER%: {rootDir}");
Console.WriteLine($"%APPVEYOR_REPO_COMMIT_MESSAGE%: {commitMessage}");
Console.WriteLine($"publish docs: {publishDocs}");
Console.WriteLine($"trigger Unity Cloud Build: {triggerCloudBuild}");


//---------- trigger Unity Cloud Build: to this before generating docs to prevent some intermediate file to be part of this
if (!triggerCloudBuild) {
	Console.WriteLine("not triggering Unity Cloud Build");
} else {
	try {
		string cloudBuildRepo = "mapbox/mapbox-sdk-unity";
		Console.WriteLine("about to trigger Unity Cloud Build ...");
		string projectDir = Path.Combine(rootDir, "sdkproject");
		Console.WriteLine($"sdkproject directory: {projectDir}");

		Environment.CurrentDirectory = projectDir;
		if(!RunCommand("del /F /S /Q .gitignore")){
			Console.Error.WriteLine("could not delete .gitignore");
			Environment.Exit(1);
		}

		Environment.CurrentDirectory = rootDir;
		List<string> cmds = new List<string>(new string[]{
			"git rm -r --cached sdkproject",
			"git commit -m \"Removed sdkproject from repository\""
		});
		foreach (var cmd in cmds) {
			if (!RunCommand(cmd)) {
				Console.Error.WriteLine("triggering Unity Cloud Build failed");
				Environment.Exit(1);
			}
		}

		Environment.CurrentDirectory = projectDir;
		cmds = new List<string>(new string[]{
			"git init .",
			"mkdir Assets\StreamingAssets",
			$"echo {mapboxAccessToken} > Assets\StreamingAssets\MapboxAccess.text",
			"git add .",
			$"git commit -m \"pushed via [{originalCommit}] by [{commitAuthor}]\"",
			$"git remote add origin https://{githubToken}@github.com/{cloudBuildRepo}.git",
			"git checkout -b CloudBuild",
			"git push -f origin CloudBuild"
		});
		foreach (var cmd in cmds) {
			if (!RunCommand(cmd)) {
				Console.Error.WriteLine("triggering Unity Cloud Build failed");
				Environment.Exit(1);
			}
		}
	} finally {
		Environment.CurrentDirectory = rootDir;
	}
}


//---------- documentation
Console.WriteLine("downloading docfx ...");
if (!RunCommand("powershell Invoke-WebRequest https://github.com/dotnet/docfx/releases/download/v2.14.1/docfx.zip -OutFile docfx.zip", true, true)) {
	Console.Error.WriteLine("could not download docfx");
	Environment.Exit(1);
}

Console.WriteLine("extracting docfx ...");
if (!RunCommand("7z x docfx.zip -aoa -o%CD%\\docfx | %windir%\\system32\\find \"ing archive\"", true)) {
	Console.Error.WriteLine("could not extract docfx");
	Environment.Exit(1);
}


Console.WriteLine("building docs ....");

if (!RunCommand(@"docfx documentation\docfx_project\docfx.json", true)) {
	Console.Error.WriteLine("generating docs failed");
	Environment.Exit(1);
}
Console.WriteLine("docs successfully generated");


if (!publishDocs) {
	Console.WriteLine("not publishing docs");
} else {
	try {
		Console.WriteLine("about to publish docs...");
		string docsDir = Path.Combine(rootDir, "documentation", "docfx_project", "_site");
		Console.WriteLine($"docs directory: {docsDir}");
		Environment.CurrentDirectory = docsDir;
		List<string> cmds = new List<string>(new string[]{
			"git init .",
			"git add .",
			$"git commit -m \"pushed via [{originalCommit}] by [{commitAuthor}]\"",
			$"git remote add origin https://{githubToken}@github.com/{repoName}.git",
			"git checkout -b gh-pages",
			"git push -f origin gh-pages"
		});
		foreach (var cmd in cmds) {
			if (!RunCommand(cmd)) {
				Console.Error.WriteLine("publishing docs failed");
				Environment.Exit(1);
			}
		}
	} finally {
		Environment.CurrentDirectory = rootDir;
	}
}



//---------- unitypackage
Console.WriteLine("creating unitypackage ...");
if (!RunCommand(@"UnityPackager.exe unitypackage.config sdkproject\Assets\ mapbox-unity-sdk_LATEST.unitypackage", logOnErrorOnly: true)) {
	Console.Error.WriteLine("could not create unitypackage");
	Environment.Exit(1);
}


//---------- finish
Console.WriteLine("====== summary =======");
Console.WriteLine("  * docs created successfully");
Console.WriteLine(string.Format("  * docs {0}", publishDocs ? "published successfully" : "not published"));
Console.WriteLine(string.Format("  * Unity Cloud Build {0}", triggerCloudBuild ? "triggered" : "not triggered"));
if (raiseErrorOnFailure == "0") {
	Console.WriteLine("  * unitypackage created, but %UNITYPACKAGER_RAISE_ERROR_ON_FAILURE% was set to '0'!!!!!");
	Console.WriteLine("  * unitypackage probably won't import into Unity !!!!!!!");
} else {
	Console.WriteLine("  * unitypackage created successfully");
}
Console.WriteLine("====== build script: DONE =======");
