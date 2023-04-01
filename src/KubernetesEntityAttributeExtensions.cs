using k8s.Models;

internal static class KubernetesEntityAttributeExtensions
{
    /// <summary>
    /// Gets the apiVersion value, prefixed with the group if there is one.
    /// </summary>
    public static string GetApiVersion( this KubernetesEntityAttribute attr )
    {
        if ( string.IsNullOrEmpty( attr.Group ) )
        {
            return ( attr.ApiVersion );
        }

        return string.Concat( attr.Group, "/", attr.ApiVersion );
    }

    /// <summary>
    /// Gets the plural name; generates from kind as fallback method
    /// </summary>
    public static string GetPluralName( this KubernetesEntityAttribute attr )
    {
        if ( string.IsNullOrEmpty( attr.PluralName ) )
        {
            return string.Concat( attr.Kind, "s" ).ToLower();
        }

        return attr.PluralName.ToLower();
    }

    /// <summary>
    /// Gets the kind value, suffixed with the group if there is one
    /// </summary>
    public static string GetKind( this KubernetesEntityAttribute attr )
    {
        if ( string.IsNullOrEmpty( attr.Group ) )
        {
            return attr.Kind.ToLower();
        }

        return $"{attr.Kind.ToLower()}.{attr.Group}";
    }
}
