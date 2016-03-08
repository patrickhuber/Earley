﻿using System;

namespace Pliant.RegularExpressions
{
    public class RegexFactor : RegexNode
    {
        public RegexAtom Atom { get; private set; }

        public RegexFactor(RegexAtom atom)
        {
            Atom = atom;
            _hashCode = ComputeHashCode();
        }
        
        private readonly int _hashCode;

        int ComputeHashCode()
        {
            return HashUtil.ComputeHash(
                    Atom.GetHashCode());
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null)
                return false;
            var factor = obj as RegexFactor;
            if (factor == null)
                return false;
            return factor.Atom.Equals(Atom);
        }

        public override RegexNodeType NodeType
        {
            get { return RegexNodeType.RegexFactor; }
        }
    }

    public class RegexFactorIterator : RegexFactor
    {
        public RegexIterator Iterator { get; private set; }

        public RegexFactorIterator(RegexAtom atom, RegexIterator iterator)
            : base(atom)
        {
            Iterator = iterator;
            _hashCode = ComputeHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null)
                return false;
            var factor = obj as RegexFactor;
            if ((object)factor == null)
                return false;
            return factor.Atom.Equals(Atom);
        }
        
        private readonly int _hashCode ;

        int ComputeHashCode()
        {
            return HashUtil.ComputeHash(
                Atom.GetHashCode(),
                Iterator.GetHashCode());
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override RegexNodeType NodeType
        {
            get { return RegexNodeType.RegexFactorIterator; }
        }
    }
}