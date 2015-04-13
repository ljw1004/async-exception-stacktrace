Namespace Global
    Public Module AsyncStackTraceWinrtExtensions

        <Extension>
        Public Function Log(Of T)(task As IAsyncOperation(Of T),
                             Optional label As String = Nothing,
                             Optional arg As Object = Nothing,
                             <CallerMemberName> Optional member As String = "",
                             <CallerLineNumber> Optional line As Integer = 0,
                             <CallerFilePath> Optional path As String = "") As System.Threading.Tasks.Task(Of T)
            Return AsyncStackTraceExtensions.Log(task.AsTask(), label, arg, member, line, path)
        End Function

        <Extension>
        Public Function Log(task As IAsyncAction,
                             Optional label As String = Nothing,
                             Optional arg As Object = Nothing,
                             <CallerMemberName> Optional member As String = "",
                             <CallerLineNumber> Optional line As Integer = 0,
                             <CallerFilePath> Optional path As String = "") As System.Threading.Tasks.Task
            Return AsyncStackTraceExtensions.Log(task.AsTask(), label, arg, member, line, path)
        End Function

    End Module
End Namespace
