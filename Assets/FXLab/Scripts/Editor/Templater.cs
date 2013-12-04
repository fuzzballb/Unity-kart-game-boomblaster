using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class TemplaterPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
    {
        foreach (string asset in importedAssets.Where(path => path.EndsWith(".template", System.StringComparison.InvariantCultureIgnoreCase)))
        {
            ApplyTemplater(asset);
        }
    }

    private struct Indentation
    {
        public int TabCount;
    }

    private static void ApplyTemplater(string templatePath)
    {
        var templateLines = File.ReadAllLines(templatePath);
        var fileKeywords = templateLines.Where(line => line.StartsWith("#file ")).Select(line =>
        {
            return line.Replace("#file ", string.Empty);
        }).Select(p => p.Split(' ')).ToList();

        var restrictPermutationKeywords = templateLines.Where(line => line.StartsWith("#restrictpermutation ")).Select(line =>
        {
            return line.Replace("#restrictpermutation ", string.Empty);
        }).Select(p => p.Split(new char[] { ' ' }, 2)).ToList();

        var permutationKeywords = templateLines.Where(line => line.StartsWith("#permutations ")).Select(line =>
        {
            return line.Replace("#permutations ", string.Empty);
        }).Select(p => p.Split(' ')).ToList();
        
        var settingParameters = templateLines.Where(line => line.StartsWith("#setting ")).Select(line =>
        {
            return line.Replace("#setting ", string.Empty);
        }).Select(p => p.Split(' ')).ToArray();
       
        var indentSetting = settingParameters.FirstOrDefault(setting => setting[0] == "indentation");
        var allowIndent = indentSetting != null;
        Indentation indentation = new Indentation();
        if (allowIndent)
        {
            indentation.TabCount = int.Parse(indentSetting[1]);
        }

        templateLines = templateLines.Where(line => !line.StartsWith("#file ") &&
                                                    !line.StartsWith("#restrictpermutation ") &&
                                                    !line.StartsWith("#permutations ") && 
                                                    !line.StartsWith("#setting ")).ToArray();

        foreach (var permutation in permutationKeywords)
        {
            var fileName = permutation[0];
            var permutations = GeneratePermutations(permutation.Skip(1).ToArray()).Where(p =>
                {
                    foreach (var r in restrictPermutationKeywords)
                    {
                        var first = r[0];
                        var negate = first.IndexOf('!') == 0;
                        first = first.Replace("!", string.Empty);

                        if (!p.Contains(first))
                            continue;

                        var match = CheckMatch(r[1], p.ToArray());

                        if (!negate && match)
                            return true;
                        else if (negate && !match)
                            return true;
                        return false;
                    }


                    return true;
                });
            foreach (var per in permutations)
            {
                fileKeywords.Add(new string[] { fileName }.Concat(per).ToArray());
            }
        }

        foreach (var parameter in fileKeywords)
        {
            var realKeywords = parameter.Skip(1).ToList();
            var fileNameKeywords = string.Join("_", realKeywords.ToArray());
            var fileName = parameter[0].Replace("*", fileNameKeywords);
            fileName = Directory.GetParent(templatePath) + new string(Path.DirectorySeparatorChar, 1) + fileName;
            GenerateFile(fileName, realKeywords.Select(p => new string[] { p }).ToList(), templateLines, indentation);
        }
    }

    private static void GenerateFile(string fileName, List<string[]> keywords, string[] lines, Indentation indentation)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < lines.Length; ++i)
        {
            var line = lines[i];
            if (line.Trim().StartsWith("#begin "))
            {
                var conditional = line.Trim().Replace("#begin ", string.Empty);
                var doesMatch = CheckMatch(conditional, keywords.Select(keyword => keyword[0]).ToArray());
                if (!doesMatch)
                {
                    var conditionCount = 1;
                    while (conditionCount > 0)
                    {
                        ++i;
                        if (i >= lines.Length)
                            break;
                        if (lines[i].Trim().StartsWith("#end"))
                            conditionCount--;
                        if (lines[i].Trim().StartsWith("#begin"))
                            conditionCount++;
                    }
                }
            }
            else if (line.Trim().StartsWith("#addkeyword "))
            {
                var newKeyword = line.Trim().Replace("#addkeyword ", string.Empty).Split(new char[] { ' ' }, 2);
                var oldKeyword = keywords.FirstOrDefault(k => k[0] == newKeyword[0]);
                if (oldKeyword != null)
                    keywords.Remove(oldKeyword);
                keywords.Add(newKeyword);
            }
            else if (line.Trim().StartsWith("#removekeyword "))
            {
                var keyword = line.Trim().Replace("#addkeyword ", string.Empty);
                var oldKeyword = keywords.FirstOrDefault(k => k[0] == keyword);
                if (oldKeyword != null)
                    keywords.Remove(oldKeyword);
            }
            else if (!line.Trim().StartsWith("#end"))
            {
                var fixedLine = line.Replace("#FileNameNoExtension#", Path.GetFileNameWithoutExtension(fileName))
                                       .Replace("#FileName#", Path.GetFileName(fileName));

                foreach (var keyword in keywords.Where(k => k.Length > 1))
                    fixedLine = fixedLine.Replace("#" + keyword[0] + "#", keyword[1]);

                builder.AppendLine(fixedLine);
            }
        }

        if (indentation.TabCount != 0)
        {
            var indent = 0;
            lines = builder.ToString().Split('\n');

            for (int i = 0; i < lines.Length; ++i)
            {
                var line = lines[i];

                var index = 0;
                for (; index < line.Length; ++index)
                {
                    if (line[index] == '}')
                        indent--;
                    else if (!char.IsWhiteSpace(line[index]))
                        break;
                }

                lines[i] = new string('\t', indent) + line.Trim();

                for (; index < line.Length; ++index)
                {
                    if (line[index] == '}')
                        indent--;
                    else if (line[index] == '{')
                        indent++;
                }
            }

            var lineList = lines.ToList();
            for (int i = 0; i < lineList.Count - 1; )
            {
                if (string.IsNullOrEmpty(lineList[i].Trim()) && string.IsNullOrEmpty(lineList[i + 1].Trim()))
                    lineList.RemoveAt(i);
                else
                    ++i;
            }

            builder = new StringBuilder();
            builder.Append(string.Join("\n", lineList.ToArray()));
        }

        var content = builder.ToString();
        if (File.Exists(fileName) && File.ReadAllText(fileName) == content)
            return;

        File.WriteAllText(fileName, content, ASCIIEncoding.ASCII);
        AssetDatabase.ImportAsset(fileName);
    }

    private static bool CheckMatch(string line, params string[] definedSymbols)
    {
        var index = 0;
        return "true" == CheckMatch(line, ref index, definedSymbols);
    }

    private static string CheckMatch(string line, ref int index, params string[] definedSymbols)
    {
        var symbols = new List<string>();
        string currentSymbol = "";
        for (; index < line.Length; )
        {
            var currentChar = line[index];
            switch (currentChar)
            {
                case '\n':
                case '\r':
                case ' ':
                    {
                        if (currentSymbol.Length > 0)
                        {
                            symbols.Add(currentSymbol);
                            currentSymbol = "";
                        }
                        ++index;
                        break;
                    }
                case '!':
                    {
                        symbols.Add("!");
                        ++index;
                        break;
                    }
                case '(':
                    {
                        index++;
                        symbols.Add(CheckMatch(line, ref index, definedSymbols));
                        break;
                    }
                case ')':
                    {
                        if (currentSymbol.Length > 0)
                        {
                            symbols.Add(currentSymbol);
                            currentSymbol = "";
                        }

                        return CheckConditions(definedSymbols, symbols);
                    }
                default:
                    currentSymbol += currentChar;
                    ++index;
                    break;
            }
        }

        if (currentSymbol.Length > 0)
        {
            symbols.Add(currentSymbol);
            currentSymbol = "";
        }

        return CheckConditions(definedSymbols, symbols);
    }

    private static string CheckConditions(string[] definedSymbols, List<string> symbols)
    {
        var state = true;
        var isAnd = true;
        var isNot = false;
        foreach (var symbol in symbols)
        {
            if (symbol == "!")
                isNot = !isNot;
            else if (symbol == "&&")
            {
                isAnd = true;
                isNot = false;
            }
            else if (symbol == "||")
            {
                isAnd = false;
                isNot = false;
            }
            else
            {
                var transformedSymbol = symbol;

                bool hasSymbol = false;
                if (transformedSymbol == "true")
                    hasSymbol = true;
                else if (transformedSymbol == "false")
                    hasSymbol = false;
                else
                    hasSymbol = definedSymbols.Contains(transformedSymbol);

                if (isNot)
                    hasSymbol = !hasSymbol;

                state = isAnd ? state && hasSymbol : state || hasSymbol;

                isNot = false;
            }
        }
        return state.ToString().ToLower();
    }

    class StringsComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(string[] obj)
        {
            int hash = 0;
            foreach (var o in obj)
                hash ^= o.GetHashCode();
            return hash;
        }
    }

    private static IEnumerable<string[]> GeneratePermutations(string[] set, string[] unset)
    {
        if (unset.Length == 0)
        {
            return new string[][] { set };
        }

        var result = new List<string[]>();
        var newUnset = new List<string>(unset);
        foreach (var val in unset)
        {
            if (newUnset.Count == 0)
                break;

            newUnset.Remove(val);
            var alpha = GeneratePermutations(set, newUnset.ToArray());
            var beta = GeneratePermutations(set.Concat(new string[] { val }).ToArray(), newUnset.ToArray());
            result.AddRange(alpha);
            result.AddRange(beta);
        }
        return result;
    }

    private static IEnumerable<string[]> GeneratePermutations(string[] options)
    {
        return GeneratePermutations(new string[0], options).Distinct(new StringsComparer()).Where(o => o.Length > 0);
    }
}