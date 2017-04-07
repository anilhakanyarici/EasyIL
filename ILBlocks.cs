
namespace NetworkIO.ILEmitter
{
    internal interface ILBlocks
    {
        string BlockType { get; }
        bool IsEnd { get; }
        void End();
    }
}
