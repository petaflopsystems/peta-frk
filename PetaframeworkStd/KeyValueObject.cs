using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetaframeworkStd
{
    internal class KeyValueObject<K, V, O>
    {
        public K Key
        { get; set; }

        public V Value
        { get; set; }

        public O Object
        { get; set; }

        public KeyValueObject(K _Key, V _Value, O _Object)
        {
            this.Key = _Key;
            this.Value = _Value;
            this.Object = _Object;
        }
        public KeyValueObject()
        { }
    }
}