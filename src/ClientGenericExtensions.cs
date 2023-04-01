using k8s;

public static class KubernetesClientGenericExtensions
{
    /// <summary>
    /// Creates a GenericClient for the given T resource type
    /// </summary>
    public static GenericClient WithType<T>( this IKubernetes client ) where T : IKubernetesObject
    {
        var attr = typeof( T )
            .GetKubernetesEntityAttribute();

        return new GenericClient( client, attr.Group, attr.ApiVersion, attr.GetPluralName() );
    }
}
