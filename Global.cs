using System;
using System.Collections.Generic;
using System.Text;

namespace Globals.NET.RabbitMQ
{
    //public class GlobalWriter<T> : GlobalBase<T>
    //{
    //    // As long as the global writer has sent no data,
    //    // It contains the default value.
    //    public GlobalWriter(string name) : base(name) { }
    //    public GlobalWriter(string world, string name) : base(world, name) { }

    //    // A Global Value is the identical with every Global of the same universe, world & name.
    //    // The LastValueSet might however differ from its Value, because it's value is not synced back in case other writers might change it.
    //    // So the Value of a Global Writer is named LastValueSet, to make a distinction between these 2 concepts.
    //    public T LastValueSet
    //    {
    //        get
    //        {
    //            return _data;
    //        }
    //    }

    //    public T Value
    //    {
    //        set
    //        {
    //            SetValue(value);
    //        }
    //    }
    //}

    public class GlobalReader<T> : GlobalReaderBase<T>
    {
        public GlobalReader(string name, T defaultValue = default, EventHandler<GlobalEventData<T>> handler = null) : base(name, defaultValue, handler) { }
        public GlobalReader(string world, string name, T defaultValue = default, EventHandler<GlobalEventData<T>> handler = null) : base(world, name, defaultValue, handler) { }

        public T Value
        {
            get
            {
                return GetValue();
            }
        }
    }

    public class Global<T> : GlobalReaderBase<T>
    {
        public Global(string name, T defaultValue = default, EventHandler<GlobalEventData<T>> handler = null) : base(name, defaultValue, handler) { }
        public Global(string world, string name, T defaultValue = default, EventHandler<GlobalEventData<T>> handler = null) : base(world, name, defaultValue, handler) { }

        public T Value
        {
            get
            {
                return GetValue();
            }
            set
            {
                SetValue(value);
            }
        }

        internal override void InternalSetValue(T value)
        {
            // setting the value...
            base.InternalSetValue(value);

            // it is real assigned, so this is no default, even if the value is the same as the default value
            IsDefault = false;

            // ... so Global is initialized now.
            HasValue = true;
        }
    }
}
