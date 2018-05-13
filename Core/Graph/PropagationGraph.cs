// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis.Tachyon;
using OrleansClient.Communication;
using OrleansClient.Roslyn;
using Common;

namespace OrleansClient
{
	internal class PropagationResult
	{
		public PropagationKind Kind { get; private set; }
		public ISet<CallNode> ModifiedCallNodes { get; private set; }
		public bool ResultChanged { get; private set; }

		public PropagationResult(PropagationKind kind, bool resultChanged, ISet<CallNode> modifiedCallNodes)
		{
			this.Kind = kind;
			this.ResultChanged = resultChanged;
			this.ModifiedCallNodes = new HashSet<CallNode>(modifiedCallNodes);
		}
	}

	/// <summary>
	/// Propagation graph: is the main data structure of the algorithm
	/// The basic idea is that concrete types flow from the graph nodes which represent proggram expressions (variables, fields, invocaitons)
	/// The invocations are dummy nodes. Their role is to trigger the propagation of data to the callees. When a type reach an invocation 
	/// it is marked to be processesd later
	/// </summary>
	/// <typeparam name="N"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="M"></typeparam>
	[Serializable]
	internal partial class PropagationGraph
	{
		/// <summary>
		/// The work list used during the propagation
		/// </summary>
		private ISet<PropGraphNodeDescriptor> workList = new HashSet<PropGraphNodeDescriptor>();
		//private IImmutableSet<N> workList = ImmutableHashSet<N>.Empty;

		/// <summary>
		/// Simirlar to the worklist but for the propagation of a removal of a concrete type
		/// </summary>
		private ISet<PropGraphNodeDescriptor> deletionWorkList = new HashSet<PropGraphNodeDescriptor>();
		/// <summary>
		/// The graph itself. We use Nuri Yeralan library (Tachyon)
		/// </summary>
		private IGraph<GraphNodeAnnotationData, GraphEdgeAnnotationData> graph;
		/// <summary>
		/// A map to relate vertices with expressions in the program
		/// </summary>
		private IDictionary<PropGraphNodeDescriptor, long> vIndex;
		/// <summary>
		/// This is the set of invocations made by the method
		/// </summary>
		private ISet<AnalysisCallNode> callNodes;

		public ICollection<AnalysisCallNode> CallNodes
		{
			get { return callNodes; }
			//private set { callNodes = value; }
		}

		// This is not needed to be serialized. Used during propagation.
		// Can be removed if DiifProp received a codeProvider as parameter
		[NonSerialized]
		private IProjectCodeProvider codeProvider;

		internal PropagationGraph()
		{
			graph = new SerializableGraph<GraphNodeAnnotationData, GraphEdgeAnnotationData>();
			vIndex = new Dictionary<PropGraphNodeDescriptor, long>();
			callNodes = new HashSet<AnalysisCallNode>();
		}

		private ICollection<PropGraphNodeDescriptor> Nodes
		{
			get { return vIndex.Keys; }
		}

		private GraphNode<GraphNodeAnnotationData> AddVertex(PropGraphNodeDescriptor analysisNode)
		{
			long index;
			GraphNode<GraphNodeAnnotationData> node = null;

			if (!vIndex.TryGetValue(analysisNode, out index))
			{
				var data = new GraphNodeAnnotationData(analysisNode);
				index = (long)vIndex.Count;

				vIndex[analysisNode] = index;
				graph.Add(index, data);
			}

			node = graph.GetNode(index);
			return node;
		}

		private GraphNode<GraphNodeAnnotationData> AddVertex(AnalysisCallNode m, CallNode callNode)
		{
			Contract.Assert(callNode != null);
			var v = AddVertex(m);

			if (callNode != null)
			{
				v.Value.CallNode = callNode;
			}

			return v;
		}

		public void Add(PropGraphNodeDescriptor analysisNode)
		{
			var v = AddVertex(analysisNode);
		}

		internal void Add(PropGraphNodeDescriptor n, string edge, TypeDescriptor t)
		{
			var v = AddVertex(n);
			v.Value.AddType(edge, t);
			v.Value.AddedTypes.Add(t);
		}

		internal void Add(PropGraphNodeDescriptor n, string edge, IEnumerable<TypeDescriptor> ts)
		{
			var v = AddVertex(n);
			v.Value.AddTypes(edge, ts);
			v.Value.AddedTypes.UnionWith(ts);
		}

