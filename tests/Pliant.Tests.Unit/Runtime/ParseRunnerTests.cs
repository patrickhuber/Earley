﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pliant.Automata;
using Pliant.Builders.Expressions;
using Pliant.Charts;
using Pliant.Ebnf;
using Pliant.Forest;
using Pliant.Grammars;
using Pliant.LexerRules;
using Pliant.Runtime;
using Pliant.Tokens;
using System;
using System.Text;

namespace Pliant.Tests.Unit.Runtime
{
    [TestClass]
    public class ParseRunnerTests
    {
        private GrammarLexerRule _whitespaceRule;
        private GrammarLexerRule _wordRule;

        public ParseRunnerTests()
        {
            _whitespaceRule = CreateWhitespaceRule();
            _wordRule = CreateWordRule();
        }

        private static GrammarLexerRule CreateWhitespaceRule()
        {
            ProductionExpression 
                S = "S", 
                whitespace = "whitespace";

            S.Rule =
                whitespace
                | whitespace + S;
            whitespace.Rule =
                new WhitespaceTerminal();

            var grammar = new GrammarExpression(S, new[] { S, whitespace }).ToGrammar();
            return new GrammarLexerRule(nameof(whitespace), grammar);
        }

        private static GrammarLexerRule CreateWordRule()
        {
            ProductionExpression 
                W = "W", 
                word = "word";

            W.Rule =
                word
                | word + W;
            word.Rule = (Expr)
                new RangeTerminal('a', 'z')
                | new RangeTerminal('A', 'Z')
                | new RangeTerminal('0', '9');

            var wordGrammar = new GrammarExpression(W, new[] { W, word }).ToGrammar();
            return new GrammarLexerRule(nameof(word), wordGrammar);
        }

        [TestMethod]
        public void ParseRunnerShouldParseSimpleWordSentence()
        {
            ProductionExpression S = "S";
            S.Rule =
                _whitespaceRule
                | _whitespaceRule + S
                | _wordRule
                | _wordRule + S;
            var grammar = new GrammarExpression(S, new[] { S }).ToGrammar();
            var input = "this is input";
            var parseEngine = new ParseEngine(grammar);
            RunParse(parseEngine, input);
        }

        [TestMethod]
        public void ParseRunnerShouldIgnoreWhitespace()
        {
            // a <word boundary> abc <word boundary> a <word boundary> a
            const string input = "a abc a a";
            ProductionExpression A = "A";
            A.Rule =
                _wordRule + A
                | _wordRule;
            var grammar = new GrammarExpression(
                A,
                new[] { A },
                new[] {_whitespaceRule })
                .ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            RunParse(parseEngine, input);
        }

        [TestMethod]
        public void ParseRunnerShouldEmitTokenBetweenLexerRulesAndEndOfFile()
        {
            const string input = "aa";
            ProductionExpression S = "S";
            S.Rule = 'a' + S | 'a';
            var grammar = new GrammarExpression(S, new[] { S }).ToGrammar();
            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);

            var chart = GetParseEngineChart(parseEngine);
            Assert.IsTrue(parseRunner.Read());
            Assert.AreEqual(1, chart.EarleySets.Count);
            Assert.IsTrue(parseRunner.Read());
            Assert.AreEqual(3, chart.EarleySets.Count);
        }

        [TestMethod]
        public void ParseRunnerShouldUseExistingMatchingLexemesToPerformMatch()
        {
            const string input = "aaaa";

            ProductionExpression A = "A";
            A.Rule = (Expr)'a' + 'a';
            var aGrammar = new GrammarExpression(A, new[] { A }).ToGrammar();
            var a = new GrammarLexerRule("a", aGrammar);

            ProductionExpression S = "S";
            S.Rule = a + S | a;
            var grammar = new GrammarExpression(S, new[] { S }).ToGrammar();
            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);

