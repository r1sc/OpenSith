using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDCogParser
{
    public enum TokenType 
    { 
        LineEnd,
        Equals,
        Colon,
        Semi,
        LParen,
        RParen,
        Greater,
        Lesser,
        Plus,
        Minus,
        Comma,
        Text,
        Int,
        Float,
        Hex
    }

    public class Token
    {
        public TokenType TokenType { get; set; }
        public string Text { get; set; }

        public Token(TokenType tokenType, string text)
        {
            TokenType = tokenType;
            Text = text;
        }

        public override string ToString()
        {
            return Enum.GetName(typeof(TokenType), TokenType) + "=" + Text;
        }
    }

    public static class Lexer
    {
        public static Queue<Token> Tokenize(string input)
        {
            var tokens = new Queue<Token>();
            void Push(TokenType tokenType, string text = null)
            {
                tokens.Enqueue(new Token(tokenType, text));
            }

            int i = 0;
            bool PeekMatch(char c, int k)
            {
                if ((i+k) == input.Length)
                    return false;
                return input[i+k] == c;
            }

            bool PeekMatchStr(string str)
            {
                for (int k = 0; k < str.Length; k++)
                {
                    if (!PeekMatch(str[k], k))
                        return false;
                }
                return true;
            }
            
            for (i = 0; i < input.Length; i++)
            {
                if (input[i] == ' ' || input[i] == '\t' || input[i] == '\r')
                    continue;
                else if (input[i] == '#' || PeekMatchStr("//"))
                {
                    while (i < input.Length && input[i] != '\n')
                    {
                        i++;
                    }
                }

                else if (input[i] == '\n')
                    Push(TokenType.LineEnd);
                
                else if (input[i] == '=')
                    Push(TokenType.Equals);

                else if (input[i] == ':')
                    Push(TokenType.Colon);

                else if (input[i] == ';')
                    Push(TokenType.Semi);

                else if (input[i] == '(')
                    Push(TokenType.LParen);

                else if (input[i] == ')')
                    Push(TokenType.RParen);

                else if (input[i] == '>')
                    Push(TokenType.Greater);

                else if (input[i] == '<')
                    Push(TokenType.Lesser);

                else if (input[i] == '+')
                    Push(TokenType.Plus);

                else if (input[i] == '-')
                    Push(TokenType.Minus);

                else if (input[i] == ',')
                    Push(TokenType.Comma);

                else if (char.IsLetter(input[i]))
                {
                    var sb = new StringBuilder();
                    while (true)
                    {
                        if (i == input.Length)
                            break;
                        else if (!char.IsLetterOrDigit(input[i]) && input[i] != '_' && input[i] != '.')
                            break;
                        sb.Append(input[i]);
                        i++;
                    }
                    i--;
                    Push(TokenType.Text, sb.ToString());
                }

                else if (char.IsDigit(input[i]))
                {
                    var sb = new StringBuilder();
                    if (PeekMatchStr("0x"))
                    {
                        i += 2;
                        while (true)
                        {
                            if (i == input.Length)
                                break;
                            else if (!char.IsDigit(input[i]))
                                break;
                            sb.Append(input[i]);
                            i++;
                        }
                        Push(TokenType.Hex, sb.ToString());

                    }
                    else
                    {
                        while (true)
                        {
                            if (i == input.Length)
                                break;
                            else if (!char.IsDigit(input[i]))
                                break;
                            sb.Append(input[i]);
                            i++;
                        }
                        if (input[i] == '.')
                        {
                            sb.Append('.');
                            i++;
                            while (true)
                            {
                                if (i >= input.Length)
                                    break;
                                else if (!char.IsDigit(input[i]))
                                    break;
                                sb.Append(input[i]);
                                i++;
                            }
                            i--;
                            Push(TokenType.Float, sb.ToString());
                        }
                        else
                        {
                            i--;
                            Push(TokenType.Int, sb.ToString());
                        }
                    }
                }
            }
            return tokens;
        }
    }
}
