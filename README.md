
---

**In order to use this library, you need to have an Anymate account. Please visit [anymate.io](https://www.anymate.io) for more information.**

---

Anymate .NET SDK is made for .NET Standard 2.0, .NET Standard 2.1 and .NET 5. 

The SDK is available as open source and can be found on [our github page][githublink]. 
You can find the documentation for the SDK as part of the [Anymate Developer docs][anymatedocs].

We have also published it at [nuget.org][nugetlink]. Installing the Anymate package is done with the following command: 


``` C#
// Install via dotnet
dotnet add package Anymate
dotnet restore

```


``` c#
// install via NuGet
PM> Install-Package Anymate
```

Once installed, you use the library by adding it under usings

``` C#
using Anymate;
```

After anymate has been imported, you have to initialize the AnymateService class to communicate with Anymate.
The functions exposed in the client mirror the endpoints available in the API. We recommend going to the individual pages to learn more.
We have built the SDK to give you as much flexibility as possible. It is possible to create your own classes (or records in C# 9), and use these or we have made some Models that are readily available as part of the package. 

Likewise, all functions are available in async and normal versions.

**Create Task models for each Process**

    We recommend making models that mirror the Tasks for each Process. The functions exposed in AnymateService are flexible and have overloads which take [generic type parameters][csharpgenerics]. 


The SDK is built to automatically take care of authentication with Anymate as well as refreshing access_tokens as needed. Once the AnymateService is initialized, you don't have to worry about it.
You can see an example of a simple automation based on the Allocator Pattern below, where the automation script is working in one process and creating new tasks in another.

``` C#
    using Anymate;

   // ...
   // Namespace, class and function code omitted
   // ...

    //Authentication Variables
    var client_id = "My client id";
    var client_secret = "My API secret";
    var username = "Mate Username";
    var password = "Mate Password";

    //Process Keys
    var processKey = "myProcessKey";
    var targetProcessKey = "targetProcessKey";

    //Initialize AnymateService
    IAnymateService anymateService = new AnymateService(client_id, client_secret, username, password);


    var okToRun = anymateService.OkToRun(processKey);
    if (!okToRun.OkToRun)
    {
        //If its not ok to run, then return and do nothing
        return;
    }

    // ...
    // Businesslogic omitted. We assume that newTasks is an array that contains tasks ready to be created
    // ..
    var newTasks = new List<object>();

    // Start the run
    var run = anymateService.StartOrGetRun(processKey);

    foreach (var task in newTasks)
    {
        try
        {
    // Create a new task
    var createdTask = anymateService.CreateTask(task, targetProcessKey);
    // Optional: Read response from CreateTask and do something if needed
        }
        catch
        {
    // Exception handling
        }
    }
    
    // Tell Anymate that the run has finished
    anymateService.FinishRun(new {RunId = run.RunId});
    
```

**Use the IAnymateService interface**
```
    AnymateService implements the IAnymateService, making it easy to mock Anymate when writing unit tests.
```


Making a script to process Tasks is equally simple.
``` C#
    //Authentication Variables
    var client_id = "My client id";
    var client_secret = "My API secret";
    var username = "Mate Username";
    var password = "Mate Password";

    //Process Keys
    var processKey = "myProcessKey";

    //Initialize AnymateService
    var anymateService = new AnymateService(client_id, client_secret, username, password);


    var okToRun = anymateService.OkToRun(processKey);
    if (!okToRun.OkToRun)
    {
        //If its not ok to run, then return and do nothing
        return;
    }

    // We have created a model called "MyTaskModel" which the Task is created as.
    var task = anymateService.TakeNext<MyTaskModel>(processKey);

    //Our workloop continues while the TaskId is above 0. If the queue is empty, the TaskId will be -1.
    while (task.TaskId > 0)
    {            
       try
        {
            // Businesslogic omitted. We have created a dummy function to take the Task as input and return if it is solved (true) or goes to manual (false)
            var taskIsSolved = PerformBusinessLogic(task);
            if (taskIsSolved)
                {
                //The task was solved
                var solvedResult = anymateService.Solved(new
                            {TaskId = task.TaskId, Reason = "Solved", Comment = "The Task was solved"});
                }
                else
                {
                //The task needs to go to Manual
                var manualResult = anymateService.Manual(new
                            {TaskId = task.TaskId, Reason = "Manual", Comment = "The Task was sent to Manual"});
                }
            }
            catch
            {
            //An exception happened and we are sending the Task to retry
            var retryResult = anymateService.Retry(new
                        {TaskId = task.TaskId, Reason = "Exception", Comment = "Sending the Task to retry later"});
            }

        //We are done with the Task and taking the next in the queue
        task = anymateService.TakeNext<MyTaskModel>(processKey);
    }

```


## Enterprise On-Premises

The anymate SDK supports customers who have Anymate installed On-Premises with an Enterprise license out of the box.
In order to let anymate know you are running on a on-premises license, simply initialize AnymateService using the overload with client uri and auth uri - this way, it has On Premises mode enabled and is ready for use.

``` C#

    using Anymate;

   // ...
   // Namespace, class and function code omitted
   // ...

    //Authentication Variables
    var client_id = "My client id";
    var client_secret = "My API secret";
    var username = "Mate Username";
    var password = "Mate Password";

    //URI's for the on-premises installation
    var client_uri = "http://localanymateclient";
    var auth_uri = "http://localanymateauth";

    //Initialize AnymateService
    IAnymateService anymateService = new AnymateService(client_id, client_secret, username, password, client_uri, auth_uri);

```
[anymatedocs]: http://docs.anymate.io/developer/SDK/dotnet/
[githublink]: https://github.com/anymate/AnymateDotnetSDK/
[nugetlink]: https://www.nuget.org/packages/Anymate/
[csharpgenerics]: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/generic-type-parameters
