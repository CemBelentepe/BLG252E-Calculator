using System;
using System.Collections.Generic;

namespace Calculator
{
    class Token
    {
        public enum TokenType
        {
            OPEN_PAREN, CLOSE_PAREN,
            PLUS, MINUS, STAR, SLASH,
            NUMBER,
            ERROR, EOF
        }

        public TokenType type;
        public string lexeme;

        public Token(TokenType type, string lexeme)
        {
            this.type = type;
            this.lexeme = lexeme;
        }

        public override string ToString()
        {
            return $"Type: {type}, lexeme: {lexeme}";
        }

        public double GetDouble()
        {
            return double.Parse(lexeme);
        }
    }

    class Scanner
    {
        private string source;
        private int startingPos;
        private int currentPos;

        public Scanner(string source)
        {
            this.source = source;
            this.startingPos = 0;
            this.currentPos = 0;
        }

        public List<Token> Scan()
        {
            List<Token> tokens = new List<Token>();

            do
            {
                tokens.Add(ScanToken());
            } while (tokens[tokens.Count - 1].type != Token.TokenType.EOF);

            return tokens;
        }

        private Token ScanToken()
        {
            SkipWhitespace();

            startingPos = currentPos;
            if (IsAtEnd())
                return MakeToken(Token.TokenType.EOF);

            char c = Advance();
            switch (c)
            {
                case '(':
                    return MakeToken(Token.TokenType.OPEN_PAREN);
                case ')':
                    return MakeToken(Token.TokenType.CLOSE_PAREN);
                case '+':
                    return MakeToken(Token.TokenType.PLUS);
                case '-':
                    return MakeToken(Token.TokenType.MINUS);
                case '*':
                    return MakeToken(Token.TokenType.STAR);
                case '/':
                    return MakeToken(Token.TokenType.SLASH);
                default:
                    if (char.IsDigit(c))
                        return NumberLiteral();
                    else
                        return MakeToken(Token.TokenType.ERROR);
            }
        }

        private Token NumberLiteral()
        {
            while (Char.IsDigit(Peek()))
            {
                Advance();
            }
            if (Match('.'))
            {
                while (Char.IsDigit(Peek()))
                {
                    Advance();
                }
            }
            return MakeToken(Token.TokenType.NUMBER);
        }

        private void SkipWhitespace()
        {
            while (Char.IsWhiteSpace(Peek()))
            {
                Advance();
            }
        }

        private char Peek()
        {
            if (currentPos < source.Length)
                return source[currentPos];
            else
                return '\0';
        }

        private char Advance()
        {
            return source[currentPos++];
        }

        private bool Match(char c)
        {
            if (IsAtEnd() || source[currentPos] != c)
            {
                return false;
            }
            else
            {
                currentPos++;
                return true;
            }
        }

        private bool IsAtEnd()
        {
            return currentPos == source.Length;
        }

        public Token MakeToken(Token.TokenType type)
        {
            return new Token(type, source.Substring(startingPos, currentPos - startingPos));
        }
    }

    class Parser
    {
        private List<Token> tokens;
        private int currentToken;
        private bool error;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
            this.currentToken = 0;
            this.error = false;
        }

        public Expr Parse()
        {
            Expr expr = ParseExpr();
            if (error || currentToken + 1 != tokens.Count)
                return null;
            else
                return expr;
        }

        private Expr ParseExpr()
        {
            return Addition();
        }

        private Expr Addition()
        {
            Expr left = Multiplation();
            while (Match(Token.TokenType.PLUS) || Match(Token.TokenType.MINUS))
            {
                Token op = Consumed();
                Expr right = Multiplation();

                left = new ExprBinary(left, right, op);
            }

            return left;
        }

        private Expr Multiplation()
        {
            Expr left = Unary();
            while (Match(Token.TokenType.STAR) || Match(Token.TokenType.SLASH))
            {
                Token op = Consumed();
                Expr right = Unary();

                left = new ExprBinary(left, right, op);
            }

            return left;
        }

