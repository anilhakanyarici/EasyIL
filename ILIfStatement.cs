using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILIfStatement : ILBlocks
    {
        private int _currentBlock;
        private List<Block> _blocks;
        private Label _endIf;
        private LocalBuilder _localComparerResult;
        private bool _usedElse;
        private bool _isEnd;

        public ILIfStatement OuterIfStatement { get; private set; }
        public ILGenerator Generator { get; private set; }
        public ILCoder Coding { get; private set; }

        internal ILIfStatement(ILCoder coding, ILData comparer)
        {
            this.Coding = coding;
            this.Generator = coding.Generator;
            this.OuterIfStatement = this.Coding.CurrentIfBlock;
            this.Coding.CurrentIfBlock = this;
            this._blocks = new List<Block>();
            this._endIf = this.Generator.DefineLabel();
            this._localComparerResult = this.Generator.DeclareLocal(typeof(bool));
            this.Generator.Emit(OpCodes.Ldc_I4_0);
            this.Generator.Emit(OpCodes.Stloc, this._localComparerResult);

            Block thisBlock = this.getNewBlock(comparer);
            ((IILPusher)comparer).Push();
            this.Generator.Emit(OpCodes.Brfalse_S, thisBlock.EndBlock);
            this.Generator.Emit(OpCodes.Ldc_I4_1);
            this.Generator.Emit(OpCodes.Stloc, this._localComparerResult);
        }

        public void ElseIf(ILData comparer)
        {
            this.Generator.Emit(OpCodes.Ldloc, this._localComparerResult);
            this.Generator.Emit(OpCodes.Brtrue_S, this._endIf);
            Block thisBlock = this.getNewBlock(comparer);
            Block lastBlock = this._blocks[thisBlock.BlockNo - 1];
            this.Generator.MarkLabel(lastBlock.EndBlock);
            ((IILPusher)comparer).Push();
            this.Generator.Emit(OpCodes.Brfalse_S, thisBlock.EndBlock);
            this.Generator.Emit(OpCodes.Ldc_I4_1);
            this.Generator.Emit(OpCodes.Stloc, this._localComparerResult);
        }
        public void Else()
        {
            this.Generator.Emit(OpCodes.Ldloc, this._localComparerResult);
            this.Generator.Emit(OpCodes.Brtrue_S, this._endIf);
            Block lastBlock = this._blocks[this._currentBlock - 1];
            this.Generator.MarkLabel(lastBlock.EndBlock);
            this._usedElse = true;
        }
        public void End()
        {
            if (this._usedElse)
            {
                this.Generator.MarkLabel(this._endIf);
            }
            else
            {
                Block lastBlock = this._blocks[this._blocks.Count - 1];
                this.Generator.MarkLabel(lastBlock.EndBlock);
                this.Generator.MarkLabel(this._endIf);
            }
            this.Coding.CurrentIfBlock = this.OuterIfStatement;
            this._isEnd = true;
        }

        private Block getNewBlock(ILData comparer)
        {
            if (comparer.ILType != typeof(bool))
                throw new InvalidOperationException("Comparer type must be boolean.");

            Block block = new Block { BlockNo = this._currentBlock, Comparer = comparer, EndBlock = this.Generator.DefineLabel() };
            this._blocks.Add(block);
            this._currentBlock++;
            return block;
        }

        private class Block
        {
            public int BlockNo;
            public ILData Comparer;
            public Label EndBlock;
        }

        string ILBlocks.BlockType { get { return this.GetType().Name; } }
        bool ILBlocks.IsEnd { get { return this._isEnd; } }
    }
}
