using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILWhile : ILBlocks
    {
        private bool _isEnd;

        protected Label BeginLabel;
        protected Label EndLabel;

        protected virtual ILLazy Comparer { get; set; }

        public ILCoder Coding { get; private set; }
        public ILGenerator Generator { get; private set; }
        
        internal ILWhile(ILCoder coding)
        {
            this.Coding = coding;
            this.Generator = coding.Generator;
        }
        internal ILWhile(ILCoder coding, ILLazy comparer)
        {
            this.Coding = coding;
            this.Generator = coding.Generator;
            this.Comparer = comparer;

            if (comparer.ILType == typeof(bool))
            {
                this.BeginLabel = this.Coding.DefineLabel();
                this.EndLabel = this.Coding.DefineLabel();

                this.Generator.MarkLabel(this.BeginLabel);
                ((IILPusher)this.Comparer).Push();
                this.Generator.Emit(OpCodes.Brfalse_S, this.EndLabel);
            }
            else
                throw new InvalidOperationException("Comparer type must be Boolean.");
        }

        public virtual void Break()
        {
            this.Generator.Emit(OpCodes.Br_S, this.EndLabel);
        }
        public virtual void Continue()
        {
            this.Generator.Emit(OpCodes.Br_S, this.BeginLabel);
        }
        public virtual void End()
        {
            this.Generator.Emit(OpCodes.Br_S, this.BeginLabel);
            this.Generator.MarkLabel(this.EndLabel);
            this._isEnd = true;
        }

        bool ILBlocks.IsEnd { get { return this._isEnd; } }
        string ILBlocks.BlockType { get { return this.GetType().Name; } }
    }

    public class ILFor : ILWhile, ILBlocks
    {
        private bool _isEnd;
        private ILLazy _operator;

        public ILLocal I { get; private set; }
        
        internal ILFor(ILCoder coding, ILLocal i, ILLazy comparer, ILLazy operating)
            : base(coding, comparer)
        {
            if (comparer.ILType == typeof(bool))
            {
                this.I = i;
                this.Comparer = comparer;
                this._operator = operating;
            }
            else
                throw new InvalidOperationException("Comparer type must be Boolean.");
        }

        public override void Continue()
        {
            this.I.AssignFrom(this._operator);
            this.Generator.Emit(OpCodes.Br_S, this.BeginLabel);
        }
        public override void End()
        {
            this.I.AssignFrom(this._operator);
            this.Generator.Emit(OpCodes.Br_S, this.BeginLabel);
            this.Generator.MarkLabel(this.EndLabel);
            this._isEnd = true;
        }

        bool ILBlocks.IsEnd { get { return this._isEnd; } }
        string ILBlocks.BlockType { get { return this.GetType().Name; } }
    }
    public class ILForeach : ILWhile, ILBlocks
    {
        private static readonly MethodInfo getEnumerator = typeof(IEnumerable).GetMethod("GetEnumerator");
        private static readonly MethodInfo moveNext = typeof(IEnumerator).GetMethod("MoveNext");

        private bool _isEnd;
        private ILLocal _enumerator;
        private ILLocal _current;
        private ILProperty _currentProp;
        private ILLazy _moveNext;
        private ILWhile _innerWhile;

        public ILLocal Item { get { return this._current; } }

        internal ILForeach(ILCoder coding, ILVariable enumerable)
            : base(coding)
        {
            this._current = coding.Null.ToLocal();
            this._enumerator = enumerable.Invoke(ILForeach.getEnumerator, null, null).ToLocal();
            this._currentProp = this._enumerator.GetProperty("Current");
            this._moveNext = this._enumerator.Invoke(ILForeach.moveNext, null, null);

            this._innerWhile = coding.While(this._moveNext);
            this.Item.AssignFrom(this._currentProp.Get());
        }

        public override void Break()
        {
            this._innerWhile.Break();
        }
        public override void Continue()
        {
            this._innerWhile.Continue();
        }
        public override void End()
        {
            this._innerWhile.End();
            this._isEnd = true;
        }

        bool ILBlocks.IsEnd { get { return this._isEnd; } }
        string ILBlocks.BlockType { get { return this.GetType().Name; } }
    }
    public class ILDoWhile : ILWhile, ILBlocks
    {
        private bool _isEnd;

        public ILDoWhile(ILCoder coding)
            : base(coding)
        {
            base.BeginLabel = this.Generator.DefineLabel();
            base.EndLabel = this.Generator.DefineLabel();
            this.Generator.MarkLabel(base.BeginLabel);
        }

        public override void End()
        {

        }
        public void While(ILLazy comparer)
        {
            this.Comparer = comparer;
            ((IILPusher)comparer).Push();
            this.Generator.Emit(OpCodes.Brtrue_S, base.BeginLabel);
            this.Generator.MarkLabel(base.EndLabel);
            this._isEnd = true;
        }

        bool ILBlocks.IsEnd { get { return this._isEnd; } }
        string ILBlocks.BlockType { get { return this.GetType().Name; } }
    }
}
