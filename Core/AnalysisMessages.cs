﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Common;
using OrleansClient.Communication;
using System;

namespace OrleansClient.Analysis
{
    [Serializable]
    public abstract class Message : IMessage
    {
        public IEntityDescriptor Source { get; private set; }

        internal Message(IEntityDescriptor source)
        {
            this.Source = source;
        }

        public abstract MessageHandler Handler();
    }

    [Serializable]
    internal class CallerMessage : Message
    {
        public CallMessageInfo CallMessageInfo { get; private set; }

        internal CallerMessage(IEntityDescriptor source, CallMessageInfo messageInfo)
            : base(source)
        {
            this.CallMessageInfo = messageInfo;
        }

        public override MessageHandler Handler()
        {
            return (MessageHandler)Delegate.CreateDelegate(typeof(Func<MethodEntity, IMessage>),
                             typeof(MethodEntity).GetMethod("ProcessMessage"));
        }

        public override string ToString()
        {
            return this.CallMessageInfo.ToString();
        }
        public override bool Equals(object obj)
        {
            var other = (CallerMessage)obj;
            return base.Equals(obj)  && this.CallMessageInfo.Equals(other.CallMessageInfo);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + this.CallMessageInfo.GetHashCode();
        }

    }

    [Serializable]
	internal class CalleeMessage : Message
	{
		public ReturnMessageInfo ReturnMessageInfo { get; private set; }
		internal CalleeMessage(IEntityDescriptor source, ReturnMessageInfo messageInfo)
			: base(source)
		{
			this.ReturnMessageInfo = messageInfo;
		}

		public override MessageHandler Handler()
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			return this.ReturnMessageInfo.ToString();
		}
        public override bool Equals(object obj)
        {
            var other = (CalleeMessage)obj;
            return base.Equals(other) && this.ReturnMessageInfo.Equals(other.ReturnMessageInfo);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + this.ReturnMessageInfo.GetHashCode(); 
        }
	}
}
