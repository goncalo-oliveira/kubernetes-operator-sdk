using k8s.Models;

public static class TypeKubernetesEntityAttributeExtensions
{
    /// <summary>
    /// Extracts the KubernetesEntityAttribute from the given type. Throws ArgumentException if not found.
    /// </summary>
    public static KubernetesEntityAttribute GetKubernetesEntityAttribute( this Type type )
    {
        var attr = TryGetKubernetesEntityAttribute( type );

        if ( attr == null )
        {
            throw new ArgumentException( $"Type '{type.Name}' is missing a 'KubernetesEntity' attribute." );
        }

        return ( attr );
    }

    /// <summary>
    /// Extracts the KubernetesEntityAttribute from the given type. Returns null if not found.
    /// </summary>
    public static KubernetesEntityAttribute? TryGetKubernetesEntityAttribute( this Type type )
        => type.GetCustomAttributes( typeof( KubernetesEntityAttribute ), false )
            .Cast<KubernetesEntityAttribute>()
            .SingleOrDefault();
}
