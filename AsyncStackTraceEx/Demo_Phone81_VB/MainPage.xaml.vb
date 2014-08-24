Public NotInheritable Class MainPage
    Inherits Page

    Private Async Sub button1_Click(sender As Object, e As RoutedEventArgs) Handles button1.Click
        Dim ex As Exception = Nothing
        Try
            Await TestAsync().Log()
        Catch ex1 As Exception
            ex = ex1 ' workaround because up to VS2013 you can't await inside catch blocks
        End Try
        If ex IsNot Nothing AndAlso Await PromptToSendEmailAsync() Then
            Await SendEmailAsync(ex.Message, ex.StackTraceEx)
        End If

    End Sub

    Async Function PromptToSendEmailAsync() As Task(Of Boolean)
        Dim md As New Windows.UI.Popups.MessageDialog("A error occured. Do you want to send a problem report?", "Error")
        Dim r As Boolean? = Nothing
        md.Commands.Add(New Windows.UI.Popups.UICommand("Yes", Sub() r = True))
        md.Commands.Add(New Windows.UI.Popups.UICommand("No", Sub() r = False))
        Await md.ShowAsync()
        Return r.HasValue AndAlso r.Value
    End Function

    Async Function SendEmailAsync(message As String, details As String) As task
        Dim emailTo = "lu@wischik.com"
        Dim emailSubject = "DemoApp problem report"
        Dim emailBody = "I encountered a problem with AsyncStackTraceEx..." & vbCrLf & vbCrLf & message & vbCrLf & vbCrLf & "Details:" & vbCrLf & details
        Dim url = "mailto:?to=" + emailTo + "&subject=" + emailSubject + "&body=" + Uri.EscapeDataString(emailBody)
        Await Windows.System.Launcher.LaunchUriAsync(New Uri(url))
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

End Class
