// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System;
using Common;
using System.Linq;

namespace OrleansClient
{
	/// <summary>
	/// This class represents the data that a node in the PropGraph can carry
	/// Types: concrete Types (we store the possible types per incoming edge)
	/// AddedTypes: is the set of concrete types we have added. Used to propagate an addition of a type or an edge in the graph
	/// DeletedTypes: is the set of concrete types we have deleted. Used to propagate a removal of a type or an edge in the graph
	/// Node: is tne node value (the graph from Nuri uses numbers for vertex)
	/// CallNode: some node may include information about an invocation (the call nodes)
	/// </summary>
	[Serializable]
	internal class GraphNodeAnnotationData
	{
		// Stores the possible types per incoming edge (edge/caller context/callee).
		private IDictionary<string, ISet<TypeDescriptor>> _types;

		internal ISet<TypeDescriptor> AddedTypes { get; private set; }
		internal ISet<TypeDescriptor> DeletedTypes { get; private set; }
		internal ISet<MethodDescriptor> Delegates { get; set; }
		internal PropGraphNodeDescriptor Node { get; set; }
		internal CallInfo CallNode { get; set; }
		internal bool HasRetValue { get; set; }

		internal GraphNodeAnnotationData(PropGraphNodeDescriptor node)
		{
			_types = new Dictionary<string, ISet<TypeDescriptor>>();

			this.Node = node;
			this.HasRetValue = false;
			this.AddedTypes = new HashSet<TypeDescriptor>();
			this.DeletedTypes = new HashSet<TypeDescriptor>();
			this.Delegates = new HashSet<MethodDescriptor>();			
		}

		internal IEnumerable<string> Edges
		{
			get { return _types.Keys; }
		}

		internal ISet<TypeDescriptor> Types
		{
			get
			{
				var result = new HashSet<TypeDescriptor>();

				foreach (var set in _types.Values)
				{
					result.UnionWith(set);
				}

				return result;
			}
		}

		internal IEnumerable<TypeDescriptor> GetTypes(string edge)
		{
			var result = Enumerable.Empty<TypeDescriptor>();
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (exists)
			{
				result = edgeTypes;
			}

			return result;
		}

		internal void AddType(string edge, TypeDescriptor type)
		{
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (!exists)
			{
				edgeTypes = new HashSet<TypeDescriptor>();
				_types.Add(edge, edgeTypes);
			}

			edgeTypes.Add(type);
		}

		internal void AddTypes(string edge, IEnumerable<TypeDescriptor> types)
		{
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (!exists)
			{
				edgeTypes = new HashSet<TypeDescriptor>();
				_types.Add(edge, edgeTypes);
			}

			edgeTypes.UnionWith(types);
		}

		internal void RemoveType(string edge, TypeDescriptor type)
		{
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (exists)
			{
				edgeTypes.Remove(type);
			}
		}

		internal void RemoveTypes(string edge, IEnumerable<TypeDescriptor> types)
		{
			var removedTypes = new HashSet<TypeDescriptor>();
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (exists)
			{
				edgeTypes.ExceptWith(types);
			}
		}
	}

	/// <summary>
	/// This class represents the data that an edge in the PropGraph can carry
	/// </summary>
	[Serializable]
	internal class GraphEdgeAnnotationData
	{
		internal ISet<TypeDescriptor> Types { get; set; }

		internal GraphEdgeAnnotationData()
		{
			this.Types = new HashSet<TypeDescriptor>();
		}
	}
}
