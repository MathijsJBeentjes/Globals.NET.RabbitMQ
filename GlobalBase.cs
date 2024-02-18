using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Collections.Generic;

namespace Globals.NET.RabbitMQ
{
    // Structure of the GlobalEventData EventArgs
    public class GlobalEventData<T> : EventArgs
    {
        public T PrevData;
        public T Data;
        // true if the value received is initial. That is: the last value of the global, assigned some moment in the past.
        // false if the value is just changed by another global.
        public bool isInitialValue;
        // If there are no other globals with a value, then the global is assigned a default value.
        public bool isDefault;
        // true if the change comes from the global itself.
        public bool fromSelf;
    }

    // The Main Generic Global Class
    public abstract class GlobalBase<T>: IDisposable
    {
        // ***** Fields *****

        // The field containing the data of type T
        internal T _data;
        internal bool disposed = false;
        // communication object
        internal IModel channel;
        // when true, stop all handlers
        internal bool Disposing = false;

        // unique id used to commmunicate between globals
        private Guid _id = Guid.NewGuid();
        private readonly string _defaultWorld = "GlobalWorld";

        // ***** Constructors *****

        public GlobalBase(string name)
        {
            World = _defaultWorld;
            InitGlobalBase(name);
        }

        // Constructor including a World
        public GlobalBase(string world, string name)
        {
            World = world;
            InitGlobalBase(name);
        }


        // ***** Properties *****

        public string Name { get; set; }
        public string World { get; }
        // A unique identifier of the global.
        internal Guid ID { get { return _id; } }

        // The name of the base queue.
        internal string BaseQueueName
        {
            get
            {
                return World + "." + Name + "." + ID.ToString();
            }
        }

        // ***** Methods *****

        // Public, static

        // Operator, to make life more simple
        public static implicit operator T(GlobalBase<T> g)
        {
            return g._data;
        }

        // Public

        // String representation
        public override string ToString()
        {
            return _data.ToString();
        }

        // Disposible pattern
        public void Dispose()
        {
            // Flag Disposing is used to stop all communication: the object is shutting down.
            Disposing = true;
           // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Internal

        // To Be Overridden
        internal virtual void InternalSetValue(T value)
        {
            // Data is set...
            _data = value;
        }

        // Only for Global(Writer)
        // Is called when a Global has assigned a value.
        internal void SetValue(T value)
        {
            // Data is set...
            InternalSetValue(value);

            // Send it into the Global World!
            SendData(value);

            // As the Global now contains data, via the initqueue below other globals can request the value.
            // But only if it is a (Global)Reader.
            // In this base class the SetupInitQueueReceive is mt. 
            // Globals call this queue on initialization.
            SetupInitQueueReceive();
        }

        // If the Global has name "<Name>", then the queuename for requesting the initial value is "<World>.<Name>.init"
        internal string InitQueue
        {
            get
            {
                return World + "." + Name + ".init";
            }
        }

        // The queue to receive initialisation requests from other globals (same name & type)
        internal void DeclareInitQueue()
        {
            channel.QueueDeclare(queue: InitQueue, exclusive: false, durable: true, autoDelete: true, arguments: new Dictionary<string, object>() { { "x-expires", 1000 } }); 
        }


        // Consuming the init value requests as the Global has a non default value.
        internal virtual void SetupInitQueueReceive()
        { }

        // Sends the Current Value of the Global to one other Global with the same name, but which is not yet initialized.
        // It is a private queue with a queuename equal to the internal ID (Guid) of that global 
        internal void SendInitData(T data, string routingKey)
        {
            try
            {
                var message = System.Text.Json.JsonSerializer.Serialize(data);
                var body = Encoding.UTF8.GetBytes(message);
                IBasicProperties props = channel.CreateBasicProperties();
                props.CorrelationId = ID.ToString();
                channel.BasicPublish(exchange: "Globals",
                                             routingKey: routingKey,
                                             basicProperties: props,
                                             body: body);
            }
            catch (Exception e)
            {
                throw new GlobalsException<T>("Exception on sending the initial data.", e, Name, _data);
            }
        }

        // Protected

        // Disposible pattern
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (channel != null && channel.IsOpen)
                {
                    channel.Close();
                }
                GlobalsServer.Stop(ID);
            }

            disposed = true;
        }


        // Private

        // Constructor logic
        private void InitGlobalBase(string name)
        {
            GlobalsServer.Start(ID, World, name);

            Name = name;

            channel = GlobalsServer.connection.CreateModel();
            channel.ExchangeDeclare(exchange: "Globals", type: "direct", durable: true, autoDelete: true);
        }

        // Sends the changed data to the Global World.
        private void SendData(T data)
        {
            try
            {
                var message = System.Text.Json.JsonSerializer.Serialize(data);
                var body = Encoding.UTF8.GetBytes(message);
                IBasicProperties props = channel.CreateBasicProperties();
                props.CorrelationId = ID.ToString();
                props.Timestamp = new AmqpTimestamp(Convert.ToInt64((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds));
                channel.BasicPublish(exchange: "Globals",
                                             routingKey: Name,
                                             basicProperties: props,
                                             body: body);
            }
            catch (Exception e)
            {
                throw new GlobalsException<T>("Exception on sending the data.", e, Name, _data);
            }
        }
    }
}