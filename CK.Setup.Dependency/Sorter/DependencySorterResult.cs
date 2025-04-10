using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CK.Setup;

/// <summary>
/// Encapsulates the result of the <see cref="G:DependencySorter.OrderItems"/> methods.
/// </summary>
public sealed class DependencySorterResult<T> : IDependencySorterResult where T : class, IDependentItem
{
    readonly IReadOnlyList<CycleExplainedElement>? _cycle;
    readonly int _maxHeadRank;
    readonly int _maxGroupRank;
    readonly int _maxItemRank;
    int _itemIssueWithStructureErrorCount;
    bool _requiredMissingIsError;

    internal DependencySorterResult( List<DependencySorter<T>.Entry>? result,
                                     List<CycleExplainedElement>? cycle,
                                     List<DependentItemIssue> itemIssues,
                                     int startErrorCount,
                                     bool hasStartFatal,
                                     bool hasSevereStructureError,
                                     int maxHeadRank,
                                     int maxGroupRank,
                                     int maxItemRank )
    {
        Debug.Assert( (result != null) == (cycle == null && !hasStartFatal && !hasSevereStructureError) );
        HasStartFatal = hasStartFatal;
        StartErrorCount = startErrorCount;
        HasSevereStructureError = hasSevereStructureError;
        _maxHeadRank = maxHeadRank;
        _maxGroupRank = maxGroupRank;
        _maxItemRank = maxItemRank;
        if( result == null )
        {
            SortedItems = null;
            _cycle = cycle?.ToArray();
        }
        else
        {
            SortedItems = result;
            _cycle = null;
        }
        ItemIssues = itemIssues != null && itemIssues.Count > 0 ? itemIssues : Array.Empty<DependentItemIssue>();
        _requiredMissingIsError = true;
        _itemIssueWithStructureErrorCount = -1;
    }

    /// <inheritdoc />
    public IReadOnlyList<ICycleExplainedElement>? CycleDetected => _cycle;

    /// <summary>
    /// Gets the list of <see cref="ISortedItem{T}"/>: null if <see cref="CycleDetected"/> is not null
    /// or <see cref="HasStartFatal"/> or <see cref="HasSevereStructureError"/> are true.
    /// </summary>
    public readonly IReadOnlyList<ISortedItem<T>>? SortedItems;

    /// <inheritdoc />
    public int MaxHeadRank => _maxHeadRank;

    /// <inheritdoc />
    public int MaxGroupRank => _maxGroupRank;

    /// <inheritdoc />
    public int MaxItemRank => _maxItemRank;

    IReadOnlyList<ISortedItem>? IDependencySorterResult.SortedItems => SortedItems;

    /// <inheritdoc />
    public IReadOnlyList<DependentItemIssue> ItemIssues { get; }

    /// <inheritdoc />
    public int StartErrorCount { get; }

    /// <inheritdoc />
    public bool HasStartFatal { get; }

    /// <inheritdoc />
    public bool HasSevereStructureError { get; }

    /// <inheritdoc />
    public bool ConsiderRequiredMissingAsStructureError
    {
        get { return _requiredMissingIsError; }
        set
        {
            if( _requiredMissingIsError != value )
            {
                _itemIssueWithStructureErrorCount = -1;
                _requiredMissingIsError = value;
            }
        }
    }

    /// <inheritdoc />
    public bool HasRequiredMissing
    {
        get
        {
            Debug.Assert( (!ConsiderRequiredMissingAsStructureError || !ItemIssues.Any( m => m.RequiredMissingCount > 0 )) || HasStructureError, "MissingIsError && Exist(Missing) => HasStructureError" );
            return ItemIssues.Any( m => m.RequiredMissingCount > 0 );
        }
    }

    /// <inheritdoc />
    public bool HasStructureError => StructureErrorCount > 0;

    /// <inheritdoc />
    public int StructureErrorCount
    {
        get
        {
            if( _itemIssueWithStructureErrorCount < 0 )
            {
                if( _requiredMissingIsError )
                {
                    _itemIssueWithStructureErrorCount = ItemIssues.Count( m => m.StructureError != DependentItemStructureError.None );
                }
                else
                {
                    _itemIssueWithStructureErrorCount = ItemIssues.Count( m => (m.StructureError != DependentItemStructureError.None
                        && m.StructureError != DependentItemStructureError.MissingDependency
                        && m.StructureError != DependentItemStructureError.MissingGeneralization) );
                }
            }
            return _itemIssueWithStructureErrorCount;
        }
    }

    /// <inheritdoc />
    [MemberNotNullWhen( true, nameof( SortedItems ) )]
    public bool IsComplete => CycleDetected == null && !HasStructureError && !HasStartFatal && StartErrorCount == 0;

    /// <inheritdoc />
    public string? CycleExplainedString => CycleDetected != null ? String.Join( " ", CycleDetected ) : null;

    /// <inheritdoc />
    public string? RequiredMissingDependenciesExplained
    {
        get
        {
            string s = String.Join( "', '", ItemIssues.Where( d => d.RequiredMissingCount > 0 )
                                                      .Select( d => "'" + d.Item.FullName + "' => {'" + String.Join( "', '", d.RequiredMissingDependencies ) + "'}" ) );
            return s.Length == 0 ? null : s;
        }
    }

    /// <inheritdoc />
    public void LogError( IActivityMonitor monitor )
    {
        Throw.CheckNotNullArgument( monitor );
        if( HasStructureError )
        {
            foreach( var bug in ItemIssues.Where( d => d.StructureError != DependentItemStructureError.None ) )
            {
                bug.LogError( monitor );
            }
        }
        if( CycleDetected != null )
        {
            monitor.Error( $"Cycle detected: {CycleExplainedString}." );
        }
        if( HasStartFatal )
        {
            monitor.Error( "A fatal error has been raised during sort start." );
        }
        if( StartErrorCount > 0 )
        {
            monitor.Error( $"{StartErrorCount} error(s) have been raised during sort start." );
        }
    }

}
