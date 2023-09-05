Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports ChatServerVB2.TcpChatServer
Imports NetCoreServer
'*** original source here https://github.com/chronoxor/NetCoreServer#features
'*** This is an entire client server comms library.  It is avaialble via NuGet and it requires VS2022 and .Net7.0
'*** I have re-writen the example to work with VB.NET and added extra features

'*** and translated from C# to VB.NET here https://converter.telerik.com/
'*** which saved me heaps of guessing.  I almost had it right but I think the translator used ByVal for variables and I did not so
'*** maybe copies of vars were being used and so it did not work as I expected, i.e. events were not handled
'*** See also https://learn.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/objects-and-classes/inheritance-basics
'*** for inheritance and overrides

Namespace TcpChatServer
    Class ChatSession
        Inherits TcpSession

        Public Sub New(ByVal server As TcpServer)
            MyBase.New(server)
        End Sub

        Protected Overrides Sub OnConnected()
            Console.WriteLine($"Chat TCP session with Id {Id} connected!")
            Dim message As String = "Hello from TCP chat! Please send a message or '!' to disconnect the client!"
            SendAsync(message)
        End Sub

        Protected Overrides Sub OnDisconnected()
            Console.WriteLine($"Chat TCP session with Id {Id} disconnected!")
        End Sub

        Protected Overrides Sub OnReceived(ByVal buffer As Byte(), ByVal offset As Long, ByVal size As Long)
            Dim message As String = Encoding.UTF8.GetString(buffer, CInt(offset), CInt(size))
            Console.WriteLine("Incoming: " & message)

            '*** how do we find which client sent the message?
            '*** by creating a function that returns this object (i.e. Me) we then can read its
            '*** properties.  e.g. theSession.Socket.RemoteEndPoint is in the form 127.0.0.1:63915
            '*** which is an IP and port.   The end points are also given an GUID but this is possibly less useful than an IP
            '*** especially if we chose to hard-code that IP
            Console.WriteLine($"from: {theSession.Socket.RemoteEndPoint.ToString}")


            Server.Multicast(message)
            If message = "!" Then Disconnect()
        End Sub

        Protected Overrides Sub OnError(ByVal [error] As SocketError)
            Console.WriteLine($"Chat TCP session caught an error with code {[error]}")
        End Sub

        Public Function theSession() As TcpSession
            Return Me
        End Function


    End Class

    Class ChatServer
        Inherits TcpServer

        Public Sub New(ByVal address As IPAddress, ByVal port As Integer)
            MyBase.New(address, port)
        End Sub

        Protected Overrides Function CreateSession() As TcpSession
            '*** translator got this right, I had Return MyBase.CreateSession()
            Return New ChatSession(Me)
        End Function

        Protected Overrides Sub OnError(ByVal [error] As SocketError)
            Console.WriteLine($"Chat TCP server caught an error with code {[error]}")
        End Sub

        Public Function arse() As Integer
            Return Me.Sessions.Count
        End Function


    End Class
End Namespace

Module Program
    Sub Main(ByVal args As String())
        Dim port As Integer = 1111
        If args.Length > 0 Then port = Integer.Parse(args(0))
        Console.WriteLine($"TCP server port: {port}")
        Console.WriteLine()
        Dim server = New ChatServer(IPAddress.Any, port)
        Console.Write("Server starting...")
        server.Start()
        Console.WriteLine("Done!")
        Console.WriteLine("Press Enter to stop the server or '!' to restart the server...")

        While True
            Dim line As String = Console.ReadLine()
            If String.IsNullOrEmpty(line) Then Exit While

            If line = "!" Then
                Console.Write("Server restarting...")
                server.Restart()
                Console.WriteLine("Done!")
                Continue While
            End If

            If line = "@" Then
                Console.Write($"Sessions {server.arse}")
                Continue While
            End If


            line = "(admin) " & line
            server.Multicast(line)
        End While

        Console.Write("Server stopping...")
        server.[Stop]()
        Console.WriteLine("Done!")
    End Sub
End Module
