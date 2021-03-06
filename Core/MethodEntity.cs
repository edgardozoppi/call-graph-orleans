﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Common;
using OrleansClient.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OrleansClient.Analysis
{
	/// <summary>
	/// Method entities contains all the relavant data for the analysis o a given method
	/// In particular, the input/output parameters and the "propagation graph" where concrete type propagate through method variables
	/// </summary>
	/// <typeparam name="E"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="M"></typeparam>
	/// 
	[Serializable]
	internal class MethodEntity : Entity
	{
		/// <summary>
		///  This is how we name entities in the dispacther
		/// </summary>
		public IEntityDescriptor EntityDescriptor { get; private set; }

		/// <summary>
		/// This is for having a RTA like set of instantiated types 
		/// We need this give type to  unsupported expression using the declaredType instead of the concrete type 
		/// </summary>
		public ISet<TypeDescriptor> InstantiatedTypes { get; private set; }

		/// This is for the propagation of removal of concrete types
		/// It is currently not used. 
		//public ISet<T> TypesRequestedTobeRemoved { get; private set; }

		/// <summary>
		/// This is a flag to indicate that information has been propagated at least once
		/// </summary>
        public bool HasBeenPropagated { get { return PropGraph.HasBeenPropagated; } }

        public bool CanBeAnalized { get; private set; }
		/// <summary>
		/// This maintain the set of methods that calls this method
		/// This is used to compute the Caller of the CG 
		/// and to know to which method a return message should be send
		/// </summary>
		private ISet<CallContext> callers = new HashSet<CallContext>();
		//private IImmutableSet<CallContext> callers = ImmutableHashSet<CallContext>.Empty;

		// I need this to yield a copy to avoid problems in recursion
		public ISet<CallContext> Callers
		{
			get { return new HashSet<CallContext>(callers); }
		}

		/// <summary>
		/// The method being analyzed
		/// </summary>
		public MethodDescriptor MethodDescriptor { get; private set; }

        /// <summary>
        /// This group together all input/out data
        /// That is: parameters, return, out values
        /// It will also include potential effects that we need to propagate (like a R/W effect over a field)
        /// </summary>
        internal MethodInterfaceData MethodInterfaceData { get; private set; }

		internal CodeGraphModel.DeclarationAnnotation DeclarationInfo { get; private set; }
		internal CodeGraphModel.SymbolReference ReferenceInfo { get; private set; }

		/// <summary>
		/// The next properties obtains info from MethodDataInterface
		/// </summary>
		public VariableNode ThisRef
        {
            get { return MethodInterfaceData.ThisRef; }
        }
        /// <summary>
        /// These are the node correspoding to the parameters
        /// </summary>
        public IList<ParameterNode> ParameterNodes
        {
            get { return MethodInterfaceData.Parameters; }

        }
        /// <summary>
        /// Return the node rerpesenting the ret value
        /// </summary>
        public VariableNode ReturnVariable
        {
            get { return MethodInterfaceData.ReturnVariable; }
        }

		/// <summary>
		/// This is the important part of the this class 
		/// The propagation graph constains of all information about concrete types that a variable can have
		/// </summary>       
		private PropagationGraph propGraph; 
		///public PropagationGraph PropGraph { get; private set; }
        public PropagationGraph PropGraph { get { return propGraph; } }

        private IDictionary<AnonymousMethodDescriptor, MethodEntity> anonymousMethods;

		/// <summary>
		/// We use this mapping as a cache of already computed callees info
		/// </summary>
		//public MethodEntityProcessor EntityProcessor { get; private set; }

		//public ISet<E> NodesProcessing = new HashSet<E>();
		//public ISet<CallConext<M,E>> NodesProcessing = new HashSet<CallConext<M,E>>();

		/// <summary>
		/// A method entity is built using the methods interface (params, return) and the propagation graph that is 
		/// initially populated with the variables, parameters and calls of the method 
		/// </summary>
		/// <param name="methodDescriptor"></param>
		/// <param name="mid"></param>
		/// <param name="propGraph"></param>
		/// <param name="instantiatedTypes"></param>
		public MethodEntity(MethodDescriptor methodDescriptor,
			MethodInterfaceData mid,
			PropagationGraph propGraph,
            IEntityDescriptor descriptor,
			IEnumerable<TypeDescriptor> instantiatedTypes,
            bool canBeAnalyzed = true)
		{
			this.MethodDescriptor = methodDescriptor;
            this.EntityDescriptor = descriptor; // EntityFactory.Create(methodDescriptor);
			this.MethodInterfaceData = mid;

			this.propGraph = propGraph;
			this.CanBeAnalized = canBeAnalyzed;
			this.InstantiatedTypes = new HashSet<TypeDescriptor>(instantiatedTypes);            
			this.anonymousMethods = new Dictionary<AnonymousMethodDescriptor, MethodEntity>();
		}

        public MethodEntity(MethodDescriptor methodDescriptor,
            MethodInterfaceData mid,
            PropagationGraph propGraph,
            IEntityDescriptor descriptor,
            IEnumerable<TypeDescriptor> instantiatedTypes,
            IDictionary<AnonymousMethodDescriptor, MethodEntity> anonymousMethods,
			CodeGraphModel.DeclarationAnnotation declarationInfo,
            CodeGraphModel.SymbolReference referenceInfo)
            : this(methodDescriptor, mid, propGraph, descriptor, instantiatedTypes)
        {
            this.anonymousMethods = anonymousMethods;
			this.DeclarationInfo = declarationInfo;
			this.ReferenceInfo = referenceInfo;
        }

        public MethodEntity GetAnonymousMethodEntity(AnonymousMethodDescriptor methodDescriptor)
        {
            if (anonymousMethods.ContainsKey(methodDescriptor))
            {
                return anonymousMethods[methodDescriptor];
            }
            else 
            { }
            return null;
        }

		public IEnumerable<MethodEntity> GetAnonymousMethodEntities()
		{
			return anonymousMethods.Values;
		}

		/// <summary>
		/// Copy the information regarding parameters and callers from one entity and other
		/// The PropGraph of the old entity is used but copied
		/// This is used when one method is updated to recompute the PropGraph using the original input values
		/// </summary>
		/// <param name="oldEntity"></param>
		internal void CopyInterfaceDataAndCallers(MethodEntity entity)
		{
			Contract.Requires(this.ParameterNodes != null);
			foreach (var parameter in this.ParameterNodes)
			{
				if (parameter != null)
				{
					this.PropGraph.Add(parameter, entity.PropGraph.GetTypes(parameter));
					this.PropGraph.AddToWorkList(parameter);
				}
			}
			if (this.ThisRef != null)
			{
				this.PropGraph.Add(this.ThisRef, entity.PropGraph.GetTypes(entity.ThisRef));
				this.PropGraph.AddToWorkList(this.ThisRef);
			}

			foreach (var callersContext in entity.Callers)
			{
				this.AddToCallers(callersContext);
			}
		}

		/// <summary>
		/// Return an EntityProccesor. It is the class that performs the analysis using the data in the entity
		/// </summary>
		/// <param name="dispatcher"></param>
		/// <returns></returns>
        //public IEntityProcessor GetEntityProcessor(IDispatcher dispatcher)
        //{
        //    //if (this.EntityProcessor == null)
        //    //{
        //    //    EntityProcessor = new MethodEntityProcessor(this, dispatcher, true);
        //    //}
        //    //return EntityProcessor;
        //    return new MethodEntityProcessor(this, dispatcher, true);
        //}

        public void AddToCallers(CallContext context)
        {
			int count = callers.Count;
            //callers = callers.Add(context);
            callers.Add(context);
			if(callers.Count>count)
			{
				if (this.ReturnVariable != null)
				{
					this.propGraph.AddToWorkList(this.ReturnVariable);
				}
			}
        }

        /// <summary>
        /// This is used by the incremental analysis when a caller is removed or modified
        /// </summary>
        /// <param name="context"></param>
        public void RemoveFromCallers(CallContext context)
        {
            //callers = callers.Remove(context);
            callers.Remove(context);
        }

//        /// <summary>
//        /// Obtains the concrete types that a method expression (e.g, variable, parameter, field) may refer 
//        /// </summary>
//        /// <param name="n"></param>
//        /// <returns></returns>
//        public ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor n)
//        {
//            if (n != null)
//            {
//                return this.PropGraph.GetTypes(n);
//            }
//            else
//            {
//                return new HashSet<TypeDescriptor>();
//            }
//        }

//        /// <summary>
//        /// Obtains the methods a delegate var may refer
//        /// </summary>
//        /// <param name="n"></param>
//        /// <returns></returns>
//        internal ISet<MethodDescriptor> GetDelegates(DelegateVariableNode n)
//        {
//            return this.PropGraph.GetDelegates(n);
//        }

//        internal ISet<TypeDescriptor> GetPotentialTypes(PropGraphNodeDescriptor analysisNode)
//        {
//            var result = new HashSet<TypeDescriptor>();
//            foreach (var typeDescriptor in PropGraph.GetTypes(analysisNode))
//            {
//                if (typeDescriptor.IsConcreteType)
//                {
//                    result.Add(typeDescriptor);
//                }
//                else
//                {
//                    result
//                        .UnionWith(InstantiatedTypes
//                            .Where(candidateTypeDescriptor => CodeProvider.IsSubtype(candidateTypeDescriptor,typeDescriptor)));
////							.Where(iType => iType.IsSubtype(t)));
//                }
//            }
//            return result;
//        }

		public void Save(string path)
		{
			PropGraph.Save(path);
		}

		public override string ToString()
		{
			return this.MethodDescriptor.ToString();
		}
	}

    [Serializable]
	internal class MethodEntityDescriptor : IEntityDescriptor
	{
		private MethodDescriptor methodDescriptor;

        public MethodDescriptor MethodDescriptor
		{
			get { return methodDescriptor; }
			private set { methodDescriptor = value; }
		}
        internal MethodEntityDescriptor(MethodDescriptor methodDescriptor)
		{
			this.methodDescriptor = methodDescriptor;
		}
		public override bool Equals(object obj)
		{
			MethodEntityDescriptor md = obj as MethodEntityDescriptor;
			return md != null && methodDescriptor.Equals(md.methodDescriptor);
		}
		public override int GetHashCode()
		{
			return methodDescriptor.GetHashCode();
		}
		public override string ToString()
		{
			return methodDescriptor.ToString();
		}
	}

    [Serializable]
	internal class MethodInterfaceData
	{
		/// <summary>
		/// Future use: I include this to support other input/output info
		/// in addition to the parameters and retvalue
		/// </summary>
		internal MethodInterfaceData()
		{
			this.InputData = new Dictionary<string, PropGraphNodeDescriptor>();
			this.OutputData = new Dictionary<string, PropGraphNodeDescriptor>();
		}

		public VariableNode ThisRef { get; internal set; }

		public IList<ParameterNode> Parameters { get; internal set; }

		public VariableNode ReturnVariable { get; internal set; }

		public IDictionary<string, PropGraphNodeDescriptor> OutputData { get; internal set; }

		public IDictionary<string, PropGraphNodeDescriptor> InputData { get; internal set; }
	}
}
