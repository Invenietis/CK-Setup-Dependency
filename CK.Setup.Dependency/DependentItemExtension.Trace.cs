using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup;

public static partial class DependentItemExtension
{
    #region Trace & ToStringDetails for IDependentItem

    /// <summary>
    /// Traces a set of <see cref="IDependentItem"/> to a monitor.
    /// </summary>
    /// <param name="this">This set of dependent items.</param>
    /// <param name="monitor">Monitor to use.</param>
    public static void Trace( this IEnumerable<IDependentItem> @this, IActivityMonitor monitor ) => Log( @this, monitor, LogLevel.Trace );

    /// <summary>
    /// Debug a set of <see cref="IDependentItem"/> to a monitor.
    /// </summary>
    /// <param name="this">This set of dependent items.</param>
    /// <param name="monitor">Monitor to use.</param>
    public static void Debug( this IEnumerable<IDependentItem> @this, IActivityMonitor monitor ) => Log( @this, monitor, LogLevel.Debug );

    /// <summary>
    /// Log a set of <see cref="IDependentItem"/> to a monitor.
    /// </summary>
    /// <param name="this">This set of dependent items.</param>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="level">The loglevel to use.</param>
    public static void Log( this IEnumerable<IDependentItem> @this, IActivityMonitor monitor, LogLevel level )
    {
        using( monitor.OpenGroup( level, "Dependent items (C - for container, G - for group and I - for item)" ) )
        {
            foreach( var i in @this ) monitor.Log( level, i.ToStringDetails() );
        }
    }

    /// <summary>
    /// Returns detailed information: FullName, Container, Group, Generalization, Requires, 
    /// RequiredBy, Groups and Chilren if this is a Container.
    /// </summary>
    /// <param name="this">This dependent item.</param>
    /// <returns>Detaile information.</returns>
    public static string ToStringDetails( this IDependentItem @this )
    {
        StringBuilder b = new StringBuilder();
        DependentItemKind kind = DependentItemKind.Item;
        IDependentItemGroup? g = @this as IDependentItemGroup;
        if( g != null )
        {
            if( @this is IDependentItemContainerTyped c )
            {
                kind = c.ItemKind;
            }
            else kind = DependentItemKind.Group;
        }
        b.Append( kind.ToString()[0] ).Append( " - " );
        b.Append( "FullName = " ).Append( @this.FullName ).Append( " (" ).Append( @this.GetType().Name ).AppendLine( ")" )
            .Append( "| Container = " ).AppendOneName( @this.Container ).AppendLine()
            .Append( "| Generalization = " ).AppendOneName( @this.Generalization ).AppendLine()
            .Append( "| Requires = " ).AppendNames( @this.Requires ).AppendLine()
            .Append( "| RequiredBy = " ).AppendNames( @this.RequiredBy ).AppendLine()
            .Append( "| Groups = " ).AppendNames( @this.Groups ).AppendLine();
        if( g != null )
        {
            b.Append( "| Children = " ).AppendNames( g.Children ).AppendLine();
        }
        return b.ToString();
    }

    static StringBuilder AppendNames( this StringBuilder @this, IEnumerable<IDependentItemRef>? e )
    {
        if( e != null )
        {
            var en = e.GetEnumerator();
            if( en != null && en.MoveNext() )
            {
                @this.AppendOneName( en.Current );
                while( en.MoveNext() )
                {
                    @this.Append( ", " ).AppendOneName( en.Current );
                }
            }
        }
        return @this;
    }

    static StringBuilder AppendOneName( this StringBuilder @this, IDependentItemRef? o )
    {
        if( o == null ) @this.Append( "(null)" );
        else
        {
            if( o.Optional ) @this.Append( '?' );
            @this.Append( o.FullName ).Append( " (" ).Append( o.GetType().Name ).Append( ')' );
        }
        return @this;
    }

    #endregion

    #region Trace for ISortedItem

    /// <summary>
    /// Traces detailed information for a set of <see cref="ISortedItem"/>.
    /// </summary>
    /// <param name="this">This et of sorte item.</param>
    /// <param name="monitor">Monitor to use.</param>
    public static void Trace( this IEnumerable<ISortedItem> @this, IActivityMonitor monitor ) => Log( @this, monitor, LogLevel.Trace );

    /// <summary>
    /// Debug detailed information for a set of <see cref="ISortedItem"/>.
    /// </summary>
    /// <param name="this">This et of sorte item.</param>
    /// <param name="monitor">Monitor to use.</param>
    public static void Debug( this IEnumerable<ISortedItem> @this, IActivityMonitor monitor ) => Log( @this, monitor, LogLevel.Debug );

    /// <summary>
    /// Log detailed information for a set of <see cref="ISortedItem"/>.
    /// </summary>
    /// <param name="this">This et of sorte item.</param>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="level">The log level.</param>
    public static void Log( this IEnumerable<ISortedItem> @this, IActivityMonitor monitor, LogLevel level )
    {
        using( monitor.OpenGroup( level, $"Sorted items (C - for container, G - for group and I - for item)" ) )
        {
            foreach( var i in @this )
                if( i.HeadForGroup == null ) monitor.Log( level, i.ToStringDetails() );
        }
    }

    /// <summary>
    /// Returns detailed information.
    /// </summary>
    /// <param name="this">This sorted item.</param>
    /// <returns>Detailed information.</returns>
    public static string ToStringDetails( this ISortedItem @this )
    {
        StringBuilder b = new StringBuilder();
        b.Append( @this.ItemKind.ToString()[0] ).Append( " - " ).Append( @this.FullName ).Append( " -[" ).Append( @this.Rank ).Append( "] (" ).Append( @this.Item.GetType().Name ).AppendLine( ")" )
            .Append( "| Container = " ).Append( @this.Container != null ? @this.Container.FullName : "(null)" ).AppendLine()
            .Append( "| Generalization = " ).Append( @this.Generalization != null ? @this.Generalization.FullName : "(null)" ).AppendLine()
            .Append( "| Requires = " ).AppendStrings( @this.Requires.Select( o => o.FullName ) ).AppendLine()
            .Append( "| Groups = " ).AppendStrings( @this.Groups.Select( o => o.FullName ) ).AppendLine();

        if( @this.ItemKind != DependentItemKind.Item )
        {
            b.Append( "| Children = " ).AppendStrings( @this.Children.Select( o => o.FullName ) ).AppendLine();
        }
        return b.ToString();
    }
    #endregion



}
