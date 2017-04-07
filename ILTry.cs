using System;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILTry : ILBlocks
    {
        private bool _isEnd;

        public ILCoder Coding { get; private set; }
        public Label FinallyLabel { get; private set; }
        public ILGenerator Generator { get; private set; }
        public ILTry OuterTryBlock { get; private set; }

        public ILTry(ILCoder coding)
        {
            this.Coding = coding;
            this.Generator = coding.Generator;
            this.FinallyLabel = this.Generator.BeginExceptionBlock();
            this.OuterTryBlock = this.Coding.CurrentTryBlock;
            this.Coding.CurrentTryBlock = this;
        }

        public void Catch(Type exception)
        {
            if (typeof(Exception).IsAssignableFrom(exception))
                this.Generator.BeginCatchBlock(exception);
            else
                throw new InvalidOperationException("Exception type must be assignable from System.Exception.");
        }
        public void Finally()
        {
            this.Generator.BeginFinallyBlock();
        }
        public void End()
        {
            this.Generator.EndExceptionBlock();
            this.Coding.CurrentTryBlock = this.OuterTryBlock;
            this._isEnd = true;
        }

        bool ILBlocks.IsEnd { get { return this._isEnd; } }
        string ILBlocks.BlockType { get { return this.GetType().Name; } }
    }
}