            var chart = GetParseEngineChart(parseEngine);
            Assert.IsTrue(parseRunner.Read());
            Assert.AreEqual(1, chart.EarleySets.Count);
            Assert.IsTrue(parseRunner.Read());
            Assert.AreEqual(1, chart.EarleySets.Count);
        }

        [TestMethod]
        public void ParseRunnerWhenNoLexemesMatchCharacterShouldCreateNewLexeme()
        {
            const string input = "aaaa";

            ProductionExpression A = "A", S = "S";

            A.Rule = (Expr)'a' + 'a';
            var aGrammar = new GrammarExpression(A, new[] { A }).ToGrammar();
            var a = new GrammarLexerRule("a", aGrammar);

            S.Rule = a + S | a;
            var grammar = new GrammarExpression(S, new[] { S }).ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);

            var chart = GetParseEngineChart(parseEngine);
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(parseRunner.Read());
            Assert.AreEqual(2, chart.EarleySets.Count);
        }

        [TestMethod]
        public void ParseRunnerShouldEmitTokenWhenIgnoreCharacterIsEncountered()
        {
            const string input = "aa aa";
            ProductionExpression S = "S";

            S.Rule = _wordRule + S | _wordRule;

            var grammar = new GrammarExpression(
                S,
                new[] { S },
                new[] { _whitespaceRule })
                .ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, input);
            var chart = GetParseEngineChart(parseEngine);
            for (int i = 0; i < 2; i++)
                Assert.IsTrue(parseRunner.Read());
            Assert.IsTrue(parseRunner.Read());
            Assert.AreEqual(2, chart.EarleySets.Count);
        }

        [TestMethod]
        public void ParseRunnerShouldEmitTokenWhenCharacterMatchesNextProduction()
        {
            const string input = "aabb";
            ProductionExpression A = "A";
            A.Rule =
                'a' + A
                | 'a';
            var aGrammar = new GrammarExpression(A, new[] { A }).ToGrammar();
            var a = new GrammarLexerRule("a", aGrammar);

            ProductionExpression B = "B";
            B.Rule =
                'b' + B
                | 'b';
            var bGrammar = new GrammarExpression(B, new[] { B }).ToGrammar();
            var b = new GrammarLexerRule("b", bGrammar);

            ProductionExpression S = "S";
            S.Rule = (Expr)
                a + b;
            var grammar = new GrammarExpression(S, new[] { S }).ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            var chart = GetParseEngineChart(parseEngine);
            var parseRunner = new ParseRunner(parseEngine, input);
            for (int i = 0; i < input.Length; i++)
            {
                Assert.IsTrue(parseRunner.Read());
                if (i < 2)
                    Assert.AreEqual(1, chart.Count);
                else if (i < 3)
                    Assert.AreEqual(2, chart.Count);
                else
                    Assert.AreEqual(3, chart.Count);
            }
        }

        [TestMethod]
        public void ParseRunnerGivenIgnoreCharactersWhenOverlapWithTerminalShouldChooseTerminal()
        {
            var input = "word \t\r\n word";

            var endOfLine = new StringLiteralLexerRule(
                Environment.NewLine,
                new TokenType("EOL"));
            ProductionExpression S = "S";
            S.Rule = (Expr)_wordRule + endOfLine + _wordRule;
            var grammar = new GrammarExpression(
                S,
                new[] { S },
                new[] { _whitespaceRule })
                .ToGrammar();

            var parseEngine = new ParseEngine(grammar);
            RunParse(parseEngine, input);
        }

        [TestMethod]
        public void ParseRunnerShouldRunInCompleteIgnoreRulesBeforeMovingToGrammarLexerRules()
        {
            var ebnfGrammar = new EbnfGrammar();
            var parseEngine = new ParseEngine(ebnfGrammar);

            var input = @"
            /* letters and digits */
            letter			~ /[a-zA-Z]/;";
            RunParse(parseEngine, input);
        }

        [TestMethod]
        public void ParseRunnerShouldHandleCleanupOfUnUsedIgnoreLexemes()
        {
            var ebnfGrammar = new EbnfGrammar();
            var parseEngine = new ParseEngine(ebnfGrammar);

            var stringBuilder = new StringBuilder()
            .AppendLine("ws = [ ows ] ; /* white space */")
            .AppendLine("ows = \"_\" ; /* obligatory white space */");

            RunParse(parseEngine, stringBuilder.ToString());

            var chart = parseEngine.Chart;
            Assert.IsTrue(chart.EarleySets.Count > 7);
            var seventhSet = chart.EarleySets[7];
            Assert.IsNotNull(seventhSet);

            Assert.AreEqual(1, seventhSet.Completions.Count);
            var onlyCompletion = seventhSet.Completions[0];
            Assert.IsNotNull(onlyCompletion);

            var parseNode = onlyCompletion.ParseNode as IInternalForestNode;
            var parseNodeAndNode = parseNode.Children[0];
            var tokenParseNode = parseNodeAndNode.Children[0] as ITokenForestNode;
            var token = tokenParseNode.Token;
            Assert.AreEqual(EbnfGrammar.TokenTypes.Identifier, token.TokenType);
            Assert.AreEqual("ows", token.Value);
        }

        private static Chart GetParseEngineChart(ParseEngine parseEngine)
        {
            return new PrivateObject(parseEngine).GetField("_chart") as Chart;
        }

        private static void RunParse(ParseEngine parseEngine, string input)
        {
            var parseRunner = new ParseRunner(parseEngine, input);
            for (int i = 0; i < input.Length; i++)
                Assert.IsTrue(parseRunner.Read(), $"Error parsing at position {i}");
            Assert.IsTrue(parseRunner.ParseEngine.IsAccepted());
        }
    }
}