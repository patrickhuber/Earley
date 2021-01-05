﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pliant.Builders.Expressions;
using Pliant.Captures;
using Pliant.Grammars;
using Pliant.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace Pliant.Tests.Unit
{
    [TestClass]
    public class ParseEngineLexemeTests
    {
        [TestMethod]
        public void ParseEngineLexemeShouldConsumeWhitespace()
        {
            ProductionExpression 
                S = "S",
                W = "W";

            S.Rule = W | W + S;
            W.Rule = new WhitespaceTerminal();

            var grammar = new GrammarExpression(S, new[] { S, W }).ToGrammar();

            var lexerRule = new GrammarLexerRule(
                "whitespace",
                grammar);

            var input = "\t\r\n\v\f ";
            var lexeme = new ParseEngineLexeme(lexerRule, input.AsCapture(), 0);            
            for (int i = 0; i < input.Length; i++)
                Assert.IsTrue(lexeme.Scan(), $"Unable to recognize input[{i}]");
            Assert.IsTrue(lexeme.IsAccepted());
        }

        [TestMethod]
        public void ParseEngineLexemeShouldConsumeCharacterSequence()
        {
            ProductionExpression sequence = "sequence";
            sequence.Rule = (Expr)'a' + 'b' + 'c' + '1' + '2' + '3';

            var grammar = new GrammarExpression(sequence, new[] { sequence }).ToGrammar();
            var lexerRule = new GrammarLexerRule(new TokenType("sequence"), grammar);
            var input = "abc123";
            var lexeme = new ParseEngineLexeme(lexerRule, input.AsCapture(), 0);
            for (int i = 0; i < input.Length; i++)
                Assert.IsTrue(lexeme.Scan());
            Assert.IsTrue(lexeme.IsAccepted());
        }

        [TestMethod]
        public void ParseEngineLexemeShouldMatchLongestAcceptableTokenWhenGivenAmbiguity()
        {
            var lexemeList = new List<ParseEngineLexeme>();

            ProductionExpression There = "there";
            There.Rule = (Expr)'t' + 'h' + 'e' + 'r' + 'e';
            var thereGrammar = new GrammarExpression(There, new[] { There })
                .ToGrammar();
            var thereLexerRule = new GrammarLexerRule(new TokenType(There.ProductionModel.LeftHandSide.NonTerminal.Value), thereGrammar);
            
            ProductionExpression Therefore = "therefore";
            Therefore.Rule = (Expr)'t' + 'h' + 'e' + 'r' + 'e' + 'f' + 'o' + 'r' + 'e';
            var thereforeGrammar = new GrammarExpression(Therefore, new[] { Therefore })
                .ToGrammar();
            var thereforeLexerRule = new GrammarLexerRule(new TokenType(Therefore.ProductionModel.LeftHandSide.NonTerminal.Value), thereforeGrammar);
            

            var input = "therefore";

            var thereLexeme = new ParseEngineLexeme(thereLexerRule, input.AsCapture(), 0);
            lexemeList.Add(thereLexeme);

            var thereforeLexeme = new ParseEngineLexeme(thereforeLexerRule, input.AsCapture(), 0);
            lexemeList.Add(thereforeLexeme);

            var i = 0;
            for (; i < input.Length; i++)
            {
                var passedLexemes = lexemeList
                    .Where(l => l.Scan())
                    .ToList();

                // all existing lexemes have failed
                // fall back onto the lexemes that existed before
                // we read this character
                if (passedLexemes.Count() == 0)
                    break;

                lexemeList = passedLexemes;
            }

            Assert.AreEqual(i, input.Length);
            Assert.AreEqual(1, lexemeList.Count);
            var remainingLexeme = lexemeList[0];
            Assert.IsNotNull(remainingLexeme);
            Assert.IsTrue(remainingLexeme.IsAccepted());
        }
    }
}