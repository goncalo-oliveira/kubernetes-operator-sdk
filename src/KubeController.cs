using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// A Kubernetes Controller service that watches and reconciles the T resource type
/// </summary>
public abstract class KubeController<T> : BackgroundService where T : IKubernetesObject, IMetadata<V1ObjectMeta>
{
    private readonly ILogger logger;
    private readonly IKubernetes client;

    public KubeController( ILoggerFactory loggerFactory, IKubernetes kubernetesClient )
    {
        logger = loggerFactory.CreateLogger( GetType() );
        client = kubernetesClient;
    }

    /// <summary>
    /// Gets the namespace where the service should look for changes. If empty, the service will look cluster-wide.
    /// </summary>
    protected string Namespace { get; init; } = string.Empty;

    /// <summary>
    /// Gets the label selector to use when looking for resources. Default is null.
    /// </summary>
    protected string? LabelSelector { get; init; } = null;

    /// <summary>
    /// Triggered once when the service execution starts
    /// </summary>
    protected virtual Task InitializeAsync( CancellationToken cancellationToken )
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered when a T resource is deleted
    /// </summary>
    protected abstract Task DeletedAsync( T obj );

    /// <summary>
    /// Triggered when a T resource is created or modified
    /// </summary>
    protected abstract Task ReconcileAsync( T obj );

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    public sealed override Task StartAsync( CancellationToken cancellationToken = default )
    {
        logger.LogInformation( "Started." );

        return base.StartAsync( cancellationToken );
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public override Task StopAsync( CancellationToken cancellationToken = default )
    {
        logger.LogInformation( "Stopped." );

        return base.StopAsync( cancellationToken );
    }

    /// <summary>
    /// Triggered when the service starts.
    /// </summary>
    protected sealed override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            await InitializeAsync( stoppingToken );
        }
        catch ( Exception ex )
        {
            logger.LogError( ex, "Failed to initialize '{Name}' controller. {Error}", typeof( T ).Name, ex.Message );

            throw;

            // await StopAsync( CancellationToken.None );
            // return;
        }

        var attr = typeof( T )
            .TryGetKubernetesEntityAttribute();

        if ( attr == null )
        {
            // T is not a valid kubernetes entity; missing the KubernetesEntity attribute
            // cancel execution
            logger.LogError( "Type '{Name}' is missing a 'KubernetesEntity' attribute.", typeof( T ).Name );

            await StopAsync(  CancellationToken.None);
            return;
        }

        while ( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                if ( string.IsNullOrEmpty ( Namespace ) )
                {
                    logger.LogInformation(
                        "watching for '{Name}.{Version}' objects cluster-wide.",
                        attr.GetPluralName(),
                        attr.GetApiVersion()
                    );
                }
                else
                {
                    logger.LogInformation(
                        "watching for '{Name}.{Version}' objects in '{Namespace}' namespace.",
                        attr.GetPluralName(),
                        attr.GetApiVersion(),
                        Namespace
                    );
                }

                var result = await ListObjectAsync( attr, stoppingToken )
                    .ConfigureAwait( false );

                var watcher = await CreateWatcherAsync( result, attr.GetKind(), stoppingToken );

                while ( watcher.Watching && !stoppingToken.IsCancellationRequested )
                {
                    await Task.Delay( 1000, stoppingToken );
                }
            }
            catch ( k8s.Autorest.HttpOperationException ex )
            {
                logger.LogError( "{Error}\n{Content}", ex.Message, ex.Response.Content );

                await Task.Delay( 3000, stoppingToken );
            }
        }
    }

    /// <summary>
    /// Lists objects with the given attributes
    /// </summary>
    private Task<k8s.Autorest.HttpOperationResponse<object>> ListObjectAsync( KubernetesEntityAttribute attr, CancellationToken cancellationToken )
    {
        // if no namespace is defined, the service will look for resources cluster-wide
        if ( string.IsNullOrEmpty( Namespace ) )
        {
            return client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                group: attr.Group,
                version: attr.ApiVersion,
                plural: attr.GetPluralName(),
                labelSelector: LabelSelector,
                watch: true,
                cancellationToken: cancellationToken
            );
        }

        // otherwise, the service will look for in the given namespace only
        return client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
            group: attr.Group,
            version: attr.ApiVersion,
            namespaceParameter: Namespace,
            plural: attr.GetPluralName(),
            labelSelector: LabelSelector,
            watch: true,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Creates a watcher object from a result with watch=true
    /// </summary>
    private async Task<Watcher<T>> CreateWatcherAsync( k8s.Autorest.HttpOperationResponse<object> result, string kind, CancellationToken cancellationToken )
    {
        var watcher = result.Watch<T, object>( 
            onEvent: async ( type, item ) =>
            {
                logger.LogInformation( $"{kind}/{item.Name()} {type.ToString().ToLower()}." );

                switch ( type )
                {
                    case WatchEventType.Added:
                    case WatchEventType.Modified:
                    await ReconcileAsync( item );
                    break;

                    case WatchEventType.Deleted:
                    await DeletedAsync( item );
                    break;

                    case WatchEventType.Error:
                    break;

                    default:
                    break;
                }
            }
            , onError: ex =>
            {
                logger.LogError( ex, "Watcher error. {Error}", ex.Message );
            }
        );

        // wait for watcher to initialize
        while ( !watcher.Watching && !cancellationToken.IsCancellationRequested )
        {
            await Task.Delay( 1000, cancellationToken );
        }

        return watcher;
    }
}