		//public void AddDelegate(DelegateVariableNode delegateVariableNode, MethodDescriptor methodDescriptor)
		public void AddDelegate(VariableNode delegateVariableNode, MethodDescriptor methodDescriptor)
		{
			var v = AddVertex(delegateVariableNode);

			if (v.Value.Delegates == null)
			{
				v.Value.Delegates = new HashSet<MethodDescriptor>();
			}

			v.Value.Delegates.Add(methodDescriptor);
		}

		public void AddCall(CallNode call, AnalysisCallNode callNode)
		{
			var v = AddVertex(callNode, call);
			callNodes.Add(callNode);
			AddToWorkList(callNode);
		}

		//public void AddDelegateCall(DelegateCallExp<M, T, N> call, N callNode)
		//{
		//    var v = AddVertex(callNode, call);
		//    callNodes.Add(callNode);
		//}

		internal void AddRet(PropGraphNodeDescriptor rv)
		{
			var v = AddVertex(rv);
			v.Value.HasRetValue = true;
		}

		public CallInfo GetInvocationInfo(PropGraphNodeDescriptor callNode)
		{
			Contract.Requires(IsCallNode(callNode) || IsDelegateCallNode(callNode));
			long index;

			if (vIndex.TryGetValue(callNode, out index))
			{
				var v = graph.GetNode(index);
				return v.Value.CallNode.ToCallInfo();
			}

			return null;
		}

		public CallNode GetInvocationNode(PropGraphNodeDescriptor callNode)
		{
			Contract.Requires(IsCallNode(callNode) || IsDelegateCallNode(callNode));
			long index;

			if (vIndex.TryGetValue(callNode, out index))
			{
				var v = graph.GetNode(index);
				return v.Value.CallNode;
			}

			return null;
		}

		[Pure]
		public bool IsCallNode(PropGraphNodeDescriptor n)
		{
			var index = vIndex[n];
			var v = graph.GetNode(index);
			return v.Value.CallNode != null && v.Value.CallNode is MethodCallNode;
		}

		bool IsRetNode(PropGraphNodeDescriptor n)
		{
			var index = vIndex[n];
			var v = graph.GetNode(index);
			return v.Value.HasRetValue;
		}

		[Pure]
		public bool IsDelegateCallNode(PropGraphNodeDescriptor n)
		{
			var index = vIndex[n];
			var v = graph.GetNode(index);
			return v.Value.CallNode != null && v.Value.CallNode is DelegateCallNode;

		}

		internal void AddTypes(PropGraphNodeDescriptor n, IEnumerable<TypeDescriptor> ts)
		{
			var v = AddVertex(n);
			v.Value.AddedTypes.UnionWith(ts);
		}

		internal void RemoveTypes(PropGraphNodeDescriptor n, IEnumerable<TypeDescriptor> ts)
		{
			var v = AddVertex(n);
			v.Value.DeletedTypes.UnionWith(ts);
		}

		internal PropGraphNodeDescriptor GetAnalysisNode(GraphNode<GraphNodeAnnotationData> v)
		{
			return v.Value.Node;
		}

		public void AddEdge(PropGraphNodeDescriptor n1, PropGraphNodeDescriptor n2)
		{
			Add(n1);
			Add(n2);
			var sourceId = vIndex[n1];
			var targetId = vIndex[n2];

			if (!graph.Contains(sourceId, targetId))
			{
				var data = new GraphEdgeAnnotationData();

				graph.Add(sourceId, targetId, data);
			}
		}

		internal ISet<TypeDescriptor> TypesInEdge(PropGraphNodeDescriptor n1, PropGraphNodeDescriptor n2)
		{
			long iN1, iN2;

			if (vIndex.TryGetValue(n1, out iN1) && vIndex.TryGetValue(n2, out iN2))
			{
				var e = graph.GetEdge(iN1, iN2);
				return e.Value.Types;
			}
			else
			{
				return new HashSet<TypeDescriptor>();
			}
		}

		public void ReplaceNode(PropGraphNodeDescriptor nOld, PropGraphNodeDescriptor nNew)
		{
			long index;

			if (vIndex.TryGetValue(nOld, out index))
			{
				var v = graph.GetNode(index);
				v.Value.Node = nNew;
				vIndex[nNew] = index;
				vIndex.Remove(nOld);
			}
		}

