﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Pliant.Languages.Pdl
{
    public enum PdlNodeType
    {
        PdlDefinition,
        PdlDefinitionConcatenation,
        PdlBlockRule,
        PdlBlockSetting,
        PdlBlockLexerRule,
        PdlRule,
        PdlSetting,
        PdlLexerRule,
        PdlExpression,
        PdlExpressionAlteration,
        PdlTerm,
        PdlTermConcatenation,
        PdlFactorIdentifier,
        PdlFactorLiteral,
        PdlFactorRegex,
        PdlFactorOptional,
        PdlFactorGrouping,
        PdlFactorRepetition,
        PdlSettingIdentifier,
        PdlQualifiedIdentifier,
        PdlQualifiedIdentifierConcatenation,
        PdlLexerRuleTerm,
        PdlLexerRuleTermConcatenation,
        PdlLexerRuleFactorLiteral,
        PdlLexerRuleFactorRegex,
        PdlLexerRuleExpression,
        PdlLexerRuleExpressionAlteration,
        PdlExpressionEmpty,
    }
}
