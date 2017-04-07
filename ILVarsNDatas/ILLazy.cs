
namespace NetworkIO.ILEmitter
{
    public abstract class ILLazy : ILData
    {
        public sealed override PinnedState PinnedState { get { return PinnedState.Lazy; } }

        internal ILLazy(ILCoder coding)
            : base(coding)
        {

        }

    }
}
