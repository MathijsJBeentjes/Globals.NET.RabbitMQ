using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;


namespace Globals.NET.RabbitMQ
{
    public abstract class GlobalReaderBase<T> : GlobalBase<T>
    {
        // ***** Fields *****

        internal T _defaultvalue;
        internal EventingBasicConsumer baseConsumer = null;
        internal EventingBasicConsumer initConsumer = null;
        internal EventingBasicConsumer initValueConsumer = null;

        private bool _initQueueReceiveSetUp = false;
        private readonly object _disposeLock = new object();

        // ***** Constructors *****
        public GlobalReaderBase(string name, T defaultValue, EventHandler<GlobalEventData<T>> handler = null) : base(name)
        {
            GlobalReaderBaseSub(defaultValue, handler);
        }

        public GlobalReaderBase(string world, string name, T defaultValue, EventHandler<GlobalEventData<T>> handler = null) : base(world, name)
        {
            GlobalReaderBaseSub(defaultValue, handler);
        }


        // ***** Events *****
        public event EventHandler<GlobalEventData<T>> DataChanged;


        // ***** Properties *****

        // Returns true if the Global has a value. This can be the default value as well.
        // During initialization this value will be false during some time.
        public bool HasValue { get; set; }

        // True if initialization is done and there is no value assigned yet to it.
        // Only in that case Isdefault = true.
        public bool IsDefault { get; internal set; } = false;

        // Returns true if the Global has a value and it is not the default value
        public bool HasRealValue
        {
            get
            {
                return HasValue && !IsDefault;
            }
        }


        // ***** Methods *****

        // Public

        // Blocks untill intialization is done, and the 'real' (non default) value is obtained of another Global    
        public T WaitForRealValue()
        {
            while (!HasValue || IsDefault) { Thread.Sleep(100); };
            return _data;
        }


        // Internal 

        // Consuming the init value requests as the Global has a non default value.
        internal override void SetupInitQueueReceive()
        {
            if (_initQueueReceiveSetUp)
            {
                // In case it is allready intialized, just quit
                return;
            }

            try
            {
                // code below can take some time, avoid that another SetupInitQueueReceive() is called BEFORE _initQueueReceiveSetUp is set to true
                _initQueueReceiveSetUp = true;

                DeclareInitQueue();
                initConsumer = new EventingBasicConsumer(channel);
                initConsumer.Received += InitConsumer_Received;
                channel.BasicConsume(queue: InitQueue, autoAck: true, consumer: initConsumer);
            }
            catch (Exception e)
            {
                _initQueueReceiveSetUp = false;
                throw new GlobalsException<T>("Exception on setting up the receiving queue for the initial value.", e, Name, _data);
            }
        }

        // Called by the Value getters of the Global(Reader)
        internal T GetValue()
        {
            // Blocks if you try to read a value but there isn't.
            // Waiting for the value to arrive.
            // That can be the default value - if no other globals with a non default value exist, or a 'real' (non-default) value
            // Better is subscribe on the DataChanged event.
            while (!HasValue)
            {
                Thread.Sleep(100);
            }
            return _data;
        }

        // The name of the queue to receive the initial value from another Global
        internal string GetInitValueQueueName
        {
            get
            {
                return InitQueue + "." + ID.ToString();
            }
        }

        // Protected

