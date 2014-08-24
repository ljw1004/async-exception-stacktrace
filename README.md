# async-exception-stacktrace

**Scenario**: I've written my app and released it to users. Some of them have reported crashes but I don't know where. So I released an update which captures unhandled exceptions and invites the users to email these to me, in the hope that I can figure out what's wrong.

![](http://blogs.msdn.com/resized-image.ashx/__size/240x400/__key/communityserver-blogs-components-weblogfiles/00-00-01-12-06/8032.report1.png) ![](http://blogs.msdn.com/resized-image.ashx/__size/240x400/__key/communityserver-blogs-components-weblogfiles/00-00-01-12-06/0045.report2.png)

**Problem**: On Windows Phone it's not possible to deploy the PDB alongside the EXE/DLL. Without it, Exception.StackTrace is unable to provide line-numbers, and so all my phone error-reports come back without line-numbers and I don't know how to debug.

**Solution**: I'll write a small helper which adds those line numbers to exception stack-traces, without needing PDBs.



# How it works

Here's a comparison of the output.

**Using the traditional PDB-based Exception.StackTrace** *(the line numbers parts are absent on phone since they depend on PDBs)*

```
  at VB$StateMachine_3_BarAsync.MoveNext() ~~in Class1.vb:line 24~~
--- End of stack trace from previous location where exception was thrown ---
at TaskAwaiter.ThrowForNonSuccess(Task task)
  at TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
at TaskAwaiter.GetResult()
  at VB$StateMachine_2_FooAsync.MoveNext() ~~in Class1.vb:line 19~~
--- End of stack trace from previous location where exception was thrown ---
  at TaskAwaiter.ThrowForNonSuccess(Task task)
  at TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
  at TaskAwaiter.GetResult()
  at VB$StateMachine_1_TestAsync.MoveNext() ~~in Class1.vb:line 14~~
--- End of stack trace from previous location where exception was thrown ---
   at TaskAwaiter.ThrowForNonSuccess(Task task)
   at TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at TaskAwaiter`1.GetResult()
   at VB$StateMachine_0_Button1_Click.MoveNext() ~~in Class1.vb:line 5 ~~
   at VB$StateMachine_3_BarAsync.MoveNext() ~~in Class1.vb:line 24~~
--- End of stack trace from previous location where exception was thrown ---
   at TaskAwaiter.ThrowForNonSuccess(Task task)
   at TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at TaskAwaiter.GetResult()
   at VB$StateMachine_2_FooAsync.MoveNext() ~~in Class1.vb:line 19~~
--- End of stack trace from previous location where exception was thrown ---
   at TaskAwaiter.ThrowForNonSuccess(Task task)
   at TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at TaskAwaiter.GetResult()
   at VB$StateMachine_1_TestAsync.MoveNext() ~~in Class1.vb:line 14~~
--- End of stack trace from previous location where exception was thrown ---
   at TaskAwaiter.ThrowForNonSuccess(Task task)
   at TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at TaskAwaiter`1.GetResult()
   at VB$StateMachine_0_Button1_Click.MoveNext() ~~in Class1.vb:line 5~~
```

**Using this package, Exception.StackTraceEx** *(the line numbers parts are present even on Phone)*
```
   at Test.BarAsync
   at Test.FooAsync()#BarAsync in Class1.vb:19
   at Test.TestAsync()#FooAsync(True) in Class1.vb:14
   at Test.Button1_Click() in Class1.vb:5 
```

 
I noticed that all the exceptions I ever cared about either arose in async operations in the framework, or in my own async methods. That let me use a fluent syntax, like this: (I've given the examples in C#, to balance out the fact that implementation is in VB...)
```cs
private async void Button1_Click(object sender, RoutedEventArgs e)
{
    try
    {
        await TestAsync().Log();    // so exception's StackTraceEx will show "Button1Click() in MainPage.xaml.cs:53"
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.StackTraceEx());  // this retrieves the version of the stacktrace that includes line numbers
    }
}
```

Whenever I await something, I stick on `.Log()` at the end. This ensures that line numbers are preserved for any exception that comes out of the awaited task. And rather than retrieving the `Exception.StackTrace` property, I retrieve the stack-trace through my `Exception.StackTraceEx()` extension method. This cleans up the async callstack into something more readable, and inserts back all those line numbers. I also added two other optional overloads of await-point logging, to get richer information in the exception's async callstack; I use that richer information to (optionally) indicate which API I'm calling, and what arguments I'm passing to it. After all, it's often the arguments to a method that are crucial to understanding why it threw the exception.
```cs
await BarAsync(b).Log("BarAsync");           // so the exception stacktrace shows "...#BarAsync in MainPage.xaml.cs:61"
await FooAsync(true).Log("FooAsync", true);  // so the exception stacktrace shows "...#FooAsync(true) in MainPage.xaml.cs:72"
```
