using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DDCAP.AltayCTF
{
    public static class ServiceMapGenerator
    {
        public static string GenerateProjectMap(string rootDir)
        {
            if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) return "";

            StringBuilder map = new StringBuilder();
            map.AppendLine("=== SERVICE SKELETON & DEPENDENCY MAP ===");
            map.AppendLine("This is a structural map of the project. It shows classes, functions, API endpoints, and potential sinks. Use this to understand the architecture without reading all files.");
            
            string[] extensions = { ".py", ".js", ".ts", ".php", ".go", ".cs", ".java", ".rb", ".json" };
            
            string[] files = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
            
            Regex rxStructure = new Regex(@"(app\.(get|post|put|delete|patch|all|route|use)|router\.|@app\.|@\w*RequestMapping|@Get|@Post|@Put|@Delete|@Patch|function\s+\w+|def\s+\w+|class\s+\w+|func\s+\w+|fn\s+\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex rxDanger = new Regex(@"(eval\(|exec\(|system\(|shell_exec|os\.popen|subprocess|SELECT\s+.*FROM|UPDATE\s+.*SET|INSERT\s+INTO|DELETE\s+FROM|fs\.readFile|fs\.writeFile|open\(|process\.exec)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex rxDeps = new Regex(@"^\s*(import\s+|from\s+[\w\.]+\s+import|require\s*\(|require_once\s*\(|include\s+|include_once\s+|using\s+|use\s+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            int fileCount = 0;
            foreach (string file in files)
            {
                if (file.Contains("\\node_modules\\") || file.Contains("\\vendor\\") || file.Contains("\\.git\\") || file.Contains("\\venv\\") || file.Contains("\\env\\") || file.Contains("\\__pycache__\\")) continue;

                string ext = Path.GetExtension(file).ToLowerInvariant();
                bool supported = false;
                foreach (string e in extensions) if (e == ext) supported = true;
                if (!supported && !file.EndsWith("requirements.txt") && !file.EndsWith("go.mod") && !file.EndsWith("Dockerfile") && !file.EndsWith("docker-compose.yml")) continue;

                string relPath = file.Substring(rootDir.Length).TrimStart('\\', '/');
                List<string> highlights = new List<string>();

                try
                {
                    if (ext == ".json")
                    {
                        if (relPath.EndsWith("package.json") || relPath.EndsWith("composer.json"))
                        {
                            highlights.Add("  [Dependency File]");
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (file.EndsWith("requirements.txt") || file.EndsWith("go.mod") || file.EndsWith("Dockerfile") || file.EndsWith("docker-compose.yml"))
                    {
                        highlights.Add("  [Infrastructure/Dependencies File]");
                    }
                    else
                    {
                        List<string> deps = new List<string>();
                        string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string line = lines[i].Trim();
                            if (line.Length == 0 || line.Length > 200) continue; 
                            
                            if (rxDeps.IsMatch(line))
                            {
                                string cleanDep = line.Replace("import ", "").Replace("require(", "").Replace("require_once(", "").Replace(")", "").Replace("from ", "").Replace("'", "").Replace("\"", "").Replace(";", "").Replace("using ", "").Replace("include ", "").Replace("include_once ", "").Replace("use ", "");
                                if (cleanDep.Length > 0 && cleanDep.Length < 60) deps.Add(cleanDep);
                            }
                            else if (rxStructure.IsMatch(line))
                            {
                                highlights.Add("  L" + (i + 1) + ": " + line);
                            }
                            else if (rxDanger.IsMatch(line))
                            {
                                highlights.Add("  [!] L" + (i + 1) + " (SINK/DB): " + line);
                            }
                        }

                        if (deps.Count > 0)
                        {
                            // Clean up and truncate to save tokens
                            string joinedDeps = string.Join(", ", deps);
                            if (joinedDeps.Length > 150) joinedDeps = joinedDeps.Substring(0, 147) + "...";
                            highlights.Insert(0, "  [Dependencies]: " + joinedDeps);
                        }
                    }

                    if (highlights.Count > 0)
                    {
                        fileCount++;
                        map.AppendLine("File: " + relPath);
                        foreach (string h in highlights) map.AppendLine(h);
                        map.AppendLine();
                    }
                }
                catch { }
            }
            
            map.AppendLine(string.Format("Total interesting files mapped: {0}", fileCount));
            map.AppendLine("=========================================");
            return map.ToString();
        }
    }
}
