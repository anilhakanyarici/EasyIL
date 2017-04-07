using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILCodeBlock :ILBlocks
    {
        private bool _isEnd;
        private Label _beginLabel;
        private Label _continueLabel;
        private Label _endLabel;

        public ILGenerator Generator { get; private set; }
        public ILCoder Coding { get; private set; }

        public ILCodeBlock(ILCoder coding)
        {
            this.Coding = coding;
            this.Generator = coding.Generator;
            this._beginLabel = this.Generator.DefineLabel();
            this._continueLabel = this.Generator.DefineLabel();
            this._endLabel = this.Generator.DefineLabel();

            this.Generator.Emit(OpCodes.Br_S, this._endLabel);
            this.Generator.MarkLabel(this._beginLabel);
        }

        public void End()
        {
            this.Generator.Emit(OpCodes.Br_S, this._continueLabel);
            this.Generator.MarkLabel(this._endLabel);
            this._isEnd = true;
        }
        public void RunBlock(ILData ifTrue)
        {
            ((IILPusher)ifTrue).Push();
            this.Generator.Emit(OpCodes.Brtrue_S, this._beginLabel);
            this.Generator.MarkLabel(this._continueLabel);
        }

        string ILBlocks.BlockType { get { return this.GetType().Name; } }
        bool ILBlocks.IsEnd { get { return this._isEnd; } }
    }
}
