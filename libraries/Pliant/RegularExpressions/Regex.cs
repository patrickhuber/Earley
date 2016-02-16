﻿using System;

namespace Pliant.RegularExpressions
{
    public class Regex : RegexNode
    {
        public bool StartsWith { get; private set; }
        public RegexExpression Expression { get; private set; }
        public bool EndsWith { get; private set; }

        public override RegexNodeType NodeType
        {
            get { return RegexNodeType.Regex; }
        }

        public Regex(
            bool startsWith,
            RegexExpression expression,
            bool endsWith)
        {
            StartsWith = startsWith;
            EndsWith = endsWith;
            Expression = expression;
            _hashCode = new Lazy<int>(ComputeHashCode);
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null)
                return false;

            var other = obj as Regex;
            if ((object)null == other)
                return false;

            return other.EndsWith == EndsWith
                && other.StartsWith == StartsWith
                && other.Expression.Equals(Expression);
        }
        
        private readonly Lazy<int> _hashCode;

        public override int GetHashCode()
        {
            return _hashCode.Value;
        }

        private int ComputeHashCode()
        {
            return HashUtil.ComputeHash(
                StartsWith.GetHashCode(),
                Expression.GetHashCode(),
                EndsWith.GetHashCode());
        }
    }
}