		internal IEnumerable<string> GetEdges(PropGraphNodeDescriptor analysisNode)
		{
			var result = Enumerable.Empty<string>();
			long index;

			if (vIndex.TryGetValue(analysisNode, out index))
			{
				var v = graph.GetNode(index);
				return v != null ? v.Value.Edges : result;
			}

			return result;
		}

		internal IEnumerable<TypeDescriptor> GetTypes(PropGraphNodeDescriptor analysisNode, string edge)
		{
			var result = Enumerable.Empty<TypeDescriptor>();
			long index;

			if (vIndex.TryGetValue(analysisNode, out index))
			{
				var v = graph.GetNode(index);
				return v != null ? v.Value.GetTypes(edge) : result;
			}

			return result;
		}

		internal ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor analysisNode)
		{
			var result = new HashSet<TypeDescriptor>();
			long index;

			if (vIndex.TryGetValue(analysisNode, out index))
			{
				var v = graph.GetNode(index);
				return v != null ? v.Value.Types : result;
			}

			return result;
		}

		internal void AddTypes(PropGraphNodeDescriptor analysisNode, string edge, IEnumerable<TypeDescriptor> types)
		{
			long index;

			if (vIndex.TryGetValue(analysisNode, out index))
			{
				var v = graph.GetNode(index);

				if (v != null)
				{
					v.Value.AddTypes(edge, types);
				}
			}
		}

		internal void RemoveTypes(PropGraphNodeDescriptor analysisNode, string edge, IEnumerable<TypeDescriptor> types)
		{
			long index;

			if (vIndex.TryGetValue(analysisNode, out index))
			{
				var v = graph.GetNode(index);

				if (v != null)
				{
					v.Value.RemoveTypes(edge, types);
				}
			}
		}

		public ISet<MethodDescriptor> GetDelegates(PropGraphNodeDescriptor analysisNode)
		{
			var result = new HashSet<MethodDescriptor>();
			long index;

			if (vIndex.TryGetValue(analysisNode, out index))
			{
				var v = graph.GetNode(index);
				return v != null ? v.Value.Delegates : result;
			}

			return result;
		}

		internal ISet<TypeDescriptor> GetAddedTypes(PropGraphNodeDescriptor m)
		{
			var res = new HashSet<TypeDescriptor>();
			long index;

			if (vIndex.TryGetValue(m, out index))
			{
				var v = graph.GetNode(index);
				return v != null ? v.Value.AddedTypes : res;
			}

			return res;
		}

		internal ISet<TypeDescriptor> GetDeletedTypes(PropGraphNodeDescriptor m)
		{
			var res = new HashSet<TypeDescriptor>();
			long index;

			if (vIndex.TryGetValue(m, out index))
			{
				var v = graph.GetNode(index);
				return v != null ? v.Value.DeletedTypes : res;
			}

			return res;
		}

		//public bool DiffProp(IEnumerable<T> src, N n)
		//{
		//    var ts = GetTypes(n);
		//    int c = ts.Count;
		//    ts.UnionWith(src.Where(t => !ts.Contains(t)));
		//    if (ts.Count > c)
		//    {
		//        this.workList.Add(n);
		//        return true;
		//    }
		//    return false;
		//}

		internal bool DiffProp(PropGraphNodeDescriptor n, string edge, IEnumerable<TypeDescriptor> src, PropagationKind propKind)
		{
			if (propKind == PropagationKind.REMOVE_TYPES || propKind == PropagationKind.REMOVE_ASSIGNMENT)
			{
				return DiffDelProp(n, edge, src);
			}
			else
			{
				return DiffProp(n, edge, src);
			}
		}

		internal bool DiffProp(PropGraphNodeDescriptor n, string edge, IEnumerable<TypeDescriptor> src)
		{
			if (IsCallNode(n) || IsDelegateCallNode(n))
			{
				var msg = string.Format("DiffProp invoked with a call node '{0}'", n);
				throw new Exception(msg);
			}

			var newTypes = GetAddedTypes(n);
			if (newTypes.IsSupersetOf(src))
				return false;

			var oldTypes = GetTypes(n);
			var compatibleTypes = src.Where(t => !oldTypes.Contains(t) && IsAssignable(t, n));
			AddTypes(n, edge, compatibleTypes);
			newTypes = GetTypes(n);
			newTypes = new HashSet<TypeDescriptor>(newTypes);

			if (newTypes.Count > oldTypes.Count)
			{
				newTypes.ExceptWith(oldTypes);
				this.AddToWorkList(n);
				this.AddTypes(n, newTypes);
				return true;
			}

			return false;
		}