        private Expr Unary()
        {
            if (Match(Token.TokenType.PLUS) || Match(Token.TokenType.MINUS))
            {
                Token op = Consumed();
                Expr expr = Primary();
                return new ExprUnary(expr, op);
            }
            else
            {
                return Primary();
            }
        }

        private Expr Primary()
        {
            if (Match(Token.TokenType.OPEN_PAREN))
            {
                Expr expr = ParseExpr();
                if (Match(Token.TokenType.CLOSE_PAREN))
                    return expr;
                else
                {
                    Console.WriteLine($"Expected a ')' after {tokens[currentToken].lexeme}");
                    error = true;
                    return null;
                }
            }
            else if (Match(Token.TokenType.NUMBER))
            {
                return new ExprValue(Consumed());
            }
            else
            {
                Advance();
                Console.WriteLine($"Invalid token {Consumed().lexeme}");
                error = true;
                return null;
            }
        }

        private bool Match(Token.TokenType type)
        {
            if (currentToken < tokens.Count && tokens[currentToken].type == type)
            {
                currentToken++;
                return true;
            }
            else
            {
                return false;
            }
        }

        private Token Consumed()
        {
            return tokens[currentToken - 1];
        }

        private Token Advance()
        {
            return tokens[currentToken++];
        }
    }

    class Value
    {
        public readonly double value;

        public Value(double value)
        {
            this.value = value;
        }

        public static Value operator +(Value left, Value right)
        {
            return new Value(left.value + right.value);
        }

        public static Value operator -(Value left, Value right)
        {
            return new Value(left.value - right.value);
        }

        public static Value operator *(Value left, Value right)
        {
            return new Value(left.value * right.value);
        }

        public static Value operator /(Value left, Value right)
        {
            return new Value(left.value / right.value);
        }

        public static Value operator -(Value val)
        {
            return new Value(-val.value);
        }
    }

    abstract class Expr
    {
        public abstract Value Evaluate();
    }

    class ExprBinary : Expr
    {
        private Expr left;
        private Expr right;
        private Token op;

        public ExprBinary(Expr left, Expr right, Token op)
        {
            this.left = left;
            this.right = right;
            this.op = op;
        }

        public override Value Evaluate()
        {
            switch (op.type)
            {
                case Token.TokenType.PLUS:
                    return left.Evaluate() + right.Evaluate();
                case Token.TokenType.MINUS:
                    return left.Evaluate() - right.Evaluate();
                case Token.TokenType.STAR:
                    return left.Evaluate() * right.Evaluate();
                case Token.TokenType.SLASH:
                    return left.Evaluate() / right.Evaluate();
                default:
                    return null;
            }
        }
    }

    class ExprUnary : Expr
    {
        private Expr expr;
        private Token op;

        public ExprUnary(Expr expr, Token op)
        {
            this.expr = expr;
            this.op = op;
        }

        public override Value Evaluate()
        {
            if (op.type == Token.TokenType.PLUS)
                return expr.Evaluate();
            else if (op.type == Token.TokenType.MINUS)
                return -expr.Evaluate();
            else
                return null;
        }
    }

    class ExprValue : Expr
    {
        private Token token;

        public ExprValue(Token token)
        {
            this.token = token;
        }

        public override Value Evaluate()
        {
            return new Value(token.GetDouble());
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("Enter your calculation\n> ");
                string line = Console.ReadLine();

                if (line == null || line.Length == 0) break;

                Scanner scanner = new Scanner(line);
                List<Token> tokens = scanner.Scan();

                // foreach (var token in tokens)
                //     Console.WriteLine(token);

                Parser parser = new Parser(tokens);
                Expr root = parser.Parse();

                if (root != null)
                {
                    double val = root.Evaluate().value;
                    Console.WriteLine($"= {val}");
                }
                else
                {
                    Console.WriteLine("Invalid Expression");
                }
                Console.WriteLine();
            }
            Console.WriteLine("Terminating...");
        }
    }
}
