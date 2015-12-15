﻿using System.Collections.Generic;

namespace Pliant.Collections
{
    public class ReadWriteList<T> : List<T>, IList<T>, IReadOnlyList<T>
    {
        public ReadWriteList() : base()
        {
        }

        public ReadWriteList(IList<T> list) : base(list)
        {
        }
    }
}