        // Disposible pattern
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                lock (_disposeLock)
                {
                    if (channel != null && channel.IsOpen)
                    {
                        CloseConsumer(ref baseConsumer, BaseConsumer_Received);
                        CloseConsumer(ref initConsumer, InitConsumer_Received);
                        CloseConsumer(ref initValueConsumer, InitValueConsumer_Received);

                        if (ConsumerCount(InitQueue) == 0)
                        {
                            channel.QueueDelete(InitQueue, true, false);
                        }

                        base.Dispose(disposing);
                    }
                }
            }            
        }


        // Private
        // A request for an inial value is processed.
        // Initial value data is sent to the requesting private queue
        private void InitConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            SendInitData(_data, e.BasicProperties.CorrelationId);
        }

        // Removes eventhandler and closes the consumer
        private void CloseConsumer(ref EventingBasicConsumer consumer, EventHandler<BasicDeliverEventArgs> handler)
        {
            if (consumer != null)
            {
                consumer.Received -= handler;
                consumer = null;
            }
        }

          //  Constructor logic
        private void GlobalReaderBaseSub(T defaultValue, EventHandler<GlobalEventData<T>> handler)
        {
            if (handler != null)
            {
                DataChanged += handler;
            }
            _defaultvalue = defaultValue;

            // Set up the base queue for receiving data
            SetupBaseQueue();

            // Send out a request for the initial value
            GetInitialValue();
        }

        // This is the base queue for receiving the changed values
        private void SetupBaseQueue()
        {
            try
            {
                channel.QueueDeclare(queue: BaseQueueName, durable: true, autoDelete: true);
                channel.QueueBind(queue: BaseQueueName, exchange: "Globals", routingKey: Name);

                baseConsumer = new EventingBasicConsumer(channel);
                baseConsumer.Received += BaseConsumer_Received;

                channel.BasicConsume(queue: BaseQueueName, autoAck: true, consumer: baseConsumer);

            }
            catch (GlobalsException<T>)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GlobalsException<T>("Exception on setting up the base queue for consumption.", e, Name, _data);
            }
        }

        // A value change of the Global has been taken place. Process it.
        private void BaseConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                T newData = System.Text.Json.JsonSerializer.Deserialize<T>(message);

                OnDataChanged(new GlobalEventData<T>()
                {
                    PrevData = _data,
                    Data = newData,
                    isInitialValue = false,
                    isDefault = false,
                    fromSelf = e.BasicProperties.CorrelationId == ID.ToString(),
                });
            }
            catch (GlobalsException<T>)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GlobalsException<T>("Exception on consuming the base queue.", ex, Name, _data);
            }
        }
    
        // Passes the event to DataChanged, in case it is assigned.
        private void OnDataChanged(GlobalEventData<T> e)
        {
            if (Disposing)
            {
                return;
            }

            _data = e.Data;
            HasValue = true;
            IsDefault = e.isDefault;

            try
            {
                DataChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                throw new GlobalsException<T>("Exception on invoking the Data Changed handler.", ex, Name, _data);
            }

            //// As the Global has a value now, it can react on intialization requests from other Globals.
            SetupInitQueueReceive();
        }

        // Consumercount is needed to find out how many subscribers there are on a queue
        private uint ConsumerCount(string queueName)
        {
            var queueDeclareOK = channel.QueueDeclarePassive(queueName);
            return queueDeclareOK.ConsumerCount;
        }

        // This is the queue to receive the initial value via a private queue.
        private void SetupInitValueReceiver()
        {
            channel.QueueDeclare(queue: GetInitValueQueueName, durable: true, autoDelete: true);
            channel.QueueBind(queue: GetInitValueQueueName, exchange: "Globals", routingKey: ID.ToString());

            initValueConsumer = new EventingBasicConsumer(channel);
            initValueConsumer.Received += InitValueConsumer_Received;
   
            channel.BasicConsume(queue: GetInitValueQueueName, autoAck: true, consumer: initValueConsumer);
            var body = Encoding.UTF8.GetBytes("");
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.CorrelationId = ID.ToString();
            properties.ReplyTo = ID.ToString();
        }

        // An inital value is received from another Global
        private void InitValueConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            if (Disposing)
            {
                return;
            }
            else
            {
                lock (_disposeLock)
                {
                    if (!Disposing)
                    {
                        // Remove Consumer
                        CloseConsumer(ref initValueConsumer, InitValueConsumer_Received);

                        // Init Value is received, queue can be removed
                        channel.QueueDelete(GetInitValueQueueName);
                    }
                }
            }

            try
            {
                // InitValue will only be set if it is non-initialized or containing a default value
                if (!HasValue || IsDefault)
                {
                    var message = Encoding.UTF8.GetString(e.Body.ToArray());

                    T data = System.Text.Json.JsonSerializer.Deserialize<T>(message);

                    OnDataChanged(new GlobalEventData<T>() { Data = data, isInitialValue = true, isDefault = false });
                }
            }
            catch (GlobalsException<T>)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GlobalsException<T>("Exception on consuming the initial value queue.", ex, Name, _data);
            }
        }

        // Setup the queue to get the inial value, if there are Globals with an inital value.
        private void GetInitialValue()
        {
            if (Disposing)
            {
                return;
            }
            try
            {
                // Declare init queue
                DeclareInitQueue();

                if (ConsumerCount(InitQueue) == 0)
                {  // No globals with non-default value
                    _data = _defaultvalue;
                    IsDefault = true;
                    HasValue = true;
                    OnDataChanged(new GlobalEventData<T>() { Data = _data, isInitialValue = true, isDefault = true, fromSelf = true });
                }
                else
                {  // Found at least one Global!
                    SetupInitValueReceiver();

                    var body = Encoding.UTF8.GetBytes(ID.ToString());

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.CorrelationId = ID.ToString();
                    properties.ReplyTo = ID.ToString();

                    channel.BasicPublish(exchange: "",
                               routingKey: InitQueue,
                               basicProperties: properties,
                               body: body);
                }
            }
            catch (GlobalsException<T>)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GlobalsException<T>("Exception on getting the initial value.", e, Name, _data);
            }
        }
    }
}