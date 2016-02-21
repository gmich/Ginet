# Ginet

[![Build status](https://ci.appveyor.com/api/projects/status/4ctmsofu3ilvak50?svg=true)](https://ci.appveyor.com/project/gmich/ginet)

A fluent networking library build on top of lidgren network.

##Quick start

Ginet favors message exchange. All types that are handled via the INetworkManager must be marked with the GinetPackage attribute.

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

Create and configure

```
          var server = new NetworkServer("MyServer", cfg =>
          {
             cfg.Port = 1234;
             cfg.ConnectionTimeout = 5.0f;
             //Additional configuration
          });
          server.IncomingMessageHandler.LogTraffic();
```        

Register the classes that are marked with the GinetPackage attribute

```
          server.PackageConfigurator.RegisterPackages(Assembly.Load("Packages.Assembly.Name"));
```

Configure package handling

```          
          server.RespondTo<ChatMessage>((msg, im, om) =>
          {
              server.Out.Info($"Received {msg.Message}");
              server.SendToAllExcept(om, im.SenderConnection, NetDeliveryMethod.ReliableOrdered, channel: 0);
          });

```

Configure context specific behavior 

```
        server.IncomingMessageHandler.OnMessage(NetIncomingMessageType.ConnectionApproval, im =>
        {
           //Configure connection approval
           im.SenderConnection.Approve();
        });
```

----

###Client

Create and configure

```
         var client = new NetworkClient("Chat", cfg =>
         {  
            //client configuration
         });
         client.PackageConfigurator.RegisterPackages(Assembly.Load("Ginet.Chat.Packages"));
```

Connect

```
         client.Connect("localhost", 1234, new ConnectionApprovalMessage
         {
             Sender = "Me",
             Password = "1234"
         });
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
       var lifter = client.LiftSender((msg, peer) =>
              peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered));
              
       lifter.Send(new MyPackage { Message = "Hello" });
```

---

###Logging

For custom logging targets implement the IAppender interface and pass it in the client , server creation

```
          var server = new NetworkServer(
          "MyServer",
          cfg => {},
          output: new MyAppenderImplementation());
```

The default IAppender is the Console.WriteLine ActionAppender

```
          var server = new NetworkServer(
          "MyServer",
          cfg => {},
          output: new ActionAppender(Console.WriteLine));
```

----

###Custom serializers

For custom encoding / decoding for a type, simply implement the IPackageSerializer interface add the PackageSerializerAttribute to the class

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