		internal bool IsAssignable(TypeDescriptor t1, PropGraphNodeDescriptor analysisNode)
		{
			// Contract.Assert(this.codeProvider!=null);
			if (codeProvider == null)
			{
				return true;
			}

			var res = true;
			// Ugly
			TypeDescriptor type1 = t1;

			var type2 = analysisNode.Type;

			//			if (!type1.IsSubtype(type2))
			// Diego: This requires a Code Provider. Now it will simply fail.
			if (!this.codeProvider.IsSubtypeAsync(type1, type2).Result)
			{
				if (!type2.IsDelegate)
				{
					if (!IsCallNode(analysisNode) && !IsDelegateCallNode(analysisNode))
					{
						return false;
					}
				}
			}
			//foreach(var t2 in ts.AsSet())
			//{
			//    AnalysisType type2 = (AnalysisType)t2;
			//    if (!type1.IsSubtype(type2))
			//        if (!IsCallNode(n) && !IsDelegateCallNode(n))
			//            return false;
			//}
			return res;
		}

		internal bool DiffDelProp(PropGraphNodeDescriptor n, string edge, IEnumerable<TypeDescriptor> src)
		{
			if (IsCallNode(n) || IsDelegateCallNode(n))
			{
				var msg = string.Format("DiffDelProp invoked with a call node '{0}'", n);
				throw new Exception(msg);
			}

			var delTypes = GetDeletedTypes(n);
			if (delTypes.IsSupersetOf(src))
				return false;

			var oldTypes = GetTypes(n);
			oldTypes = new HashSet<TypeDescriptor>(oldTypes);
			RemoveTypes(n, edge, src);
			var newTypes = GetTypes(n);

			if (newTypes.Count < oldTypes.Count)
			{
				oldTypes.ExceptWith(newTypes);
				this.AddToDeletionWorkList(n);
				this.RemoveTypes(n, oldTypes);
				return true;
			}

			return false;
		}

		public bool DiffPropDelegates(IEnumerable<MethodDescriptor> src, PropGraphNodeDescriptor analysisNode)
		{
			var ts = GetDelegates(analysisNode);
			int c = ts.Count;
			ts.UnionWith(src.Where(t => !ts.Contains(t)));
			if (ts.Count > c)
			{
				this.AddToWorkList(analysisNode);
				return true;
			}
			return false;
		}

		public void RemoveAddedTypes()
		{
			foreach (var n in this.Nodes)
			{
				this.GetAddedTypes(n).Clear();
			}
		}

		public void RemoveDeletedTypes()
		{
			foreach (var n in this.Nodes)
			{
				this.GetDeletedTypes(n).Clear();
			}
		}

		#region Deprecated
		//public bool PropagateOneNode(N n)
		//{
		//    bool someNodeChanged = false;
		//    workList.Add(n);
		//    while(workList.Count>0)
		//    {
		//        N  n1 = workList.First();
		//        workList.Remove(n1);
		//        someNodeChanged= PropagateOneNodeToAdj(n);
		//    }
		//    return someNodeChanged;
		//}

		//public bool PropagateNodes()
		//{

		//    bool someNodeChanged = false;
		//    ISet<N> workList = new HashSet<N>();
		//    foreach(var n in Nodes)
		//    {
		//        someNodeChanged= PropagateOneNode(n);
		//    }
		//    //workList.Add(n);
		//    //while (workList.Count > 0)
		//    //{
		//    //    N n1 = workList.First();
		//    //    workList.Remove(n1);
		//    //    someNodeChanged = PropagateOneNode(n, workList);
		//    //}
		//    return someNodeChanged;
		//}
		#endregion

		private GraphNode<GraphNodeAnnotationData> GetVertex(PropGraphNodeDescriptor n)
		{
			return graph.GetNode(vIndex[n]);
		}

		public void AddToWorkList(IEnumerable<PropGraphNodeDescriptor> nodes)
		{
			workList.UnionWith(nodes);
		}

		public void AddToWorkList(PropGraphNodeDescriptor n)
		{
			workList.Add(n);
			//workList = workList.Add(n);
		}

		public void RemoveFromWorkList(PropGraphNodeDescriptor n)
		{
			workList.Remove(n);
			//workList = workList.Remove(n);
		}

		public void AddToDeletionWorkList(IEnumerable<PropGraphNodeDescriptor> nodes)
		{
			deletionWorkList.UnionWith(nodes);
		}

