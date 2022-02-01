namespace PetaframeworkStd.Interfaces
{
    public interface IBaseService
    {
        string Path { get; }
        string Name { get; }

        string StackTrace { get; }
    }
}