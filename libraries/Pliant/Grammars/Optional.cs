﻿using System.Collections.Generic;

namespace Pliant.Grammars
{
    public class Optional : Grouping, IOptional
    {
        public Optional(IEnumerable<ISymbol> items)
            : base(items) { }

        public override SymbolType SymbolType
        {
            get { return SymbolType.Optional; }
        }
    }
}