		public void AddToDeletionWorkList(PropGraphNodeDescriptor n)
		{
			deletionWorkList.Add(n);
			//deletionWorkList = deletionWorkList.Add(n);
		}

		public void RemoveFromDeletionWorkList(PropGraphNodeDescriptor n)
		{
			deletionWorkList.Remove(n);
			//deletionWorkList = deletionWorkList.Remove(n);
		}

		internal void SetCodeProvider(IProjectCodeProvider codeProvider)
		{
			this.codeProvider = codeProvider;
		}

		internal PropagationEffects Propagate(IProjectCodeProvider codeProvider)
		{
			this.codeProvider = codeProvider;

			var calls = new HashSet<CallInfo>();
			var retModified = false;

			while (workList.Count > 0)
			{
				var analysisNode = workList.First();
				this.RemoveFromWorkList(analysisNode);

				if (IsCallNode(analysisNode) || IsDelegateCallNode(analysisNode))
				{
					calls.Add(GetInvocationInfo(analysisNode));
					continue;
				}
				if (IsRetNode(analysisNode))
				{
					retModified = true;
				}

				var v = GetVertex(analysisNode);
				var types = GetAddedTypes(analysisNode);

				foreach (var v1 in graph.GetTargets(v.Id))
				{
					var n1 = GetAnalysisNode(v1);

					if (IsCallNode(n1) || IsDelegateCallNode(n1))
					{
						calls.Add(GetInvocationInfo(n1));
					}
					else
					{
						DiffProp(n1, analysisNode.ToString(), types);
					}

					//var e = graph.GetEdge(v.Id, v1.Id);
					//e.Value.Types = types;

					DiffPropDelegates(GetDelegates(analysisNode), n1);
				}
			}

			HasBeenPropagated = true;
			return new PropagationEffects(calls, retModified, PropagationKind.ADD_TYPES);
		}

		internal PropagationEffects PropagateDeletion()
		{
			var calls = new HashSet<CallInfo>();
			var retModified = false;

			while (deletionWorkList.Count > 0)
			{
				var analysisNode = deletionWorkList.First();
				this.RemoveFromDeletionWorkList(analysisNode);

				if (IsCallNode(analysisNode) || IsDelegateCallNode(analysisNode))
				{
					calls.Add(GetInvocationInfo(analysisNode));
					continue;
				}
				if (IsRetNode(analysisNode))
				{
					retModified = true;
				}

				var v = GetVertex(analysisNode);
				var types = GetDeletedTypes(analysisNode);

				foreach (var v1 in graph.GetTargets(v.Id))
				{
					var n1 = GetAnalysisNode(v1);

					if (IsCallNode(n1) || IsDelegateCallNode(n1))
					{
						calls.Add(GetInvocationInfo(n1));
					}
					else
					{
						DiffDelProp(n1, analysisNode.ToString(), types);
					}

					//if (DiffDelProp(n1, analysisNode.ToString(), types))
					//{
					//	var e = graph.GetEdge(v.Id, v1.Id);
					//	e.Value.Types.ExceptWith(types);
					//}

					// DiffPropDelegates(GetDelegates(analysisNode), n1);
				}
			}

			return new PropagationEffects(calls, retModified, PropagationKind.REMOVE_TYPES);
		}

		private IEnumerable<TypeDescriptor> GetInGoingTypesFromEdges(Vertex v)
		{
			var res = new HashSet<TypeDescriptor>();
			foreach (var e in graph.GetInEdges(v.Id))
			{
				res.UnionWith(e.Value.Types);
			}
			return res;
		}


