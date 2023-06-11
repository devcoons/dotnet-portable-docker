The PortableDocker library provides a simple way to interact with Docker in C#. It allows you to deploy Docker and start the Docker engine using different operating modes. You can also execute Docker commands and receive the output and success status.

# Installation
To use the PortableDocker library in your C# project, you need to include the library in your project's dependencies. You can do this by referencing the library assembly or by using a package manager.

# API Reference

## Class `Docker`

The Docker class is the main class in the PortableDocker library. It provides methods to start, stop, and execute Docker commands.

### Events
- `OnProgressStatusChange`: This event is triggered when the progress status of the Docker engine changes. It provides a ProgressStatus delegate as a parameter, which can be used to handle the event.
- `OnResults`: This event is triggered when the execution of a Docker command is complete. It provides a DockerResults delegate as a parameter, which can be used to handle the event.
### Methods
- `Task<bool> Start()`: Starts the Docker engine. Returns a Task<bool> indicating whether the Docker engine was started successfully.
- `Task<bool> Stop()`: Stops the Docker engine. Returns a Task<bool> indicating whether the Docker engine was stopped successfully.
- `Task<DockerResult> Execute(string cmd)`: Executes a Docker command. Takes a string parameter cmd representing the Docker command to execute. Returns a Task<DockerResult> containing the execution result, including the success status and output of the command.
### Static Methods
- `static void OnExitOrFailure()`: A static method that can be called to handle the exit or failure of the Docker process. It kills the Docker process if it is running.

## Class `DockerResult`

The DockerResult class represents the result of executing a Docker command.

### Properties
- `bool Success`: Indicates whether the Docker command executed successfully.
- `string Output`: The output of the Docker command execution.

## Delegate `ProgressStatus`

The ProgressStatus delegate represents a method that can handle the progress status change event. It takes a string parameter msg representing the progress status message.

# Example Usage

Here is an example of how to use the PortableDocker library:

```
using System;
using System.Threading.Tasks;
using PortableDocker;

class Program
{
    static async Task Main(string[] args)
    {
        Docker docker = new Docker();

        // Subscribe to progress status change event
        docker.OnProgressStatusChange += HandleProgressStatusChange;

        // Subscribe to Docker results event
        docker.OnResults += HandleDockerResults;

        // Start the Docker engine
        bool started = await docker.Start();
        if (started)
        {
            Console.WriteLine("Docker engine started successfully.");

            // Execute a Docker command
            DockerResult result = await docker.Execute("ps -a");
            if (result.Success)
            {
                Console.WriteLine("Docker command executed successfully.");
                Console.WriteLine("Output:");
                Console.WriteLine(result.Output);
            }
            else
            {
                Console.WriteLine("Failed to execute Docker command.");
                Console.WriteLine("Error:");
                Console.WriteLine(result.Output);
            }

            // Stop the Docker engine
            bool stopped = await docker.Stop();
            if (stopped)
            {
                Console.WriteLine("Docker engine stopped successfully.");
            }
            else
            {
                Console.WriteLine("Failed to stop Docker engine.");
            }
        }
        else
        {
            Console.WriteLine("Failed to start Docker engine.");
        }
    }

    static void HandleProgressStatusChange(string msg)
    {
        Console.WriteLine("Progress Status: " + msg);
    }

    static void HandleDockerResults(DockerResult result)
    {
        if (result.Success)
        {
            Console.WriteLine("Docker command executed successfully.");
            Console.WriteLine("Output:");
            Console.WriteLine(result.Output);
        }
        else
        {
            Console.WriteLine("Failed to execute Docker command.");
            Console.WriteLine("Error:");
            Console.WriteLine(result.Output);
        }
    }
}
```

In the example above, we create a Docker instance and subscribe to the progress status change event and Docker results event. We then start the Docker engine, execute a Docker command, and stop the Docker engine. The progress status change and execution results are displayed in the console.

Note: Make sure to handle exceptions appropriately in your application for error handling and fault tolerance.