# The Globals Design Pattern


The [Globals Communication Pattern](https://www.beentjessoftware.nl/globals-design-pattern) describes a way to make communication between systems death simple, hiding away all technical details.

This package contains a .NET & RabbitMQ implementation of this pattern.  

For the examples, click [here](https://github.com/MathijsJBeentjes/Globals.NET.RabbitMQ.Examples)

Working (C#): On the sending side, you declare a Global of a certain type and name.

```cs
using (Global<string> HelloWorld = new Global<string>("HelloWorld"))    // a declaration...
{
    HelloWorld.Value = "Hello, World!";     // assign a value, and we are done!
    ...
}
```

On the receiving side, you declare the same Global (same name, same type) and add a DataChanged handler. 

```cs
// a declaration plus event handler
using (Global<string> HelloWorld = new Global<string>("HelloWorld", handler: HelloWorld_DataChanged))
{  
    ...
}

private static void HelloWorld_DataChanged(object sender, GlobalEventData<string> e)
{
    Console.WriteLine(e.Data);   // Output: Hello, world!
}
```

On the moment a value is assigned by the Sending side, a DataChanged event is raised on the receiving side.

With this mechanism, you can send a stream of data as well:


```cs
using (Global<string> HelloWorld = new Global<string>("HelloWorld"))    // a declaration...
{
    for (int i = 0; i < 100; i++)
    {
        HelloWorld.Value = $"Hello, World! This is greeting number {i}!"; 
    }
    ...
}
```
On the receiving side, 100 DataChanged events are raised, in the same order as it was sent.

Below the complete example to send and receive a single greeting over the network:

### *The Sender:*

```cs
using System;
using Globals.NET.RabbitMQ;  // the package

namespace Sender
{
    class Program
    {
        static void Main()
        {
            using (Global<string> HelloWorld = new Global<string>("HelloWorld"))    // a declaration...
            {
                HelloWorld.Value = "Hello, World!";     // assign a value, and we are done!

                Console.ReadLine();
                Console.WriteLine("Stopping...");
            };
        }
    }
}
```



### *The Receiver:*

```cs
using System;
using Globals.NET.RabbitMQ;  // the package

namespace Receiver
{
    class Program
    {
        static void Main()
        {
            using Global<string> HelloWorld = new Global<string>("HelloWorld", handler: HelloWorld_DataChanged);  // a declaration plus event handler

            Console.ReadLine();
            Console.WriteLine("Stopping...");
        }
        private static void HelloWorld_DataChanged(object sender, GlobalEventData<string> e)
        {
            Console.WriteLine(e.Data);   // Output: Hello, world!
        }
    }
}
```



### *The Rule:*

```
Globals with the same name have the same value.
```




## Getting Started

### Prerequisites

- This package uses [RabbitMQ](https://www.rabbitmq.com/download.html) as the communication layer, this should be installed first.
- This package is created to be used within the .NET Framework (.NET Standard 2.0), a version of Visual Studio can be downloaded [here](https://visualstudio.microsoft.com/downloads/).


### Installing

Once the Prerequisites section is done, create you first Hello World application following the steps below. In this description it is assumed you work with some version of Visual Studio. We will start with:

### *The Sender:*

#### 1. Start  Visual Studio and create an empty Console Application:
a. Create a new project
b. Select as project type: Console App, C#, either .NET Core or .NET Framework.
c. Click Next
d. Give your project a nice name, like "HelloWorld_Sender"
d. Click on 'Create'

Visual Studio opens the editor with a small program:

```cs
using System;

namespace HelloWorld_Sender
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
```

#### 2. Install the Globals.NET.RabbitMQ NuGet package:
a. Select Tools - NuGet Package Manager - Manage NuGet Packages for Solution...
b. Click on the Browse tab
c. In the Search bar, type Globals.NET.RabbitMQ.
d. Select the Globals.NET.RabbitMQ package in the window below the search bar.
e. Check the HellowWorld_Sender checkbox and click on install.

After installation is complete:

f. Close the window

#### 3. Copy & Paste the Sender Code in program.cs:

~~~cs
using System;
using Globals.NET.RabbitMQ;  // the package

namespace Sender
{
    class Program
    {
        static void Main()
        {
            using (var HelloWorld = new Global<string>("HelloWorld"))    // a declaration...
            {
                HelloWorld.Value = "Hello, World!";     // assign a value, and we are done!

                Console.ReadLine();
                Console.WriteLine("Stopping...");
            };
        }
    }
}
~~~

#### 4. Add an App.Config file to your project and enter 5 configuration parameters:

a. Right-click on the project HelloWorld_Sender, and click Add - New Item.
b. In the search bar, type 'config'.
c. Select the ' Application Configuration File' and click 'Add' .

Your App.Config file is added and opened.

d. Add your 5 RabbitMQ parameters:

~~~cs
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="HostName" value="localhost"/>
    <add key="Port" value="5672"/>
    <add key="UserName" value="guest"/>
    <add key="Password" value="guest"/>
    <add key="VirtualHost" value="/"/>
  </appSettings>
</configuration>
~~~

To get your parameters right, please see the [RabbitMQ](https://www.rabbitmq.com/#getstarted) documentation

### *The Receiver:*

Follow the same steps as with creating the sender, but now give the project the name HelloWorld_Receiver, and replace your program.cs code with:

```cs
using System;
using Globals.NET.RabbitMQ;  // the package

namespace Receiver
{
    class Program
    {
        static void Main()
        {
            using var HelloWorld = new Global<string>("HelloWorld", handler: HelloWorld_DataChanged);  // a declaration plus event handler

            Console.ReadLine();
            Console.WriteLine("Stopping...");
        }
        private static void HelloWorld_DataChanged(object sender, GlobalEventData<string> e)
        {
            Console.WriteLine(e.Data);   // The data is received here!
        }
    }
}
```

## Built With / Tools Used

* [Microsoft Visual Studio 2019](https://visualstudio.microsoft.com/downloads/) - The Development Environment
* [RabbitMQ](https://www.rabbitmq.com/) - The Message Broker
* [Newtonsoft](https://www.nuget.org/packages/Newtonsoft.Json) - Essential package by the King
* [Word 365](https://www.office.com/) - Used to write the [article](http://beentjessoftware.nl/globals/globalsdesignpattern.pdf)
* [Notepad++](https://notepad-plus-plus.org/) - Used to write this Readme
* [Heavy Duty Calculator](http://www.heavydutycalculator.com/) - If you want to get Numeric

## Resources

* [Theoretical background Globals Communication Pattern](http://beentjessoftware.nl/globals/globalsdesignpattern.pdf)
* [Source Code](https://github.com/MathijsJBeentjes/Globals.NET.RabbitMQ)
* [Examples](https://github.com/MathijsJBeentjes/Globals.NET.RabbitMQ.Examples)

## Author

* **[Mathijs Beentjes](http://www.linkedin.com/in/mathijs-beentjes-mjb)** 


## License

This project is licensed under the [MIT License](https://licenses.nuget.org/MIT)


