Imports System.Threading.Tasks

Module Module1

    Sub Main()
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Public Async Function MainAsync() As Task
        Try
            Await TestAsync().Log()
        Catch ex As Exception
            Console.WriteLine(ex.StackTraceEx)
        End Try
    End Function

    Async Function TestAsync() As Task(Of Integer)
        Await FooAsync(0).Log("FooAsync", 0)
        Await FooAsync(3).Log("FooAsync", 3)
        Return 1
    End Function

    Async Function FooAsync(i As Integer) As Task
        If i <= 1 Then Await BarAsync(i = 0).Log("BarAsync", i = 0) : Return
        Await FooAsync(i - 1).Log("FooAsync", i - 1)
    End Function

    Async Function BarAsync(b As Boolean) As Task
        Await Task.Delay(1).Log("Delay", 1)
        If Not b Then Throw New InvalidOperationException("oops")
    End Function

End Module
