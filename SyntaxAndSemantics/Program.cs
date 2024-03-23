using System;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxAndSemantics
{
    class Derivation
    {
        static void Main(string[] args)
        {
            bool quit = false;
            do
            {
                Console.WriteLine("* Derivation LHS *");
                Console.Write("\nEnter the sentence (or press 'q' to quit): ");
                string sentence = Console.ReadLine()?.Trim().ToUpper();
                if (sentence == "Q")
                {
                    Console.Write("Are you sure you want to quit? (Y/N): ");
                    string response = Console.ReadLine()?.Trim().ToUpper();
                    if (response == "Y")
                    {
                        Console.WriteLine("\nThank you for trying!");
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                        break;
                    }
                }
                else
                {
                    Derive(sentence);
                }
            } while (!quit);

        }

        static void Derive(string sentence)
        {
            Console.WriteLine("Sentence: " + sentence);
            Console.WriteLine("Output: <assign> =>");

            try
            {
                var container = tokenAnalyze(sentence.Where(c => !char.IsWhiteSpace(c) && (char.IsLetterOrDigit(c) || IsOperator(c))).ToList());
                Console.WriteLine("\t: " + Structure("<assign>", container, 0) + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: (" + ex.Message + ")\n");
            }

            Console.WriteLine();
        }

        static string Structure(string structure, tokenContainer container, int sub)
        {
            if (container.Entries.Count == 0 || string.IsNullOrWhiteSpace(structure))
                return "";

            if (structure.Contains("<assign>"))
                structure = "<id> = <expression>";

            while (container.Entries.Any())
            {
                string sb = "\t: " + structure;
                if (sub > 0)
                {
                    sb += new string(')', sub);
                    if (sub > 1)
                        sb += string.Concat(Enumerable.Repeat(" <cont>", sub - 1));
                }
                Console.WriteLine(sb);

                var entry = container.Entries[0];

                if (structure.Contains("<id>"))
                {
                    if (!(entry is tokenValue) || !((tokenValue)entry).IsExpression)
                        throw new Exception("Invalid expression found.");
                    structure = structure.Replace("<id>", ((tokenValue)entry).Value);

                    container.Entries.RemoveAt(0);
                    if (!container.Entries.Any())
                        break;
                    entry = container.Entries[0];

                    if (!(entry is tokenValue) || ((tokenValue)entry).IsExpression || !structure.Contains(((tokenValue)entry).Value))
                        throw new Exception("Invalid value found.");

                    container.Entries.RemoveAt(0);
                    continue;
                }

                if (structure.Contains("<expression>"))
                {
                    if (entry is tokenValue)
                    {
                        if (!((tokenValue)entry).IsExpression)
                            throw new Exception("'" + ((tokenValue)entry).Value + "' expected.");
                        if (!container.Entries.Any())
                            throw new Exception("Syntax error.");

                        if (container.Entries.Count > 1)
                        {
                            var nextEntry = (tokenValue)container.Entries[1];
                            if (nextEntry.IsExpression)
                                throw new Exception("'" + nextEntry.Value + "' expected.");
                            structure = structure.Replace("<expression>", "<id> " + nextEntry.Value + " <expression>");
                        }
                        else
                        {
                            structure = structure.Replace("<expression>", "<id>");
                        }
                        continue;
                    }

                    var child = (tokenContainer)entry;
                    container.Entries.RemoveAt(0);

                    var opIdx = child.Entries.FindIndex(itm => itm is tokenValue && !((tokenValue)itm).IsExpression);
                    if (opIdx == -1)
                        throw new Exception("Syntax error.");
                    var opValue = (tokenValue)child.Entries[opIdx];
                    structure = structure.Replace("<expression>", "(<id> " + opValue.Value + " <expression>");
                    structure = Structure(structure, child, sub + 1);

                    if (container.Entries.Any())
                    {
                        entry = container.Entries[0];
                        if (!(entry is tokenValue) || ((tokenValue)entry).IsExpression)
                            throw new Exception("Syntax error.");
                        sb = structure + " " + ((tokenValue)entry).Value + " <expression>";
                        structure = sb;
                        container.Entries.RemoveAt(0);
                        continue;
                    }
                }
            }

            if (sub > 0)
                structure += new string(')', sub);
            return structure;
        }

        static tokenContainer tokenAnalyze(List<char> exp)
        {
            var con = new tokenContainer();
            while (exp.Any())
            {
                if (char.IsLetterOrDigit(exp[0]))
                {
                    con.Entries.Add(new tokenValue(exp[0].ToString(), true));
                    exp.RemoveAt(0);
                    continue;
                }
                if (IsOperator(exp[0]))
                {
                    if (exp[0] == '(')
                    {
                        var close = GetCloseParenthesisIndex(exp);
                        if (close == -1)
                            throw new Exception("Parentheses not balance.");

                        var newArray = exp.GetRange(1, close - 1);

                        con.Entries.Add(tokenAnalyze(newArray));
                        exp.RemoveRange(0, close + 1);
                        continue;
                    }
                    con.Entries.Add(new tokenValue(exp[0].ToString(), false));
                    exp.RemoveAt(0);
                }
            }
            return con;
        }

        static int GetCloseParenthesisIndex(List<char> c)
        {
            if (!c.Any())
                return -1;
            var open = new Stack<char>();
            for (int i = 0; i < c.Count; i++)
            {
                if (c[i] == '(')
                {
                    open.Push('(');
                    continue;
                }
                if (c[i] == ')')
                {
                    if (!open.Any())
                        return -1;
                    open.Pop();
                    if (!open.Any())
                        return i;
                }
            }
            return -1;
        }

        static bool IsOperator(char c)
        {
            return "+-*/^=()".Contains(c);
        }
    }

    public abstract class Tokens { }

    public class tokenContainer : Tokens
    {
        public List<Tokens> Entries { get; set; }

        public tokenContainer()
        {
            Entries = new List<Tokens>();
        }
    }

    public class tokenValue : Tokens
    {
        public string Value { get; set; }
        public bool IsExpression { get; set; }

        public tokenValue(string value, bool isExpression)
        {
            Value = value;
            IsExpression = isExpression;
        }
    }
}
