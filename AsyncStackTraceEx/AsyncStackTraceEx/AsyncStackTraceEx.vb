Imports System.Text
Imports System.Runtime.CompilerServices
Imports System.Threading.Tasks

Namespace Global
    Public Module AsyncStackTraceExtensions

        Private Class ExceptionLog
            Public Label As String
            Public Arg As String
            Public Member As String
            Public Path As String
            Public Line As Integer

            Public ReadOnly Property LabelAndArg As String
                Get
                    LabelAndArg = ""
                    If Label IsNot Nothing Then LabelAndArg &= "#" & Label
                    If Label IsNot Nothing AndAlso Arg IsNot Nothing Then LabelAndArg &= "(" & Arg & ")"
                End Get
            End Property

        End Class

        Private Sub LogInternal(ex As Exception, log As ExceptionLog)
            If ex.Data.Contains("AsyncStackTrace") Then
                CType(ex.Data("AsyncStackTrace"), Queue(Of ExceptionLog)).Enqueue(log)
            Else
                Dim logs As New Queue(Of ExceptionLog) : logs.Enqueue(log)
                ex.Data("AsyncStackTrace") = logs
            End If
        End Sub

        <Extension>
        Public Function StackTraceEx(ex As Exception) As String
            Static Dim emptyLog As New Queue(Of ExceptionLog)
            Dim logs = If(ex.Data.Contains("AsyncStackTrace"), CType(ex.Data("AsyncStackTrace"), Queue(Of ExceptionLog)), emptyLog)
            logs = New Queue(Of ExceptionLog)(logs)

            Dim sb As New StringBuilder

            For Each s In ex.StackTrace.Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries)

                ' Get rid of stack-frames that are part of the BCL async machinery
                If s.StartsWith("   at ") Then s = s.Substring(6) Else Continue For
                If s = "System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)" Then Continue For
                If s = "System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)" Then Continue For
                If s = "System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()" Then Continue For
                If s = "System.Runtime.CompilerServices.TaskAwaiter.GetResult()" Then Continue For

                ' Get rid of stack-frames that are part of the runtime exception handling machinery
                If s = "System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()" Then Continue For

                ' Get rid of stack-frames that are part of .NET Native machiner
                If s.Contains("!<BaseAddress>+0x") Then Continue For

                ' Get rid of stack frames from VB and C# compiler-generated async state machine
                Static Dim re1 As New Text.RegularExpressions.Regex("VB\$StateMachine_[\d]+_(.+)\.MoveNext\(\)")
                Static Dim re2 As New Text.RegularExpressions.Regex("<([^>]+)>[^.]+\.MoveNext\(\)")
                s = re1.Replace(s, "$1")
                s = re2.Replace(s, "$1")

                ' If the stack trace had PDBs, "Alpha.Beta.GammaAsync in c:\code\module1.vb:line 53"
                Static Dim re3 As New Text.RegularExpressions.Regex("^(.*) in (.*):line ([0-9]+)$")
                Dim re3match = re3.Match(s)
                s = If(re3match.Success, re3match.Groups(1).Value, s)
                Dim pdbfile = If(re3match.Success, re3match.Groups(2).Value, Nothing)
                Dim pdbline = If(re3match.Success, CType(Integer.Parse(re3match.Groups(3).Value), Integer?), Nothing)

                ' Get rid of stack frames from AsyncStackTrace
                If s.EndsWith("AsyncStackTraceExtensions.Log`1") Then Continue For
                If s.EndsWith("AsyncStackTraceExtensions.Log") Then Continue For
                If s.Contains("AsyncStackTraceExtensions.Log<") Then Continue For

                ' Extract the method name, "Alpha.Beta.GammaAsync"
                Static Dim re4 As New Text.RegularExpressions.Regex("^.*\.([^.]+)$")
                Dim re4match = re4.Match(s)
                Dim fullyQualified = s
                Dim member = If(re4match.Success, re4match.Groups(1).Value, "")


                ' Now attempt to relate this to the log
                ' We'll assume that every logged call is in the stack (Q. will this assumption be violated by inlining?)
                ' We'll assume that not every call in the stack was logged, since users might chose not to log
                ' We'll assume that the bottom-most stack frame wasn't logged
                If logs.Count > 0 AndAlso logs.Peek().Member = member AndAlso sb.Length > 0 Then
                    Dim log = logs.Dequeue()
                    sb.AppendFormat("   at {1}{2} in {3}:line {4}{0}", vbCrLf, fullyQualified, log.LabelAndArg, IO.Path.GetFileName(log.Path), log.Line)
                ElseIf pdbfile IsNot Nothing Then
                    sb.AppendFormat("   at {1} in {2}:line {3}{0}", vbCrLf, fullyQualified, IO.Path.GetFileName(pdbfile), pdbline)
                Else
                    sb.AppendFormat("   at {1}{0}", vbCrLf, fullyQualified)
                End If
            Next
            If logs.Count > 0 AndAlso sb.Length > 0 Then sb.AppendLine("---------------- extra logged stackframes:")
            For Each log As ExceptionLog In logs
                sb.AppendFormat("   at {1}{2} in {3}:line {4}{0}", vbCrLf, log.Member, log.LabelAndArg, IO.Path.GetFileName(log.Path), log.Line)
            Next

            Return sb.ToString()
        End Function

        <Extension>
        Public Async Function Log(Of T)(task As Task(Of T),
                             Optional label As String = Nothing,
                             Optional arg As Object = Nothing,
                             <CallerMemberName> Optional member As String = "",
                             <CallerLineNumber> Optional line As Integer = 0,
                             <CallerFilePath> Optional path As String = "") As System.Threading.Tasks.Task(Of T)
            Try
                Return Await task
            Catch ex As Exception
                LogInternal(ex, New ExceptionLog With {.Label = label, .Arg = If(arg IsNot Nothing, arg.ToString(), ""), .Member = member, .Line = line, .Path = path})
                Throw
            End Try
        End Function

        <Extension>
        Public Async Function Log(task As Task,
                             Optional label As String = Nothing,
                             Optional arg As Object = Nothing,
                             <CallerMemberName> Optional member As String = "",
                             <CallerLineNumber> Optional line As Integer = 0,
                             <CallerFilePath> Optional path As String = "") As System.Threading.Tasks.Task
            Try
                Await task
            Catch ex As Exception
                LogInternal(ex, New ExceptionLog With {.Label = label, .Arg = If(arg IsNot Nothing, arg.ToString(), ""), .Member = member, .Line = line, .Path = path})
                Throw
            End Try
        End Function

    End Module

End Namespace

