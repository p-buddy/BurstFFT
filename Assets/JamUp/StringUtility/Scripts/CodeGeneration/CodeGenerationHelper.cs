using System;
using System.Collections.Generic;
using System.Linq;

namespace JamUp.StringUtility
{
    public static class CodeGenerationHelper
    {
        public static void ReplaceTemplatedString(this List<string> lines, string templateString, string generatedString)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                lines[index] = lines[index].Replace(templateString, generatedString);
            }
        }

        public static List<string> GetSection(this List<string> lines, in Section section, bool excludeSectionGuards = true)
        {
            List<string> sectionLines = new List<string>(lines.Count);
            bool inSection = false;
            foreach (string line in lines)
            {
                if (line.Contains(section.SectionClose))
                {
                    inSection = false;
                    if (!excludeSectionGuards)
                    {
                        sectionLines.Add(line);
                    }
                }
                
                if (inSection)
                {
                    sectionLines.Add(line);
                }
                
                if (line.Contains(section.SectionOpen))
                {
                    inSection = true;
                    if (!excludeSectionGuards)
                    {
                        sectionLines.Add(line);
                    }
                }
            }

            return sectionLines;
        }

        public static void RemoveSection(this List<string> lines, in Section section)
        {
            bool insideSectionToDelete = false;
            for (var index = lines.Count - 1; index >= 0; index--)
            {
                if (insideSectionToDelete)
                {
                    insideSectionToDelete = !lines[index].Contains(section.SectionOpen);
                    lines.RemoveAt(index);
                    continue;
                }

                if (lines[index].Contains(section.SectionClose))
                {
                    lines.RemoveAt(index);
                    insideSectionToDelete = true;
                }
            }
        }
        
        public static void RemoveSectionIdentifiers(this List<string> lines, in Section section)
        {
            for (var index = lines.Count - 1; index >= 0; index--)
            {
                if (lines[index].Contains(section.SectionClose) || lines[index].Contains(section.SectionOpen))
                {
                    lines.RemoveAt(index);
                }
            }
        }
        
        public static void ReplaceSectionIdentifiers(this List<string> lines, in Section section, string replacementString)
        {
            for (var index = lines.Count - 1; index >= 0; index--)
            {
                if (lines[index].Contains(section.SectionClose) || lines[index].Contains(section.SectionOpen))
                {
                    lines[index] = replacementString;
                }
            }
        }
        
        public static void AddToBeginningOfSection(this List<string> lines, in Section section, params string[] toAdd)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                if (lines[index].Contains(section.SectionOpen))
                {
                    for (int toAddIndex = toAdd.Length - 1; toAddIndex >= 0; toAddIndex--)
                    {
                        lines.Insert(index + 1, toAdd[toAddIndex]);
                    }

                    index += toAdd.Length;
                }
            }
        }

        public static void AddToEndOfSection(this List<string> lines, in Section section, params string[] toAdd)
        {
            bool insideSection = false;
            string spacing = null;

            for (int index = 0; index < lines.Count; index++)
            {
                if (insideSection)
                {
                    if (lines[index].Contains(section.SectionClose))
                    {
                        for (int toAddIndex = toAdd.Length - 1; toAddIndex >= 0; toAddIndex--)
                        {
                            lines.Insert(index, $"{spacing}{toAdd[toAddIndex]}");
                        }

                        index += toAdd.Length;
                        insideSection = false;
                    }
                }
                
                if (lines[index].Contains(section.SectionOpen))
                {
                    spacing = GetLeadingWhitespace(lines[index]);
                    insideSection = true;
                }
            }
        }

        public static void ReplaceTemplates(this List<string> lines, params TemplateToReplace[] replacements)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                foreach (TemplateToReplace replacement in replacements)
                {
                    lines[index] = lines[index].Replace(replacement.TemplateString, replacement.ReplacementString);
                }
            }
        }
        
        public static void RemoveMultipleEmptyLines(this List<string> lines)
        {
            bool previousWasEmpty = false;
            for (int index = lines.Count - 1; index >= 0; index--)
            {
                bool isEmpty = lines[index] == String.Empty;
                if (isEmpty && previousWasEmpty)
                {
                    lines.RemoveAt(index);
                }
                previousWasEmpty = isEmpty;
            }
        }
        
        public static string GetLeadingWhitespace(string str)
        {
            return str.Replace(str.TrimStart(), "");
        }
    }
}