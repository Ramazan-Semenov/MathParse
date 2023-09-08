using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConsoleApp16
{
    public class Program
    {
        public static void Main()
        {
            /*Regex regex = new Regex(@"\(([^()]+)\)*");

            foreach (Match match in regex.Matches("You id is (1) and your number is (0000(ч)00000)"))
            {
                Console.WriteLine(match.Value);
            }
            /*  Console.WriteLine(Calculator.Calc("(1 + 7)*3")); // 8
              Console.WriteLine(Calculator.Calc("70 - 1")); // 69
              Console.WriteLine(Calculator.Calc("70 % 3")); // 1
              Console.WriteLine(Calculator.Calc("4 / 2")); // 2*/
            Console.WriteLine("Start");

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Parallel.For(0, 1000000, i => { MathParser.Parse("((1+1)*2)*5+((2+2)+1)*2"); });

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);

            Console.WriteLine("stop");
            Console.Read();
        }
        public static class Calculator_2
        {
            // Добавьте отлов ошибок на случай неверных выражений!
            public static double Calc(string Expression) => CSharpScript.EvaluateAsync<double>(Expression).Result;
        }
        public static class Calculator
        {
            // Определим функции для доступных операторов
            private static Dictionary<string, Func<double, double, double>> Operators { get; } = new Dictionary<string, Func<double, double, double>> {
        { "+", (a, b) => a + b },
        { "-", (a, b) => a - b },
        { "/", (a, b) => a / b },
        { "%", (a, b) => a % b },
        { "*", (a, b) => a * b }
    };
            // Регулярное выражение
            // (-?\d+(?:\,\d+)?) - число типа double (может быть с запятой или без)
            // (.) - одиночный символ (наш оператор)
            private static Regex Regex { get; } = new Regex(@"(-?\d+(?:\,\d+)?)(.)(-?\d+(?:\,\d+)?)");

            public static double Calc(string Expression)
            {
                // Очистим нашу строку от "мусора" в лице пробелов и прочих нехороших знаков
                Expression = string.Concat(Expression.Where(x => char.IsDigit(x) || Operators.ContainsKey(char.ToString(x)) || x == ','));

                try
                {
                    Match match = Regex.Match(Expression);

                    // Если все прошло успешно, то:
                    // под индексом 1 лежит первое число
                    // под индексом 2 - оператор
                    // а под индексом 3 - второе число
                    if (match.Success && match.Groups.Count == 4)
                        // Достанем из словаря нужную нам функцию и запустим ее с двумя параметрами
                        return Operators[match.Groups[2].Value](double.Parse(match.Groups[1].Value), double.Parse(match.Groups[3].Value));

                    // Выражение некорректно и не может быть вычислено 
                    throw new EvaluateException();
                }
                catch
                {
                    // Выражение некорректно и не может быть вычислено 
                    throw new EvaluateException();
                }
            }
        }

        public static class MathParser
        {
            public static bool TryParse(string str)
            {
                try
                {
                    Parse(str);
                    return true;
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            public static double Parse(string str)
            {
                // Парсинг функций
                string[] func = { "sin", "cos", "ctan", "tan" };
                for (int i = 0; i < func.Length; i++)
                {
                    Match matchFunc = Regex.Match(str, string.Format(@"{0}\(({1})\)", func[i], @"[1234567890\.\+\-\*\/^%]*"));
                    if (matchFunc.Groups.Count > 1)
                    {
                        string inner = matchFunc.Groups[0].Value.Substring(1 + func[i].Length, matchFunc.Groups[0].Value.Trim().Length - 2 - func[i].Length);
                        string left = str.Substring(0, matchFunc.Index);
                        string right = str.Substring(matchFunc.Index + matchFunc.Length);

                        switch (i)
                        {
                            case 0:
                                return Parse(left + Math.Sin(Parse(inner)) + right);

                            case 1:
                                return Parse(left + Math.Cos(Parse(inner)) + right);

                            case 2:
                                return Parse(left + Math.Tan(Parse(inner)) + right);

                            case 3:
                                return Parse(left + 1.0 / Math.Tan(Parse(inner)) + right);
                        }
                    }
                }

                // Парсинг скобок
                Match matchSk = Regex.Match(str, string.Format(@"\(({0})\)", @"[1234567890\.\+\-\*\/^%]*"));

                if (matchSk.Groups.Count > 1)
                {
                    var x = matchSk.Groups[0];
                    string inner = matchSk.Groups[0].Value.Substring(1, matchSk.Groups[0].Value.Trim().Length - 2);
                    string left = str.Substring(0, matchSk.Index);
                    string right = str.Substring(matchSk.Index + matchSk.Length);
                    return Parse(left + Parse(inner) + right);
                }

                // Парсинг действий
                Match matchMulOp = Regex.Match(str, string.Format(@"({0})\s?({1})\s?({0})\s?", RegexNum, RegexMulOp));
                Match matchAddOp = Regex.Match(str, string.Format(@"({0})\s?({1})\s?({2})\s?", RegexNum, RegexAddOp, RegexNum));
                var match = (matchMulOp.Groups.Count > 1) ? matchMulOp : (matchAddOp.Groups.Count > 1) ? matchAddOp : null;
                if (match != null)
                {
                    string left = str.Substring(0, match.Index);
                    string right = str.Substring(match.Index + match.Length);
                    string val = ParseAct(match).ToString(CultureInfo.InvariantCulture);
                    return Parse(string.Format("{0}{1}{2}", left, val, right));
                }

                // Парсинг числа
                try
                {
                    return double.Parse(str, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    throw new FormatException(string.Format("Неверная входная строка '{0}'", str));
                }
            }

            private const string RegexNum = @"[-]?\d+\.?\d*";
            private const string RegexMulOp = @"[\*\/^%]";
            private const string RegexAddOp = @"[\+\-]";

            private static double ParseAct(Match match)
            {
                double a = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                double b = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

                switch (match.Groups[2].Value)
                {
                    case "+":
                        return a + b;

                    case "-":
                        return a - b;

                    case "*":
                        return a * b;

                    case "/":
                        return a / b;

                    case "^":
                        return Math.Pow(a, b);

                    case "%":
                        return a % b;

                    default:
                        throw new FormatException(string.Format("Неверная входная строка '{0}'", match.Value));
                }
            }
        }
    }

    public static class Calculator
    {
        // Определим функции для доступных операторов
        private static Dictionary<char, Func<double, double, double>> Operators { get; } = new Dictionary<char, Func<double, double, double>> {
        { '+', (a, b) => a + b },
        { '-', (a, b) => a - b },
        { '/', (a, b) => a / b },
        { '%', (a, b) => a % b },
        { '*', (a, b) => a * b }
    };

        public static double Calc(string Expression)
        {
            // Очистим нашу строку от "мусора" в лице пробелов и прочих нехороших знаков
            Expression = string.Concat(Expression.Where(x => char.IsDigit(x) || Operators.ContainsKey(x) || x == ','));

            try
            {
                int begin = 0;
                int end = 0;

                // Заведем переменные для хранения наших чисел и оператора
                string a = string.Empty;
                string b = string.Empty;
                char op = '\0';

                // Считаем переменную a из строки
                while (end < Expression.Length && (char.IsDigit(Expression[end]) || Expression[end] == ','))
                    end++;
                a = Expression.Substring(0, end);

                // Если строка закончилась, значит нам отдали только одно число. Его же и вернем
                if (end == Expression.Length)
                    return double.Parse(a);

                // Считаем оператор
                op = Expression[end++];

                begin = end;

                // Считаем переменную b
                while (end < Expression.Length && (char.IsDigit(Expression[end]) || Expression[end] == ','))
                    end++;
                b = Expression.Substring(begin, end - begin);

                // Достанем из словаря нужную нам функцию и запустим ее с двумя параметрами
                return Operators[op](double.Parse(a), double.Parse(b));
            }
            catch
            {
                // Выражение некорректно и не может быть вычислено 
                throw new EvaluateException();
            }
        }
    }
}