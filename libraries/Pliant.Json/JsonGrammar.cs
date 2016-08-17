﻿using Pliant.Automata;
using Pliant.Builders.Expressions;
using Pliant.Grammars;
using Pliant.LexerRules;
using Pliant.RegularExpressions;

namespace Pliant.Json
{
    public class JsonGrammar : GrammarWrapper
    {
        private static IGrammar _innerGrammar;

        static JsonGrammar()
        {
            ProductionExpression
                Json = "Json",
                Object = "Object",
                Pair = "Pair",
                PairRepeat = "PairRepeat",
                Array = "Array",
                Value = "Value",
                ValueRepeat = "ValueRepeat";

            var number = new NumberLexerRule();
            var @string = String();

            Json.Rule =
                Value;

            Object.Rule =
                '{' + PairRepeat + '}';

            PairRepeat.Rule =
                Pair
                | Pair + ',' + PairRepeat
                | (Expr)null;

            Pair.Rule =
                (Expr)@string + ':' + Value;

            Array.Rule =
                '[' + ValueRepeat + ']';

            ValueRepeat.Rule =
                Value
                | Value + ',' + ValueRepeat
                | (Expr)null;

            Value.Rule = (Expr)
                @string
                | number
                | Object
                | Array
                | "true"
                | "false"
                | "null";

            _innerGrammar = new GrammarExpression(
                Json,
                null,
                new[] { new WhitespaceLexerRule() })
            .ToGrammar();
        }

        public JsonGrammar() 
            : base(_innerGrammar) { }
        
        private static BaseLexerRule String()
        {
            // ["][^"]+["]
            const string pattern = "[\"][^\"]+[\"]";
            return CreateRegexDfa(pattern);
        }

        private static BaseLexerRule CreateRegexDfa(string pattern)
        {
            var regexParser = new RegexParser();
            var regex = regexParser.Parse(pattern);
            var regexCompiler = new RegexCompiler();
            var dfa = regexCompiler.Compile(regex);
            return new DfaLexerRule(dfa, pattern);
        }

    }
}