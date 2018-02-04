# Ginet

[![Build status](https://ci.appveyor.com/api/projects/status/4ctmsofu3ilvak50?svg=true)](https://ci.appveyor.com/project/gmich/ginet) 

A fluent networking library build on top of [lidgren network](https://github.com/lidgren/lidgren-network-gen3). Ginet extends lidgren network with a fluent and functional API.

##Quick start

Ginet is available as a [NuGet package](https://www.nuget.org/packages/Ginet/). You can install it using the NuGet Package Console window:

```
PM> Install-Package Ginet -Pre
```

Ginet favors message exchange. All classes that are handled via the `INetworkManager` must be marked with the `GinetPackageAttribute`. Ginet inferres messages by adding a unique byte id.

*The classes must be marked as public with public getters / setters and contain only primitive types. The client and server registered packages should be the same*. 
For complex serialization you can provide your [custom serializer](https://github.com/gmich/Ginet/blob/master/README.md#custom-serializers) via attribute.



```
          [GinetPackage]
          public class MyPackage
          {
             public string Message { get; set; }
          }
          
          [GinetPackage]
          public class ConnectionApprovalMessage
          {
             public string Sender { get; set; }
             public string Password { get; set; }
          }
```

###Server

Create, configure and register the classes that are marked with the GinetPackage attribute

```
          var server = new NetworkServer("MyServer",
                    builder =>
                    {
                        //via reflection
                        builder.RegisterPackages(Assembly.Load("Packages.Assembly.Name"));
                        //or manually
                        builder.RegisterPackage<MyPackage>();
                    }
                    cfg =>
                    {
                       cfg.Port = 1234;
                       cfg.ConnectionTimeout = 5.0f;
                       //Additional configuration
                    });
          server.IncomingMessageHandler.LogTraffic();
```        

Start the server

``` 
          server.Start(NetDeliveryMethod.ReliableOrdered, channel: 0);
```

Process Incoming Messages in a separate thread

```
          server.ProcessMessagesInBackground();           
```

If you have an update loop you can await the message processing in an async method

```
        await server.ProcessMessages()
```

Configure how to respond to incoming packages

```          
          server.Broadcast<ChatMessage>((msg, im, om) =>
          {
              server.Out.Info($"Received {msg.Message}");
              server.SendToAllExcept(om, im.SenderConnection, NetDeliveryMethod.ReliableOrdered, channel: 0);
          }, 
          packageTransformer: msg => 
              msg.Message += "this is broadcasted");
          
          server.BroadcastExceptSender<ChatMessage>((sender, msg) =>
          {
              server.Out.Info($"Broadcasting {msg.Message}. Received from: {sender}");
          });

```

Configure context specific behavior 

```
        server.IncomingMessageHandler.OnMessage(NetIncomingMessageType.ConnectionApproval, incomingMsg =>
        {
           //Configure connection approval
           var parsedMsg = server.ReadAs<ConnectionApprovalMessage>(incomingMsg);
           if(parsedMsg.Password == "my secret and encrypted password")
           {
                incomingMsg.SenderConnection.Approve();
                incomingMsg.SenderConnection.Tag = parsedMsg.Sender;
           }
           else
           {
                incomingMsg.SenderConnection.Deny();
           }
        });
```

```
         server.IncomingMessageHandler.OnConnectionChange(NetConnectionStatus.Disconnected, incomingMsg =>
         {
                 server.SendToAllExcept(
                     server.ConvertToOutgoingMessage(new MyPackage
                     { Message = $"Disconnected {incomingMsg.SenderConnection}" }),
                     im.SenderConnection,
                     NetDeliveryMethod.ReliableOrdered,
                     channel: 0);
          });
```        

```
         server.IncomingMessageHandler.OnConnection(server.Host.Connections.First(), (msgType, incomingMsg) =>
         {
                 //...
         });
```    

Upon adding a handler, an `IDisposable` object is returned. Disposing it causes the handler deregistration.

```
        var handlerDisposable = server.IncomingMessageHandler.OnMessage(NetIncomingMessageType.Data, im => { });
        
        handlerDisposable.Dispose();
```

----

###Client

Create, configure and register the classes that are marked with the GinetPackage attribute

```
         var client = new NetworkClient("Chat", 
                   builder=>
                        builder.RegisterPackages(Assembly.Load("Packages.Assembly.Name"));
                   cfg =>
                   {  
                      //client configuration
                   });
```

Start / Connect

```
         client.Start(NetDeliveryMethod.ReliableOrdered, channel: 0);
         client.Connect("localhost", 1234, new ConnectionApprovalMessage
         {
             Sender = "Me",
             Password = "1234"
         });
```

Process Incoming messages

```
         client.ProcessMessagesInBackground();           
```

Handle incoming messages

```
        client.IncomingMessageHandler
              .OnPackage<MyPackage>((msg, sender) => 
                  Console.WriteLine($"Received {msg.Message} from {sender.SenderConnection}"));
                  
```
Send a message

```
       client.Send(new MyPackage { Message = "Hello" },
                  (om,peer) => peer.SendMessage(om,NetDeliveryMethod.ReliableOrdered));
``` 
                 
Create a message sender lifter

```
       IPackageSender packageSender = client.LiftSender((msg, peer) =>
              peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered));
              
       packageSender.Send(new MyPackage { Message = "Hello" });
```

---

###Logging

For custom logging targets implement the `IAppender` interface and pass it in the client , server creation

```
          var server = new NetworkServer(
          "MyServer",
          cfg => {},
          output: new MyAppenderImplementation());
```

The default `IAppender` uses the `Console.WriteLine` in the `ActionAppender` constructor.

```
          var server = new NetworkServer(
          "MyServer",
          cfg => {},
          output: new ActionAppender(Console.WriteLine));
```

----

###Custom serializers

For custom encoding / decoding for a type, simply implement the `IPackageSerializer` interface add the `PackageSerializerAttribute` to the class

```
       [GinetPackage]
       [PackageSerializer(typeof(MyCustomSerializer))]
       public class MyPackage
       {
          public string Message { get; set; }
       }
```      

----

More examples [here](https://github.com/gmich/Ginet/tree/master/Samples/Chat).
