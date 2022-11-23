See An [Gurux](http://www.gurux.org/ "Gurux") for an overview.


Join the Gurux Community or follow [@Gurux](https://twitter.com/guruxorg "@Gurux") for project updates.

Open Source GXSerial media component, made by Gurux Ltd, is a part of GXMedias set of media components, which programming interfaces help you implement communication by chosen connection type. Our media components also support the following connection types: network, terminal.

For more info check out [Gurux](http://www.gurux.org/ "Gurux").

We are updating documentation on Gurux web page. 

If you have problems you can ask your questions in Gurux [Forum](http://www.gurux.org/forum).

Build
=========================== 
If you want to build example you need Nuget package manager for Visual Studio.
You can get it here:
https://visualstudiogallery.msdn.microsoft.com/27077b70-9dad-4c64-adcf-c7cf6bc9970c

Simple example
=========================== 
Before use you must set following settings:
* PortName
* BaudRate
* DataBits
* Parity
* StopBits

It is also good to listen following events:
* OnError
* OnReceived
* OnMediaStateChange
* OnPortAdd
* OnPortRemove

```csharp

GXSerial cl = new GXSerial();
cl.PortName = Gurux.Serial.GXSerial.GetPorts()[0];
cl.BaudRate = 9600;
cl.DataBits = 8;
cl.Parity = System.IO.Ports.Parity.Odd;
cl.StopBits = System.IO.Ports.StopBits.One;
cl.Open();

```

Data is send with send command:

```csharp
cl.Send("Hello World!");
```
It's good to listen possible errors and show them.

```csharp
serial.OnError += (sender, ex) =>
{
   // Show error.
};
```

In default mode received data is coming as asynchronously from OnReceived event.

```csharp
serial.OnReceived += (sender, e) =>
{
    try
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (hex.Checked)
            {
                receivedData.Append(GXCommon.ToHex((byte[])e.Data));
            }
            else
            {
                receivedData.Append(ASCIIEncoding.ASCII.GetString((byte[])e.Data));
            }
        });
    }
    catch (System.Exception ex)
    {
        ShowError(ex);
    }
};

```
Data can be send as syncronous if needed:

```csharp
lock (cl.Synchronous)
{
    string reply = "";
    ReceiveParameters<string> p = new ReceiveParameters<string>()
    //ReceiveParameters<byte[]> p = new ReceiveParameters<byte[]>()
    {
       //Wait time tells how long data is waited.
       WaitTime = 1000,
       //Eop tells End Of Packet charachter.
       Eop = '\r'
    };
    cl.Send("Hello World!", null);
    if (gxNet1.Receive(p))
    {
	reply = Convert.ToString(p.Reply);
    }
}
```