using System;
using RabbitMQ.Client;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Generic;

namespace Globals.NET.RabbitMQ
{
    // Static Class for communication with the (RabbitMQ) Server
    public static class GlobalsServer
    {
        public static RabbitMQSettings Settings = new RabbitMQSettings();

        // RabbitMQ Communication objects
        internal static ConnectionFactory factory;
        internal static IConnection connection;
        private static IModel channel;
       
        // Start/Stop logic
        private static int _started = 0;
        // A lock to keep things simple
        private static readonly object _lock = new object();

        // Default settings that can be changed in the app.config.
        public class RabbitMQSettings
        {
            public string HostName = "localhost";
            public int Port = 5672;
            public string UserName = "guest";
            public string Password = "guest";
            public string VirtualHost = "/";

            internal RabbitMQSettings()
            {
                if (ConfigurationManager.AppSettings["HostName"] != null) HostName = ConfigurationManager.AppSettings["HostName"];
                if (ConfigurationManager.AppSettings["Port"] != null) Port = int.Parse(ConfigurationManager.AppSettings["Port"]);
                if (ConfigurationManager.AppSettings["UserName"] != null) UserName = ConfigurationManager.AppSettings["UserName"];
                if (ConfigurationManager.AppSettings["Password"] != null) Password = ConfigurationManager.AppSettings["Password"];
                if (ConfigurationManager.AppSettings["VirtualHost"] != null) VirtualHost = ConfigurationManager.AppSettings["VirtualHost"];
            }
        }

        // Start the connection with the server
        internal static void Start(Guid id, string world, string name)
        {
            try
            {
                lock (_lock)
                {
                    if (_started == 0)
                    {                        
                        factory = new ConnectionFactory()
                        {
                            HostName = Settings.HostName,
                            Port = Settings.Port,
                            UserName = Settings.UserName,
                            Password = Settings.Password,
                            VirtualHost = Settings.VirtualHost
                        };

                        connection = factory.CreateConnection();

                        channel = connection.CreateModel();
                        channel.ExchangeDeclare(exchange: "Globals", type: "direct", durable: true, autoDelete: true);
                    }
                    _started++;
                }
            }
            catch (Exception e)
            {
                throw new GlobalsException("Exception on starting the Globals Server.", e);
            }
        }

        // Stop the connection with the server
        internal static void Stop(Guid id)
        {
            try
            {
                lock (_lock)
                {
                    if (_started == 1)
                    {
                        try
                        {
                            channel.ExchangeDelete("Globals", true);
                        }
                        catch
                        { } //Diaper

                        channel.Close();
                        connection.Close();

                        channel.Dispose();
                        connection.Dispose();

                        factory = null;
                    }
                    _started--;
                }
            }
            catch (Exception e)
            {
                throw new GlobalsException("Exception on stopping the Globals Server.", e);
            }
        }
    }
}