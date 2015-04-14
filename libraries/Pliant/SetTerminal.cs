﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pliant
{
    public class SetTerminal : ITerminal
    {
        ISet<char> _characterSet;

        public SetTerminal(params char[] characters)
            : this(new HashSet<char>(characters))
        {            
        }

        public SetTerminal(ISet<char> characterSet)
        {
            _characterSet = characterSet;
        }

        public bool IsMatch(char character)
        {
            return _characterSet.Contains(character);
        }

        public SymbolType SymbolType { get { return SymbolType.Terminal; } }
    }
}