﻿using Pliant.Forest;
using Pliant.Grammars;

namespace Pliant.Charts
{
    public interface IStateFactory
    {
        IDottedRuleRegistry DottedRuleRegistry { get; }

        IState NewState(IProduction production, int position, int origin);

        IState NextState(IState state, IForestNode parseNode = null);
    }
}