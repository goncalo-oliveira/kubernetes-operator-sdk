# Kubernetes Operator SDK

This library provides a set of tools to create a Kubernetes operator with dotnet. It's built on top of Microsoft's host model.

## Getting Started

The package can be installed from NuGet

```bash
dotnet add package KubernetesOperatorSdk
```

To create a controller service, we have to inherit our service class from `KubeController<>` and specify the resource type we want to watch. The example below is going to handle a *Thing* custom resource definition.

```csharp
public sealed class V1Alpha1ThingController : KubeController<V1Alpha1Thing>
{
    /*
    we'll probably need this
    */
    private readonly IKubernetes client;

    /*
    ILoggerFactory and IKubernetes are required by the base class
    */
    public V1Alpha1ThingController( ILoggerFactory loggerFactory, IKubernetes kubernetesClient )
        : base( loggerFactory, kubernetesClient )
    {
        client = kubernetesClient;

        /*
        This is optional, but if required, it has to be set in the constructor.
        When set, the service will only watch resources in the given namespace.
        If this is not set, the service will watch resources cluster-wide.
        */
        //Namespace = "my-namespace";
    }

    protected override Task InitializeAsync( CancellationToken cancellationToken )
    {
        /*
        Overriding this method is optional. If you need to initialize something before
        the service starts watching resources, this is the place to do so.
        It can also be used to do any checks; StopAsync() can be called here to cancel the execution.
        */
        return Task.CompletedTask;
    }

    protected override Task DeletedAsync( V1Alpha1Thing obj )
    {
        /*
        This method is invoked when a resource object is deleted. In this case, a Thing object.
        */

        return Task.CompletedTask;
    }

    protected override async Task ReconcileAsync( V1Alpha1Thing obj )
    {
        /*
        This method is invoked when a resource object is added or modified. In this case, a Thing object.

        Caution: Keep in mind that an object can be "added" when the service starts watching.
        For that reason, we don't indicate whether the object was added or modified.
        */

        return Task.CompletedTask;
    }
}
```

Since our controller is a hosted service, we can add it to a service collection, by calling the `AddHostedService` method.

```csharp
IServiceCollection services = ...;

services.AddHostedService<V1Alpha1ThingController>();
```
