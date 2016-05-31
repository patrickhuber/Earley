﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pliant.Forest;
using Pliant.Automata;
using Pliant.Builders;
using Pliant.Grammars;
using Pliant.RegularExpressions;
using Pliant.Tokens;

namespace Pliant.Tests.Unit.Forest
{
    [TestClass]
    public class ForestNodeVisitorTests
    {
        private readonly ILexerRule _whitespace;

        private static ILexerRule CreateWhitespaceRule()
        {
            var start = new DfaState();
            var end = new DfaState(true);
            var startToEnd = new DfaTransition(new WhitespaceTerminal(), end);
            var endToEnd = new DfaTransition(new WhitespaceTerminal(), end);
            start.AddTransition(startToEnd);
            end.AddTransition(endToEnd);
            return new DfaLexerRule(start, new TokenType("whitespace"));
        }

        public ForestNodeVisitorTests()
        {
            _whitespace = CreateWhitespaceRule();
        }

        [TestMethod]
        public void NodeVisitorShouldWalkSimpleRegex()
        {
            var regexGrammar = new RegexGrammar();
            var regexParseEngine = new ParseEngine(regexGrammar);
            var regexLexer = new ParseRunner(regexParseEngine, @"[(]\d[)]");
            while (!regexLexer.EndOfStream())
            {
                if (!regexLexer.Read())
                    Assert.Fail("error parsing input at position {0}", regexLexer.Position);
            }
            Assert.IsTrue(regexParseEngine.IsAccepted());

            var nodeVisitorStateManager = new MultiPassForestNodeVisitorStateManager();
            var nodeVisitor = new LoggingNodeVisitor(
                new SinglePassForestNodeVisitorStateManager());
            var root = regexParseEngine.GetParseForestRoot();
            root.Accept(nodeVisitor);
            Assert.AreEqual(31, nodeVisitor.VisitLog.Count);
        }

        [TestMethod]
        public void NodeVisitorShouldEnumerateAllParseTrees()
        {
            ProductionBuilder And = "AND",
                Panda = "Panda",
                AAn = "AAn",
                ShootsLeaves = "ShootsAndLeaves",
                EatsShootsLeaves = "EatsShootsLeaves"
                ;
            And.Definition = (_)'a' + 'n' + 'd';
            var and = new GrammarBuilder(And, new[] { And }).ToGrammar();

            Panda.Definition = (_)'p' + 'a' + 'n' + 'd' + 'a';
            var panda = new GrammarBuilder(Panda, new[] { Panda }).ToGrammar();

            AAn.Definition = (_)'a' | (_)'a' + 'n';
            var aAn = new GrammarBuilder(AAn, new[] { AAn }).ToGrammar();

            ShootsLeaves.Definition =
                (_)"shoots"
                | (_)"leaves";
            var shootsLeaves = new GrammarBuilder(ShootsLeaves, new[] { ShootsLeaves }).ToGrammar();

            EatsShootsLeaves.Definition =
                (_)'e' + 'a' + 't' + 's'
                | (_)'s' + 'h' + 'o' + 'o' + 't' + 's'
                | (_)'l' + 'e' + 'a' + 'v' + 'e' + 's';
            var eatsShootsLeaves = new GrammarBuilder(EatsShootsLeaves, new[] { EatsShootsLeaves }).ToGrammar();

            ProductionBuilder
                S = "S", NP = "NP", VP = "VP", NN = "NN",
                NNS = "NNS", DT = "DT", CC = "CC", VBZ = "VBZ";

            S.Definition =
                NP + VP + '.';
            NP.Definition =
                NN
                | NNS
                | DT + NN
                | NN + NNS
                | NNS + CC + NNS;
            VP.Definition = VBZ + NP
                | VP + VBZ + NNS
                | VP + CC + VP
                | VP + VP + CC + VP
                | VBZ;
            CC.Definition = new GrammarLexerRule(nameof(CC), and);
            DT.Definition = new GrammarLexerRule(nameof(DT), aAn);
            NN.Definition = new GrammarLexerRule(nameof(NN), panda);
            NNS.Definition = new GrammarLexerRule(nameof(NNS), shootsLeaves);
            VBZ.Definition = new GrammarLexerRule(nameof(VBZ), eatsShootsLeaves);

            var grammar = new GrammarBuilder(
                S,
                new[] { S, NP, VP, CC, DT, NN, NNS, VBZ },
                new[] { _whitespace })
                .ToGrammar();
            var sentence = "a panda eats shoots and leaves.";

            var parseEngine = new ParseEngine(grammar);
            var parseRunner = new ParseRunner(parseEngine, sentence);

            while (!parseRunner.EndOfStream())
            {
                Assert.IsTrue(parseRunner.Read(),
                $"Error parsing position: {parseRunner.Position}");
            }
            Assert.IsTrue(parseRunner.ParseEngine.IsAccepted());
        }
    }
}