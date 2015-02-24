﻿using System;
namespace Pliant
{
    public interface IState
    {
        int Position { get; }
        IProduction Production { get; }
        int Origin { get; }
        ISymbol CurrentSymbol();
        bool IsComplete();
        StateType StateType { get; }
    }
}
