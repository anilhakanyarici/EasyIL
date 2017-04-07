
namespace NetworkIO.ILEmitter
{
    internal interface IBuilder
    {
        bool IsBuild { get; }
        void OnBuild();
    }
}