		internal ISet<TypeDescriptor> GetPotentialTypes(PropGraphNodeDescriptor n, MethodCallInfo callInfo, IProjectCodeProvider codeProvider)
		{
			return GetPotentialTypesAsync(n, callInfo, codeProvider).Result;

			//var result = new HashSet<TypeDescriptor>();
			//var types = this.GetTypes(n);

			//if (types.Count() == 0)
			//{
			//    /// We get the instantiated type that are compatible with the receiver type
			//    types.UnionWith(
			//        callInfo.InstantiatedTypes
			//            .Where(type => codeProvider.IsSubtype(type, callInfo.Receiver.Type)));
			//}
			//if (types.Count() == 0)
			//{
			//    types.Add(callInfo.Receiver.Type);
			//}

			//foreach (var typeDescriptor in types)
			//{
			//    // TO-DO fix by adding a where T: AnalysisType
			//    if (typeDescriptor.IsConcreteType)
			//    {
			//        result.Add(typeDescriptor);
			//    }
			//    else
			//    {
			//        // If it is a declaredTyped it means we were not able to compute a concrete type
			//        // Therefore, we instantiate all compatible types for the set of instantiated types
			//        //result.UnionWith(this.InstatiatedTypes.Where(iType => iType.IsSubtype(typeDescriptor)));
			//        Contract.Assert(callInfo.InstantiatedTypes != null);
			//        // Diego: This requires a Code Provider. Now it will simply fail.
			//        result.UnionWith(callInfo.InstantiatedTypes.Where(candidateTypeDescriptor
			//                                => codeProvider.IsSubtype(candidateTypeDescriptor, typeDescriptor)));
			//    }
			//}
			//return result;
		}

		internal ISet<MethodDescriptor> ComputeCalleesForNode(CallInfo invoInfo, IProjectCodeProvider codeProvider)
		{
			//TODO: Ugly... but we needed this refactor for moving stuff to the common project 
			if (invoInfo is MethodCallInfo)
			{
				return ComputeCalleesForCallNode((MethodCallInfo)invoInfo, codeProvider);
			}
			Contract.Assert(invoInfo is DelegateCallInfo);
			return ComputeCalleesForDelegateNode((DelegateCallInfo)invoInfo, codeProvider);
		}

		internal ISet<MethodDescriptor> ComputeCalleesForCallNode(MethodCallInfo callInfo, IProjectCodeProvider codeProvider)
		{
			return ComputeCalleesForCallNodeAsync(callInfo, codeProvider).Result;

			//var calleesForNode = new HashSet<MethodDescriptor>();
			//if (callInfo.Receiver != null)
			//{
			//    // I replaced the invocation for a local call to mark that functionality is missing
			//    //var callees = GetPotentialTypes(this.Receiver, propGraph)
			//    //    .Select(t => this.Callee.FindMethodImplementation(t));
			//    var callees = GetPotentialTypes(callInfo.Receiver, callInfo, codeProvider)
			//            .Select(t => codeProvider.FindMethodImplementation(callInfo.Method, t));
			//    calleesForNode.UnionWith(callees);
			//}
			//else
			//{
			//    calleesForNode.Add(callInfo.Method);
			//}
			//return calleesForNode;
		}

		internal ISet<MethodDescriptor> ComputeCalleesForDelegateNode(DelegateCallInfo callInfo, IProjectCodeProvider codeProvider)
		{
			return GetDelegateCallees(callInfo.Delegate, codeProvider);
		}

		private ISet<MethodDescriptor> GetDelegateCallees(VariableNode delegateNode, IProjectCodeProvider codeProvider)
		{
			return GetDelegateCalleesAsync(delegateNode, codeProvider).Result;

			//var callees = new HashSet<MethodDescriptor>();
			//var typeDescriptors = this.GetTypes(delegateNode);
			//foreach (var delegateInstance in this.GetDelegates(delegateNode))
			//{
			//    if (typeDescriptors.Count() > 0)
			//    {
			//        foreach (var typeDescriptor in typeDescriptors)
			//        {
			//            // TO-DO!!!
			//            // Ugly: I'll fix it
			//            //var aMethod = delegateInstance.FindMethodImplementation(type);
			//            var aMethod = codeProvider.FindMethodImplementation(delegateInstance, typeDescriptor);
			//            callees.Add(aMethod);
			//        }
			//    }
			//    else
			//    {
			//        // if Count is 0, it is a delegate that do not came form an instance variable
			//        callees.Add(delegateInstance);
			//    }
			//}

			//return callees;
		}

		public void Save(string path)
		{
			var dataAdapter = new GraphvizGraphDataAdapter(path);

			dataAdapter.Save(graph);
		}

		public bool HasBeenPropagated { get; private set; }

		/// <summary>
		///  This function is used to try to get a node using his name
		///  It can be use (for instance) in the incremental analysis
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		internal PropGraphNodeDescriptor FindNodeInPropationGraph(string text)
		{

			var nodes = this.Nodes.Where(n => n.ToString().Substring(0, text.Length).Equals(text));
			if (nodes.Count() > 0)
				return (PropGraphNodeDescriptor)nodes.First();
			return null;
		}
	}
}